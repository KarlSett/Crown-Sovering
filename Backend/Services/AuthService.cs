using System;
using System.Linq;
using Crown__.Backend.Repositories;
using Crown__.Backend.Models;
using BC = BCrypt.Net.BCrypt;

namespace Crown__.Backend.Services
{
    public class ResultadoLogin
    {
        public bool Exito { get; private set; }
        public string Mensaje { get; private set; }
        public Usuario Usuario { get; private set; }

        public static ResultadoLogin Correcto(Usuario usuario)
        {
            return new ResultadoLogin { Exito = true, Usuario = usuario };
        }

        public static ResultadoLogin Fallo(string mensaje)
        {
            return new ResultadoLogin { Exito = false, Mensaje = mensaje };
        }
    }

    public class AuthService
    {
        // Los roles de la base que usa el panel (el frontend usa los mismos nombres).
        private static readonly string[] RolesConAcceso = { "Administrador", "Gerente", "Recepcionista", "Servicios" };

        private const int MaxIntentos = 5;
        private const int MinutosBloqueo = 15;
        private const string Modulo = "Autenticación";
        private const string AccionCorrecta = "Inicio de sesión";
        private const string AccionFallida = "Inicio de sesión fallido";
        private const string CredencialesInvalidas = "Usuario o contraseña incorrectos.";

        private readonly UsuarioRepository usuarios;
        private readonly AuditoriaRepository registros = new AuditoriaRepository();
        private readonly AuditoriaService auditoria = new AuditoriaService();

        public AuthService() : this(new UsuarioRepository()) { }

        public AuthService(UsuarioRepository usuarios)
        {
            this.usuarios = usuarios;
        }

        public ResultadoLogin Ingresar(string username, string contrasena)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(contrasena))
                return ResultadoLogin.Fallo("Escriba su usuario y su contraseña.");

            var usuario = usuarios.ObtenerPorUsername(username.Trim());
            if (usuario == null)
            {
                Auditar(null, AccionFallida, "Usuario inexistente: " + username.Trim());
                return ResultadoLogin.Fallo(CredencialesInvalidas);
            }

            if (!usuario.Activo)
            {
                Auditar(usuario.Id, "Inicio de sesión rechazado", "Cuenta desactivada");
                return ResultadoLogin.Fallo("Cuenta desactivada. Contacte al administrador.");
            }

            // La tabla usuario no guarda intentos ni bloqueos: se cuentan en la auditoría.
            int fallos = registros.FallosRecientes(usuario.Id, AuditoriaRepository.Cambio(Modulo, AccionFallida),
                                                   AuditoriaRepository.Cambio(Modulo, AccionCorrecta), MinutosBloqueo);
            if (fallos >= MaxIntentos)
            {
                Auditar(usuario.Id, "Inicio de sesión rechazado", "Cuenta bloqueada");
                return ResultadoLogin.Fallo("Cuenta bloqueada por demasiados intentos fallidos. Intente de nuevo en " + MinutosBloqueo + " minutos.");
            }

            if (!ContrasenaValida(contrasena, usuario.PasswordHash))
            {
                Auditar(usuario.Id, AccionFallida, "Contraseña incorrecta (intento " + (fallos + 1) + ")");
                return ResultadoLogin.Fallo(CredencialesInvalidas);
            }

            if (!RolesConAcceso.Contains(usuario.Rol))
            {
                Auditar(usuario.Id, "Inicio de sesión rechazado", "Rol sin acceso: " + usuario.Rol);
                return ResultadoLogin.Fallo("Su rol no tiene acceso a esta aplicación.");
            }

            Auditar(usuario.Id, AccionCorrecta, null);
            return ResultadoLogin.Correcto(usuario);
        }

        private void Auditar(int? id, string accion, string motivo)
        {
            auditoria.Registrar(id, Modulo, accion, motivo);
        }

        private static bool ContrasenaValida(string contrasena, string hash)
        {
            try
            {
                return BC.Verify(contrasena, hash);
            }
            catch (Exception)
            {
                // hash con formato inválido en la base
                return false;
            }
        }
    }
}

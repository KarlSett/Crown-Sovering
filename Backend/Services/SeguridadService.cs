using System;
using System.Collections.Generic;
using System.Linq;
using Crown__.Backend.Models;
using Crown__.Backend.Repositories;
using BC = BCrypt.Net.BCrypt;

namespace Crown__.Backend.Services
{
    // Usuarios, roles y configuración del hotel (solo Administrador).
    // Cada operación devuelve null si salió bien, o el motivo del problema.
    public class SeguridadService
    {
        private const int CostoBcrypt = 11;

        private readonly SeguridadRepository repositorio = new SeguridadRepository();
        private readonly AuditoriaService auditoria = new AuditoriaService();

        private string Autorizar(string actorId, out int actor)
        {
            return Autorizacion.Verificar(actorId, out actor, "Administrador");
        }

        // ---------------------------------------------------------------- Lecturas
        public List<UsuarioGestion> Usuarios() { return repositorio.Usuarios(); }
        public List<EmpleadoFicha> Empleados() { return repositorio.Empleados(); }
        public List<RolGestion> Roles() { return repositorio.Roles(); }
        public List<AsignacionVista> Asignaciones() { return repositorio.Asignaciones(); }
        public HotelVista DatosHotel() { return repositorio.DatosHotel(); }
        public string Moneda() { return repositorio.ValorDe("moneda"); }
        public List<ParametroVista> Parametros() { return repositorio.Parametros(); }
        public List<PoliticaVista> Politicas() { return repositorio.Politicas(); }
        public List<ImpuestoVista> Impuestos() { return repositorio.Impuestos(); }
        public List<ModuloVista> Modulos() { return repositorio.Modulos(); }
        public List<AuditoriaFila> Auditoria(string usuario, DateTime? desde, DateTime? hasta) { return repositorio.Auditoria(usuario, desde, hasta); }

        // Al menos 8 caracteres, con una letra y un número.
        public static string ValidarContrasena(string contrasena)
        {
            if (string.IsNullOrEmpty(contrasena) || contrasena.Length < 8) return "La contraseña debe tener al menos 8 caracteres.";
            if (!contrasena.Any(char.IsLetter) || !contrasena.Any(char.IsDigit)) return "La contraseña debe tener letras y números.";
            return null;
        }

        // ---------------------------------------------------------------- Seguridad
        public string NuevoUsuario(string actorId, string username, EmpleadoFicha empleado, string contrasena, bool activo)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(username)) return "Escribe el nombre de usuario.";
            if (username.Trim().Length < 3 || username.Trim().Contains(" ")) return "El usuario debe tener al menos 3 caracteres y no llevar espacios.";
            error = ValidarContrasena(contrasena);
            if (error != null) return error;
            if (repositorio.ExisteUsuario(username.Trim())) return "Ya existe el usuario " + username.Trim() + ".";

            repositorio.NuevoUsuario(username.Trim(), empleado == null ? (int?)null : empleado.Id, BC.HashPassword(contrasena, CostoBcrypt), activo);
            auditoria.Registrar(actor, "Seguridad", "Crear usuario", username.Trim());
            return null;
        }

        public string CambiarPassword(string actorId, UsuarioGestion usuario, string contrasena)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (usuario == null) return "Selecciona un usuario.";
            error = ValidarContrasena(contrasena);
            if (error != null) return error;

            repositorio.CambiarPassword(usuario.Id, BC.HashPassword(contrasena, CostoBcrypt));
            auditoria.Registrar(actor, "Seguridad", "Cambiar contraseña", usuario.Username);
            return null;
        }

        public string CambiarEstado(string actorId, UsuarioGestion usuario, bool activar)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (usuario == null) return "Selecciona un usuario.";
            if (!activar && usuario.Id == actor) return "No puedes desactivar tu propia cuenta.";
            if (!activar && usuario.Roles.Contains("Administrador") && repositorio.AdministradoresActivos(usuario.Id) == 0)
                return "Es el único administrador activo: no se puede desactivar.";

            new UsuarioRepository().CambiarActivo(usuario.Id, activar);
            auditoria.Registrar(actor, "Seguridad", activar ? "Activar usuario" : "Desactivar usuario", usuario.Username);
            return null;
        }

        public string NuevoEmpleado(string actorId, string nombre, string apellido, string correo, string telefono)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido)) return "Escribe el nombre y el apellido.";
            if (!string.IsNullOrWhiteSpace(correo) && !correo.Contains("@")) return "El correo no es válido.";

            repositorio.NuevoEmpleado(nombre.Trim(), apellido.Trim(), correo, telefono);
            auditoria.Registrar(actor, "Seguridad", "Crear empleado", nombre.Trim() + " " + apellido.Trim());
            return null;
        }

        public string NuevoRol(string actorId, string nombre, string descripcion)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre del rol.";
            if (repositorio.ExisteRol(nombre.Trim())) return "Ya existe el rol " + nombre.Trim() + ".";

            repositorio.NuevoRol(nombre.Trim(), descripcion);
            auditoria.Registrar(actor, "Seguridad", "Crear rol", nombre.Trim());
            return null;
        }

        public string AsignarRol(string actorId, UsuarioGestion usuario, RolGestion rol)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (usuario == null || rol == null) return "Selecciona el usuario y el rol.";
            if (repositorio.TieneRol(usuario.Id, rol.Id)) return usuario.Username + " ya tiene el rol " + rol.Nombre + ".";

            repositorio.AsignarRol(usuario.Id, rol.Id);
            auditoria.Registrar(actor, "Seguridad", "Asignar rol", usuario.Username + " → " + rol.Nombre);
            return null;
        }

        public string QuitarRol(string actorId, AsignacionVista asignacion)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (asignacion == null) return "Selecciona una asignación.";
            if (asignacion.Rol == "Administrador" && asignacion.UsuarioId == actor) return "No puedes quitarte tu propio rol de administrador.";
            if (asignacion.Rol == "Administrador" && repositorio.AdministradoresActivos(asignacion.UsuarioId) == 0)
                return "Es el único administrador activo: no se le puede quitar el rol.";

            repositorio.QuitarRol(asignacion.UsuarioId, asignacion.RolId);
            auditoria.Registrar(actor, "Seguridad", "Quitar rol", asignacion.Usuario + " ✕ " + asignacion.Rol);
            return null;
        }

        // ---------------------------------------------------------------- Configuración
        public string GuardarHotel(string actorId, HotelVista hotel, string moneda)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (hotel == null) return "No hay un hotel registrado en la base.";
            if (string.IsNullOrWhiteSpace(hotel.Nombre)) return "Escribe el nombre del hotel.";
            if (hotel.Categoria < 1 || hotel.Categoria > 5) return "La categoría va de 1 a 5 estrellas.";
            if (!string.IsNullOrWhiteSpace(hotel.Correo) && !hotel.Correo.Contains("@")) return "El correo no es válido.";

            var anterior = repositorio.DatosHotel();
            repositorio.GuardarHotel(hotel);
            if (!string.IsNullOrWhiteSpace(moneda)) repositorio.GuardarParametro("moneda", moneda);
            auditoria.Registrar(actor, "Configuración", "Actualizar hotel", "hotel", anterior == null ? null : anterior.Nombre + " · " + anterior.Categoria + "★", hotel.Nombre + " · " + hotel.Categoria + "★");
            return null;
        }

        public string GuardarParametro(string actorId, string clave, string valor)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(clave)) return "Escribe la clave.";

            string anterior = repositorio.ValorDe(clave.Trim());
            repositorio.GuardarParametro(clave.Trim(), valor ?? "");
            auditoria.Registrar(actor, "Configuración", "Parámetro", clave.Trim(), anterior, valor);
            return null;
        }

        public string GuardarPolitica(string actorId, string nombre, string valor, string descripcion)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre de la política.";

            repositorio.GuardarPolitica(nombre.Trim(), valor ?? "", descripcion);
            auditoria.Registrar(actor, "Configuración", "Política", nombre.Trim(), null, valor);
            return null;
        }

        public string GuardarImpuesto(string actorId, string nombre, decimal porcentaje, bool activo)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre del impuesto.";
            if (porcentaje < 0 || porcentaje > 100) return "El porcentaje debe estar entre 0 y 100.";

            repositorio.GuardarImpuesto(nombre.Trim(), porcentaje, activo);
            auditoria.Registrar(actor, "Configuración", "Impuesto", nombre.Trim(), null, porcentaje + " %");
            return null;
        }

        public string CambiarModulo(string actorId, ModuloVista modulo, bool activo)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (modulo == null) return "Selecciona un módulo.";

            repositorio.CambiarModulo(modulo.Id, activo);
            auditoria.Registrar(actor, "Configuración", activo ? "Activar módulo" : "Desactivar módulo", modulo.Nombre);
            return null;
        }
    }
}

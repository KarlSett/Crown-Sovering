using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    public class UsuarioService
    {
        private readonly UsuarioRepository usuarios;
        private readonly AuditoriaService auditoria = new AuditoriaService();

        public UsuarioService() : this(new UsuarioRepository()) { }

        public UsuarioService(UsuarioRepository usuarios)
        {
            this.usuarios = usuarios;
        }

        // Devuelve null si todo salió bien, o el mensaje del problema.
        public string CambiarActivo(string actorId, string username, bool activo)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Administrador");
            if (error != null) return error;

            var usuario = usuarios.ObtenerPorUsername(username);
            if (usuario == null) return "El usuario ya no existe.";
            if (usuario.Id == actor && !activo) return "No puede desactivar su propia cuenta.";

            usuarios.CambiarActivo(usuario.Id, activo);
            auditoria.Registrar(actor, "Usuarios", activo ? "Activar usuario" : "Desactivar usuario", usuario.Username,
                Json(usuario.Activo), Json(activo));
            return null;
        }

        private static string Json(bool activo)
        {
            return "{\"activo\": " + (activo ? "true" : "false") + "}";
        }
    }
}

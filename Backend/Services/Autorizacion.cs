using System;
using System.Linq;
using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    // Las operaciones se autorizan aquí y no solo ocultando botones en la pantalla.
    public static class Autorizacion
    {
        // Devuelve null si el usuario existe, está activo y tiene alguno de los roles; si no, el motivo.
        public static string Verificar(string actorId, out int actor, params string[] roles)
        {
            if (!int.TryParse(actorId, out actor)) return "Sesión no válida.";

            var usuario = new UsuarioRepository().ObtenerPorId(actor);
            if (usuario == null || !usuario.Activo || !roles.Any(r => usuario.Roles.Contains(r)))
                return "Su rol no tiene permiso para esta operación.";
            return null;
        }

        // Los servicios que siguen usando esta versión (identificador Guid) todavía no se adaptaron a la base nueva.
        public static string Verificar(string actorId, out Guid actor, params string[] roles)
        {
            actor = Guid.Empty;
            return "Esta función todavía no está disponible con la base de datos nueva.";
        }
    }
}

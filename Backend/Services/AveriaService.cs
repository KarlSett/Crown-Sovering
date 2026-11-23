using System;
using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    public class AveriaService
    {
        private readonly AveriaRepository averias = new AveriaRepository();
        private readonly AuditoriaService auditoria = new AuditoriaService();

        // prioridad llega como en el formulario: "Alta" o "Crítica (bloquea habitación)".
        // Devuelve null si todo salió bien, o el motivo del problema.
        public string Registrar(string actorId, string habitacion, string prioridad, string descripcion)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Recepcionista", "Administrador", "Servicios", "Gerente");
            if (error != null) return error;

            if (string.IsNullOrWhiteSpace(habitacion)) return "Falta la habitación.";
            if (string.IsNullOrWhiteSpace(descripcion)) return "Describe la avería.";

            string nivel = prioridad.Split('(')[0].Trim();
            error = averias.Registrar(habitacion, actor, descripcion.Trim(), nivel);
            if (error == null) auditoria.Registrar(actor, "Mantenimiento", "Registrar avería", "Hab. " + habitacion + " · " + nivel);
            return error;
        }

        public string Resolver(string actorId, int id)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Recepcionista", "Administrador", "Servicios", "Gerente");
            if (error != null) return error;

            error = averias.Resolver(id, actor);
            if (error == null) auditoria.Registrar(actor, "Mantenimiento", "Resolver avería", "MT-" + id.ToString("000"));
            return error;
        }
    }
}

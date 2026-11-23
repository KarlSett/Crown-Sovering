using System.Collections.Generic;
using Crown__.Backend.Models;
using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    public class HabitacionesService
    {
        private readonly HabitacionesRepository habitaciones = new HabitacionesRepository();
        private readonly AuditoriaService auditoria = new AuditoriaService();

        public List<HabitacionResumen> Listar()
        {
            return habitaciones.Listar();
        }

        // Devuelve null si todo salió bien, o el motivo del problema.
        public string CambiarEstado(string actorId, string numero, string nuevoEstado, string motivo)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Recepcionista", "Administrador");
            if (error != null) return error;

            error = habitaciones.CambiarEstado(numero, nuevoEstado, actor, motivo ?? "Cambio manual");
            if (error == null) auditoria.Registrar(actor, "Habitaciones", "Cambiar estado", "Hab. " + numero + " → " + nuevoEstado);
            return error;
        }
    }
}

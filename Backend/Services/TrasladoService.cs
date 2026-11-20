using System;
using System.Collections.Generic;
using Crown__.Backend.Models;
using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    public class TrasladoService
    {
        private readonly TrasladoRepository traslados = new TrasladoRepository();
        private readonly AuditoriaService auditoria = new AuditoriaService();

        public List<TrasladoVista> DeHoy()
        {
            return traslados.Del(DateTime.Today);
        }

        public List<TrasladoVista> Todos()
        {
            return traslados.Todos();
        }

        // Devuelve null si todo salió bien, o el motivo del problema.
        public string Registrar(string actorId, string codigoReserva, string origen, string destino, DateTime fechaHora, bool cargarAlFolio)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Recepcionista", "Administrador");
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(codigoReserva)) return "Selecciona la habitación del huésped.";
            if (fechaHora < DateTime.Now.AddMinutes(-5)) return "La hora del traslado ya pasó.";

            error = traslados.Registrar(codigoReserva, origen, destino, fechaHora, cargarAlFolio);
            if (error == null) auditoria.Registrar(actor, "Transporte", "Programar traslado", codigoReserva);
            return error;
        }
    }
}

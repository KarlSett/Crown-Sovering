using System.Collections.Generic;
using Crown__.Backend.Models;
using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    public class ReservasAvanzadasService
    {
        private readonly ReservasAvanzadasRepository repositorio = new ReservasAvanzadasRepository();
        private readonly AuditoriaService auditoria = new AuditoriaService();

        public List<CanalVista> Canales() { return repositorio.Canales(); }
        public List<GrupoVista> Grupos() { return repositorio.Grupos(); }
        public List<EsperaVista> ListaEspera() { return repositorio.ListaEspera(); }
        public DetalleReservaVista Detalle(int reservaId) { return repositorio.Detalle(reservaId); }

        // Cada operación devuelve null si salió bien, o el motivo del problema.
        public string GuardarCanal(string actorId, string nombre, decimal comision)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Administrador", "Gerente");
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre del canal.";
            if (comision < 0 || comision > 100) return "La comisión debe estar entre 0 y 100.";
            if (repositorio.ExisteCanal(nombre.Trim())) return "Ya existe el canal " + nombre.Trim() + ".";

            repositorio.GuardarCanal(nombre.Trim(), comision);
            auditoria.Registrar(actor, "Reservas", "Crear canal", nombre.Trim());
            return null;
        }

        public string GuardarGrupo(string actorId, string nombre)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Recepcionista", "Administrador", "Gerente");
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre del grupo.";

            repositorio.GuardarGrupo(nombre.Trim());
            auditoria.Registrar(actor, "Reservas", "Crear grupo", nombre.Trim());
            return null;
        }

        public string GuardarSolicitud(string actorId, int reservaId, string descripcion)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Recepcionista", "Administrador", "Gerente");
            if (error != null) return error;
            if (reservaId == 0) return "Selecciona una reserva.";
            if (string.IsNullOrWhiteSpace(descripcion)) return "Describe la solicitud.";

            repositorio.GuardarSolicitud(reservaId, descripcion.Trim());
            auditoria.Registrar(actor, "Reservas", "Solicitud especial", ReservaRegistro.CodigoDe(reservaId));
            return null;
        }
    }
}

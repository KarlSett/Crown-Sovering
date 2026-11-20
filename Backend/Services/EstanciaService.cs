using System;
using System.Collections.Generic;
using Crown__.Backend.Models;
using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    public class EstanciaService
    {
        private readonly EstanciaRepository estancias = new EstanciaRepository();
        private readonly LlaveRepository llaves = new LlaveRepository();
        private readonly AuditoriaService auditoria = new AuditoriaService();

        public string HacerCheckIn(string actorId, string codigo)
        {
            return Ejecutar(actorId, codigo, "Check-in", (id, actor) => estancias.HacerCheckIn(id, actor));
        }

        public string HacerCheckOut(string actorId, string codigo)
        {
            return Ejecutar(actorId, codigo, "Check-out", (id, actor) => estancias.HacerCheckOut(id, actor));
        }

        public string MoverHabitacion(string actorId, string codigoReserva, int nuevaHabitacionId)
        {
            return Ejecutar(actorId, codigoReserva, "Cambio de habitación", (id, actor) => estancias.MoverHabitacion(id, nuevaHabitacionId, actor));
        }

        public string Cancelar(string actorId, string codigo)
        {
            return Ejecutar(actorId, codigo, "Cancelar reserva", (id, actor) => estancias.Cancelar(id));
        }

        public static readonly string[] TiposCheckin = { "NORMAL", "EXPRESS", "ANTICIPADO" };
        public static readonly string[] TiposCheckout = { "NORMAL", "EXPRESS", "TARDIO" };

        public EstanciaDetalle Detalle(int reservaId)
        {
            return llaves.Detalle(reservaId);
        }

        public List<LlaveVista> Llaves(int estanciaId)
        {
            return llaves.Llaves(estanciaId);
        }

        public string ActualizarTipos(string actorId, EstanciaDetalle estancia, string tipoCheckin, string tipoCheckout)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Recepcionista", "Administrador");
            if (error != null) return error;
            if (estancia == null) return "Selecciona una estancia.";
            if (estancia.Estado != "ALOJADO") return "Solo se pueden cambiar los tipos de una estancia en curso.";

            llaves.ActualizarTipos(estancia.EstanciaId, tipoCheckin, tipoCheckout);
            auditoria.Registrar(actor, "Estancias", "Cambiar tipo de check-in/out", "Estancia " + estancia.EstanciaId);
            return null;
        }

        public string EmitirLlave(string actorId, EstanciaDetalle estancia, string codigo)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Recepcionista", "Administrador");
            if (error != null) return error;
            if (estancia == null || estancia.Estado != "ALOJADO") return "Solo se emiten llaves a una estancia en curso.";
            if (string.IsNullOrWhiteSpace(codigo)) return "Escribe el código de la llave.";
            codigo = codigo.Trim();
            if (llaves.ExisteLlaveActiva(estancia.EstanciaId, codigo)) return "La llave " + codigo + " ya está emitida a esta estancia.";

            llaves.Emitir(estancia.EstanciaId, codigo);
            auditoria.Registrar(actor, "Estancias", "Emitir llave", codigo);
            return null;
        }

        public string DevolverLlave(string actorId, LlaveVista llave)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Recepcionista", "Administrador");
            if (error != null) return error;
            if (llave == null) return "Selecciona una llave.";
            if (llave.Estado != "Emitida") return "La llave ya fue devuelta.";

            llaves.Devolver(llave.Id);
            auditoria.Registrar(actor, "Estancias", "Devolver llave", llave.Codigo);
            return null;
        }

        // Devuelve null si todo salió bien, o el motivo del problema.
        private string Ejecutar(string actorId, string codigo, string accion, Func<int, int, string> operacion)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Recepcionista", "Administrador");
            if (error != null) return error;

            int reservaId = ReservaRegistro.IdDe(codigo);
            if (reservaId == 0) return "El código de reserva no es válido.";

            error = operacion(reservaId, actor);
            if (error == null) auditoria.Registrar(actor, "Reservas", accion, codigo);
            return error;
        }
    }
}

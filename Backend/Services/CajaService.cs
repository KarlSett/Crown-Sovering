using System;
using System.Collections.Generic;
using Crown__.Backend.Models;
using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    public class CajaService
    {
        private static readonly string[] Roles = { "Recepcionista", "Administrador", "Gerente" };

        private readonly CajaRepository caja = new CajaRepository();
        private readonly FolioRepository folios = new FolioRepository();
        private readonly AuditoriaService auditoria = new AuditoriaService();

        public TurnoVista TurnoAbierto(string actorId)
        {
            int actor;
            return int.TryParse(actorId, out actor) ? caja.TurnoAbierto(actor) : null;
        }

        public List<PagoVista> Pagos(TurnoVista turno) { return caja.Pagos(turno == null ? DateTime.Today : turno.Apertura); }
        public List<MovimientoCajaVista> Movimientos(TurnoVista turno) { return turno == null ? new List<MovimientoCajaVista>() : caja.Movimientos(turno.Id); }
        public List<MetodoPagoVista> Metodos() { return caja.Metodos(); }
        public List<DevolucionVista> Devoluciones() { return caja.Devoluciones(); }

        // Cada operación devuelve null si salió bien, o el motivo del problema.
        public string AbrirTurno(string actorId)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, Roles);
            if (error != null) return error;

            error = caja.AbrirTurno(actor);
            if (error == null) auditoria.Registrar(actor, "Caja", "Abrir turno", null);
            return error;
        }

        public string CerrarTurno(string actorId, TurnoVista turno, decimal contado)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, Roles);
            if (error != null) return error;
            if (turno == null) return "No hay un turno abierto.";
            if (contado < 0) return "El monto contado no puede ser negativo.";

            error = caja.CerrarTurno(turno, contado);
            if (error == null) auditoria.Registrar(actor, "Caja", "Cerrar turno", "Esperado " + turno.Esperado + " · contado " + contado);
            return error;
        }

        public string RegistrarMovimiento(string actorId, TurnoVista turno, string tipo, decimal monto, string motivo)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, Roles);
            if (error != null) return error;
            if (turno == null) return "Abre un turno de caja primero.";
            if (monto <= 0) return "El monto debe ser mayor que cero.";
            if (tipo != "INGRESO" && tipo != "EGRESO") return "El tipo de movimiento no es válido.";

            caja.RegistrarMovimiento(turno.Id, tipo, monto);
            auditoria.Registrar(actor, "Caja", "Movimiento " + tipo.ToLowerInvariant(), motivo);
            return null;
        }

        public string GuardarMetodo(string actorId, string nombre, bool activo)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Administrador", "Gerente");
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre del método.";
            if (caja.ExisteMetodo(nombre.Trim())) return "Ya existe el método " + nombre.Trim() + ".";

            caja.GuardarMetodo(nombre.Trim(), activo);
            auditoria.Registrar(actor, "Caja", "Crear método de pago", nombre.Trim());
            return null;
        }
    }
}

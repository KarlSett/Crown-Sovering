using System;
using System.Collections.Generic;
using System.Linq;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;

namespace Crown__.Backend.Repositories
{
    // Turno de caja, cobros, movimientos, cierre y métodos de pago.
    // Esperado del cierre = cobros e ingresos menos egresos y devoluciones del turno.
    public class CajaRepository
    {
        public TurnoVista TurnoAbierto(int usuarioId)
        {
            return ConexionDb.Consultar(
                @"SELECT t.turno_id, COALESCE(t.fecha_apertura, LOCALTIMESTAMP), t.fecha_cierre, t.estado,
                         COALESCE((SELECT SUM(CASE WHEN m.tipo IN ('EGRESO', 'DEVOLUCION') THEN -m.monto ELSE m.monto END)
                                   FROM public.movimientocaja m WHERE m.turno_id = t.turno_id), 0)
                  FROM public.turnocaja t WHERE t.usuario_id = @u AND t.estado = 'ABIERTO' ORDER BY t.turno_id DESC LIMIT 1",
                r => new TurnoVista { Id = r.GetInt32(0), Apertura = r.GetDateTime(1), Cierre = r.IsDBNull(2) ? (DateTime?)null : r.GetDateTime(2), Estado = r.GetString(3), Esperado = r.GetDecimal(4) },
                new NpgsqlParameter("u", usuarioId)).FirstOrDefault();
        }

        public string AbrirTurno(int usuarioId)
        {
            if (TurnoAbierto(usuarioId) != null) return "Ya tienes un turno de caja abierto.";
            ConexionDb.Ejecutar("INSERT INTO public.turnocaja (usuario_id, fecha_apertura, estado) VALUES (@u, LOCALTIMESTAMP, 'ABIERTO')", new NpgsqlParameter("u", usuarioId));
            return null;
        }

        public string CerrarTurno(TurnoVista turno, decimal contado)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    string error = CerrarTurnoEn(conn, tx, turno.Id, contado);
                    if (error == null) tx.Commit();
                    return error;
                }
            }
        }

        internal static string CerrarTurnoEn(NpgsqlConnection conn, NpgsqlTransaction tx, int turnoId, decimal contado)
        {
            object estado = ReservaRepository.Escalar(conn, tx, "SELECT estado FROM public.turnocaja WHERE turno_id = @id FOR UPDATE", new NpgsqlParameter("id", turnoId));
            if (estado == null || (string)estado != "ABIERTO") return "El turno ya no está abierto.";

            decimal esperado = Convert.ToDecimal(ReservaRepository.Escalar(conn, tx,
                @"SELECT COALESCE(SUM(CASE WHEN tipo IN ('EGRESO', 'DEVOLUCION') THEN -monto ELSE monto END), 0)
                  FROM public.movimientocaja WHERE turno_id = @id", new NpgsqlParameter("id", turnoId)));
            ReservaRepository.Escalar(conn, tx,
                @"INSERT INTO public.cierrecaja (turno_id, monto_esperado, monto_real, diferencia, fecha)
                  VALUES (@turno, @esperado, @real, @diferencia, LOCALTIMESTAMP)",
                new NpgsqlParameter("turno", turnoId), new NpgsqlParameter("esperado", esperado),
                new NpgsqlParameter("real", contado), new NpgsqlParameter("diferencia", contado - esperado));
            ReservaRepository.Escalar(conn, tx, "UPDATE public.turnocaja SET estado = 'CERRADO', fecha_cierre = LOCALTIMESTAMP WHERE turno_id = @id", new NpgsqlParameter("id", turnoId));
            return null;
        }

        // Pagos desde la apertura del turno (o de hoy si no hay turno abierto).
        public List<PagoVista> Pagos(DateTime desde)
        {
            return ConexionDb.Consultar(
                @"SELECT p.pago_id, COALESCE(p.fecha, LOCALTIMESTAMP), e.reserva_id, hu.nombre || ' ' || hu.apellido, mp.nombre, p.monto
                  FROM public.pago p
                  JOIN public.metodopago mp ON mp.metodo_pago_id = p.metodo_pago_id
                  JOIN public.folio f ON f.folio_id = p.folio_id
                  JOIN public.estancia e ON e.estancia_id = f.estancia_id
                  JOIN public.reserva r ON r.reserva_id = e.reserva_id
                  JOIN public.huesped hu ON hu.huesped_id = r.huesped_principal_id
                  WHERE p.estado = 'COMPLETADO' AND p.fecha >= @desde ORDER BY p.fecha DESC, p.pago_id DESC",
                r => new PagoVista { Id = r.GetInt32(0), Fecha = r.GetDateTime(1), Reserva = ReservaRegistro.CodigoDe(r.GetInt32(2)), Huesped = r.GetString(3), Metodo = r.GetString(4), Monto = r.GetDecimal(5) },
                new NpgsqlParameter("desde", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = desde });
        }

        public List<MovimientoCajaVista> Movimientos(int turnoId)
        {
            return ConexionDb.Consultar(
                "SELECT tipo, monto, COALESCE(fecha, LOCALTIMESTAMP), pago_id FROM public.movimientocaja WHERE turno_id = @id ORDER BY movimiento_id DESC",
                r => new MovimientoCajaVista { Tipo = r.GetString(0), Monto = r.GetDecimal(1), Fecha = r.GetDateTime(2), Pago = r.IsDBNull(3) ? "—" : "P-" + r.GetInt32(3) },
                new NpgsqlParameter("id", turnoId));
        }

        public void RegistrarMovimiento(int turnoId, string tipo, decimal monto)
        {
            ConexionDb.Ejecutar("INSERT INTO public.movimientocaja (turno_id, tipo, monto, fecha) VALUES (@t, @tipo, @monto, LOCALTIMESTAMP)",
                new NpgsqlParameter("t", turnoId), new NpgsqlParameter("tipo", tipo), new NpgsqlParameter("monto", monto));
        }

        public List<MetodoPagoVista> Metodos()
        {
            return ConexionDb.Consultar("SELECT metodo_pago_id, nombre, COALESCE(activo, true) FROM public.metodopago ORDER BY nombre",
                r => new MetodoPagoVista { Id = r.GetInt32(0), Nombre = r.GetString(1), Activo = r.GetBoolean(2) });
        }

        public bool ExisteMetodo(string nombre)
        {
            return ConexionDb.Consultar("SELECT 1 FROM public.metodopago WHERE lower(nombre) = lower(@n)", r => r.GetInt32(0), new NpgsqlParameter("n", nombre)).Any();
        }

        public void GuardarMetodo(string nombre, bool activo)
        {
            ConexionDb.Ejecutar("INSERT INTO public.metodopago (hotel_id, nombre, activo) VALUES ((SELECT MIN(hotel_id) FROM public.hotel), @n, @a)",
                new NpgsqlParameter("n", nombre), new NpgsqlParameter("a", activo));
        }

        public List<DevolucionVista> Devoluciones()
        {
            return ConexionDb.Consultar(
                @"SELECT 'P-' || d.pago_id, 'Devolución', d.monto, COALESCE(d.motivo, ''), COALESCE(d.estado, '') FROM public.devolucion d
                  UNION ALL
                  SELECT 'P-' || a.pago_id, 'Anulación', p.monto, a.motivo, 'ANULADA' FROM public.anulacion a JOIN public.pago p ON p.pago_id = a.pago_id",
                r => new DevolucionVista { Pago = r.GetString(0), Tipo = r.GetString(1), Monto = r.GetDecimal(2), Motivo = r.GetString(3), Estado = r.GetString(4) });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;

namespace Crown__.Backend.Repositories
{
    // Cargos y pagos de la estancia. El saldo del folio se recalcula cada vez: cargos - pagos.
    public class FolioRepository
    {
        public List<FolioResumen> Resumenes()
        {
            const string sql = @"SELECT r.reserva_id, hu.nombre || ' ' || hu.apellido, h.numero,
                                        COALESCE((SELECT SUM(c.cantidad * c.precio_unitario) FROM public.cargo c WHERE c.folio_id = f.folio_id), 0),
                                        COALESCE((SELECT SUM(p.monto) FROM public.pago p WHERE p.folio_id = f.folio_id AND p.estado = 'COMPLETADO'), 0),
                                        COALESCE(f.saldo, 0), COALESCE(f.estado, 'ABIERTO')
                                 FROM public.folio f
                                 JOIN public.estancia e ON e.estancia_id = f.estancia_id
                                 JOIN public.reserva r ON r.reserva_id = e.reserva_id
                                 JOIN public.huesped hu ON hu.huesped_id = r.huesped_principal_id
                                 LEFT JOIN public.reservahabitacion rh ON rh.reserva_id = r.reserva_id
                                 LEFT JOIN public.habitacion h ON h.habitacion_id = rh.habitacion_id
                                 ORDER BY r.reserva_id DESC";
            return ConexionDb.Consultar(sql, r => new FolioResumen
            {
                Codigo = ReservaRegistro.CodigoDe(r.GetInt32(0)),
                Huesped = r.GetString(1),
                Habitacion = r.IsDBNull(2) ? null : r.GetString(2),
                Cargos = r.GetDecimal(3),
                Abonos = r.GetDecimal(4),
                Saldo = r.GetDecimal(5),
                Estado = r.GetString(6) == "ABIERTO" ? "Abierto" : "Cerrado"
            });
        }

        const string BaseMovimientos = @"FROM (SELECT c.cargo_id AS id, COALESCE(c.descripcion, cc.nombre) AS concepto, c.folio_id,
                                                    c.cantidad * c.precio_unitario AS cargo, 0::numeric AS abono, c.fecha, cc.nombre AS tipo
                                             FROM public.cargo c JOIN public.conceptocargo cc ON cc.concepto_id = c.concepto_id
                                             UNION ALL
                                             SELECT p.pago_id, 'Pago · ' || mp.nombre, p.folio_id, 0, p.monto, p.fecha, 'Pago'
                                             FROM public.pago p JOIN public.metodopago mp ON mp.metodo_pago_id = p.metodo_pago_id
                                             WHERE p.estado = 'COMPLETADO') t
                                         JOIN public.folio f ON f.folio_id = t.folio_id
                                         JOIN public.estancia e ON e.estancia_id = f.estancia_id
                                         JOIN public.reserva r ON r.reserva_id = e.reserva_id
                                         JOIN public.huesped hu ON hu.huesped_id = r.huesped_principal_id
                                         LEFT JOIN public.reservahabitacion rh ON rh.reserva_id = r.reserva_id
                                         LEFT JOIN public.habitacion h ON h.habitacion_id = rh.habitacion_id";
        const string ColumnasMovimiento = @"SELECT t.id, t.concepto, r.reserva_id, hu.nombre || ' ' || hu.apellido, t.cargo, t.abono,
                                                   COALESCE(f.saldo, 0), COALESCE(t.fecha, LOCALTIMESTAMP), h.numero ";

        public List<Movimiento> UltimosMovimientos(int limite)
        {
            return ConexionDb.Consultar(ColumnasMovimiento + BaseMovimientos + " ORDER BY t.fecha DESC, t.id DESC LIMIT @limite",
                MapearMovimiento, new NpgsqlParameter("limite", limite));
        }

        // Cargos cuyo concepto o descripción empieza con alguno de los prefijos (por ejemplo "Spa" o "Lavander").
        public List<Movimiento> UltimosCargos(string[] prefijos, int limite)
        {
            var parametros = new List<NpgsqlParameter> { new NpgsqlParameter("limite", limite) };
            var condiciones = new List<string>();
            for (int i = 0; i < prefijos.Length; i++)
            {
                condiciones.Add("(t.tipo ILIKE @p" + i + " || '%' OR t.concepto ILIKE @p" + i + " || '%')");
                parametros.Add(new NpgsqlParameter("p" + i, prefijos[i]));
            }
            return ConexionDb.Consultar(
                ColumnasMovimiento + BaseMovimientos + " WHERE t.cargo > 0 AND (" + string.Join(" OR ", condiciones) + ") ORDER BY t.fecha DESC, t.id DESC LIMIT @limite",
                MapearMovimiento, parametros.ToArray());
        }

        static Movimiento MapearMovimiento(NpgsqlDataReader r)
        {
            return new Movimiento
            {
                Id = r.GetInt32(0),
                Concepto = r.GetString(1),
                Reserva = ReservaRegistro.CodigoDe(r.GetInt32(2)),
                Huesped = r.GetString(3),
                Cargo = r.GetDecimal(4),
                Abono = r.GetDecimal(5),
                SaldoFolio = r.GetDecimal(6),
                Fecha = r.GetDateTime(7),
                Habitacion = r.IsDBNull(8) ? null : r.GetString(8)
            };
        }

        public decimal CobradoHoy()
        {
            var total = ConexionDb.Consultar("SELECT COALESCE(SUM(monto), 0) FROM public.pago WHERE estado = 'COMPLETADO' AND fecha::date = CURRENT_DATE", r => r.GetDecimal(0));
            return total.Count == 0 ? 0 : total[0];
        }

        // Un cargo sube el saldo del folio de la estancia en curso. Devuelve false y el motivo en error si no se puede.
        public bool RegistrarCargo(string codigoReserva, string concepto, string descripcion, decimal importe, out string error)
        {
            return EnTransaccion(out error, (NpgsqlConnection c, NpgsqlTransaction t, out string e) => RegistrarCargoEn(c, t, codigoReserva, concepto, descripcion, importe, out e));
        }

        public bool RegistrarPago(string codigoReserva, string metodo, decimal importe, out string error, int usuarioId = 0)
        {
            return EnTransaccion(out error, (NpgsqlConnection c, NpgsqlTransaction t, out string e) => RegistrarPagoEn(c, t, codigoReserva, metodo, importe, out e, usuarioId));
        }

        delegate bool Operacion(NpgsqlConnection conn, NpgsqlTransaction tx, out string error);

        static bool EnTransaccion(out string error, Operacion operacion)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    bool ok = operacion(conn, tx, out error);
                    if (ok) tx.Commit();
                    return ok;
                }
            }
        }

        internal static bool RegistrarCargoEn(NpgsqlConnection conn, NpgsqlTransaction tx, string codigoReserva, string concepto,
                                              string descripcion, decimal importe, out string error)
        {
            int folioId;
            if (!BuscarFolio(conn, tx, codigoReserva, out folioId, out error)) return false;

            object conceptoId = ReservaRepository.Escalar(conn, tx, "SELECT concepto_id FROM public.conceptocargo WHERE nombre = @nombre",
                new NpgsqlParameter("nombre", concepto));
            if (conceptoId == null) { error = "No existe el concepto de cargo " + concepto + "."; return false; }

            ReservaRepository.Escalar(conn, tx,
                @"INSERT INTO public.cargo (folio_id, concepto_id, descripcion, cantidad, precio_unitario, fecha)
                  VALUES (@folio, @concepto, @descripcion, 1, @importe, LOCALTIMESTAMP)",
                new NpgsqlParameter("folio", folioId), new NpgsqlParameter("concepto", (int)conceptoId),
                new NpgsqlParameter("descripcion", descripcion), new NpgsqlParameter("importe", importe));
            Recalcular(conn, tx, folioId);
            return true;
        }

        internal static bool RegistrarPagoEn(NpgsqlConnection conn, NpgsqlTransaction tx, string codigoReserva, string metodo,
                                             decimal importe, out string error, int usuarioId = 0)
        {
            int folioId;
            if (!BuscarFolio(conn, tx, codigoReserva, out folioId, out error)) return false;

            decimal saldo = Convert.ToDecimal(ReservaRepository.Escalar(conn, tx, "SELECT COALESCE(saldo, 0) FROM public.folio WHERE folio_id = @id",
                new NpgsqlParameter("id", folioId)));
            if (importe > saldo)
            {
                error = "El pago (Bs " + importe.ToString("N2", CultureInfo.InvariantCulture) + ") supera el saldo pendiente (Bs "
                    + saldo.ToString("N2", CultureInfo.InvariantCulture) + ").";
                return false;
            }

            object metodoId = ReservaRepository.Escalar(conn, tx, "SELECT metodo_pago_id FROM public.metodopago WHERE nombre = @nombre AND COALESCE(activo, true)",
                new NpgsqlParameter("nombre", metodo));
            if (metodoId == null) { error = "El método de pago " + metodo + " no está disponible."; return false; }

            int pagoId = (int)ReservaRepository.Escalar(conn, tx,
                @"INSERT INTO public.pago (folio_id, metodo_pago_id, monto, fecha, estado)
                  VALUES (@folio, @metodo, @monto, LOCALTIMESTAMP, 'COMPLETADO') RETURNING pago_id",
                new NpgsqlParameter("folio", folioId), new NpgsqlParameter("metodo", (int)metodoId), new NpgsqlParameter("monto", importe));
            // si quien cobra tiene el turno de caja abierto, el cobro queda en sus movimientos
            if (usuarioId > 0)
                ReservaRepository.Escalar(conn, tx,
                    @"INSERT INTO public.movimientocaja (turno_id, pago_id, tipo, monto, fecha)
                      SELECT turno_id, @pago, 'COBRO', @monto, LOCALTIMESTAMP FROM public.turnocaja
                      WHERE usuario_id = @usuario AND estado = 'ABIERTO' ORDER BY turno_id DESC LIMIT 1",
                    new NpgsqlParameter("pago", pagoId), new NpgsqlParameter("monto", importe), new NpgsqlParameter("usuario", usuarioId));
            Recalcular(conn, tx, folioId);
            return true;
        }

        // El folio abierto de la estancia en curso de la reserva.
        static bool BuscarFolio(NpgsqlConnection conn, NpgsqlTransaction tx, string codigoReserva, out int folioId, out string error)
        {
            error = null;
            folioId = 0;
            int reservaId = ReservaRegistro.IdDe(codigoReserva);
            object id = reservaId == 0 ? null : ReservaRepository.Escalar(conn, tx,
                @"SELECT f.folio_id FROM public.folio f JOIN public.estancia e ON e.estancia_id = f.estancia_id
                  WHERE e.reserva_id = @reserva AND e.estado = 'ALOJADO' AND f.estado = 'ABIERTO'
                  ORDER BY f.folio_id LIMIT 1 FOR UPDATE OF f", new NpgsqlParameter("reserva", reservaId));
            if (id == null) { error = "No hay un folio abierto para la reserva " + codigoReserva + ". El huésped debe estar alojado."; return false; }
            folioId = (int)id;
            return true;
        }

        internal static void Recalcular(NpgsqlConnection conn, NpgsqlTransaction tx, int folioId)
        {
            ReservaRepository.Escalar(conn, tx,
                @"UPDATE public.folio SET saldo =
                    COALESCE((SELECT SUM(cantidad * precio_unitario) FROM public.cargo WHERE folio_id = @id), 0)
                  - COALESCE((SELECT SUM(monto) FROM public.pago WHERE folio_id = @id AND estado = 'COMPLETADO'), 0)
                  WHERE folio_id = @id", new NpgsqlParameter("id", folioId));
        }
    }
}

using System;
using Crown__.Backend.Data;
using Npgsql;

namespace Crown__.Backend.Repositories
{
    // Check-in, check-out y cancelación. Cada operación devuelve null si salió bien, o el motivo.
    public class EstanciaRepository
    {
        public string HacerCheckIn(int reservaId, int usuarioId) { return EnTransaccion(c => HacerCheckInEn(c.Item1, c.Item2, reservaId, usuarioId)); }
        public string HacerCheckOut(int reservaId, int usuarioId) { return EnTransaccion(c => HacerCheckOutEn(c.Item1, c.Item2, reservaId, usuarioId)); }
        public string MoverHabitacion(int reservaId, int nuevaHabitacionId, int usuarioId) { return EnTransaccion(c => MoverHabitacionEn(c.Item1, c.Item2, reservaId, nuevaHabitacionId, usuarioId)); }
        public string Cancelar(int reservaId) { return EnTransaccion(c => CancelarEn(c.Item1, c.Item2, reservaId)); }

        // Saldo pendiente de los folios abiertos, por reserva.
        public System.Collections.Generic.Dictionary<int, decimal> SaldosPorReserva()
        {
            const string sql = @"SELECT e.reserva_id, SUM(f.saldo)
                                 FROM public.folio f JOIN public.estancia e ON e.estancia_id = f.estancia_id
                                 WHERE e.estado = 'ALOJADO' GROUP BY e.reserva_id";
            var saldos = new System.Collections.Generic.Dictionary<int, decimal>();
            foreach (var par in ConexionDb.Consultar(sql, r => Tuple.Create(r.GetInt32(0), r.GetDecimal(1)))) saldos[par.Item1] = par.Item2;
            return saldos;
        }

        static string EnTransaccion(Func<Tuple<NpgsqlConnection, NpgsqlTransaction>, string> operacion)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    string error = operacion(Tuple.Create(conn, tx));
                    if (error == null) tx.Commit();
                    return error;
                }
            }
        }

        internal static string HacerCheckInEn(NpgsqlConnection conn, NpgsqlTransaction tx, int reservaId, int usuarioId)
        {
            string estado, numero;
            DateTime entrada, salida;
            decimal tarifa;
            if (!LeerReserva(conn, tx, reservaId, out estado, out numero, out entrada, out salida, out tarifa)) return "No existe la reserva " + Models.ReservaRegistro.CodigoDe(reservaId) + ".";
            if (estado != "CONFIRMADA") return "La reserva está " + ReservaRepository.ParaPantalla(estado).ToLowerInvariant() + ": no se puede hacer el check-in.";
            if (numero == null) return "La reserva no tiene habitación asignada.";
            if (entrada.Date > DateTime.Today) return "La llegada está prevista para el " + entrada.ToString("dd/MM/yyyy") + ".";

            string error = HabitacionesRepository.CambiarEstadoEn(conn, tx, numero, "Ocupada", usuarioId, "Check-in " + Models.ReservaRegistro.CodigoDe(reservaId), false);
            if (error != null) return error;

            int noches = Math.Max(1, (salida.Date - entrada.Date).Days);
            int estanciaId = (int)Escalar(conn, tx,
                @"INSERT INTO public.estancia (reserva_id, fecha_checkin, tipo_checkin, estado)
                  VALUES (@reserva, LOCALTIMESTAMP, 'NORMAL', 'ALOJADO') RETURNING estancia_id", P("reserva", reservaId));
            int folioId = (int)Escalar(conn, tx,
                @"INSERT INTO public.folio (estancia_id, nombre, tipo, saldo, estado)
                  VALUES (@estancia, 'Folio principal', 'INDIVIDUAL', @saldo, 'ABIERTO') RETURNING folio_id",
                P("estancia", estanciaId), P("saldo", noches * tarifa));
            Escalar(conn, tx,
                @"INSERT INTO public.cargo (folio_id, concepto_id, descripcion, cantidad, precio_unitario, fecha)
                  SELECT @folio, concepto_id, @descripcion, @noches, @precio, LOCALTIMESTAMP
                  FROM public.conceptocargo WHERE nombre = 'Alojamiento'",
                P("folio", folioId), P("descripcion", "Alojamiento " + noches + (noches == 1 ? " noche" : " noches")),
                P("noches", noches), P("precio", tarifa));
            CambiarEstado(conn, tx, reservaId, "CHECK_IN");
            return null;
        }

        internal static string HacerCheckOutEn(NpgsqlConnection conn, NpgsqlTransaction tx, int reservaId, int usuarioId)
        {
            string estado, numero;
            DateTime entrada, salida;
            decimal tarifa;
            if (!LeerReserva(conn, tx, reservaId, out estado, out numero, out entrada, out salida, out tarifa)) return "No existe la reserva " + Models.ReservaRegistro.CodigoDe(reservaId) + ".";
            if (estado != "CHECK_IN") return "La reserva está " + ReservaRepository.ParaPantalla(estado).ToLowerInvariant() + ": no se puede hacer el check-out.";

            object saldo = Escalar(conn, tx,
                @"SELECT COALESCE(SUM(f.saldo), 0) FROM public.folio f
                  JOIN public.estancia e ON e.estancia_id = f.estancia_id
                  WHERE e.reserva_id = @reserva AND e.estado = 'ALOJADO'", P("reserva", reservaId));
            if (Convert.ToDecimal(saldo) > 0) return "El folio tiene un saldo pendiente de Bs " + Convert.ToDecimal(saldo).ToString("0.00") + ". Registre el pago antes del check-out.";

            string error = HabitacionesRepository.CambiarEstadoEn(conn, tx, numero, "Sucia", usuarioId, "Check-out " + Models.ReservaRegistro.CodigoDe(reservaId), false);
            if (error != null) return error;
            CrearTareaLimpieza(conn, tx, numero, reservaId);

            Escalar(conn, tx, @"UPDATE public.folio SET estado = 'CERRADO'
                                WHERE estancia_id IN (SELECT estancia_id FROM public.estancia WHERE reserva_id = @reserva AND estado = 'ALOJADO')", P("reserva", reservaId));
            Escalar(conn, tx, @"UPDATE public.estancia SET fecha_checkout = LOCALTIMESTAMP, tipo_checkout = 'NORMAL', estado = 'FINALIZADA'
                                WHERE reserva_id = @reserva AND estado = 'ALOJADO'", P("reserva", reservaId));
            CambiarEstado(conn, tx, reservaId, "CHECK_OUT");
            return null;
        }

        // Cambia a un huésped alojado a otra habitación libre: la anterior queda Sucia y la nueva Ocupada.
        internal static string MoverHabitacionEn(NpgsqlConnection conn, NpgsqlTransaction tx, int reservaId, int nuevaHabitacionId, int usuarioId)
        {
            string estado, numeroActual;
            DateTime entrada, salida;
            decimal tarifa;
            if (!LeerReserva(conn, tx, reservaId, out estado, out numeroActual, out entrada, out salida, out tarifa)) return "No existe la reserva " + Models.ReservaRegistro.CodigoDe(reservaId) + ".";
            if (estado != "CHECK_IN") return "Solo se cambia de habitación a un huésped alojado.";

            string numeroNuevo = null, estadoNuevo = null;
            using (var cmd = new NpgsqlCommand("SELECT numero, estado FROM public.habitacion WHERE habitacion_id = @id FOR UPDATE", conn, tx))
            {
                cmd.Parameters.AddWithValue("id", nuevaHabitacionId);
                using (var r = cmd.ExecuteReader())
                    if (r.Read()) { numeroNuevo = r.GetString(0); estadoNuevo = r.GetString(1); }
            }
            if (numeroNuevo == null) return "La habitación elegida ya no existe.";
            if (numeroNuevo == numeroActual) return "El huésped ya está en la habitación " + numeroNuevo + ".";
            if (estadoNuevo != "DISPONIBLE" && estadoNuevo != "INSPECCIONADA" && estadoNuevo != "LIMPIA")
                return "La habitación " + numeroNuevo + " no está lista (" + Models.EstadosHabitacion.ParaPantalla(estadoNuevo).ToLowerInvariant() + ").";

            object cruce = ReservaRepository.Escalar(conn, tx,
                @"SELECT 1 FROM public.reservahabitacion rh JOIN public.reserva r ON r.reserva_id = rh.reserva_id
                  WHERE rh.habitacion_id = @h AND r.reserva_id <> @r AND r.estado IN ('CONFIRMADA', 'CHECK_IN')
                    AND rh.fecha_entrada < @salida AND rh.fecha_salida > CURRENT_DATE LIMIT 1",
                P("h", nuevaHabitacionId), P("r", reservaId), ReservaRepository.Fecha("salida", salida));
            if (cruce != null) return "La habitación " + numeroNuevo + " tiene otra reserva en esas fechas.";

            Escalar(conn, tx, "UPDATE public.reservahabitacion SET habitacion_id = @nueva WHERE reserva_id = @r", P("nueva", nuevaHabitacionId), P("r", reservaId));
            string motivo = "Cambio de habitación " + Models.ReservaRegistro.CodigoDe(reservaId);
            string error = HabitacionesRepository.CambiarEstadoEn(conn, tx, numeroActual, "Sucia", usuarioId, motivo, false);
            if (error != null) return error;
            CrearTareaLimpieza(conn, tx, numeroActual, reservaId);
            return HabitacionesRepository.CambiarEstadoEn(conn, tx, numeroNuevo, "Ocupada", usuarioId, motivo, false);
        }

        internal static string CancelarEn(NpgsqlConnection conn, NpgsqlTransaction tx, int reservaId)
        {
            string estado, numero;
            DateTime entrada, salida;
            decimal tarifa;
            if (!LeerReserva(conn, tx, reservaId, out estado, out numero, out entrada, out salida, out tarifa)) return "No existe la reserva " + Models.ReservaRegistro.CodigoDe(reservaId) + ".";
            if (estado != "CONFIRMADA") return "La reserva está " + ReservaRepository.ParaPantalla(estado).ToLowerInvariant() + ": no se puede cancelar.";
            CambiarEstado(conn, tx, reservaId, "CANCELADA");
            return null;
        }

        static bool LeerReserva(NpgsqlConnection conn, NpgsqlTransaction tx, int reservaId, out string estado, out string numero,
                                out DateTime entrada, out DateTime salida, out decimal tarifa)
        {
            const string sql = @"SELECT r.estado, h.numero, r.fecha_entrada, r.fecha_salida, COALESCE(tf.precio_base, 0)
                                 FROM public.reserva r
                                 LEFT JOIN public.reservahabitacion rh ON rh.reserva_id = r.reserva_id
                                 LEFT JOIN public.habitacion h ON h.habitacion_id = rh.habitacion_id
                                 LEFT JOIN LATERAL (SELECT precio_base FROM public.tarifa
                                                    WHERE tipo_habitacion_id = h.tipo_habitacion_id ORDER BY tarifa_id LIMIT 1) tf ON true
                                 WHERE r.reserva_id = @id
                                 FOR UPDATE OF r";
            using (var cmd = new NpgsqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("id", reservaId);
                using (var r = cmd.ExecuteReader())
                {
                    estado = numero = null; entrada = salida = DateTime.MinValue; tarifa = 0;
                    if (!r.Read()) return false;
                    estado = r.GetString(0);
                    numero = r.IsDBNull(1) ? null : r.GetString(1);
                    entrada = r.GetDateTime(2);
                    salida = r.GetDateTime(3);
                    tarifa = r.GetDecimal(4);
                    return true;
                }
            }
        }

        // La habitación que queda sucia entra a la cola de Limpieza.
        static void CrearTareaLimpieza(NpgsqlConnection conn, NpgsqlTransaction tx, string numeroHabitacion, int reservaId)
        {
            Escalar(conn, tx,
                @"INSERT INTO public.tarealimpieza (habitacion_id, reserva_id, tipo, fecha_programada, estado)
                  SELECT habitacion_id, @reserva, 'LIMPIEZA', LOCALTIMESTAMP, 'PENDIENTE' FROM public.habitacion WHERE numero = @numero",
                P("reserva", reservaId), P("numero", numeroHabitacion));
        }

        static void CambiarEstado(NpgsqlConnection conn, NpgsqlTransaction tx, int reservaId, string estado)
        {
            Escalar(conn, tx, "UPDATE public.reserva SET estado = @estado WHERE reserva_id = @id", P("estado", estado), P("id", reservaId));
        }

        static NpgsqlParameter P(string nombre, object valor) { return new NpgsqlParameter(nombre, valor); }

        static object Escalar(NpgsqlConnection conn, NpgsqlTransaction tx, string sql, params NpgsqlParameter[] parametros)
        {
            return ReservaRepository.Escalar(conn, tx, sql, parametros);
        }
    }
}

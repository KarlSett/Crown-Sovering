using System;
using System.Collections.Generic;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;

namespace Crown__.Backend.Repositories
{
    public class HabitacionesRepository
    {
        // El huésped actual sale de la estancia en curso (ALOJADO) de la reserva que ocupa la habitación.
        public List<HabitacionResumen> Listar()
        {
            const string sql = @"SELECT h.numero, t.nombre, p.nombre, h.estado, t.descripcion, t.capacidad, g.huesped, g.entrada, g.salida, g.reserva_id
                                 FROM public.habitacion h
                                 JOIN public.piso p ON p.piso_id = h.piso_id
                                 JOIN public.tipohabitacion t ON t.tipo_habitacion_id = h.tipo_habitacion_id
                                 LEFT JOIN LATERAL (
                                     SELECT hu.nombre || ' ' || hu.apellido AS huesped, rh.fecha_entrada AS entrada, rh.fecha_salida AS salida, r.reserva_id
                                     FROM public.reservahabitacion rh
                                     JOIN public.reserva r ON r.reserva_id = rh.reserva_id
                                     JOIN public.estancia e ON e.reserva_id = r.reserva_id AND e.estado = 'ALOJADO'
                                     JOIN public.huesped hu ON hu.huesped_id = r.huesped_principal_id
                                     WHERE rh.habitacion_id = h.habitacion_id
                                     ORDER BY rh.fecha_entrada DESC
                                     LIMIT 1) g ON true
                                 ORDER BY p.nombre, h.numero";
            return ConexionDb.Consultar(sql, r => new HabitacionResumen
            {
                Numero = r.GetString(0),
                Tipo = r.GetString(1),
                Piso = r.GetString(2),
                Estado = EstadosHabitacion.ParaPantalla(r.GetString(3)),
                Descripcion = r.IsDBNull(4) ? null : r.GetString(4),
                Capacidad = r.GetInt32(5),
                Huesped = r.IsDBNull(6) ? null : r.GetString(6),
                Entrada = r.IsDBNull(7) ? (DateTime?)null : r.GetDateTime(7),
                Salida = r.IsDBNull(8) ? (DateTime?)null : r.GetDateTime(8),
                Reserva = r.IsDBNull(9) ? null : ReservaRegistro.CodigoDe(r.GetInt32(9))
            });
        }

        // Devuelve null si se cambió, o el motivo por el que no se pudo.
        public string CambiarEstado(string numero, string nuevoEstado, int usuarioId, string motivo)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    string error = CambiarEstadoEn(conn, tx, numero, nuevoEstado, usuarioId, motivo, true);
                    if (error == null) tx.Commit();
                    return error;
                }
            }
        }

        // validar = false lo usan el check-in y el check-out, que son los únicos que ocupan o liberan una habitación.
        internal static string CambiarEstadoEn(NpgsqlConnection conn, NpgsqlTransaction tx, string numero, string nuevoEstado,
                                               int usuarioId, string motivo, bool validar)
        {
            int habitacionId;
            string actual;
            using (var cmd = new NpgsqlCommand("SELECT habitacion_id, estado FROM public.habitacion WHERE numero = @numero FOR UPDATE", conn, tx))
            {
                cmd.Parameters.AddWithValue("numero", numero);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return "No existe la habitación " + numero + ".";
                    habitacionId = r.GetInt32(0);
                    actual = EstadosHabitacion.ParaPantalla(r.GetString(1));
                }
            }

            if (validar && !EstadosHabitacion.Permitida(actual, nuevoEstado))
                return "La habitación " + numero + " está " + actual.ToLowerInvariant() + ": no puede pasar a " + nuevoEstado.ToLowerInvariant() + ".";

            string nuevoBase = EstadosHabitacion.ParaBase(nuevoEstado);
            Ejecutar(conn, tx, "UPDATE public.habitacion SET estado = @estado WHERE habitacion_id = @id",
                new NpgsqlParameter("estado", nuevoBase), new NpgsqlParameter("id", habitacionId));
            // el estado anterior se cierra y el nuevo queda abierto en el historial
            Ejecutar(conn, tx, "UPDATE public.habitacionestadohistorial SET fecha_fin = LOCALTIMESTAMP WHERE habitacion_id = @id AND fecha_fin IS NULL",
                new NpgsqlParameter("id", habitacionId));
            Ejecutar(conn, tx, @"INSERT INTO public.habitacionestadohistorial (habitacion_id, estado, fecha_inicio, motivo)
                                 VALUES (@id, @estado, LOCALTIMESTAMP, @motivo)",
                new NpgsqlParameter("id", habitacionId), new NpgsqlParameter("estado", nuevoBase),
                new NpgsqlParameter("motivo", (object)motivo ?? DBNull.Value));
            return null;
        }

        private static void Ejecutar(NpgsqlConnection conn, NpgsqlTransaction tx, string sql, params NpgsqlParameter[] parametros)
        {
            using (var cmd = new NpgsqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddRange(parametros);
                cmd.ExecuteNonQuery();
            }
        }
    }
}

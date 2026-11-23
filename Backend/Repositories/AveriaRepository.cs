using System;
using System.Collections.Generic;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;

namespace Crown__.Backend.Repositories
{
    // Una avería es una incidencia de la habitación (la severidad va en tipo) con su orden de mantenimiento.
    // La base no guarda quién la reportó: eso queda en la auditoría.
    public class AveriaRepository
    {
        public List<Averia> ObtenerTodas()
        {
            const string sql = @"SELECT i.incidencia_id, COALESCE(h.numero, '-'), i.descripcion, i.tipo,
                                        CASE WHEN i.estado = 'RESUELTA' THEN 'Resuelta' ELSE 'Pendiente' END, COALESCE(i.fecha, LOCALTIMESTAMP)
                                 FROM public.incidencia i
                                 LEFT JOIN public.habitacion h ON h.habitacion_id = i.habitacion_id
                                 ORDER BY i.fecha DESC, i.incidencia_id DESC";
            return ConexionDb.Consultar(sql, r => new Averia
            {
                Id = r.GetInt32(0),
                Habitacion = r.GetString(1),
                Descripcion = r.GetString(2),
                Prioridad = r.GetString(3),
                Estado = r.GetString(4),
                Reporta = "—",
                Fecha = r.GetDateTime(5)
            });
        }

        // Una avería crítica bloquea la habitación (pasa a Mantenimiento). Devuelve null si se guardó, o el motivo.
        public string Registrar(string numeroHabitacion, int usuario, string descripcion, string prioridad)
        {
            return EnTransaccion((conn, tx) => RegistrarEn(conn, tx, numeroHabitacion, usuario, descripcion, prioridad));
        }

        // Al resolver, si la habitación estaba bloqueada y no quedan otras averías críticas, vuelve a Disponible.
        public string Resolver(int id, int usuario)
        {
            return EnTransaccion((conn, tx) => ResolverEn(conn, tx, id, usuario));
        }

        static string EnTransaccion(Func<NpgsqlConnection, NpgsqlTransaction, string> operacion)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    string error = operacion(conn, tx);
                    if (error == null) tx.Commit();
                    return error;
                }
            }
        }

        internal static string RegistrarEn(NpgsqlConnection conn, NpgsqlTransaction tx, string numeroHabitacion, int usuario, string descripcion, string prioridad)
        {
            int habitacionId;
            string estado;
            using (var cmd = new NpgsqlCommand("SELECT habitacion_id, estado FROM public.habitacion WHERE numero = @numero FOR UPDATE", conn, tx))
            {
                cmd.Parameters.AddWithValue("numero", numeroHabitacion);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return "No existe la habitación " + numeroHabitacion + ".";
                    habitacionId = r.GetInt32(0);
                    estado = r.GetString(1);
                }
            }

            bool critica = prioridad == "Crítica";
            if (critica && estado == "OCUPADA")
                return "La habitación " + numeroHabitacion + " está ocupada: reubique al huésped antes de bloquearla por una avería crítica.";

            int incidencia = (int)ReservaRepository.Escalar(conn, tx,
                @"INSERT INTO public.incidencia (habitacion_id, tipo, descripcion, estado, fecha)
                  VALUES (@habitacion, @tipo, @descripcion, 'ABIERTA', LOCALTIMESTAMP) RETURNING incidencia_id",
                new NpgsqlParameter("habitacion", habitacionId), new NpgsqlParameter("tipo", prioridad), new NpgsqlParameter("descripcion", descripcion));
            ReservaRepository.Escalar(conn, tx,
                "INSERT INTO public.ordenmantenimiento (incidencia_id, fecha_inicio, estado) VALUES (@incidencia, LOCALTIMESTAMP, 'PENDIENTE')",
                new NpgsqlParameter("incidencia", incidencia));

            if (critica && estado != "MANTENIMIENTO")
                return HabitacionesRepository.CambiarEstadoEn(conn, tx, numeroHabitacion, "Mantenimiento", usuario, "Avería crítica", false);
            return null;
        }

        internal static string ResolverEn(NpgsqlConnection conn, NpgsqlTransaction tx, int id, int usuario)
        {
            int habitacionId = 0;
            string numero = null, estadoHabitacion = null, estado = null;
            using (var cmd = new NpgsqlCommand(
                @"SELECT i.estado, h.habitacion_id, h.numero, h.estado FROM public.incidencia i
                  LEFT JOIN public.habitacion h ON h.habitacion_id = i.habitacion_id
                  WHERE i.incidencia_id = @id FOR UPDATE OF i", conn, tx))
            {
                cmd.Parameters.AddWithValue("id", id);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return "La orden ya no existe.";
                    estado = r.GetString(0);
                    if (!r.IsDBNull(1)) { habitacionId = r.GetInt32(1); numero = r.GetString(2); estadoHabitacion = r.GetString(3); }
                }
            }
            if (estado == "RESUELTA") return "La orden ya estaba resuelta.";

            var p = new NpgsqlParameter("id", id);
            ReservaRepository.Escalar(conn, tx, "UPDATE public.incidencia SET estado = 'RESUELTA' WHERE incidencia_id = @id", p);
            ReservaRepository.Escalar(conn, tx,
                "UPDATE public.ordenmantenimiento SET fecha_fin = LOCALTIMESTAMP, estado = 'COMPLETADA' WHERE incidencia_id = @id", new NpgsqlParameter("id", id));

            if (numero == null || estadoHabitacion != "MANTENIMIENTO") return null;
            object criticasAbiertas = ReservaRepository.Escalar(conn, tx,
                "SELECT COUNT(*) FROM public.incidencia WHERE habitacion_id = @h AND estado <> 'RESUELTA' AND tipo = 'Crítica'", new NpgsqlParameter("h", habitacionId));
            if (Convert.ToInt32(criticasAbiertas) > 0) return null;
            return HabitacionesRepository.CambiarEstadoEn(conn, tx, numero, "Disponible", usuario, "Avería resuelta", false);
        }
    }
}

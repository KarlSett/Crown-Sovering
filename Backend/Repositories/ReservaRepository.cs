using System;
using System.Collections.Generic;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;
using NpgsqlTypes;

namespace Crown__.Backend.Repositories
{
    // Estados en la base: CONFIRMADA, CHECK_IN, CHECK_OUT, CANCELADA.
    public class ReservaRepository
    {
        const string SqlListar = @"SELECT r.reserva_id, hu.nombre || ' ' || hu.apellido, c.nombre, h.numero, t.nombre,
                                          r.fecha_entrada, r.fecha_salida, r.estado, COALESCE(tf.precio_base, 0), r.cantidad_personas
                                   FROM public.reserva r
                                   JOIN public.huesped hu ON hu.huesped_id = r.huesped_principal_id
                                   JOIN public.canalreserva c ON c.canal_id = r.canal_id
                                   LEFT JOIN public.reservahabitacion rh ON rh.reserva_id = r.reserva_id
                                   LEFT JOIN public.habitacion h ON h.habitacion_id = rh.habitacion_id
                                   LEFT JOIN public.tipohabitacion t ON t.tipo_habitacion_id = h.tipo_habitacion_id
                                   LEFT JOIN LATERAL (SELECT precio_base FROM public.tarifa
                                                      WHERE tipo_habitacion_id = t.tipo_habitacion_id ORDER BY tarifa_id LIMIT 1) tf ON true
                                   ORDER BY r.fecha_entrada DESC, r.reserva_id DESC";

        public List<ReservaRegistro> ObtenerTodas()
        {
            return ConexionDb.Consultar(SqlListar, r => new ReservaRegistro
            {
                Id = r.GetInt32(0),
                Huesped = r.GetString(1),
                Canal = r.GetString(2),
                Habitacion = r.IsDBNull(3) ? null : r.GetString(3),
                Tipo = r.IsDBNull(4) ? null : r.GetString(4),
                CheckIn = r.GetDateTime(5),
                CheckOut = r.GetDateTime(6),
                Estado = ParaPantalla(r.GetString(7)),
                Tarifa = r.GetDecimal(8),
                Personas = r.GetInt32(9)
            });
        }

        public static string ParaPantalla(string estado)
        {
            switch (estado)
            {
                case "CONFIRMADA": return "Confirmada";
                case "CHECK_IN": return "Check-In";
                case "CHECK_OUT": return "Check-Out";
                case "CANCELADA": return "Cancelada";
                default: return estado;
            }
        }

        // Guarda el huésped (si es nuevo), la reserva y su habitación en una sola transacción.
        // Devuelve null y el motivo en error si no hay habitación disponible.
        public ReservaRegistro Crear(NuevaReserva datos, out string error)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    var reserva = CrearEn(conn, tx, datos, out error);
                    if (reserva != null) tx.Commit();
                    return reserva;
                }
            }
        }

        internal ReservaRegistro CrearEn(NpgsqlConnection conn, NpgsqlTransaction tx, NuevaReserva datos, out string error)
        {
            error = null;
            string preferida = datos.HabitacionPreferida ?? "";

            // una reserva a la vez, para no asignar la misma habitación dos veces
            Escalar(conn, tx, "LOCK TABLE public.reserva IN SHARE ROW EXCLUSIVE MODE");

            object capacidad = Escalar(conn, tx, "SELECT capacidad FROM public.tipohabitacion WHERE nombre = @tipo",
                new NpgsqlParameter("tipo", datos.TipoHabitacion));
            if (capacidad == null) { error = "El tipo de habitación " + datos.TipoHabitacion + " no existe."; return null; }
            if (datos.Huespedes > Convert.ToInt32(capacidad))
            {
                error = "Una habitación " + datos.TipoHabitacion + " admite hasta " + capacidad + " huéspedes.";
                return null;
            }

            int habitacionId;
            string numero;
            if (!BuscarHabitacion(conn, tx, datos.TipoHabitacion, preferida, datos.Llegada, datos.Salida, out habitacionId, out numero))
            {
                error = preferida.Length > 0
                    ? "La habitación " + preferida + " no está disponible como " + datos.TipoHabitacion + " en esas fechas."
                    : "No hay habitaciones " + datos.TipoHabitacion + " disponibles en esas fechas.";
                return null;
            }

            object canal = Escalar(conn, tx, "SELECT canal_id FROM public.canalreserva WHERE nombre = @canal", new NpgsqlParameter("canal", datos.Canal));
            if (canal == null) { error = "El canal " + datos.Canal + " no existe en la base."; return null; }

            int huespedId = ObtenerOCrearHuesped(conn, tx, datos);
            int reservaId = (int)Escalar(conn, tx,
                @"INSERT INTO public.reserva (huesped_principal_id, canal_id, fecha_reserva, fecha_entrada, fecha_salida, estado, cantidad_personas)
                  VALUES (@huesped, @canal, LOCALTIMESTAMP, @llegada, @salida, 'CONFIRMADA', @personas)
                  RETURNING reserva_id",
                new NpgsqlParameter("huesped", huespedId), new NpgsqlParameter("canal", (int)canal),
                Fecha("llegada", datos.Llegada), Fecha("salida", datos.Salida), new NpgsqlParameter("personas", datos.Huespedes));
            Escalar(conn, tx,
                @"INSERT INTO public.reservahabitacion (reserva_id, habitacion_id, fecha_entrada, fecha_salida)
                  VALUES (@reserva, @habitacion, @llegada, @salida)",
                new NpgsqlParameter("reserva", reservaId), new NpgsqlParameter("habitacion", habitacionId),
                Fecha("llegada", datos.Llegada), Fecha("salida", datos.Salida));
            Escalar(conn, tx, "INSERT INTO public.reservahuesped (reserva_id, huesped_id) VALUES (@reserva, @huesped)",
                new NpgsqlParameter("reserva", reservaId), new NpgsqlParameter("huesped", huespedId));

            return new ReservaRegistro
            {
                Id = reservaId,
                Huesped = datos.Nombre,
                Canal = datos.Canal,
                Habitacion = numero,
                Tipo = datos.TipoHabitacion,
                CheckIn = datos.Llegada,
                CheckOut = datos.Salida,
                Estado = "Confirmada",
                Tarifa = datos.Tarifa,
                Personas = datos.Huespedes
            };
        }

        // Reserva una habitación concreta para un huésped que ya existe. Devuelve null y el motivo en error si no se puede.
        public ReservaRegistro CrearPorIds(int huespedId, int habitacionId, DateTime llegada, DateTime salida, string canal, int personas, out string error)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    var reserva = CrearPorIdsEn(conn, tx, huespedId, habitacionId, llegada, salida, canal, personas, out error);
                    if (reserva != null) tx.Commit();
                    return reserva;
                }
            }
        }

        internal ReservaRegistro CrearPorIdsEn(NpgsqlConnection conn, NpgsqlTransaction tx, int huespedId, int habitacionId,
                                               DateTime llegada, DateTime salida, string canal, int personas, out string error)
        {
            error = null;
            Escalar(conn, tx, "LOCK TABLE public.reserva IN SHARE ROW EXCLUSIVE MODE");

            string numero = null, tipo = null, estado = null;
            int capacidad = 0;
            using (var cmd = new NpgsqlCommand(
                @"SELECT h.numero, t.nombre, h.estado, t.capacidad FROM public.habitacion h
                  JOIN public.tipohabitacion t ON t.tipo_habitacion_id = h.tipo_habitacion_id WHERE h.habitacion_id = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("id", habitacionId);
                using (var r = cmd.ExecuteReader())
                    if (r.Read()) { numero = r.GetString(0); tipo = r.GetString(1); estado = r.GetString(2); capacidad = r.GetInt32(3); }
            }
            if (numero == null) { error = "La habitación ya no existe."; return null; }
            if (estado == "MANTENIMIENTO" || estado == "BLOQUEADA" || estado == "FUERA_DE_SERVICIO") { error = "La habitación " + numero + " no está operativa."; return null; }
            if (personas > capacidad) { error = "Una habitación " + tipo + " admite hasta " + capacidad + " huéspedes."; return null; }

            object ocupada = Escalar(conn, tx,
                @"SELECT 1 FROM public.reservahabitacion rh JOIN public.reserva r ON r.reserva_id = rh.reserva_id
                  WHERE rh.habitacion_id = @h AND r.estado IN ('CONFIRMADA', 'CHECK_IN')
                    AND rh.fecha_entrada < @salida AND rh.fecha_salida > @llegada LIMIT 1",
                new NpgsqlParameter("h", habitacionId), Fecha("llegada", llegada), Fecha("salida", salida));
            if (ocupada != null) { error = "La habitación " + numero + " ya está reservada en esas fechas."; return null; }

            object huesped = Escalar(conn, tx, "SELECT nombre || ' ' || apellido FROM public.huesped WHERE huesped_id = @id", new NpgsqlParameter("id", huespedId));
            if (huesped == null) { error = "El huésped ya no existe."; return null; }
            object canalId = Escalar(conn, tx, "SELECT canal_id FROM public.canalreserva WHERE nombre = @canal", new NpgsqlParameter("canal", canal));
            if (canalId == null) { error = "El canal " + canal + " no existe en la base."; return null; }

            int reservaId = (int)Escalar(conn, tx,
                @"INSERT INTO public.reserva (huesped_principal_id, canal_id, fecha_reserva, fecha_entrada, fecha_salida, estado, cantidad_personas)
                  VALUES (@huesped, @canal, LOCALTIMESTAMP, @llegada, @salida, 'CONFIRMADA', @personas) RETURNING reserva_id",
                new NpgsqlParameter("huesped", huespedId), new NpgsqlParameter("canal", (int)canalId),
                Fecha("llegada", llegada), Fecha("salida", salida), new NpgsqlParameter("personas", personas));
            Escalar(conn, tx,
                "INSERT INTO public.reservahabitacion (reserva_id, habitacion_id, fecha_entrada, fecha_salida) VALUES (@r, @h, @llegada, @salida)",
                new NpgsqlParameter("r", reservaId), new NpgsqlParameter("h", habitacionId), Fecha("llegada", llegada), Fecha("salida", salida));
            Escalar(conn, tx, "INSERT INTO public.reservahuesped (reserva_id, huesped_id) VALUES (@r, @h)",
                new NpgsqlParameter("r", reservaId), new NpgsqlParameter("h", huespedId));

            return new ReservaRegistro
            {
                Id = reservaId, Huesped = (string)huesped, Canal = canal, Habitacion = numero, Tipo = tipo,
                CheckIn = llegada, CheckOut = salida, Estado = "Confirmada", Personas = personas
            };
        }

        // Una habitación del tipo pedido, que no esté fuera de uso ni tenga otra reserva que se cruce con las fechas.
        static bool BuscarHabitacion(NpgsqlConnection conn, NpgsqlTransaction tx, string tipo, string preferida,
                                     DateTime llegada, DateTime salida, out int id, out string numero)
        {
            const string sql = @"SELECT h.habitacion_id, h.numero
                                 FROM public.habitacion h
                                 JOIN public.tipohabitacion t ON t.tipo_habitacion_id = h.tipo_habitacion_id
                                 WHERE t.nombre = @tipo
                                   AND h.estado NOT IN ('MANTENIMIENTO', 'BLOQUEADA', 'FUERA_DE_SERVICIO')
                                   AND (@preferida = '' OR h.numero = @preferida)
                                   AND NOT EXISTS (SELECT 1 FROM public.reservahabitacion rh
                                                   JOIN public.reserva r ON r.reserva_id = rh.reserva_id
                                                   WHERE rh.habitacion_id = h.habitacion_id
                                                     AND r.estado IN ('CONFIRMADA', 'CHECK_IN')
                                                     AND rh.fecha_entrada < @salida AND rh.fecha_salida > @llegada)
                                 ORDER BY h.numero
                                 LIMIT 1";
            using (var cmd = new NpgsqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("tipo", tipo);
                cmd.Parameters.AddWithValue("preferida", preferida);
                cmd.Parameters.Add(Fecha("llegada", llegada));
                cmd.Parameters.Add(Fecha("salida", salida));
                using (var r = cmd.ExecuteReader())
                {
                    id = 0; numero = null;
                    if (!r.Read()) return false;
                    id = r.GetInt32(0);
                    numero = r.GetString(1);
                    return true;
                }
            }
        }

        static int ObtenerOCrearHuesped(NpgsqlConnection conn, NpgsqlTransaction tx, NuevaReserva datos)
        {
            string documento = (datos.TipoDocumento + " " + datos.NumeroDocumento).Trim();
            object existente = Escalar(conn, tx, "SELECT huesped_id FROM public.huesped WHERE lower(documento) = lower(@documento)",
                new NpgsqlParameter("documento", documento));
            if (existente != null) return (int)existente;

            string nombres, apellidos;
            NombrePersona.Separar(datos.Nombre, out nombres, out apellidos);
            return (int)Escalar(conn, tx,
                @"INSERT INTO public.huesped (nombre, apellido, documento, email)
                  VALUES (@nombre, @apellido, @documento, @email) RETURNING huesped_id",
                new NpgsqlParameter("nombre", nombres), new NpgsqlParameter("apellido", apellidos),
                new NpgsqlParameter("documento", documento),
                new NpgsqlParameter("email", NpgsqlDbType.Varchar) { Value = string.IsNullOrWhiteSpace(datos.Correo) ? DBNull.Value : (object)datos.Correo });
        }

        internal static NpgsqlParameter Fecha(string nombre, DateTime valor)
        {
            return new NpgsqlParameter(nombre, NpgsqlDbType.Date) { Value = valor.Date };
        }

        internal static object Escalar(NpgsqlConnection conn, NpgsqlTransaction tx, string sql, params NpgsqlParameter[] parametros)
        {
            using (var cmd = new NpgsqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddRange(parametros);
                return cmd.ExecuteScalar();
            }
        }
    }
}

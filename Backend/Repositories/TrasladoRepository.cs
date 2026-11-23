using System;
using System.Collections.Generic;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;

namespace Crown__.Backend.Repositories
{
    // Un traslado es un servicio de transporte de la estancia (reservaservicio + traslado).
    // La base no guarda vuelo, conductor ni placa: el vuelo viaja dentro del origen o destino.
    public class TrasladoRepository
    {
        const string Base = @"SELECT t.traslado_id, t.origen, t.destino, t.fecha_hora, t.estado, h.numero, hu.nombre || ' ' || hu.apellido
                                 FROM public.traslado t
                                 JOIN public.reservaservicio rs ON rs.reserva_servicio_id = t.reserva_servicio_id
                                 JOIN public.estancia e ON e.estancia_id = rs.estancia_id
                                 JOIN public.reserva r ON r.reserva_id = e.reserva_id
                                 JOIN public.huesped hu ON hu.huesped_id = r.huesped_principal_id
                                 LEFT JOIN public.reservahabitacion rh ON rh.reserva_id = r.reserva_id
                                 LEFT JOIN public.habitacion h ON h.habitacion_id = rh.habitacion_id
                                 ";

        public List<TrasladoVista> Del(DateTime dia)
        {
            return Leer(Base + "WHERE t.fecha_hora::date = @dia ORDER BY t.fecha_hora", new NpgsqlParameter("dia", NpgsqlTypes.NpgsqlDbType.Date) { Value = dia.Date });
        }

        public List<TrasladoVista> Todos()
        {
            return Leer(Base + "ORDER BY t.fecha_hora DESC");
        }

        static List<TrasladoVista> Leer(string sql, params NpgsqlParameter[] parametros)
        {
            return ConexionDb.Consultar(sql, r => new TrasladoVista
            {
                Id = r.GetInt32(0),
                Origen = r.GetString(1),
                Destino = r.GetString(2),
                FechaHora = r.GetDateTime(3),
                Estado = r.GetString(4) == "PROGRAMADO" ? "Programado" : r.GetString(4) == "COMPLETADO" ? "Completado" : r.GetString(4),
                Habitacion = r.IsDBNull(5) ? null : r.GetString(5),
                Cliente = r.GetString(6)
            }, parametros);
        }

        public string Registrar(string codigoReserva, string origen, string destino, DateTime fechaHora, bool cargarAlFolio)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    string error = RegistrarEn(conn, tx, codigoReserva, origen, destino, fechaHora, cargarAlFolio);
                    if (error == null) tx.Commit();
                    return error;
                }
            }
        }

        internal static string RegistrarEn(NpgsqlConnection conn, NpgsqlTransaction tx, string codigoReserva, string origen, string destino,
                                           DateTime fechaHora, bool cargarAlFolio)
        {
            object estancia = ReservaRepository.Escalar(conn, tx,
                "SELECT estancia_id FROM public.estancia WHERE reserva_id = @reserva AND estado = 'ALOJADO' LIMIT 1",
                new NpgsqlParameter("reserva", ReservaRegistro.IdDe(codigoReserva)));
            if (estancia == null) return "La reserva " + codigoReserva + " no tiene un huésped alojado.";

            var servicio = ReservaRepository.Escalar(conn, tx,
                "SELECT servicio_id || '|' || precio FROM public.servicio WHERE tipo = 'Transporte' AND COALESCE(activo, true) ORDER BY servicio_id LIMIT 1");
            if (servicio == null) return "No hay un servicio de transporte en el catálogo.";
            string[] partes = ((string)servicio).Split('|');

            int reservaServicio = (int)ReservaRepository.Escalar(conn, tx,
                @"INSERT INTO public.reservaservicio (estancia_id, servicio_id, fecha, estado)
                  VALUES (@estancia, @servicio, LOCALTIMESTAMP, 'PROGRAMADO') RETURNING reserva_servicio_id",
                new NpgsqlParameter("estancia", (int)estancia), new NpgsqlParameter("servicio", int.Parse(partes[0])));
            ReservaRepository.Escalar(conn, tx,
                @"INSERT INTO public.traslado (reserva_servicio_id, origen, destino, fecha_hora, estado)
                  VALUES (@rs, @origen, @destino, @fecha, 'PROGRAMADO')",
                new NpgsqlParameter("rs", reservaServicio), new NpgsqlParameter("origen", origen),
                new NpgsqlParameter("destino", destino), new NpgsqlParameter("fecha", fechaHora));

            if (!cargarAlFolio) return null;
            string error;
            decimal precio = decimal.Parse(partes[1], System.Globalization.CultureInfo.InvariantCulture);
            FolioRepository.RegistrarCargoEn(conn, tx, codigoReserva, "Transporte", "Transporte · " + origen + " → " + destino, precio, out error);
            return error;
        }
    }
}

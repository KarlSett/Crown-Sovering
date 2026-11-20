using System;
using System.Collections.Generic;
using System.Linq;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;

namespace Crown__.Backend.Repositories
{
    // Canales, grupos, lista de espera y el detalle de una reserva.
    public class ReservasAvanzadasRepository
    {
        public List<CanalVista> Canales()
        {
            return ConexionDb.Consultar("SELECT canal_id, nombre, COALESCE(comision, 0) FROM public.canalreserva ORDER BY nombre",
                r => new CanalVista { Id = r.GetInt32(0), Nombre = r.GetString(1), Comision = r.GetDecimal(2) });
        }

        public bool ExisteCanal(string nombre)
        {
            return ConexionDb.Consultar("SELECT 1 FROM public.canalreserva WHERE lower(nombre) = lower(@n)", r => r.GetInt32(0), new NpgsqlParameter("n", nombre)).Any();
        }

        public void GuardarCanal(string nombre, decimal comision)
        {
            ConexionDb.Ejecutar("INSERT INTO public.canalreserva (nombre, comision) VALUES (@n, @c)", new NpgsqlParameter("n", nombre), new NpgsqlParameter("c", comision));
        }

        public List<GrupoVista> Grupos()
        {
            return ConexionDb.Consultar("SELECT nombre, COALESCE(fecha_creacion, LOCALTIMESTAMP) FROM public.gruporeserva ORDER BY fecha_creacion DESC, grupo_id DESC",
                r => new GrupoVista { Nombre = r.GetString(0), Creado = r.GetDateTime(1) });
        }

        public void GuardarGrupo(string nombre)
        {
            ConexionDb.Ejecutar("INSERT INTO public.gruporeserva (nombre, fecha_creacion) VALUES (@n, LOCALTIMESTAMP)", new NpgsqlParameter("n", nombre));
        }

        public List<EsperaVista> ListaEspera()
        {
            return ConexionDb.Consultar(
                @"SELECT hu.nombre || ' ' || hu.apellido, t.nombre, l.fecha_entrada, l.fecha_salida, l.estado
                  FROM public.listaespera l JOIN public.huesped hu ON hu.huesped_id = l.huesped_id
                  JOIN public.tipohabitacion t ON t.tipo_habitacion_id = l.tipo_habitacion_id ORDER BY l.fecha_entrada",
                r => new EsperaVista { Huesped = r.GetString(0), TipoHabitacion = r.GetString(1), Desde = r.GetDateTime(2), Hasta = r.GetDateTime(3), Estado = r.GetString(4) });
        }

        public DetalleReservaVista Detalle(int reservaId)
        {
            var p = new NpgsqlParameter("r", reservaId);
            return new DetalleReservaVista
            {
                Huespedes = ConexionDb.Consultar(
                    @"SELECT hu.nombre || ' ' || hu.apellido || CASE WHEN hu.huesped_id = r.huesped_principal_id THEN ' (principal)' ELSE '' END
                      FROM public.reservahuesped rh JOIN public.huesped hu ON hu.huesped_id = rh.huesped_id
                      JOIN public.reserva r ON r.reserva_id = rh.reserva_id WHERE rh.reserva_id = @r", x => x.GetString(0), p).ToArray(),
                Habitaciones = ConexionDb.Consultar(
                    @"SELECT h.numero || ' · ' || to_char(rh.fecha_entrada, 'DD/MM') || ' → ' || to_char(rh.fecha_salida, 'DD/MM')
                      FROM public.reservahabitacion rh JOIN public.habitacion h ON h.habitacion_id = rh.habitacion_id WHERE rh.reserva_id = @r",
                    x => x.GetString(0), new NpgsqlParameter("r", reservaId)).ToArray(),
                Solicitudes = ConexionDb.Consultar("SELECT descripcion || ' · ' || COALESCE(estado, '') FROM public.solicitudespecial WHERE reserva_id = @r",
                    x => x.GetString(0), new NpgsqlParameter("r", reservaId)).ToArray(),
                Recursos = ConexionDb.Consultar(
                    @"SELECT ra.nombre || ' x' || rr.cantidad FROM public.reservarecurso rr JOIN public.recursoadicional ra ON ra.recurso_id = rr.recurso_id WHERE rr.reserva_id = @r",
                    x => x.GetString(0), new NpgsqlParameter("r", reservaId)).ToArray()
            };
        }

        public void GuardarSolicitud(int reservaId, string descripcion)
        {
            ConexionDb.Ejecutar("INSERT INTO public.solicitudespecial (reserva_id, descripcion, estado) VALUES (@r, @d, 'PENDIENTE')",
                new NpgsqlParameter("r", reservaId), new NpgsqlParameter("d", descripcion));
        }
    }
}

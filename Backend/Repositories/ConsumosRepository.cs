using System;
using System.Collections.Generic;
using System.Linq;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;

namespace Crown__.Backend.Repositories
{
    // Consumos de la estancia, catálogo de servicios, citas y movimientos de inventario.
    public class ConsumosRepository
    {
        public List<ConsumoLinea> Consumos(int reservaId)
        {
            return ConexionDb.Consultar(
                @"SELECT p.nombre, c.cantidad, p.precio FROM public.consumo c
                  JOIN public.producto p ON p.producto_id = c.producto_id
                  JOIN public.estancia e ON e.estancia_id = c.estancia_id
                  WHERE e.reserva_id = @r ORDER BY c.fecha, c.consumo_id",
                r => new ConsumoLinea { Producto = r.GetString(0), Cantidad = r.GetInt32(1), Precio = r.GetDecimal(2) },
                new NpgsqlParameter("r", reservaId));
        }

        public bool ExisteProducto(string nombre)
        {
            return ConexionDb.Consultar("SELECT 1 FROM public.producto WHERE lower(nombre) = lower(@n)", r => r.GetInt32(0), new NpgsqlParameter("n", nombre)).Any();
        }

        public void NuevoProducto(string nombre, string categoria, decimal precio, int stock)
        {
            ConexionDb.Ejecutar("INSERT INTO public.producto (nombre, categoria, precio, stock_actual, stock_minimo) VALUES (@n, @c, @p, @s, 0)",
                new NpgsqlParameter("n", nombre), new NpgsqlParameter("c", string.IsNullOrWhiteSpace(categoria) ? (object)DBNull.Value : categoria),
                new NpgsqlParameter("p", precio), new NpgsqlParameter("s", stock));
        }

        public List<ServicioVista> Servicios()
        {
            return ConexionDb.Consultar("SELECT servicio_id, nombre, COALESCE(tipo, ''), precio, COALESCE(activo, true) FROM public.servicio ORDER BY tipo, nombre",
                r => new ServicioVista { Id = r.GetInt32(0), Nombre = r.GetString(1), Tipo = r.GetString(2), Precio = r.GetDecimal(3), Activo = r.GetBoolean(4) });
        }

        public bool ExisteServicio(string nombre)
        {
            return ConexionDb.Consultar("SELECT 1 FROM public.servicio WHERE lower(nombre) = lower(@n)", r => r.GetInt32(0), new NpgsqlParameter("n", nombre)).Any();
        }

        public void NuevoServicio(string nombre, string tipo, decimal precio, bool activo)
        {
            ConexionDb.Ejecutar("INSERT INTO public.servicio (nombre, tipo, precio, activo) VALUES (@n, @t, @p, @a)",
                new NpgsqlParameter("n", nombre), new NpgsqlParameter("t", tipo), new NpgsqlParameter("p", precio), new NpgsqlParameter("a", activo));
        }

        public List<CitaVista> Citas()
        {
            return ConexionDb.Consultar(
                @"SELECT e.reserva_id, hu.nombre || ' ' || hu.apellido, c.fecha_hora, COALESCE(c.duracion, 0), COALESCE(c.estado, '')
                  FROM public.citaservicio c
                  JOIN public.reservaservicio rs ON rs.reserva_servicio_id = c.reserva_servicio_id
                  JOIN public.estancia e ON e.estancia_id = rs.estancia_id
                  JOIN public.reserva r ON r.reserva_id = e.reserva_id
                  JOIN public.huesped hu ON hu.huesped_id = r.huesped_principal_id ORDER BY c.fecha_hora DESC",
                r => new CitaVista { Reserva = ReservaRegistro.CodigoDe(r.GetInt32(0)), Huesped = r.GetString(1), FechaHora = r.GetDateTime(2), Duracion = r.GetInt32(3), Estado = r.GetString(4) });
        }

        public List<MovimientoInventarioVista> MovimientosInventario()
        {
            return ConexionDb.Consultar(
                @"SELECT p.nombre, m.tipo, m.cantidad, COALESCE(m.motivo, ''), COALESCE(m.fecha, LOCALTIMESTAMP)
                  FROM public.movimientoinventario m JOIN public.producto p ON p.producto_id = m.producto_id ORDER BY m.fecha DESC, m.movimiento_id DESC LIMIT 100",
                r => new MovimientoInventarioVista { Producto = r.GetString(0), Tipo = r.GetString(1), Cantidad = r.GetInt32(2), Motivo = r.GetString(3), Fecha = r.GetDateTime(4) });
        }
    }
}

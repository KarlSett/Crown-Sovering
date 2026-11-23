using System;
using System.Collections.Generic;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;

namespace Crown__.Backend.Repositories
{
    // Catálogos de servicios (productos de comidas y bebidas, spa) y las operaciones que dependen de ellos.
    // La base no guarda la duración de un servicio ni si un producto está activo: Minutos queda en 0 y Activo en true.
    public class ServicioRepository
    {
        public List<Producto> Productos()
        {
            const string sql = "SELECT producto_id, nombre, COALESCE(categoria, 'Otros'), precio, COALESCE(stock_actual, 0) FROM public.producto ORDER BY categoria, nombre";
            return ConexionDb.Consultar(sql, r => new Producto
            {
                Id = r.GetInt32(0),
                Nombre = r.GetString(1),
                Categoria = r.GetString(2),
                Precio = r.GetDecimal(3),
                Stock = r.GetInt32(4),
                Activo = true
            });
        }

        public List<ServicioSpa> ServiciosSpa()
        {
            const string sql = "SELECT servicio_id, nombre, precio FROM public.servicio WHERE tipo = 'Spa' AND COALESCE(activo, true) ORDER BY nombre";
            return ConexionDb.Consultar(sql, r => new ServicioSpa { Id = r.GetInt32(0), Nombre = r.GetString(1), Precio = r.GetDecimal(2) });
        }

        // Cobra un producto al folio y descuenta el stock, todo en una transacción.
        public string ConsumirProducto(string codigoReserva, int productoId, int cantidad)
        {
            return EnTransaccion((conn, tx) => ConsumirProductoEn(conn, tx, codigoReserva, productoId, cantidad));
        }

        public string CargarServicioSpa(string codigoReserva, int servicioId)
        {
            return EnTransaccion((conn, tx) => CargarServicioSpaEn(conn, tx, codigoReserva, servicioId));
        }

        public string AjustarStock(int productoId, int cambio)
        {
            return EnTransaccion((conn, tx) => AjustarStockEn(conn, tx, productoId, cambio, cambio > 0 ? "ENTRADA" : "MERMA"));
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

        internal static string ConsumirProductoEn(NpgsqlConnection conn, NpgsqlTransaction tx, string codigoReserva, int productoId, int cantidad)
        {
            Producto producto = LeerProducto(conn, tx, productoId);
            if (producto == null) return "El producto ya no existe.";
            if (producto.Categoria == "Insumo") return producto.Nombre + " es un insumo de cocina y no se cobra al huésped.";
            if (producto.Stock < cantidad) return "No hay stock suficiente de " + producto.Nombre + " (quedan " + producto.Stock + ").";

            string concepto = producto.Categoria == "Minibar" ? "Minibar" : "Restaurante";
            string descripcion = producto.Nombre + (cantidad > 1 ? " x" + cantidad : "");
            string error;
            if (!FolioRepository.RegistrarCargoEn(conn, tx, codigoReserva, concepto, Recortar(concepto + " · " + descripcion), producto.Precio * cantidad, out error))
                return error;

            ReservaRepository.Escalar(conn, tx,
                @"INSERT INTO public.consumo (estancia_id, producto_id, cantidad, fecha, tipo)
                  SELECT e.estancia_id, @producto, @cantidad, LOCALTIMESTAMP, @tipo
                  FROM public.estancia e WHERE e.reserva_id = @reserva AND e.estado = 'ALOJADO' LIMIT 1",
                new NpgsqlParameter("producto", productoId), new NpgsqlParameter("cantidad", cantidad),
                new NpgsqlParameter("tipo", concepto.ToUpperInvariant()), new NpgsqlParameter("reserva", ReservaRegistro.IdDe(codigoReserva)));
            return AjustarStockEn(conn, tx, productoId, -cantidad, "CONSUMO");
        }

        internal static string CargarServicioSpaEn(NpgsqlConnection conn, NpgsqlTransaction tx, string codigoReserva, int servicioId)
        {
            string nombre = null;
            decimal precio = 0;
            using (var cmd = new NpgsqlCommand("SELECT nombre, precio FROM public.servicio WHERE servicio_id = @id AND tipo = 'Spa' AND COALESCE(activo, true)", conn, tx))
            {
                cmd.Parameters.AddWithValue("id", servicioId);
                using (var r = cmd.ExecuteReader())
                    if (r.Read()) { nombre = r.GetString(0); precio = r.GetDecimal(1); }
            }
            if (nombre == null) return "El servicio ya no está disponible.";

            string error;
            FolioRepository.RegistrarCargoEn(conn, tx, codigoReserva, "Spa", Recortar("Spa · " + nombre), precio, out error);
            return error;
        }

        internal static string AjustarStockEn(NpgsqlConnection conn, NpgsqlTransaction tx, int productoId, int cambio, string tipo)
        {
            if (LeerProducto(conn, tx, productoId) == null) return "El producto ya no existe.";
            var parametros = new[] { new NpgsqlParameter("cambio", cambio), new NpgsqlParameter("id", productoId) };
            object filas = ReservaRepository.Escalar(conn, tx,
                @"WITH u AS (UPDATE public.producto SET stock_actual = COALESCE(stock_actual, 0) + @cambio
                             WHERE producto_id = @id AND COALESCE(stock_actual, 0) + @cambio >= 0 RETURNING 1)
                  SELECT COUNT(*) FROM u", parametros);
            if (Convert.ToInt32(filas) != 1) return "El stock no puede quedar negativo.";

            ReservaRepository.Escalar(conn, tx,
                @"INSERT INTO public.movimientoinventario (producto_id, tipo, cantidad, fecha, motivo)
                  VALUES (@id, @tipo, @cantidad, LOCALTIMESTAMP, NULL)",
                new NpgsqlParameter("id", productoId), new NpgsqlParameter("tipo", tipo), new NpgsqlParameter("cantidad", Math.Abs(cambio)));
            return null;
        }

        // Bloquea la fila del producto mientras dura la transacción, para que dos consumos no pisen el mismo stock.
        static Producto LeerProducto(NpgsqlConnection conn, NpgsqlTransaction tx, int id)
        {
            using (var cmd = new NpgsqlCommand(
                "SELECT nombre, COALESCE(categoria, 'Otros'), precio, COALESCE(stock_actual, 0) FROM public.producto WHERE producto_id = @id FOR UPDATE", conn, tx))
            {
                cmd.Parameters.AddWithValue("id", id);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new Producto { Id = id, Nombre = r.GetString(0), Categoria = r.GetString(1), Precio = r.GetDecimal(2), Stock = r.GetInt32(3), Activo = true };
                }
            }
        }

        static string Recortar(string texto)
        {
            return texto.Length <= 100 ? texto : texto.Substring(0, 100);
        }
    }
}

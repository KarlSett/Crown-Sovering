using System;
using System.Collections.Generic;
using System.Linq;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;

namespace Crown__.Backend.Repositories
{
    // Folios, cargos, conceptos, facturas y notas de crédito.
    public class FacturacionRepository
    {
        public List<FolioItem> Folios()
        {
            const string sql = @"SELECT f.folio_id, COALESCE(f.nombre, ''), e.reserva_id, hu.nombre || ' ' || hu.apellido, h.numero, COALESCE(f.estado, 'ABIERTO'), COALESCE(f.saldo, 0)
                                 FROM public.folio f
                                 JOIN public.estancia e ON e.estancia_id = f.estancia_id
                                 JOIN public.reserva r ON r.reserva_id = e.reserva_id
                                 JOIN public.huesped hu ON hu.huesped_id = r.huesped_principal_id
                                 LEFT JOIN public.reservahabitacion rh ON rh.reserva_id = r.reserva_id
                                 LEFT JOIN public.habitacion h ON h.habitacion_id = rh.habitacion_id
                                 ORDER BY (CASE WHEN f.estado = 'ABIERTO' THEN 0 ELSE 1 END), f.folio_id DESC";
            return ConexionDb.Consultar(sql, r => new FolioItem
            {
                FolioId = r.GetInt32(0),
                Nombre = r.GetString(1),
                Reserva = ReservaRegistro.CodigoDe(r.GetInt32(2)),
                Huesped = r.GetString(3),
                Habitacion = r.IsDBNull(4) ? null : r.GetString(4),
                Estado = r.GetString(5) == "ABIERTO" ? "Abierto" : "Cerrado",
                Saldo = r.GetDecimal(6)
            });
        }

        public List<CargoVista> Cargos(int folioId)
        {
            return ConexionDb.Consultar(
                @"SELECT COALESCE(c.fecha, LOCALTIMESTAMP), cc.nombre, COALESCE(c.descripcion, ''), c.cantidad, c.precio_unitario, c.cargo_id
                  FROM public.cargo c JOIN public.conceptocargo cc ON cc.concepto_id = c.concepto_id
                  WHERE c.folio_id = @id ORDER BY c.fecha, c.cargo_id",
                r => new CargoVista { Id = r.GetInt32(5), FechaTexto = r.GetDateTime(0).ToString("dd/MM HH:mm"), Concepto = r.GetString(1), Descripcion = r.GetString(2), Cantidad = r.GetDecimal(3), Precio = r.GetDecimal(4) },
                new NpgsqlParameter("id", folioId));
        }

        public string AgregarCargo(int folioId, int conceptoId, string descripcion, decimal cantidad, decimal precio)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    string error = AgregarCargoEn(conn, tx, folioId, conceptoId, descripcion, cantidad, precio);
                    if (error == null) tx.Commit();
                    return error;
                }
            }
        }

        internal static string AgregarCargoEn(NpgsqlConnection conn, NpgsqlTransaction tx, int folioId, int conceptoId, string descripcion, decimal cantidad, decimal precio)
        {
            object estado = ReservaRepository.Escalar(conn, tx, "SELECT estado FROM public.folio WHERE folio_id = @id FOR UPDATE", new NpgsqlParameter("id", folioId));
            if (estado == null) return "El folio ya no existe.";
            if ((string)estado != "ABIERTO") return "El folio está cerrado: no admite más cargos.";

            ReservaRepository.Escalar(conn, tx,
                @"INSERT INTO public.cargo (folio_id, concepto_id, descripcion, cantidad, precio_unitario, fecha)
                  VALUES (@folio, @concepto, @descripcion, @cantidad, @precio, LOCALTIMESTAMP)",
                new NpgsqlParameter("folio", folioId), new NpgsqlParameter("concepto", conceptoId),
                new NpgsqlParameter("descripcion", string.IsNullOrWhiteSpace(descripcion) ? (object)DBNull.Value : descripcion.Trim()),
                new NpgsqlParameter("cantidad", cantidad), new NpgsqlParameter("precio", precio));
            FolioRepository.Recalcular(conn, tx, folioId);
            return null;
        }

        // Un folio adicional para la misma estancia (por ejemplo, los gastos de una empresa).
        public string NuevoFolio(int folioBaseId, string nombre)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    string error = NuevoFolioEn(conn, tx, folioBaseId, nombre);
                    if (error == null) tx.Commit();
                    return error;
                }
            }
        }

        internal static string NuevoFolioEn(NpgsqlConnection conn, NpgsqlTransaction tx, int folioBaseId, string nombre)
        {
            object estancia = ReservaRepository.Escalar(conn, tx,
                @"SELECT f.estancia_id FROM public.folio f JOIN public.estancia e ON e.estancia_id = f.estancia_id
                  WHERE f.folio_id = @id AND e.estado = 'ALOJADO'", new NpgsqlParameter("id", folioBaseId));
            if (estancia == null) return "Solo se agregan folios a una estancia en curso.";
            if (Convert.ToInt32(ReservaRepository.Escalar(conn, tx, "SELECT COUNT(*) FROM public.folio WHERE estancia_id = @e AND lower(nombre) = lower(@n)",
                new NpgsqlParameter("e", (int)estancia), new NpgsqlParameter("n", nombre))) > 0) return "Ya existe un folio llamado " + nombre + " en esta estancia.";

            ReservaRepository.Escalar(conn, tx,
                "INSERT INTO public.folio (estancia_id, nombre, tipo, saldo, estado) VALUES (@e, @n, 'SECUNDARIO', 0, 'ABIERTO')",
                new NpgsqlParameter("e", (int)estancia), new NpgsqlParameter("n", nombre));
            return null;
        }

        // Pasa un cargo a otro folio de la misma estancia y deja constancia en transferenciafolio.
        public string TransferirCargo(int cargoId, int destinoFolioId, int usuarioId)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    string error = TransferirCargoEn(conn, tx, cargoId, destinoFolioId, usuarioId);
                    if (error == null) tx.Commit();
                    return error;
                }
            }
        }

        internal static string TransferirCargoEn(NpgsqlConnection conn, NpgsqlTransaction tx, int cargoId, int destinoFolioId, int usuarioId)
        {
            int origen = 0, estanciaOrigen = 0;
            using (var cmd = new NpgsqlCommand(@"SELECT c.folio_id, f.estancia_id FROM public.cargo c JOIN public.folio f ON f.folio_id = c.folio_id
                                                 WHERE c.cargo_id = @c FOR UPDATE OF c", conn, tx))
            {
                cmd.Parameters.AddWithValue("c", cargoId);
                using (var r = cmd.ExecuteReader())
                    if (r.Read()) { origen = r.GetInt32(0); estanciaOrigen = r.GetInt32(1); }
            }
            if (origen == 0) return "El cargo ya no existe.";
            if (origen == destinoFolioId) return "El cargo ya está en ese folio.";

            object destino = ReservaRepository.Escalar(conn, tx, "SELECT estancia_id || '|' || estado FROM public.folio WHERE folio_id = @f FOR UPDATE", new NpgsqlParameter("f", destinoFolioId));
            if (destino == null) return "El folio de destino ya no existe.";
            var partes = ((string)destino).Split('|');
            if (int.Parse(partes[0]) != estanciaOrigen) return "Solo se transfieren cargos entre folios de la misma estancia.";
            if (partes[1] != "ABIERTO") return "El folio de destino está cerrado.";
            if ((string)ReservaRepository.Escalar(conn, tx, "SELECT estado FROM public.folio WHERE folio_id = @f", new NpgsqlParameter("f", origen)) != "ABIERTO") return "El folio de origen está cerrado.";

            ReservaRepository.Escalar(conn, tx, "UPDATE public.cargo SET folio_id = @d WHERE cargo_id = @c", new NpgsqlParameter("d", destinoFolioId), new NpgsqlParameter("c", cargoId));
            ReservaRepository.Escalar(conn, tx,
                "INSERT INTO public.transferenciafolio (folio_origen_id, folio_destino_id, cargo_id, usuario_id, fecha) VALUES (@o, @d, @c, @u, LOCALTIMESTAMP)",
                new NpgsqlParameter("o", origen), new NpgsqlParameter("d", destinoFolioId), new NpgsqlParameter("c", cargoId), new NpgsqlParameter("u", usuarioId));
            FolioRepository.Recalcular(conn, tx, origen);
            FolioRepository.Recalcular(conn, tx, destinoFolioId);
            return null;
        }

        public List<ConceptoVista> Conceptos()
        {
            return ConexionDb.Consultar("SELECT concepto_id, nombre, COALESCE(categoria, '') FROM public.conceptocargo ORDER BY nombre",
                r => new ConceptoVista { Id = r.GetInt32(0), Nombre = r.GetString(1), Categoria = r.GetString(2) });
        }

        public bool ExisteConcepto(string nombre)
        {
            return ConexionDb.Consultar("SELECT 1 FROM public.conceptocargo WHERE lower(nombre) = lower(@n)", r => r.GetInt32(0), new NpgsqlParameter("n", nombre)).Any();
        }

        public void GuardarConcepto(string nombre, string categoria)
        {
            ConexionDb.Ejecutar("INSERT INTO public.conceptocargo (nombre, categoria) VALUES (@n, @c)",
                new NpgsqlParameter("n", nombre), new NpgsqlParameter("c", string.IsNullOrWhiteSpace(categoria) ? (object)DBNull.Value : categoria));
        }

        public List<FacturaVista> Facturas(int folioId)
        {
            return ConexionDb.Consultar(
                "SELECT factura_id, numero, COALESCE(fecha, LOCALTIMESTAMP), subtotal, impuesto, total, COALESCE(estado, '') FROM public.factura WHERE folio_id = @id ORDER BY factura_id",
                r => new FacturaVista { Id = r.GetInt32(0), Numero = r.GetString(1), Fecha = r.GetDateTime(2), Subtotal = r.GetDecimal(3), Impuesto = r.GetDecimal(4), Total = r.GetDecimal(5), Estado = r.GetString(6) == "EMITIDA" ? "Emitida" : r.GetString(6) },
                new NpgsqlParameter("id", folioId));
        }

        // Factura todo lo cargado al folio, con el impuesto activo de la base (13 % si no hay ninguno).
        public string EmitirFactura(int folioId)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    string error = EmitirFacturaEn(conn, tx, folioId);
                    if (error == null) tx.Commit();
                    return error;
                }
            }
        }

        internal static string EmitirFacturaEn(NpgsqlConnection conn, NpgsqlTransaction tx, int folioId)
        {
            ReservaRepository.Escalar(conn, tx, "LOCK TABLE public.factura IN SHARE ROW EXCLUSIVE MODE");
            decimal subtotal = Convert.ToDecimal(ReservaRepository.Escalar(conn, tx,
                "SELECT COALESCE(SUM(cantidad * precio_unitario), 0) FROM public.cargo WHERE folio_id = @id", new NpgsqlParameter("id", folioId)));
            decimal yaFacturado = Convert.ToDecimal(ReservaRepository.Escalar(conn, tx,
                "SELECT COALESCE(SUM(subtotal), 0) FROM public.factura WHERE folio_id = @id AND estado = 'EMITIDA'", new NpgsqlParameter("id", folioId)));
            decimal pendiente = subtotal - yaFacturado;
            if (pendiente <= 0) return "El folio no tiene cargos pendientes de facturar.";

            object porcentaje = ReservaRepository.Escalar(conn, tx, "SELECT porcentaje FROM public.impuesto WHERE COALESCE(activo, true) ORDER BY impuesto_id LIMIT 1");
            decimal tasa = porcentaje == null ? 13m : Convert.ToDecimal(porcentaje);
            decimal impuesto = Math.Round(pendiente * tasa / 100m, 2);
            int siguiente = Convert.ToInt32(ReservaRepository.Escalar(conn, tx, "SELECT COUNT(*) + 1 FROM public.factura"));

            ReservaRepository.Escalar(conn, tx,
                @"INSERT INTO public.factura (folio_id, numero, fecha, subtotal, impuesto, total, estado)
                  VALUES (@folio, @numero, LOCALTIMESTAMP, @subtotal, @impuesto, @total, 'EMITIDA')",
                new NpgsqlParameter("folio", folioId), new NpgsqlParameter("numero", "F-" + siguiente.ToString("000000")),
                new NpgsqlParameter("subtotal", pendiente), new NpgsqlParameter("impuesto", impuesto), new NpgsqlParameter("total", pendiente + impuesto));
            return null;
        }

        public List<NotaCreditoVista> NotasCredito(int folioId)
        {
            return ConexionDb.Consultar(
                @"SELECT n.numero, f.numero, COALESCE(n.motivo, ''), n.monto FROM public.notacredito n
                  JOIN public.factura f ON f.factura_id = n.factura_id WHERE f.folio_id = @id ORDER BY n.nota_credito_id",
                r => new NotaCreditoVista { Numero = r.GetString(0), Factura = r.GetString(1), Motivo = r.GetString(2), Monto = r.GetDecimal(3) },
                new NpgsqlParameter("id", folioId));
        }

        public string CrearNotaCredito(FacturaVista factura, string motivo, decimal monto)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    string error = CrearNotaCreditoEn(conn, tx, factura.Id, motivo, monto);
                    if (error == null) tx.Commit();
                    return error;
                }
            }
        }

        internal static string CrearNotaCreditoEn(NpgsqlConnection conn, NpgsqlTransaction tx, int facturaId, string motivo, decimal monto)
        {
            object total = ReservaRepository.Escalar(conn, tx, "SELECT total FROM public.factura WHERE factura_id = @id FOR UPDATE", new NpgsqlParameter("id", facturaId));
            if (total == null) return "La factura ya no existe.";
            decimal acreditado = Convert.ToDecimal(ReservaRepository.Escalar(conn, tx,
                "SELECT COALESCE(SUM(monto), 0) FROM public.notacredito WHERE factura_id = @id", new NpgsqlParameter("id", facturaId)));
            if (monto + acreditado > Convert.ToDecimal(total))
                return "Las notas de crédito (Bs " + (monto + acreditado).ToString("0.00") + ") superan el total de la factura (Bs " + Convert.ToDecimal(total).ToString("0.00") + ").";

            int siguiente = Convert.ToInt32(ReservaRepository.Escalar(conn, tx, "SELECT COUNT(*) + 1 FROM public.notacredito"));
            ReservaRepository.Escalar(conn, tx,
                "INSERT INTO public.notacredito (factura_id, numero, motivo, monto, fecha) VALUES (@f, @numero, @motivo, @monto, LOCALTIMESTAMP)",
                new NpgsqlParameter("f", facturaId), new NpgsqlParameter("numero", "NC-" + siguiente.ToString("000000")),
                new NpgsqlParameter("motivo", string.IsNullOrWhiteSpace(motivo) ? (object)DBNull.Value : motivo.Trim()), new NpgsqlParameter("monto", monto));
            return null;
        }
    }
}

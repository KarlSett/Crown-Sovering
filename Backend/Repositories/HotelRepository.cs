using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Crown__.Backend.Data;
using Crown__.Backend.Models;

namespace Crown__.Backend.Repositories
{
    // Lecturas que alimentan las pantallas de Funcionamiento, Reservas, Habitaciones e Ingresos.
    // La base no guarda calidad, extras de la habitación ni tarifa por reserva: se usan valores por tipo.
    public class HotelRepository
    {
        public List<Habitacion> Habitaciones()
        {
            const string sql = @"SELECT h.habitacion_id, h.numero, p.nombre, t.nombre, h.estado, COALESCE(tf.precio_base, 0)
                                 FROM public.habitacion h
                                 JOIN public.piso p ON p.piso_id = h.piso_id
                                 JOIN public.tipohabitacion t ON t.tipo_habitacion_id = h.tipo_habitacion_id
                                 LEFT JOIN LATERAL (SELECT precio_base FROM public.tarifa
                                                    WHERE tipo_habitacion_id = t.tipo_habitacion_id ORDER BY tarifa_id LIMIT 1) tf ON true
                                 ORDER BY p.nombre, h.numero";
            return ConexionDb.Consultar(sql, r => new Habitacion
            {
                Id = r.GetInt32(0),
                NumeroHabitacion = r.GetString(1),
                Piso = NumeroDe(r.GetString(2)),
                CapacidadTipo = r.GetString(3),
                Calidad = r.GetString(3).StartsWith("Suite") ? "Suite" : "Estándar",
                TarifaBase = r.GetDecimal(5),
                Estado = EstadoDe(r.GetString(4)),
                LibreDeHumo = true
            });
        }

        public List<Huesped> Huespedes()
        {
            const string sql = "SELECT huesped_id, nombre, apellido, documento, email, telefono FROM public.huesped ORDER BY apellido, nombre";
            return ConexionDb.Consultar(sql, r =>
            {
                string documento = r.IsDBNull(3) ? "" : r.GetString(3);
                int corte = documento.IndexOf(' ');
                return new Huesped
                {
                    Id = r.GetInt32(0),
                    Nombres = r.GetString(1),
                    Apellidos = r.GetString(2),
                    TipoDocumento = corte > 0 ? documento.Substring(0, corte) : "CI",
                    NumeroDocumento = corte > 0 ? documento.Substring(corte + 1) : documento,
                    Correo = r.IsDBNull(4) ? "" : r.GetString(4),
                    Telefono = r.IsDBNull(5) ? "" : r.GetString(5)
                };
            });
        }

        public List<Reserva> Reservas()
        {
            const string sql = @"SELECT r.reserva_id, r.huesped_principal_id, rh.habitacion_id, c.nombre, r.estado, r.fecha_entrada, r.fecha_salida,
                                        COALESCE(tf.precio_base, 0), r.fecha_reserva
                                 FROM public.reserva r
                                 JOIN public.canalreserva c ON c.canal_id = r.canal_id
                                 JOIN public.reservahabitacion rh ON rh.reserva_id = r.reserva_id
                                 JOIN public.habitacion h ON h.habitacion_id = rh.habitacion_id
                                 LEFT JOIN LATERAL (SELECT precio_base FROM public.tarifa
                                                    WHERE tipo_habitacion_id = h.tipo_habitacion_id ORDER BY tarifa_id LIMIT 1) tf ON true";
            return ConexionDb.Consultar(sql, r => new Reserva
            {
                Id = r.GetInt32(0),
                CodigoReserva = ReservaRegistro.CodigoDe(r.GetInt32(0)),
                HuespedId = r.GetInt32(1),
                HabitacionId = r.GetInt32(2),
                Canal = CanalDe(r.GetString(3)),
                Estado = EstadoReservaDe(r.GetString(4)),
                FechaCheckinEsperado = r.GetDateTime(5).Date.AddHours(15),
                FechaCheckoutEsperado = r.GetDateTime(6).Date.AddHours(12),
                TarifaAplicada = r.GetDecimal(7),
                CreadaEl = r.IsDBNull(8) ? r.GetDateTime(5) : r.GetDateTime(8)
            });
        }

        public List<Folio> Folios()
        {
            const string sql = @"SELECT f.folio_id, e.reserva_id, r.huesped_principal_id, COALESCE(f.tipo, 'INDIVIDUAL'), COALESCE(f.estado, 'ABIERTO'), COALESCE(f.saldo, 0)
                                 FROM public.folio f
                                 JOIN public.estancia e ON e.estancia_id = f.estancia_id
                                 JOIN public.reserva r ON r.reserva_id = e.reserva_id";
            return ConexionDb.Consultar(sql, r => new Folio
            {
                Id = r.GetInt32(0),
                ReservaId = r.GetInt32(1),
                HuespedId = r.GetInt32(2),
                TipoFolio = r.GetString(3) == "INDIVIDUAL" ? "Individual" : r.GetString(3),
                Estado = r.GetString(4) == "ABIERTO" ? "Abierto" : "Cerrado",
                SaldoPendiente = r.GetDecimal(5)
            });
        }

        // Cargos (cuenta = concepto) y pagos (cuenta "Pagos", con su método) de todos los folios.
        public List<Transaccion> Transacciones()
        {
            const string sql = @"SELECT c.cargo_id, c.folio_id, c.fecha, cc.nombre, c.cantidad * c.precio_unitario, 0::numeric, 'Pendiente'
                                 FROM public.cargo c JOIN public.conceptocargo cc ON cc.concepto_id = c.concepto_id
                                 UNION ALL
                                 SELECT 1000000 + p.pago_id, p.folio_id, p.fecha, 'Pagos', 0, p.monto, mp.nombre
                                 FROM public.pago p JOIN public.metodopago mp ON mp.metodo_pago_id = p.metodo_pago_id
                                 WHERE p.estado = 'COMPLETADO'";
            return ConexionDb.Consultar(sql, r => new Transaccion
            {
                Id = r.GetInt32(0),
                FolioId = r.GetInt32(1),
                Fecha = r.IsDBNull(2) ? DateTime.Now : r.GetDateTime(2),
                Concepto = r.GetString(3),
                Cargo = r.GetDecimal(4),
                Abono = r.GetDecimal(5),
                MetodoPago = r.GetString(6),
                Usuario = "—"
            });
        }

        // "Piso 3" -> 3
        private static int NumeroDe(string texto)
        {
            var coincidencia = Regex.Match(texto ?? "", @"\d+");
            return coincidencia.Success ? int.Parse(coincidencia.Value) : 0;
        }

        // Los estados de limpieza y los bloqueos de la base se agrupan en los cinco estados que muestra el panel.
        private static EstadoHabitacion EstadoDe(string estado)
        {
            switch (estado)
            {
                case "OCUPADA": return EstadoHabitacion.Ocupada;
                case "SUCIA":
                case "EN_LIMPIEZA":
                case "LIMPIA":
                case "INSPECCIONADA": return EstadoHabitacion.Limpieza;
                case "MANTENIMIENTO":
                case "BLOQUEADA": return EstadoHabitacion.Mantenimiento;
                case "FUERA_DE_SERVICIO": return EstadoHabitacion.FueraDeServicio;
                default: return EstadoHabitacion.Disponible;
            }
        }

        private static EstadoReserva EstadoReservaDe(string estado)
        {
            switch (estado)
            {
                case "CHECK_IN": return EstadoReserva.EnCurso;
                case "CHECK_OUT": return EstadoReserva.Finalizada;
                case "CANCELADA": return EstadoReserva.Cancelada;
                default: return EstadoReserva.Confirmada;
            }
        }

        private static CanalReserva CanalDe(string canal)
        {
            CanalReserva valor;
            return Enum.TryParse(canal, out valor) ? valor : CanalReserva.Mostrador;
        }
    }
}

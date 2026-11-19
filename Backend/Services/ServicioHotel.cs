using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using Crown__.Backend.Data;
using Crown__.Backend.Models;

namespace Crown__.Backend.Services
{
    public class ServicioHotel
    {
        private static readonly ServicioHotel InstanciaUnica = new ServicioHotel();
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-EC");

        private ServicioHotel()
        {
            DatosHotel.Inicializar();
        }

        public static ServicioHotel Instancia
        {
            get { return InstanciaUnica; }
        }

        // ---------------------------------------------------------------- Funcionamiento

        public ResumenOperativo ObtenerResumenOperativo()
        {
            DateTime hoy = DateTime.Today;

            var habitaciones = DatosHotel.Habitaciones;
            var reservas = DatosHotel.Reservas;
            var transacciones = DatosHotel.Transacciones;

            var resumen = new ResumenOperativo
            {
                TotalHabitaciones = habitaciones.Count,
                Ocupadas = habitaciones.Count(h => h.Estado == EstadoHabitacion.Ocupada),
                Limpieza = habitaciones.Count(h => h.Estado == EstadoHabitacion.Limpieza),
                Mantenimiento = habitaciones.Count(h => h.Estado == EstadoHabitacion.Mantenimiento),
                FueraDeServicio = habitaciones.Count(h => h.Estado == EstadoHabitacion.FueraDeServicio),
                Disponibles = habitaciones.Count(h => h.Estado == EstadoHabitacion.Disponible &&
                                                       !ReservasSolapan(h.Id, hoy, hoy.AddDays(1), null)),
                EnCasa = reservas.Count(r => r.Estado == EstadoReserva.EnCurso),
                ReservasActivas = reservas.Count(r => r.Estado == EstadoReserva.Confirmada ||
                                                      r.Estado == EstadoReserva.EnCurso),
                Llegadas = reservas.Count(r => r.Estado == EstadoReserva.Confirmada &&
                                              r.FechaCheckinEsperado.Date == hoy),
                Salidas = reservas.Count(r => r.Estado == EstadoReserva.EnCurso &&
                                             r.FechaCheckoutEsperado.Date == hoy),
                IngresosHoy = transacciones.Where(t => t.Fecha.Date == hoy).Sum(t => t.Abono),
                SaldoPendiente = DatosHotel.Folios.Where(f => f.Estado == "Abierto").Sum(f => Math.Max(0, f.SaldoPendiente))
            };

            int operables = Math.Max(1, resumen.TotalHabitaciones - resumen.FueraDeServicio);
            resumen.Ocupacion = (double)resumen.Ocupadas / operables;

            resumen.Estados = new List<EstadoChip>
            {
                Chip("Disponibles", resumen.Disponibles, "#2E8B67"),
                Chip("Ocupadas", resumen.Ocupadas, "#2C4C63"),
                Chip("Limpieza", resumen.Limpieza, "#C9A54B"),
                Chip("Mantenimiento", resumen.Mantenimiento, "#C9793B"),
                Chip("Fuera de servicio", resumen.FueraDeServicio, "#8A94A0")
            };

            resumen.LlegadasHoy = reservas
                .Where(r => r.Estado == EstadoReserva.Confirmada && r.FechaCheckinEsperado.Date == hoy)
                .OrderBy(r => r.FechaCheckinEsperado)
                .Select(r => new LlegadaVista
                {
                    Huesped = NombreHuesped(r.HuespedId),
                    Habitacion = NumeroHabitacion(r.HabitacionId),
                    Detalle = r.Noches + (r.Noches == 1 ? " noche" : " noches") + " · " + r.Canal + " · " + Formato.Moneda(r.Total),
                    Hora = Formato.Hora(r.FechaCheckinEsperado)
                })
                .ToList();

            resumen.SalidasHoy = reservas
                .Where(r => r.Estado == EstadoReserva.EnCurso && r.FechaCheckoutEsperado.Date == hoy)
                .OrderBy(r => r.FechaCheckoutEsperado)
                .Select(r => new LlegadaVista
                {
                    Huesped = NombreHuesped(r.HuespedId),
                    Habitacion = NumeroHabitacion(r.HabitacionId),
                    Detalle = "Saldo " + Formato.Moneda(SaldoReserva(r.Id)),
                    Hora = Formato.Hora(r.FechaCheckoutEsperado)
                })
                .ToList();

            var recientes = transacciones.OrderByDescending(t => t.Fecha).Take(10).ToList();
            resumen.Movimientos = recientes.Select(t => new MovimientoVista
            {
                Hora = Formato.Hora(t.Fecha),
                Concepto = t.Concepto,
                Huesped = NombreHuespedPorFolio(t.FolioId),
                MetodoPago = t.MetodoPago,
                Monto = Formato.Moneda(t.Abono > 0 ? t.Abono : t.Cargo)
            }).ToList();

            return resumen;
        }

        private static EstadoChip Chip(string nombre, int cantidad, string color)
        {
            return new EstadoChip
            {
                Nombre = nombre,
                Cantidad = cantidad,
                Color = (Brush)new BrushConverter().ConvertFromString(color)
            };
        }

        // ---------------------------------------------------------------- Habitaciones

        public List<Habitacion> ListarHabitaciones()
        {
            return DatosHotel.Habitaciones.OrderBy(h => h.Piso).ThenBy(h => h.NumeroHabitacion).ToList();
        }

        public List<HabitacionVista> ListarDisponibilidad(DateTime inicio, DateTime fin, string capacidad, string calidad)
        {
            if (fin <= inicio)
                fin = inicio.AddDays(1);

            return DatosHotel.Habitaciones
                .OrderBy(h => h.Piso).ThenBy(h => h.NumeroHabitacion)
                .Where(h => string.IsNullOrEmpty(capacidad) || capacidad == "Todas" || h.CapacidadTipo == capacidad)
                .Where(h => string.IsNullOrEmpty(calidad) || calidad == "Todas" || h.Calidad == calidad)
                .Select(h =>
                {
                    bool noOperativa = h.Estado == EstadoHabitacion.Mantenimiento ||
                                       h.Estado == EstadoHabitacion.FueraDeServicio;

                    // El estado físico solo bloquea la venta cuando la estancia empieza hoy.
                    bool bloqueadaHoy = inicio.Date <= DateTime.Today &&
                                        (h.Estado == EstadoHabitacion.Ocupada || h.Estado == EstadoHabitacion.Limpieza);

                    bool libre = !noOperativa && !bloqueadaHoy && !ReservasSolapan(h.Id, inicio, fin, null);

                    string disponibilidad;
                    if (libre)
                        disponibilidad = "Disponible";
                    else if (noOperativa)
                        disponibilidad = "No operativa";
                    else if (bloqueadaHoy)
                        disponibilidad = h.Estado == EstadoHabitacion.Ocupada ? "Ocupada hoy" : "En limpieza";
                    else
                        disponibilidad = "Ocupada en el rango";

                    return new HabitacionVista
                    {
                        Habitacion = h,
                        Numero = h.NumeroHabitacion,
                        Piso = h.Piso.ToString(),
                        Tipo = h.CapacidadTipo,
                        Calidad = h.Calidad,
                        Capacidad = h.CapacidadPersonas + " pers.",
                        Tarifa = Formato.Moneda(h.TarifaBase),
                        Extras = h.DescripcionExtras,
                        Estado = h.Estado.ToString(),
                        Disponibilidad = disponibilidad,
                        EstaDisponible = libre
                    };
                })
                .ToList();
        }

        public List<Habitacion> HabitacionesLibres(DateTime inicio, DateTime fin)
        {
            if (fin <= inicio)
                fin = inicio.AddDays(1);

            bool empiezaHoy = inicio.Date <= DateTime.Today;

            return DatosHotel.Habitaciones
                .Where(h => h.Estado != EstadoHabitacion.Mantenimiento &&
                            h.Estado != EstadoHabitacion.FueraDeServicio &&
                            !(empiezaHoy && (h.Estado == EstadoHabitacion.Ocupada ||
                                             h.Estado == EstadoHabitacion.Limpieza)) &&
                            !ReservasSolapan(h.Id, inicio, fin, null))
                .OrderBy(h => h.Piso).ThenBy(h => h.NumeroHabitacion)
                .ToList();
        }

        public bool HabitacionDisponible(int habitacionId, DateTime inicio, DateTime fin, Guid? excluirReserva)
        {
            var hab = DatosHotel.Habitaciones.FirstOrDefault(h => h.Id == habitacionId);
            if (hab == null)
                return false;
            if (hab.Estado == EstadoHabitacion.Mantenimiento || hab.Estado == EstadoHabitacion.FueraDeServicio)
                return false;
            if (inicio.Date <= DateTime.Today &&
                (hab.Estado == EstadoHabitacion.Ocupada || hab.Estado == EstadoHabitacion.Limpieza))
                return false;
            return !ReservasSolapan(habitacionId, inicio, fin, excluirReserva);
        }

        private static bool ReservasSolapan(int habitacionId, DateTime inicio, DateTime fin, Guid? excluirReserva)
        {
            return DatosHotel.Reservas.Any(r =>
                r.HabitacionId == habitacionId &&
                r.Estado != EstadoReserva.Cancelada &&
                r.Estado != EstadoReserva.NoShow &&
                (!excluirReserva.HasValue || r.Id != excluirReserva.Value) &&
                r.FechaCheckinEsperado.Date < fin.Date &&
                r.FechaCheckoutEsperado.Date > inicio.Date);
        }

        // ---------------------------------------------------------------- Reservas

        public List<ReservaVista> ListarReservas(string texto, string estado, string canal)
        {
            IEnumerable<Reserva> consulta = DatosHotel.Reservas;

            if (!string.IsNullOrWhiteSpace(texto))
            {
                string buscado = texto.Trim().ToLowerInvariant();
                consulta = consulta.Where(r =>
                    r.CodigoReserva.ToLowerInvariant().Contains(buscado) ||
                    NombreHuesped(r.HuespedId).ToLowerInvariant().Contains(buscado) ||
                    NumeroHabitacion(r.HabitacionId).ToLowerInvariant().Contains(buscado));
            }

            if (!string.IsNullOrEmpty(estado) && estado != "Todas")
                consulta = consulta.Where(r => r.Estado.ToString() == estado);

            if (!string.IsNullOrEmpty(canal) && canal != "Todos")
                consulta = consulta.Where(r => r.Canal.ToString() == canal);

            return consulta
                .OrderBy(r => Prioridad(r.Estado))
                .ThenBy(r => r.FechaCheckinEsperado)
                .Select(AColumna)
                .ToList();
        }

        private static int Prioridad(EstadoReserva estado)
        {
            switch (estado)
            {
                case EstadoReserva.EnCurso: return 0;
                case EstadoReserva.Confirmada: return 1;
                case EstadoReserva.Finalizada: return 2;
                case EstadoReserva.Cancelada: return 3;
                default: return 4;
            }
        }

        private static ReservaVista AColumna(Reserva r)
        {
            return new ReservaVista
            {
                Reserva = r,
                Codigo = r.CodigoReserva,
                Huesped = NombreHuesped(r.HuespedId),
                Habitacion = NumeroHabitacion(r.HabitacionId),
                Canal = r.Canal.ToString(),
                Checkin = r.FechaCheckinEsperado.ToString("dd MMM", Cultura),
                Checkout = r.FechaCheckoutEsperado.ToString("dd MMM", Cultura),
                Noches = r.Noches,
                Tarifa = Formato.Moneda(r.TarifaAplicada),
                Total = Formato.Moneda(r.Total),
                Estado = EtiquetaEstado(r.Estado),
                EstadoCrudo = r.Estado.ToString(),
                Observaciones = r.Observaciones
            };
        }

        public static string EtiquetaEstado(EstadoReserva estado)
        {
            switch (estado)
            {
                case EstadoReserva.Confirmada: return "Confirmada";
                case EstadoReserva.EnCurso: return "En curso";
                case EstadoReserva.Finalizada: return "Finalizada";
                case EstadoReserva.Cancelada: return "Cancelada";
                default: return "No show";
            }
        }

        public List<Huesped> ListarHuespedes()
        {
            return DatosHotel.Huespedes.OrderBy(h => h.Apellidos).ToList();
        }

        public Reserva CrearReserva(Guid huespedId, int habitacionId, DateTime entrada, DateTime salida,
                                    CanalReserva canal, decimal tarifa, string observaciones)
        {
            if (salida <= entrada)
                throw new InvalidOperationException("La fecha de salida debe ser posterior a la de entrada.");
            if (tarifa <= 0)
                throw new InvalidOperationException("La tarifa aplicada debe ser mayor que cero.");
            if (!HabitacionDisponible(habitacionId, entrada, salida, null))
                throw new InvalidOperationException("La habitación no está disponible en el rango seleccionado.");

            var anio = entrada.Year;
            int siguiente = DatosHotel.Reservas.Count(r => r.CodigoReserva.StartsWith("CS-" + anio)) + 1;
            string codigo = "CS-" + anio + "-" + siguiente.ToString("0000");

            var reserva = new Reserva
            {
                Id = Guid.NewGuid(),
                CodigoReserva = codigo,
                HuespedId = huespedId,
                HabitacionId = habitacionId,
                Canal = canal,
                Estado = EstadoReserva.Confirmada,
                FechaCheckinEsperado = entrada.Date.AddHours(15),
                FechaCheckoutEsperado = salida.Date.AddHours(12),
                TarifaAplicada = tarifa,
                Observaciones = observaciones,
                CreadaEl = DateTime.Now
            };

            DatosHotel.Reservas.Add(reserva);
            return reserva;
        }

        public void CheckIn(Guid reservaId)
        {
            var reserva = DatosHotel.Reservas.First(r => r.Id == reservaId);
            if (reserva.Estado != EstadoReserva.Confirmada)
                throw new InvalidOperationException("Solo las reservas confirmadas pueden registrarse como llegada.");

            reserva.Estado = EstadoReserva.EnCurso;

            var habitacion = DatosHotel.Habitaciones.First(h => h.Id == reserva.HabitacionId);
            habitacion.Estado = EstadoHabitacion.Ocupada;

            if (DatosHotel.Folios.All(f => f.ReservaId != reserva.Id))
            {
                var folio = new Folio
                {
                    Id = Guid.NewGuid(),
                    ReservaId = reserva.Id,
                    HuespedId = reserva.HuespedId,
                    TipoFolio = "Individual",
                    Estado = "Abierto",
                    SaldoPendiente = reserva.Total
                };
                DatosHotel.Folios.Add(folio);

                DatosHotel.Transacciones.Add(new Transaccion
                {
                    Id = DatosHotel.Transacciones.Count + 1,
                    FolioId = folio.Id,
                    Fecha = DateTime.Now,
                    Concepto = "Hospedaje",
                    Cargo = Math.Round(reserva.Total, 2),
                    Abono = 0m,
                    MetodoPago = "Pendiente",
                    Usuario = "Recepción"
                });
                DatosHotel.RecalcularSaldos();
            }
        }

        public void CheckOut(Guid reservaId)
        {
            var reserva = DatosHotel.Reservas.First(r => r.Id == reservaId);
            if (reserva.Estado != EstadoReserva.EnCurso)
                throw new InvalidOperationException("Solo las estancias en curso pueden registrarse como salida.");

            var folio = DatosHotel.Folios.FirstOrDefault(f => f.ReservaId == reserva.Id);
            if (folio != null && folio.SaldoPendiente > 0)
            {
                DatosHotel.Transacciones.Add(new Transaccion
                {
                    Id = DatosHotel.Transacciones.Count + 1,
                    FolioId = folio.Id,
                    Fecha = DateTime.Now,
                    Concepto = "Hospedaje",
                    Cargo = 0m,
                    Abono = Math.Round(folio.SaldoPendiente, 2),
                    MetodoPago = "Efectivo",
                    Usuario = "Recepción"
                });
                DatosHotel.RecalcularSaldos();
            }

            if (folio != null)
                folio.Estado = "Cerrado";

            reserva.Estado = EstadoReserva.Finalizada;

            var habitacion = DatosHotel.Habitaciones.First(h => h.Id == reserva.HabitacionId);
            habitacion.Estado = EstadoHabitacion.Limpieza;
        }

        public void Cancelar(Guid reservaId)
        {
            var reserva = DatosHotel.Reservas.First(r => r.Id == reservaId);
            if (reserva.Estado == EstadoReserva.Finalizada || reserva.Estado == EstadoReserva.Cancelada)
                throw new InvalidOperationException("La reserva ya no puede cancelarse.");

            bool estabaEnCasa = reserva.Estado == EstadoReserva.EnCurso;
            reserva.Estado = EstadoReserva.Cancelada;

            var folio = DatosHotel.Folios.FirstOrDefault(f => f.ReservaId == reserva.Id);
            if (folio != null)
                folio.Estado = "Cerrado";

            if (estabaEnCasa)
            {
                var habitacion = DatosHotel.Habitaciones.First(h => h.Id == reserva.HabitacionId);
                if (habitacion.Estado == EstadoHabitacion.Ocupada)
                    habitacion.Estado = EstadoHabitacion.Disponible;
            }
        }

        // ---------------------------------------------------------------- Ingresos

        public ResumenIngresos ObtenerIngresos(PeriodoReporte periodo, DateTime referencia)
        {
            DateTime inicio;
            DateTime fin;
            string etiqueta;
            string titulo;
            string columna;

            CalcularRango(periodo, referencia, out inicio, out fin, out etiqueta, out titulo, out columna);

            var movimientos = DatosHotel.Transacciones
                .Where(t => t.Fecha >= inicio && t.Fecha < fin)
                .ToList();

            var resumen = new ResumenIngresos
            {
                Periodo = periodo,
                Inicio = inicio,
                Fin = fin.AddDays(-1),
                EtiquetaPeriodo = etiqueta,
                TituloDetalle = titulo,
                ColumnaDetalle = columna,
                TotalCargos = movimientos.Sum(t => t.Cargo),
                TotalAbonos = movimientos.Sum(t => t.Abono),
                Transacciones = movimientos.Count,
                Unidades = periodo == PeriodoReporte.Diario ? 1 : periodo == PeriodoReporte.Semanal ? 7 : 3,
                Efectivo = movimientos.Where(t => t.MetodoPago == "Efectivo").Sum(t => t.Abono),
                Tarjeta = movimientos.Where(t => t.MetodoPago == "Tarjeta").Sum(t => t.Abono),
                Transferencia = movimientos.Where(t => t.MetodoPago == "Transferencia").Sum(t => t.Abono)
            };

            var cuentas = movimientos
                .GroupBy(t => t.Concepto)
                .Select(g => new LineaIngreso
                {
                    Cuenta = g.Key,
                    Cargos = g.Sum(t => t.Cargo),
                    Abonos = g.Sum(t => t.Abono)
                })
                .OrderByDescending(l => l.Abonos)
                .ToList();

            decimal maxAbono = cuentas.Count == 0 ? 1m : Math.Max(1m, cuentas.Max(l => l.Abonos));
            foreach (var linea in cuentas)
            {
                linea.Participacion = resumen.TotalAbonos == 0 ? 0 : (double)(linea.Abonos / resumen.TotalAbonos);
                linea.AnchoBarra = 210.0 * (double)(linea.Abonos / maxAbono);
            }

            resumen.Cuentas = cuentas;
            resumen.Detalle = ConstruirDetalle(periodo, inicio, fin, movimientos);

            return resumen;
        }

        private static List<DetallePeriodo> ConstruirDetalle(PeriodoReporte periodo, DateTime inicio, DateTime fin,
                                                             List<Transaccion> movimientos)
        {
            var detalle = new List<DetallePeriodo>();

            if (periodo == PeriodoReporte.Diario)
            {
                foreach (var t in movimientos.OrderByDescending(t => t.Fecha).Take(16))
                {
                    detalle.Add(new DetallePeriodo
                    {
                        Etiqueta = Formato.Hora(t.Fecha) + " · " + t.Concepto,
                        Cargos = t.Cargo,
                        Abonos = t.Abono
                    });
                }
                return detalle;
            }

            if (periodo == PeriodoReporte.Semanal)
            {
                for (DateTime dia = inicio; dia < fin; dia = dia.AddDays(1))
                {
                    var delDia = movimientos.Where(t => t.Fecha.Date == dia.Date).ToList();
                    string texto = dia.ToString("ddd d MMM", Cultura);
                    detalle.Add(new DetallePeriodo
                    {
                        Etiqueta = char.ToUpper(texto[0], Cultura) + texto.Substring(1),
                        Cargos = delDia.Sum(t => t.Cargo),
                        Abonos = delDia.Sum(t => t.Abono)
                    });
                }
                return detalle;
            }

            for (DateTime mes = inicio; mes < fin; mes = mes.AddMonths(1))
            {
                DateTime siguiente = mes.AddMonths(1);
                var delMes = movimientos.Where(t => t.Fecha >= mes && t.Fecha < siguiente).ToList();
                string texto = mes.ToString("MMMM yyyy", Cultura);
                detalle.Add(new DetallePeriodo
                {
                    Etiqueta = char.ToUpper(texto[0], Cultura) + texto.Substring(1),
                    Cargos = delMes.Sum(t => t.Cargo),
                    Abonos = delMes.Sum(t => t.Abono)
                });
            }
            return detalle;
        }

        private static void CalcularRango(PeriodoReporte periodo, DateTime referencia,
                                          out DateTime inicio, out DateTime fin,
                                          out string etiqueta, out string titulo, out string columna)
        {
            switch (periodo)
            {
                case PeriodoReporte.Diario:
                    inicio = referencia.Date;
                    fin = inicio.AddDays(1);
                    etiqueta = Formato.DiaDeSemana(referencia);
                    titulo = "Movimientos del día";
                    columna = "Hora · cuenta";
                    break;

                case PeriodoReporte.Semanal:
                    int desplazamiento = ((int)referencia.DayOfWeek + 6) % 7;
                    inicio = referencia.Date.AddDays(-desplazamiento);
                    fin = inicio.AddDays(7);
                    etiqueta = "Semana del " + inicio.ToString("d MMM", Cultura) + " al " +
                               inicio.AddDays(6).ToString("d MMM yyyy", Cultura);
                    titulo = "Cierres diarios";
                    columna = "Día";
                    break;

                default:
                    int trimestre = (referencia.Month - 1) / 3;
                    inicio = new DateTime(referencia.Year, trimestre * 3 + 1, 1);
                    fin = inicio.AddMonths(3);
                    etiqueta = "T" + (trimestre + 1) + " " + referencia.Year + " · " +
                               inicio.ToString("MMM", Cultura) + " – " + fin.AddDays(-1).ToString("MMM yyyy", Cultura);
                    titulo = "Resumen mensual";
                    columna = "Mes";
                    break;
            }
        }

        public decimal SaldoReserva(Guid reservaId)
        {
            var folio = DatosHotel.Folios.FirstOrDefault(f => f.ReservaId == reservaId);
            return folio == null ? 0m : folio.SaldoPendiente;
        }

        // ---------------------------------------------------------------- Utilidades

        public static string NombreHuesped(Guid huespedId)
        {
            var huesped = DatosHotel.Huespedes.FirstOrDefault(h => h.Id == huespedId);
            return huesped == null ? "Huésped" : huesped.NombreCompleto;
        }

        public static string NombreHuespedPorFolio(Guid folioId)
        {
            var folio = DatosHotel.Folios.FirstOrDefault(f => f.Id == folioId);
            return folio == null ? "—" : NombreHuesped(folio.HuespedId);
        }

        public static string NumeroHabitacion(int habitacionId)
        {
            var habitacion = DatosHotel.Habitaciones.FirstOrDefault(h => h.Id == habitacionId);
            return habitacion == null ? "—" : habitacion.NumeroHabitacion;
        }

        public Huesped BuscarHuesped(Guid id)
        {
            return DatosHotel.Huespedes.FirstOrDefault(h => h.Id == id);
        }

        public Habitacion BuscarHabitacion(int id)
        {
            return DatosHotel.Habitaciones.FirstOrDefault(h => h.Id == id);
        }

        public string NombreCuenta(string concepto)
        {
            return Array.IndexOf(DatosHotel.CuentasIngreso, concepto) >= 0 ? concepto : "Otros ingresos";
        }
    }
}

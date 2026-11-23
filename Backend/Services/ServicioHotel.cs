using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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
                                                       !ReservasSolapan(h.Id, hoy, hoy.AddDays(1), null) && !BloqueosSolapan(h.Id, hoy, hoy.AddDays(1))),
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

                    bool libre = !noOperativa && !bloqueadaHoy && !ReservasSolapan(h.Id, inicio, fin, null) && !BloqueosSolapan(h.Id, inicio, fin);

                    string disponibilidad;
                    if (libre)
                        disponibilidad = "Disponible";
                    else if (noOperativa)
                        disponibilidad = "No operativa";
                    else if (bloqueadaHoy)
                        disponibilidad = h.Estado == EstadoHabitacion.Ocupada ? "Ocupada hoy" : "En limpieza";
                    else if (BloqueosSolapan(h.Id, inicio, fin))
                        disponibilidad = "Bloqueada";
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
                            !BloqueosSolapan(h.Id, inicio, fin) &&
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
            if (BloqueosSolapan(habitacionId, inicio, fin)) return false;
            return !ReservasSolapan(habitacionId, inicio, fin, excluirReserva);
        }

        private static bool BloqueosSolapan(int habitacionId, DateTime inicio, DateTime fin)
        {
            return DatosHotel.BloqueosHabitacion.Any(b => b.HabitacionId == habitacionId && b.Inicio.Date < fin.Date && b.Fin.Date > inicio.Date);
        }

        private static void RegistrarEstado(Habitacion habitacion, EstadoHabitacion estado, string motivo)
        {
            if (habitacion.Estado == estado) return;
            var vigente = DatosHotel.HistorialHabitaciones.LastOrDefault(h => h.HabitacionId == habitacion.Id && !h.Fin.HasValue);
            if (vigente != null) vigente.Fin = DateTime.Now;
            habitacion.Estado = estado;
            DatosHotel.HistorialHabitaciones.Add(new HistorialHabitacionDemo { HabitacionId = habitacion.Id, Estado = estado.ToString(), Inicio = DateTime.Now, Motivo = motivo });
        }

        public void BloquearHabitacion(int habitacionId, DateTime inicio, DateTime fin, string motivo)
        {
            if (fin.Date <= inicio.Date || string.IsNullOrWhiteSpace(motivo)) throw new InvalidOperationException("Indica un rango de fechas y motivo válidos.");
            var h = DatosHotel.Habitaciones.FirstOrDefault(x => x.Id == habitacionId);
            if (h == null) throw new InvalidOperationException("Selecciona una habitación.");
            if (ReservasSolapan(habitacionId, inicio, fin, null)) throw new InvalidOperationException("Hay reservas que coinciden con ese bloqueo.");
            DatosHotel.BloqueosHabitacion.Add(new BloqueoHabitacionDemo { Id = DatosHotel.BloqueosHabitacion.Count + 1, HabitacionId = habitacionId, Inicio = inicio.Date, Fin = fin.Date, Motivo = motivo.Trim() });
        }

        public List<BloqueoHabitacionDemo> ListarBloqueos()
        {
            return DatosHotel.BloqueosHabitacion.OrderByDescending(b => b.Inicio).ToList();
        }

        public List<HistorialHabitacionDemo> ListarHistorialHabitaciones(int id)
        {
            return DatosHotel.HistorialHabitaciones.Where(h => h.HabitacionId == id).OrderByDescending(h => h.Inicio).ToList();
        }

        public List<TipoHabitacionDemo> ListarTiposHabitacion() { return DatosHotel.TiposHabitacion.OrderBy(t => t.Nombre).ToList(); }
        public List<int> ListarPisos() { return DatosHotel.Pisos.OrderBy(p => p).ToList(); }
        public int AgregarPiso() { int piso = DatosHotel.Pisos.Count == 0 ? 1 : DatosHotel.Pisos.Max() + 1; DatosHotel.Pisos.Add(piso); return piso; }

        public void RegistrarTipoHabitacion(string nombre, int capacidad)
        {
            if (string.IsNullOrWhiteSpace(nombre) || capacidad < 1) throw new InvalidOperationException("Nombre y capacidad positiva son obligatorios.");
            if (DatosHotel.TiposHabitacion.Any(t => t.Nombre.Equals(nombre.Trim(), StringComparison.CurrentCultureIgnoreCase))) throw new InvalidOperationException("Ya existe ese tipo de habitación.");
            DatosHotel.TiposHabitacion.Add(new TipoHabitacionDemo { Nombre = nombre.Trim(), Capacidad = capacidad });
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

        public Huesped CrearHuesped(string nombres, string apellidos, string documento, string correo,
                                    string telefono, DateTime? nacimiento, bool autorizaDatos)
        {
            if (string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(apellidos))
                throw new InvalidOperationException("El nombre y los apellidos son obligatorios.");
            if (!string.IsNullOrWhiteSpace(documento) && DatosHotel.Huespedes.Any(h =>
                string.Equals(h.NumeroDocumento, documento.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Ya existe un huésped con ese documento.");

            var huesped = new Huesped { Id = Guid.NewGuid(), Nombres = nombres.Trim(), Apellidos = apellidos.Trim(),
                NumeroDocumento = documento == null ? null : documento.Trim(), Correo = correo == null ? null : correo.Trim(),
                Telefono = telefono == null ? null : telefono.Trim(), FechaNacimiento = nacimiento };
            DatosHotel.Huespedes.Add(huesped);
            DatosHotel.Consentimientos.Add(new ConsentimientoHuesped { Id = Guid.NewGuid(), HuespedId = huesped.Id,
                Tipo = "Tratamiento de datos", Autorizado = autorizaDatos, Fecha = DateTime.Now });
            return huesped;
        }

        public void ActualizarHuesped(Guid id, string nombres, string apellidos, string documento, string correo, string telefono, DateTime? nacimiento)
        {
            var h = DatosHotel.Huespedes.FirstOrDefault(x => x.Id == id);
            if (h == null) throw new InvalidOperationException("El huésped seleccionado ya no existe.");
            if (string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(apellidos))
                throw new InvalidOperationException("El nombre y los apellidos son obligatorios.");
            if (!string.IsNullOrWhiteSpace(documento) && DatosHotel.Huespedes.Any(x => x.Id != id &&
                string.Equals(x.NumeroDocumento, documento.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Ya existe otro huésped con ese documento.");
            h.Nombres = nombres.Trim(); h.Apellidos = apellidos.Trim();
            h.NumeroDocumento = documento == null ? null : documento.Trim();
            h.Correo = correo == null ? null : correo.Trim(); h.Telefono = telefono == null ? null : telefono.Trim(); h.FechaNacimiento = nacimiento;
        }

        public List<ConsentimientoHuesped> ListarConsentimientos(Guid huespedId)
        {
            return DatosHotel.Consentimientos.Where(c => c.HuespedId == huespedId).OrderByDescending(c => c.Fecha).ToList();
        }

        public void RegistrarPago(Guid folioId, decimal monto, string metodo)
        {
            var folio = DatosHotel.Folios.FirstOrDefault(f => f.Id == folioId);
            if (folio == null || folio.Estado != "Abierto") throw new InvalidOperationException("El folio no está abierto.");
            if (monto <= 0 || monto > folio.SaldoPendiente) throw new InvalidOperationException("El pago debe ser positivo y no superar el saldo del folio.");
            if (!DatosHotel.MetodosPago.Contains(metodo)) throw new InvalidOperationException("Selecciona un método de pago válido.");
            DatosHotel.Transacciones.Add(new Transaccion { Id = DatosHotel.Transacciones.Count + 1, FolioId = folio.Id,
                Fecha = DateTime.Now, Concepto = "Pago", Cargo = 0, Abono = monto, MetodoPago = metodo, Usuario = "Demo" });
            DatosHotel.RecalcularSaldos();
            if (folio.SaldoPendiente <= 0) folio.Estado = "Cerrado";
        }

        public void RegistrarCargo(Guid folioId, string concepto, decimal monto, int cantidad)
        {
            var folio = DatosHotel.Folios.FirstOrDefault(f => f.Id == folioId && f.Estado == "Abierto");
            if (folio == null) throw new InvalidOperationException("Selecciona un folio abierto.");
            if (string.IsNullOrWhiteSpace(concepto) || monto <= 0 || cantidad < 1) throw new InvalidOperationException("Indica concepto, cantidad y un cargo positivo.");
            DatosHotel.Transacciones.Add(new Transaccion { Id = DatosHotel.Transacciones.Count + 1, FolioId = folio.Id,
                Fecha = DateTime.Now, Concepto = concepto.Trim(), Cargo = monto, Abono = 0, Cantidad = cantidad, MetodoPago = "Pendiente", Usuario = "Demo" });
            DatosHotel.RecalcularSaldos();
        }

        public List<TareaOperativa> ListarTareas()
        {
            return DatosHotel.Tareas.OrderBy(t => t.Estado).ThenBy(t => t.Habitacion).ToList();
        }

        public void CrearTarea(string habitacion, string tipo, string descripcion)
        {
            if (string.IsNullOrWhiteSpace(habitacion) || string.IsNullOrWhiteSpace(tipo) || string.IsNullOrWhiteSpace(descripcion))
                throw new InvalidOperationException("Selecciona habitación, tipo y descripción.");
            DatosHotel.Tareas.Add(new TareaOperativa { Id = DatosHotel.Tareas.Count == 0 ? 1 : DatosHotel.Tareas.Max(t => t.Id) + 1,
                Habitacion = habitacion, Tipo = tipo, Descripcion = descripcion.Trim(), Estado = "Pendiente", Fecha = DateTime.Now });
        }

        public void AvanzarTarea(int id)
        {
            var t = DatosHotel.Tareas.FirstOrDefault(x => x.Id == id);
            if (t == null) throw new InvalidOperationException("Selecciona una tarea.");
            t.Estado = t.Estado == "Pendiente" ? "En curso" : t.Estado == "En curso" ? "Completada" : "Pendiente";
            t.Fecha = DateTime.Now;
            var habitacion = DatosHotel.Habitaciones.FirstOrDefault(h => h.NumeroHabitacion == t.Habitacion);
            if (habitacion != null && t.Tipo == "Limpieza")
                RegistrarEstado(habitacion, t.Estado == "Completada" ? EstadoHabitacion.Disponible : EstadoHabitacion.Limpieza, "Tarea de " + t.Tipo);
        }

        public Folio CrearFolio(Guid reservaId)
        {
            var reserva = DatosHotel.Reservas.FirstOrDefault(r => r.Id == reservaId);
            if (reserva == null) throw new InvalidOperationException("Selecciona una reserva existente.");
            var folio = new Folio { Id = Guid.NewGuid(), ReservaId = reserva.Id, HuespedId = reserva.HuespedId,
                TipoFolio = "Adicional", Estado = "Abierto", SaldoPendiente = 0m };
            DatosHotel.Folios.Add(folio); return folio;
        }

        public FacturaDemo EmitirFactura(Guid folioId)
        {
            var folio = DatosHotel.Folios.FirstOrDefault(f => f.Id == folioId);
            if (folio == null) throw new InvalidOperationException("Selecciona un folio.");
            string numero = "F-" + (DatosHotel.Facturas.Count + 1).ToString("00000");
            decimal subtotal = DatosHotel.Transacciones.Where(t => t.FolioId == folioId).Sum(t => t.Cargo);
            var factura = new FacturaDemo { Numero = numero, FolioId = folioId, Fecha = DateTime.Now,
                Subtotal = subtotal, Impuesto = 0m, Estado = "Emitida" };
            DatosHotel.Facturas.Add(factura); return factura;
        }

        public NotaCreditoDemo CrearNotaCredito(string facturaNumero, string motivo, decimal monto)
        {
            var factura = DatosHotel.Facturas.FirstOrDefault(f => f.Numero == facturaNumero);
            if (factura == null || string.IsNullOrWhiteSpace(motivo) || monto <= 0 || monto > factura.Total)
                throw new InvalidOperationException("Selecciona una factura y un monto positivo que no supere el total.");
            var nota = new NotaCreditoDemo { Numero = "NC-" + (DatosHotel.NotasCredito.Count + 1).ToString("00000"),
                Factura = facturaNumero, Motivo = motivo.Trim(), Monto = monto, Fecha = DateTime.Now };
            DatosHotel.NotasCredito.Add(nota); return nota;
        }

        public void TransferirCargo(int transaccionId, Guid folioDestinoId)
        {
            var t = DatosHotel.Transacciones.FirstOrDefault(x => x.Id == transaccionId && x.Cargo > 0);
            var destino = DatosHotel.Folios.FirstOrDefault(f => f.Id == folioDestinoId && f.Estado == "Abierto");
            if (t == null || destino == null || t.FolioId == destino.Id) throw new InvalidOperationException("Selecciona un cargo y un folio abierto de destino distinto.");
            t.FolioId = destino.Id; DatosHotel.RecalcularSaldos();
        }

        public LlaveDemo EmitirLlave(Guid reservaId)
        {
            if (!DatosHotel.Reservas.Any(r => r.Id == reservaId && r.Estado == EstadoReserva.EnCurso))
                throw new InvalidOperationException("Solo se emiten llaves para una estancia activa.");
            var llave = new LlaveDemo { Id = DatosHotel.Llaves.Count + 1, ReservaId = reservaId,
                Codigo = "K-" + (DatosHotel.Llaves.Count + 1).ToString("0000"), Emitida = DateTime.Now, Estado = "Activa" };
            DatosHotel.Llaves.Add(llave); return llave;
        }

        public void DevolverLlave(int id)
        {
            var llave = DatosHotel.Llaves.FirstOrDefault(k => k.Id == id && k.Estado == "Activa");
            if (llave == null) throw new InvalidOperationException("Selecciona una llave activa.");
            llave.Estado = "Devuelta"; llave.Devuelta = DateTime.Now;
        }

        public void CambiarHabitacionReserva(Guid reservaId, int habitacionId)
        {
            var reserva = DatosHotel.Reservas.FirstOrDefault(r => r.Id == reservaId && r.Estado == EstadoReserva.EnCurso);
            if (reserva == null) throw new InvalidOperationException("Selecciona una estancia activa.");
            if (!HabitacionDisponible(habitacionId, DateTime.Today, reserva.FechaCheckoutEsperado, reservaId))
                throw new InvalidOperationException("La habitación destino no está disponible.");
            var anterior = DatosHotel.Habitaciones.First(h => h.Id == reserva.HabitacionId);
            RegistrarEstado(anterior, EstadoHabitacion.Limpieza, "Cambio de habitación");
            reserva.HabitacionId = habitacionId;
            RegistrarEstado(DatosHotel.Habitaciones.First(h => h.Id == habitacionId), EstadoHabitacion.Ocupada, "Cambio de habitación");
        }

        public ProductoDemo CrearProducto(string nombre, decimal precio, int stock)
        {
            if (string.IsNullOrWhiteSpace(nombre) || precio < 0 || stock < 0) throw new InvalidOperationException("Nombre, precio y stock deben ser válidos.");
            if (DatosHotel.Productos.Any(item => string.Equals(item.Nombre, nombre.Trim(), StringComparison.CurrentCultureIgnoreCase)))
                throw new InvalidOperationException("Ya existe un producto con ese nombre.");
            var p = new ProductoDemo { Id = DatosHotel.Productos.Count == 0 ? 1 : DatosHotel.Productos.Max(x => x.Id) + 1,
                Nombre = nombre.Trim(), Precio = precio, Stock = stock, Activo = true };
            DatosHotel.Productos.Add(p); return p;
        }

        public ConsumoDemo RegistrarConsumo(Guid reservaId, int productoId, int cantidad)
        {
            var reserva = DatosHotel.Reservas.FirstOrDefault(r => r.Id == reservaId && r.Estado == EstadoReserva.EnCurso);
            var producto = DatosHotel.Productos.FirstOrDefault(p => p.Id == productoId && p.Activo);
            if (reserva == null || producto == null) throw new InvalidOperationException("Selecciona una estancia activa y un producto disponible.");
            if (cantidad < 1 || cantidad > producto.Stock) throw new InvalidOperationException("La cantidad debe estar entre 1 y el stock disponible.");
            var folio = DatosHotel.Folios.FirstOrDefault(f => f.ReservaId == reservaId && f.Estado == "Abierto");
            if (folio == null) throw new InvalidOperationException("La estancia no tiene un folio abierto.");
            var consumo = new ConsumoDemo { Id = DatosHotel.Consumos.Count + 1, ReservaId = reservaId, Producto = producto.Nombre,
                Cantidad = cantidad, Precio = producto.Precio, Fecha = DateTime.Now };
            DatosHotel.Consumos.Add(consumo); producto.Stock -= cantidad;
            DatosHotel.Transacciones.Add(new Transaccion { Id = DatosHotel.Transacciones.Count + 1, FolioId = folio.Id,
                Fecha = DateTime.Now, Concepto = producto.Nombre, Cargo = consumo.Total, Cantidad = cantidad, MetodoPago = "Pendiente", Usuario = "Demo" });
            DatosHotel.RecalcularSaldos(); return consumo;
        }

        public ConvenioDemo CrearConvenio(string nombre, string empresa, decimal tarifa, decimal limite, DateTime vencimiento)
        {
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(empresa) || tarifa <= 0 || limite < 0)
                throw new InvalidOperationException("Completa nombre, empresa y valores positivos.");
            var c = new ConvenioDemo { Id = DatosHotel.Convenios.Count + 1, Nombre = nombre.Trim(), Empresa = empresa.Trim(),
                TarifaPreferencial = tarifa, LimiteCredito = limite, Vencimiento = vencimiento, Activo = true };
            DatosHotel.Convenios.Add(c); return c;
        }

        public void ActualizarConvenio(int id, decimal tarifa, decimal limite, DateTime vencimiento, bool activo)
        {
            var c = DatosHotel.Convenios.FirstOrDefault(x => x.Id == id);
            if (c == null || tarifa <= 0 || limite < 0) throw new InvalidOperationException("Convenio o valores no válidos.");
            c.TarifaPreferencial = tarifa; c.LimiteCredito = limite; c.Vencimiento = vencimiento; c.Activo = activo;
        }

        public void AsociarReservaConvenio(int convenioId, Guid reservaId)
        {
            var convenio = DatosHotel.Convenios.FirstOrDefault(c => c.Id == convenioId && c.Activo && c.Vencimiento.Date >= DateTime.Today);
            var reserva = DatosHotel.Reservas.FirstOrDefault(r => r.Id == reservaId);
            if (convenio == null || reserva == null) throw new InvalidOperationException("Selecciona un convenio vigente y una reserva.");
            reserva.ConvenioId = convenioId; reserva.Canal = CanalReserva.Convenio; reserva.TarifaAplicada = convenio.TarifaPreferencial;
        }

        public void RegistrarTemporada(string nombre, DateTime inicio, DateTime fin)
        {
            if (string.IsNullOrWhiteSpace(nombre) || fin < inicio) throw new InvalidOperationException("Indica nombre y un rango de fechas válido.");
            DatosHotel.Temporadas.Add(new TemporadaDemo { Nombre = nombre.Trim(), Inicio = inicio.Date, Fin = fin.Date });
        }

        public void RegistrarPaquete(string nombre, string descripcion, decimal precio)
        {
            if (string.IsNullOrWhiteSpace(nombre) || precio < 0) throw new InvalidOperationException("Indica nombre y precio válido.");
            DatosHotel.Paquetes.Add(new PaqueteDemo { Nombre = nombre.Trim(), Descripcion = descripcion ?? "", Precio = precio });
        }

        public void RegistrarPromocion(string nombre, decimal descuento, DateTime inicio, DateTime fin, bool activa)
        {
            if (string.IsNullOrWhiteSpace(nombre) || descuento < 0 || descuento > 100 || fin < inicio)
                throw new InvalidOperationException("Indica una promoción y fechas válidas, con descuento entre 0 y 100.");
            DatosHotel.Promociones.Add(new PromocionDemo { Nombre = nombre.Trim(), Descuento = descuento, Inicio = inicio.Date, Fin = fin.Date, Activa = activa });
        }

        public void ActualizarTarifaBase(string tipo, decimal tarifa)
        {
            if (string.IsNullOrWhiteSpace(tipo) || tarifa <= 0) throw new InvalidOperationException("Selecciona un tipo y una tarifa positiva.");
            foreach (var h in DatosHotel.Habitaciones.Where(x => x.CapacidadTipo == tipo)) h.TarifaBase = tarifa;
        }

        public void ActualizarHotel(string nombre, int categoria, string direccion, string telefono, string correo, string moneda)
        {
            if (string.IsNullOrWhiteSpace(nombre) || (categoria != 3 && categoria != 4 && categoria != 5) || string.IsNullOrWhiteSpace(moneda))
                throw new InvalidOperationException("Nombre, categoría (3, 4 o 5) y moneda son obligatorios.");
            RegistrarAuditoria("Datos del hotel", DatosHotel.NombreHotel + " · " + DatosHotel.Categoria, nombre.Trim() + " · " + categoria);
            DatosHotel.NombreHotel = nombre.Trim(); DatosHotel.Categoria = categoria; DatosHotel.Direccion = direccion ?? "";
            DatosHotel.TelefonoHotel = telefono ?? ""; DatosHotel.EmailHotel = correo ?? ""; DatosHotel.Moneda = moneda.Trim();
        }

        public void GuardarConfiguracion(string clave, string valor)
        {
            if (string.IsNullOrWhiteSpace(clave)) throw new InvalidOperationException("La clave es obligatoria.");
            var c = DatosHotel.Configuraciones.FirstOrDefault(x => x.Clave.Equals(clave.Trim(), StringComparison.OrdinalIgnoreCase));
            string anterior = c == null ? "" : c.Valor;
            if (c == null) { c = new ConfiguracionDemo { Clave = clave.Trim() }; DatosHotel.Configuraciones.Add(c); }
            c.Valor = valor ?? ""; c.Fecha = DateTime.Now; RegistrarAuditoria("Configuración " + c.Clave, anterior, c.Valor);
        }

        public void GuardarPolitica(string nombre, string valor, string descripcion)
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new InvalidOperationException("El nombre de la política es obligatorio.");
            var p = DatosHotel.Politicas.FirstOrDefault(x => x.Nombre.Equals(nombre.Trim(), StringComparison.OrdinalIgnoreCase));
            string anterior = p == null ? "" : p.Valor;
            if (p == null) { p = new PoliticaDemo { Nombre = nombre.Trim() }; DatosHotel.Politicas.Add(p); }
            p.Valor = valor ?? ""; p.Descripcion = descripcion ?? ""; RegistrarAuditoria("Política " + p.Nombre, anterior, p.Valor);
        }

        public void GuardarImpuesto(string nombre, decimal porcentaje, bool activo)
        {
            if (string.IsNullOrWhiteSpace(nombre) || porcentaje < 0 || porcentaje > 100) throw new InvalidOperationException("Nombre y porcentaje entre 0 y 100 son obligatorios.");
            var i = DatosHotel.Impuestos.FirstOrDefault(x => x.Nombre.Equals(nombre.Trim(), StringComparison.OrdinalIgnoreCase));
            string anterior = i == null ? "" : i.Porcentaje + "% · " + i.Activo;
            if (i == null) { i = new ImpuestoDemo { Nombre = nombre.Trim() }; DatosHotel.Impuestos.Add(i); }
            i.Porcentaje = porcentaje; i.Activo = activo; RegistrarAuditoria("Impuesto " + i.Nombre, anterior, porcentaje + "% · " + activo);
        }

        public void EnviarNotificacion(string mensaje, string rol)
        {
            if (string.IsNullOrWhiteSpace(mensaje) || string.IsNullOrWhiteSpace(rol)) throw new InvalidOperationException("Escribe un mensaje y selecciona el rol destinatario.");
            DatosHotel.Notificaciones.Insert(0, new NotificacionDemo { Titulo = mensaje.Trim().Length > 48 ? mensaje.Trim().Substring(0, 48) + "…" : mensaje.Trim(),
                Mensaje = mensaje.Trim(), Rol = rol, Fecha = DateTime.Now, Estado = "Enviada (demo)" });
        }

        public void GuardarReporteProgramado(string nombre, string tipo, string formato, string frecuencia, string correo, bool activo)
        {
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(tipo) || string.IsNullOrWhiteSpace(formato) || string.IsNullOrWhiteSpace(frecuencia))
                throw new InvalidOperationException("Completa nombre, tipo, formato y frecuencia.");
            if (!string.Equals(formato, "CSV", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("La exportación disponible en esta demo es CSV.");
            DatosHotel.ReportesProgramados.Add(new ReporteProgramadoDemo { Nombre = nombre.Trim(), Tipo = tipo, Formato = formato, Frecuencia = frecuencia, Correo = correo ?? "", Activo = activo });
        }

        public void EjecutarReporte(string nombre)
        {
            var reporte = DatosHotel.ReportesProgramados.FirstOrDefault(r => r.Nombre == nombre && r.Activo);
            if (reporte == null) throw new InvalidOperationException("Selecciona un reporte activo.");
            string carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reportes");
            Directory.CreateDirectory(carpeta);
            string archivo = Path.Combine(carpeta, "reporte-" + reporte.Nombre.Replace(" ", "-") + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv");
            var lineas = new List<string>();
            if (reporte.Tipo == "Reservas")
            {
                lineas.Add("Codigo,Huesped,Habitacion,Estado,Entrada,Salida,Total");
                lineas.AddRange(DatosHotel.Reservas.Select(r => Csv(r.CodigoReserva) + "," + Csv(NombreHuesped(r.HuespedId)) + "," + Csv(NumeroHabitacion(r.HabitacionId)) + "," + Csv(r.Estado.ToString()) + "," + Csv(r.FechaCheckinEsperado.ToString("yyyy-MM-dd")) + "," + Csv(r.FechaCheckoutEsperado.ToString("yyyy-MM-dd")) + "," + r.Total.ToString(CultureInfo.InvariantCulture)));
            }
            else if (reporte.Tipo == "Ingresos")
            {
                lineas.Add("Fecha,Concepto,Cargo,Abono,MetodoPago");
                lineas.AddRange(DatosHotel.Transacciones.Select(t => Csv(t.Fecha.ToString("s")) + "," + Csv(t.Concepto) + "," + t.Cargo.ToString(CultureInfo.InvariantCulture) + "," + t.Abono.ToString(CultureInfo.InvariantCulture) + "," + Csv(t.MetodoPago)));
            }
            else if (reporte.Tipo == "Inventario")
            {
                lineas.Add("Habitacion,Piso,Tipo,Calidad,Estado,Tarifa");
                lineas.AddRange(DatosHotel.Habitaciones.Select(h => Csv(h.NumeroHabitacion) + "," + h.Piso + "," + Csv(h.CapacidadTipo) + "," + Csv(h.Calidad) + "," + Csv(h.Estado.ToString()) + "," + h.TarifaBase.ToString(CultureInfo.InvariantCulture)));
            }
            else
            {
                lineas.Add("Indicador,Valor");
                var resumen = ObtenerResumenOperativo();
                lineas.Add("Habitaciones," + resumen.TotalHabitaciones); lineas.Add("Ocupadas," + resumen.Ocupadas); lineas.Add("Disponibles," + resumen.Disponibles); lineas.Add("Llegadas," + resumen.Llegadas); lineas.Add("Salidas," + resumen.Salidas); lineas.Add("IngresosHoy," + resumen.IngresosHoy.ToString(CultureInfo.InvariantCulture));
            }
            File.WriteAllLines(archivo, lineas, new UTF8Encoding(true));
            DatosHotel.EjecucionesReportes.Insert(0, new EjecucionReporteDemo { Reporte = reporte.Nombre, Fecha = DateTime.Now, Estado = "Generado (demo)", Archivo = archivo });
        }

        private static string Csv(string valor) { return "\"" + (valor ?? "").Replace("\"", "\"\"") + "\""; }

        public DevolucionDemo CrearDevolucion(int transaccionId, decimal monto, string motivo)
        {
            var pago = DatosHotel.Transacciones.FirstOrDefault(t => t.Id == transaccionId && t.Abono > 0);
            if (pago == null || monto <= 0 || monto > pago.Abono || string.IsNullOrWhiteSpace(motivo))
                throw new InvalidOperationException("Selecciona un pago y proporciona motivo y un monto válido que no lo supere.");
            var devolucion = new DevolucionDemo { Id = DatosHotel.Devoluciones.Count + 1, TransaccionId = transaccionId,
                Monto = monto, Motivo = motivo.Trim(), Estado = "Registrada (demo)", Fecha = DateTime.Now };
            DatosHotel.Devoluciones.Add(devolucion);
            DatosHotel.Transacciones.Add(new Transaccion { Id = DatosHotel.Transacciones.Count + 1, FolioId = pago.FolioId,
                Fecha = DateTime.Now, Concepto = "Devolución: " + motivo.Trim(), Cargo = monto, Cantidad = 1, Abono = 0m,
                MetodoPago = pago.MetodoPago, Usuario = "Demo" });
            DatosHotel.RecalcularSaldos();
            return devolucion;
        }

        public EmpleadoDemo CrearEmpleado(string nombre, string apellido, string correo, string telefono)
        {
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido)) throw new InvalidOperationException("Nombre y apellido son obligatorios.");
            var e = new EmpleadoDemo { Id = DatosHotel.Empleados.Count + 1, Nombre = nombre.Trim(), Apellido = apellido.Trim(), Correo = correo ?? "", Telefono = telefono ?? "" };
            DatosHotel.Empleados.Add(e); return e;
        }

        public void CrearRol(string nombre, string descripcion)
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new InvalidOperationException("El nombre del rol es obligatorio.");
            if (DatosHotel.Roles.Any(r => r.Nombre.Equals(nombre.Trim(), StringComparison.CurrentCultureIgnoreCase))) throw new InvalidOperationException("Ese rol ya existe.");
            DatosHotel.Roles.Add(new RolDemo { Nombre = nombre.Trim(), Descripcion = descripcion ?? "" });
        }

        public void AsignarRolEmpleado(int empleadoId, string rol)
        {
            var e = DatosHotel.Empleados.FirstOrDefault(x => x.Id == empleadoId);
            if (e == null || !DatosHotel.Roles.Any(r => r.Nombre == rol)) throw new InvalidOperationException("Selecciona empleado y rol válidos.");
            if (DatosHotel.AsignacionesRoles.Any(a => a.EmpleadoId == empleadoId && a.Rol == rol)) return;
            DatosHotel.AsignacionesRoles.Add(new AsignacionRolDemo { EmpleadoId = empleadoId, Empleado = e.Nombre + " " + e.Apellido, Rol = rol });
        }

        public void CrearConceptoCargo(string nombre, string categoria)
        {
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(categoria)) throw new InvalidOperationException("Completa nombre y categoría.");
            if (DatosHotel.ConceptosCargos.Any(c => c.Nombre.Equals(nombre.Trim(), StringComparison.CurrentCultureIgnoreCase))) throw new InvalidOperationException("Ese concepto ya existe.");
            DatosHotel.ConceptosCargos.Add(new ConceptoCargoDemo { Nombre = nombre.Trim(), Categoria = categoria.Trim() });
        }

        public void CrearServicio(string nombre, string tipo, decimal precio, bool activo)
        {
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(tipo) || precio < 0) throw new InvalidOperationException("Completa nombre, tipo y precio válido.");
            DatosHotel.Servicios.Add(new ServicioCatalogoDemo { Nombre = nombre.Trim(), Tipo = tipo.Trim(), Precio = precio, Activo = activo });
        }

        public void ProgramarCitaServicio(string codigoReserva, string servicio, DateTime fecha, int duracion)
        {
            if (!DatosHotel.Reservas.Any(r => r.CodigoReserva == codigoReserva && r.Estado == EstadoReserva.EnCurso) ||
                !DatosHotel.Servicios.Any(s => s.Nombre == servicio && s.Activo) || duracion < 1)
                throw new InvalidOperationException("Selecciona una estancia activa, un servicio disponible y duración positiva.");
            DatosHotel.CitasServicios.Add(new CitaServicioDemo { Reserva = codigoReserva, Servicio = servicio, Fecha = fecha, Duracion = duracion, Estado = "Programada" });
        }

        public void ProgramarTraslado(string codigoReserva, string origen, string destino, DateTime fecha)
        {
            if (!DatosHotel.Reservas.Any(r => r.CodigoReserva == codigoReserva) || string.IsNullOrWhiteSpace(origen) || string.IsNullOrWhiteSpace(destino))
                throw new InvalidOperationException("Indica una reserva, origen y destino.");
            DatosHotel.Traslados.Add(new TrasladoDemo { Reserva = codigoReserva, Origen = origen.Trim(), Destino = destino.Trim(), Fecha = fecha });
        }

        public void CrearChecklist(string nombre, string descripcion)
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new InvalidOperationException("El nombre del checklist es obligatorio.");
            DatosHotel.Checklists.Add(new ChecklistDemo { Id = DatosHotel.Checklists.Count + 1, Nombre = nombre.Trim(), Descripcion = descripcion ?? "" });
        }

        public void CrearItemChecklist(int checklistId, string descripcion)
        {
            var c = DatosHotel.Checklists.FirstOrDefault(x => x.Id == checklistId);
            if (c == null || string.IsNullOrWhiteSpace(descripcion)) throw new InvalidOperationException("Selecciona checklist e indica la descripción del ítem.");
            DatosHotel.ItemsChecklist.Add(new ChecklistItemDemo { ChecklistId = c.Id, Checklist = c.Nombre, Descripcion = descripcion.Trim(), Completado = false });
        }

        private static void RegistrarAuditoria(string cambio, string anterior, string nuevo)
        {
            DatosHotel.Auditoria.Add(new AuditoriaDemo { Usuario = "Demo", Cambio = cambio, Anterior = anterior, Nuevo = nuevo, Fecha = DateTime.Now });
        }

        public void RegistrarConsentimiento(Guid huespedId, string tipo, bool autorizado)
        {
            if (!DatosHotel.Huespedes.Any(h => h.Id == huespedId)) throw new InvalidOperationException("Selecciona un huésped válido.");
            if (string.IsNullOrWhiteSpace(tipo)) throw new InvalidOperationException("Indica el tipo de consentimiento.");
            DatosHotel.Consentimientos.Add(new ConsentimientoHuesped { Id = Guid.NewGuid(), HuespedId = huespedId,
                Tipo = tipo.Trim(), Autorizado = autorizado, Fecha = DateTime.Now });
        }

        public Habitacion CrearHabitacion(string numero, int piso, string tipo, string calidad, decimal tarifa)
        {
            if (string.IsNullOrWhiteSpace(numero) || piso < 1 || string.IsNullOrWhiteSpace(tipo))
                throw new InvalidOperationException("Número, piso y tipo son obligatorios.");
            if (DatosHotel.Habitaciones.Any(h => h.NumeroHabitacion == numero.Trim()))
                throw new InvalidOperationException("Ya existe una habitación con ese número.");
            var habitacion = new Habitacion { Id = DatosHotel.Habitaciones.Count == 0 ? 1 : DatosHotel.Habitaciones.Max(h => h.Id) + 1,
                NumeroHabitacion = numero.Trim(), Piso = piso, CapacidadTipo = tipo, Calidad = string.IsNullOrWhiteSpace(calidad) ? "Estándar" : calidad,
                CapacidadOverride = DatosHotel.TiposHabitacion.FirstOrDefault(t => t.Nombre == tipo)?.Capacidad ?? 0,
                TarifaBase = tarifa > 0 ? tarifa : 100m, Estado = EstadoHabitacion.Disponible, LibreDeHumo = true };
            DatosHotel.Habitaciones.Add(habitacion);
            if (!DatosHotel.Pisos.Contains(piso)) DatosHotel.Pisos.Add(piso);
            return habitacion;
        }

        public void CambiarEstadoHabitacion(int id)
        {
            var h = DatosHotel.Habitaciones.FirstOrDefault(x => x.Id == id);
            if (h == null) throw new InvalidOperationException("Selecciona una habitación.");
            var estados = new[] { EstadoHabitacion.Disponible, EstadoHabitacion.Limpieza, EstadoHabitacion.Mantenimiento, EstadoHabitacion.FueraDeServicio };
            int indice = Array.IndexOf(estados, h.Estado);
            RegistrarEstado(h, estados[(indice + 1 + estados.Length) % estados.Length], "Cambio manual de inventario");
        }

        public void CambiarTipoHabitacion(int id, string tipo)
        {
            var h = DatosHotel.Habitaciones.FirstOrDefault(x => x.Id == id);
            if (h == null) throw new InvalidOperationException("Selecciona una habitación.");
            if (string.IsNullOrWhiteSpace(tipo)) throw new InvalidOperationException("Selecciona un tipo.");
            h.CapacidadTipo = tipo;
            var catalogo = DatosHotel.TiposHabitacion.FirstOrDefault(t => t.Nombre == tipo);
            h.CapacidadOverride = catalogo == null ? 0 : catalogo.Capacidad;
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
                CantidadPersonas = 1,
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
                RegistrarEstado(habitacion, EstadoHabitacion.Ocupada, "Check-in");

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
            RegistrarEstado(habitacion, EstadoHabitacion.Limpieza, "Check-out");
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
                    RegistrarEstado(habitacion, EstadoHabitacion.Disponible, "Cancelación de estancia");
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

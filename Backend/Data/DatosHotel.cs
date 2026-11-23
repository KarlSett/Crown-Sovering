using System;
using System.Collections.Generic;
using System.Linq;
using Crown__.Backend.Models;

namespace Crown__.Backend.Data
{
    /// <summary>
    /// Almacén en memoria con datos de demostración alineados al esquema PostgreSQL
    /// (hotel_config, usuarios, huespedes, habitaciones, reservas, folios,
    /// folio_transacciones, cierres_caja...). Sustituible por un repositorio ADO.NET
    /// conservando las mismas entidades de Backend/Models.
    /// </summary>
    public static class DatosHotel
    {
        private static readonly object Sync = new object();
        private static bool inicializado;

        public static List<Habitacion> Habitaciones { get; private set; }
        public static List<Huesped> Huespedes { get; private set; }
        public static List<ConsentimientoHuesped> Consentimientos { get; private set; }
        public static List<TareaOperativa> Tareas { get; private set; }
        public static List<FacturaDemo> Facturas { get; private set; }
        public static List<NotaCreditoDemo> NotasCredito { get; private set; }
        public static List<LlaveDemo> Llaves { get; private set; }
        public static List<BloqueoHabitacionDemo> BloqueosHabitacion { get; private set; }
        public static List<HistorialHabitacionDemo> HistorialHabitaciones { get; private set; }
        public static List<TipoHabitacionDemo> TiposHabitacion { get; private set; }
        public static List<int> Pisos { get; private set; }
        public static List<ProductoDemo> Productos { get; private set; }
        public static List<ConsumoDemo> Consumos { get; private set; }
        public static List<ConvenioDemo> Convenios { get; private set; }
        public static List<TemporadaDemo> Temporadas { get; private set; }
        public static List<PaqueteDemo> Paquetes { get; private set; }
        public static List<PromocionDemo> Promociones { get; private set; }
        public static List<ConfiguracionDemo> Configuraciones { get; private set; }
        public static List<PoliticaDemo> Politicas { get; private set; }
        public static List<ImpuestoDemo> Impuestos { get; private set; }
        public static List<AuditoriaDemo> Auditoria { get; private set; }
        public static List<DevolucionDemo> Devoluciones { get; private set; }
        public static List<NotificacionDemo> Notificaciones { get; private set; }
        public static List<ReporteProgramadoDemo> ReportesProgramados { get; private set; }
        public static List<EjecucionReporteDemo> EjecucionesReportes { get; private set; }
        public static List<EmpleadoDemo> Empleados { get; private set; }
        public static List<RolDemo> Roles { get; private set; }
        public static List<AsignacionRolDemo> AsignacionesRoles { get; private set; }
        public static List<ConceptoCargoDemo> ConceptosCargos { get; private set; }
        public static List<ServicioCatalogoDemo> Servicios { get; private set; }
        public static List<CitaServicioDemo> CitasServicios { get; private set; }
        public static List<TrasladoDemo> Traslados { get; private set; }
        public static List<ChecklistDemo> Checklists { get; private set; }
        public static List<ChecklistItemDemo> ItemsChecklist { get; private set; }
        public static List<Reserva> Reservas { get; private set; }
        public static List<Folio> Folios { get; private set; }
        public static List<Transaccion> Transacciones { get; private set; }
        public static List<CierreCaja> CierresCaja { get; private set; }
        public static string NombreHotel { get; set; }
        public static string Moneda { get; set; }
        public static string Direccion { get; set; }
        public static string TelefonoHotel { get; set; }
        public static string EmailHotel { get; set; }
        public static int Categoria { get; set; }

        private static int correlativoReservas;
        private static int correlativoTransacciones;

        public static readonly string[] CuentasIngreso =
        {
            "Hospedaje", "Restaurante", "Spa", "Lavandería", "Minibar", "Estacionamiento"
        };

        public static readonly List<string> MetodosPago = new List<string> { "Efectivo", "Tarjeta", "Transferencia" };
        public static bool TurnoCajaAbierto { get; set; }
        public static DateTime? AperturaCaja { get; set; }

        public static void Inicializar()
        {
            lock (Sync)
            {
                if (inicializado)
                    return;

                NombreHotel = "Crown & Sovereign";
                Moneda = "USD";
                Direccion = ""; TelefonoHotel = ""; EmailHotel = ""; Categoria = 4;

                Habitaciones = new List<Habitacion>();
                Huespedes = new List<Huesped>();
                Consentimientos = new List<ConsentimientoHuesped>();
                Tareas = new List<TareaOperativa>();
                Facturas = new List<FacturaDemo>();
                NotasCredito = new List<NotaCreditoDemo>();
                Llaves = new List<LlaveDemo>();
                BloqueosHabitacion = new List<BloqueoHabitacionDemo>(); HistorialHabitaciones = new List<HistorialHabitacionDemo>();
                TiposHabitacion = new List<TipoHabitacionDemo> { new TipoHabitacionDemo { Nombre = "Sencilla", Capacidad = 2 }, new TipoHabitacionDemo { Nombre = "Doble", Capacidad = 4 }, new TipoHabitacionDemo { Nombre = "Familiar", Capacidad = 6 }, new TipoHabitacionDemo { Nombre = "Suite", Capacidad = 3 } };
                Pisos = new List<int> { 1, 2, 3, 4 };
                Productos = new List<ProductoDemo>();
                Consumos = new List<ConsumoDemo>();
                Convenios = new List<ConvenioDemo>();
                Temporadas = new List<TemporadaDemo>();
                Paquetes = new List<PaqueteDemo>();
                Promociones = new List<PromocionDemo>();
                Configuraciones = new List<ConfiguracionDemo>(); Politicas = new List<PoliticaDemo>();
                Impuestos = new List<ImpuestoDemo>(); Auditoria = new List<AuditoriaDemo>();
                Devoluciones = new List<DevolucionDemo>();
                Notificaciones = new List<NotificacionDemo>(); ReportesProgramados = new List<ReporteProgramadoDemo>(); EjecucionesReportes = new List<EjecucionReporteDemo>();
                Empleados = new List<EmpleadoDemo>(); Roles = new List<RolDemo> { new RolDemo { Nombre = "Administrador", Descripcion = "Administración general" }, new RolDemo { Nombre = "Gerente", Descripcion = "Gestión hotelera" }, new RolDemo { Nombre = "Recepcionista", Descripcion = "Recepción y reservas" }, new RolDemo { Nombre = "Personal de servicios", Descripcion = "Operación de servicios" } }; AsignacionesRoles = new List<AsignacionRolDemo>();
                ConceptosCargos = CuentasIngreso.Select(x => new ConceptoCargoDemo { Nombre = x, Categoria = "Ingreso" }).ToList(); Servicios = new List<ServicioCatalogoDemo>(); CitasServicios = new List<CitaServicioDemo>(); Traslados = new List<TrasladoDemo>(); Checklists = new List<ChecklistDemo>(); ItemsChecklist = new List<ChecklistItemDemo>();
                Productos.Add(new ProductoDemo { Id = 1, Nombre = "Agua mineral", Precio = 3m, Stock = 48, Activo = true });
                Productos.Add(new ProductoDemo { Id = 2, Nombre = "Minibar · selección", Precio = 8.5m, Stock = 24, Activo = true });
                Productos.Add(new ProductoDemo { Id = 3, Nombre = "Lavandería · unidad", Precio = 5m, Stock = 100, Activo = true });
                Reservas = new List<Reserva>();
                Folios = new List<Folio>();
                Transacciones = new List<Transaccion>();
                CierresCaja = new List<CierreCaja>();

                var rnd = new Random(2026);
                CrearHabitaciones(rnd);
                CrearHuespedes();
                CrearReservas(rnd);
                CrearFoliosYTransacciones(rnd);
                CrearCierresCaja();
                AjustarEstadosHabitaciones();
                SembrarTareas();

                inicializado = true;
            }
        }

        private static void SembrarTareas()
        {
            Tareas.Add(new TareaOperativa { Id = 1, Habitacion = "101", Tipo = "Limpieza", Descripcion = "Preparar habitación", Estado = "Pendiente", Fecha = DateTime.Today });
            Tareas.Add(new TareaOperativa { Id = 2, Habitacion = "204", Tipo = "Inspección", Descripcion = "Revisión de salida", Estado = "Pendiente", Fecha = DateTime.Today });
            Tareas.Add(new TareaOperativa { Id = 3, Habitacion = "305", Tipo = "Limpieza", Descripcion = "Servicio de estancia", Estado = "En curso", Fecha = DateTime.Today });
            Tareas.Add(new TareaOperativa { Id = 4, Habitacion = "102", Tipo = "Limpieza", Descripcion = "Habitación lista", Estado = "Completada", Fecha = DateTime.Today });
        }

        private static void CrearHabitaciones(Random rnd)
        {
            string[] tipos = { "Sencilla", "Doble", "Familiar", "Suite" };
            string[] calidades = { "Estándar", "Superior", "Deluxe", "Presidencial" };
            decimal[] tarifasBase = { 78m, 112m, 165m, 240m };
            double[] factores = { 1.00, 1.15, 1.38, 1.75 };

            int id = 1;
            for (int piso = 1; piso <= 4; piso++)
            {
                for (int numero = 1; numero <= 6; numero++)
                {
                    int i = (piso - 1) * 6 + (numero - 1);
                    int indiceTipo = i % 4;
                    int indiceCalidad = (i / 2) % 4;

                    Habitaciones.Add(new Habitacion
                    {
                        Id = id++,
                        NumeroHabitacion = (piso * 100 + numero).ToString(),
                        Piso = piso,
                        CapacidadTipo = tipos[indiceTipo],
                        Calidad = calidades[indiceCalidad],
                        TarifaBase = Math.Round(tarifasBase[indiceTipo] * (decimal)factores[indiceCalidad], 2),
                        Estado = EstadoHabitacion.Disponible,
                        TieneVista = numero % 2 == 0,
                        TieneBalcon = piso >= 3 && numero % 3 == 0,
                        TieneJacuzzi = calidades[indiceCalidad] == "Presidencial",
                        EsHipoalergenica = i % 7 == 0,
                        LibreDeHumo = true
                    });
                }
            }
        }

        private static void CrearHuespedes()
        {
            string[,] datos =
            {
                { "Ana Lucía", "Mendoza Salazar", "Cédula", "0912345678", "ana.mendoza@correo.ec", "+593 99 812 3344", "Ecuatoriana", "Fidelidad" },
                { "Carlos Andrés", "Vera Paredes", "Cédula", "0923456789", "carlos.vera@correo.ec", "+593 98 441 2277", "Ecuatoriana", "Regular" },
                { "María Fernanda", "Espinoza Ruiz", "Pasaporte", "PE9845123", "mf.espinoza@correo.pe", "+51 987 654 321", "Peruana", "Corporativo" },
                { "Diego", "Cárdenas Molina", "Cédula", "0934567890", "diego.cardenas@correo.ec", "+593 96 771 9034", "Ecuatoriana", "Regular" },
                { "Sofía", "Ramos Ortega", "Pasaporte", "CO7712345", "sofia.ramos@correo.co", "+57 310 456 7890", "Colombiana", "Fidelidad" },
                { "James", "Holloway", "Pasaporte", "US5521987", "j.holloway@correo.com", "+1 305 552 0198", "Estadounidense", "Corporativo" },
                { "Valentina", "Suárez Cevallos", "Cédula", "0945678901", "valentina.suarez@correo.ec", "+593 99 334 5566", "Ecuatoriana", "Regular" },
                { "Andrés Felipe", "Guerrero Paz", "Cédula", "0956789012", "andres.guerrero@correo.ec", "+593 98 120 4488", "Ecuatoriana", "Corporativo" },
                { "Lucía", "Bonilla Terán", "Cédula", "0967890123", "lucia.bonilla@correo.ec", "+593 97 665 1122", "Ecuatoriana", "Regular" },
                { "Hiroshi", "Tanaka", "Pasaporte", "JP3390087", "h.tanaka@correo.jp", "+81 90 3345 6677", "Japonesa", "Fidelidad" },
                { "Elena", "Martínez Vidal", "Pasaporte", "ES8845120", "elena.martinez@correo.es", "+34 612 445 778", "Española", "Regular" },
                { "Martín", "Pazmiño Andrade", "Cédula", "0978901234", "martin.pazmino@correo.ec", "+593 99 210 7744", "Ecuatoriana", "Fidelidad" },
                { "Camila", "Zambrano Ríos", "Cédula", "0989012345", "camila.zambrano@correo.ec", "+593 95 880 1122", "Ecuatoriana", "Regular" },
                { "Gustavo", "Adum Chedraui", "Cédula", "0990123456", "gustavo.adum@correo.ec", "+593 99 400 9900", "Ecuatoriana", "Corporativo" },
                { "Rosa María", "Viteri Salcedo", "Cédula", "0901234567", "rosa.viteri@correo.ec", "+593 98 700 2233", "Ecuatoriana", "Fidelidad" },
                { "Lucas", "Ferreira Costa", "Pasaporte", "BR7719902", "lucas.ferreira@correo.br", "+55 11 99876 5432", "Brasileña", "Regular" },
                { "Natalia", "Kovács", "Pasaporte", "HU7723401", "natalia.kovacs@correo.hu", "+36 30 445 8899", "Húngara", "Corporativo" },
                { "Pedro", "Alvarado Núñez", "Cédula", "0911223344", "pedro.alvarado@correo.ec", "+593 96 223 4455", "Ecuatoriana", "Regular" },
                { "Isabel", "Cordero Mera", "Cédula", "0922334455", "isabel.cordero@correo.ec", "+593 99 556 7788", "Ecuatoriana", "Fidelidad" },
                { "Ricardo", "Bermeo Ochoa", "Cédula", "0933445566", "ricardo.bermeo@correo.ec", "+593 97 889 0011", "Ecuatoriana", "Regular" }
            };

            for (int i = 0; i < datos.GetLength(0); i++)
            {
                Huespedes.Add(new Huesped
                {
                    Id = Guid.NewGuid(),
                    Nombres = datos[i, 0],
                    Apellidos = datos[i, 1],
                    TipoDocumento = datos[i, 2],
                    NumeroDocumento = datos[i, 3],
                    Correo = datos[i, 4],
                    Telefono = datos[i, 5],
                    Nacionalidad = datos[i, 6],
                    Segmentacion = datos[i, 7]
                });
            }
        }

        private static void CrearReservas(Random rnd)
        {
            DateTime hoy = DateTime.Today;
            correlativoReservas = 1;

            // 1. Huéspedes en casa: dos salen hoy, el resto sigue alojado.
            for (int k = 0; k < 9; k++)
            {
                DateTime entrada = hoy.AddDays(-(1 + k % 4)).AddHours(15);
                DateTime salida = k < 2 ? hoy.AddHours(12) : hoy.AddDays(2 + k % 3).AddHours(12);
                Habitacion hab = BuscarHabitacionLibre(entrada, salida, rnd);
                if (hab == null) continue;

                var reserva = NuevaReserva(Huespedes[k].Id, hab, entrada, salida, EstadoReserva.EnCurso,
                                           k % 3 == 0 ? CanalReserva.Web : CanalReserva.Mostrador, hab.TarifaBase, rnd);
                Reservas.Add(reserva);
                hab.Estado = EstadoHabitacion.Ocupada;
            }

            // 2. Llegadas de hoy.
            for (int k = 9; k < 13; k++)
            {
                DateTime entrada = hoy.AddHours(14 + k % 4);
                DateTime salida = hoy.AddDays(2 + k % 3).AddHours(12);
                Habitacion hab = BuscarHabitacionLibre(entrada, salida, rnd);
                if (hab == null) continue;

                Reservas.Add(NuevaReserva(Huespedes[k].Id, hab, entrada, salida, EstadoReserva.Confirmada,
                                          (CanalReserva)(k % 6), hab.TarifaBase, rnd));
            }

            // 3. Reservas confirmadas futuras.
            for (int k = 13; k < 20; k++)
            {
                DateTime entrada = hoy.AddDays(3 + k % 15).AddHours(15);
                DateTime salida = entrada.AddDays(1 + k % 4).Date.AddHours(12);
                Habitacion hab = BuscarHabitacionLibre(entrada, salida, rnd);
                if (hab == null) continue;

                Reservas.Add(NuevaReserva(Huespedes[k].Id, hab, entrada, salida, EstadoReserva.Confirmada,
                                          (CanalReserva)((k + 2) % 6), hab.TarifaBase, rnd));
            }

            // 4. Estancias finalizadas en los últimos 120 días (base de los reportes).
            for (int k = 0; k < 28; k++)
            {
                DateTime entrada = hoy.AddDays(-110 + k * 4 + rnd.Next(0, 3)).AddHours(15);
                if (entrada >= hoy) continue;

                DateTime salida = entrada.AddDays(1 + rnd.Next(0, 5)).Date.AddHours(12);
                if (salida > hoy) salida = hoy.AddHours(12);

                Habitacion hab = BuscarHabitacionLibre(entrada, salida, rnd);
                if (hab == null) continue;

                Reservas.Add(NuevaReserva(Huespedes[rnd.Next(Huespedes.Count)].Id, hab, entrada, salida,
                                          EstadoReserva.Finalizada, (CanalReserva)(k % 6),
                                          Math.Round(hab.TarifaBase * (decimal)(0.92 + rnd.NextDouble() * 0.18), 2), rnd));
            }

            // 5. Canceladas y no-show.
            for (int k = 0; k < 3; k++)
            {
                DateTime entrada = hoy.AddDays(6 + k * 5).AddHours(15);
                DateTime salida = entrada.AddDays(2).AddHours(12);
                Habitacion hab = BuscarHabitacionLibre(entrada, salida, rnd);
                if (hab == null) continue;

                Reservas.Add(NuevaReserva(Huespedes[k * 2].Id, hab, entrada, salida, EstadoReserva.Cancelada,
                                          CanalReserva.Web, hab.TarifaBase, rnd));
            }

            for (int k = 0; k < 2; k++)
            {
                DateTime entrada = hoy.AddDays(-8 - k * 2).AddHours(15);
                DateTime salida = entrada.AddDays(2).AddHours(12);
                Habitacion hab = BuscarHabitacionLibre(entrada, salida, rnd);
                if (hab == null) continue;

                Reservas.Add(NuevaReserva(Huespedes[k + 4].Id, hab, entrada, salida, EstadoReserva.NoShow,
                                          CanalReserva.Telefono, hab.TarifaBase, rnd));
            }
        }

        private static Reserva NuevaReserva(Guid huespedId, Habitacion hab, DateTime entrada, DateTime salida,
                                            EstadoReserva estado, CanalReserva canal, decimal tarifa, Random rnd)
        {
            return new Reserva
            {
                Id = Guid.NewGuid(),
                CodigoReserva = "CS-" + entrada.Year + "-" + (correlativoReservas++).ToString("0000"),
                HuespedId = huespedId,
                HabitacionId = hab.Id,
                Canal = canal,
                Estado = estado,
                FechaCheckinEsperado = entrada,
                FechaCheckoutEsperado = salida,
                TarifaAplicada = tarifa,
                Observaciones = rnd.Next(4) == 0 ? "Solicita habitación en piso alto." : null,
                CreadaEl = entrada.AddDays(-rnd.Next(1, 20))
            };
        }

        private static Habitacion BuscarHabitacionLibre(DateTime entrada, DateTime salida, Random rnd)
        {
            DateTime inicio = entrada.Date;
            DateTime fin = salida.Date;

            var libres = Habitaciones
                .Where(h => h.Estado == EstadoHabitacion.Disponible)
                .Where(h => !Reservas.Any(r =>
                    r.HabitacionId == h.Id &&
                    r.Estado != EstadoReserva.Cancelada &&
                    r.Estado != EstadoReserva.NoShow &&
                    r.FechaCheckinEsperado.Date < fin &&
                    r.FechaCheckoutEsperado.Date > inicio))
                .ToList();

            if (libres.Count == 0)
                return null;

            return libres[rnd.Next(libres.Count)];
        }

        private static void CrearFoliosYTransacciones(Random rnd)
        {
            DateTime hoy = DateTime.Today;

            var estancias = Reservas
                .Where(r => r.Estado == EstadoReserva.EnCurso || r.Estado == EstadoReserva.Finalizada)
                .OrderBy(r => r.FechaCheckinEsperado)
                .ToList();

            foreach (var reserva in estancias)
            {
                var folio = new Folio
                {
                    Id = Guid.NewGuid(),
                    ReservaId = reserva.Id,
                    HuespedId = reserva.HuespedId,
                    TipoFolio = "Individual",
                    Estado = reserva.Estado == EstadoReserva.EnCurso ? "Abierto" : "Cerrado"
                };
                Folios.Add(folio);

                // Hospedaje: una noche por movimiento. Las noches futuras de estancias en
                // curso todavía no se facturan y no generan ingresos.
                bool pagoCompleto = rnd.Next(100) > 12;
                for (int noche = 0; noche < reserva.Noches; noche++)
                {
                    DateTime fechaNoche = reserva.FechaCheckinEsperado.Date.AddDays(noche).AddMinutes(30);
                    if (fechaNoche > DateTime.Now)
                        break;

                    bool pagada = pagoCompleto || noche < reserva.Noches - 1;
                    AgregarTransaccion(folio, fechaNoche, "Hospedaje",
                                       reserva.TarifaAplicada, pagada ? reserva.TarifaAplicada : 0m, rnd);
                }

                int consumos = rnd.Next(1, 4);
                for (int c = 0; c < consumos; c++)
                {
                    string cuenta;
                    decimal monto;
                    ElegirConsumo(rnd, out cuenta, out monto);
                    DateTime fecha = reserva.FechaCheckinEsperado.Date
                        .AddDays(rnd.Next(0, Math.Max(1, reserva.Noches)))
                        .AddHours(12 + rnd.Next(1, 10));
                    if (fecha > DateTime.Now) fecha = DateTime.Now.AddHours(-2);
                    AgregarTransaccion(folio, fecha, cuenta, monto, monto, rnd);
                }
            }

            // Consumos directos (restaurante, spa, lavandería) para que todos los días tengan movimiento.
            if (Folios.Count > 0)
            {
                for (int dia = 120; dia >= 0; dia--)
                {
                    int movimientos = rnd.Next(1, 3);
                    for (int m = 0; m < movimientos; m++)
                    {
                        var folio = Folios[rnd.Next(Folios.Count)];
                        string cuenta;
                        decimal monto;
                        ElegirConsumo(rnd, out cuenta, out monto);
                        DateTime fechaConsumo = hoy.AddDays(-dia).AddHours(10 + rnd.Next(0, 10)).AddMinutes(rnd.Next(0, 60));
                        if (fechaConsumo > DateTime.Now)
                            fechaConsumo = DateTime.Now.AddHours(-3).AddMinutes(-rnd.Next(0, 90));
                        AgregarTransaccion(folio, fechaConsumo, cuenta, monto, monto, rnd);
                    }
                }
            }

            RecalcularSaldos();
        }

        private static void ElegirConsumo(Random rnd, out string cuenta, out decimal monto)
        {
            int opcion = rnd.Next(5);
            switch (opcion)
            {
                case 0:
                    cuenta = "Restaurante";
                    monto = Math.Round(22m + (decimal)(rnd.NextDouble() * 85), 2);
                    break;
                case 1:
                    cuenta = "Spa";
                    monto = Math.Round(45m + (decimal)(rnd.NextDouble() * 140), 2);
                    break;
                case 2:
                    cuenta = "Lavandería";
                    monto = Math.Round(8m + (decimal)(rnd.NextDouble() * 30), 2);
                    break;
                case 3:
                    cuenta = "Minibar";
                    monto = Math.Round(4m + (decimal)(rnd.NextDouble() * 26), 2);
                    break;
                default:
                    cuenta = "Estacionamiento";
                    monto = Math.Round(5m + (decimal)(rnd.NextDouble() * 10), 2);
                    break;
            }
        }

        private static void AgregarTransaccion(Folio folio, DateTime fecha, string concepto,
                                               decimal cargo, decimal abono, Random rnd)
        {
            int suerte = rnd.Next(100);
            string metodo = suerte < 44 ? "Efectivo" : suerte < 84 ? "Tarjeta" : "Transferencia";

            Transacciones.Add(new Transaccion
            {
                Id = ++correlativoTransacciones,
                FolioId = folio.Id,
                Fecha = fecha,
                Concepto = concepto,
                Cargo = Math.Round(cargo, 2),
                Abono = Math.Round(abono, 2),
                MetodoPago = metodo,
                Usuario = "Recepción"
            });
        }

        public static void RecalcularSaldos()
        {
            foreach (var folio in Folios)
            {
                var movimientos = Transacciones.Where(t => t.FolioId == folio.Id).ToList();
                folio.SaldoPendiente = Math.Round(movimientos.Sum(t => t.Cargo) - movimientos.Sum(t => t.Abono), 2);

                var reserva = Reservas.FirstOrDefault(r => r.Id == folio.ReservaId);
                bool finalizada = reserva != null && reserva.Estado == EstadoReserva.Finalizada;
                folio.Estado = finalizada && folio.SaldoPendiente <= 0 ? "Cerrado" : "Abierto";
            }
        }

        private static void CrearCierresCaja()
        {
            DateTime hoy = DateTime.Today;
            CierresCaja.Clear();

            for (int dia = 120; dia >= 0; dia--)
            {
                DateTime fecha = hoy.AddDays(-dia).Date;
                var movimientos = Transacciones.Where(t => t.Fecha.Date == fecha).ToList();
                if (movimientos.Count == 0)
                    continue;

                CierresCaja.Add(new CierreCaja
                {
                    Id = CierresCaja.Count + 1,
                    Fecha = fecha,
                    FondoInicial = 200m,
                    TotalEfectivo = Math.Round(movimientos.Where(t => t.MetodoPago == "Efectivo").Sum(t => t.Abono), 2),
                    TotalTarjeta = Math.Round(movimientos.Where(t => t.MetodoPago == "Tarjeta").Sum(t => t.Abono), 2),
                    TotalTransferencia = Math.Round(movimientos.Where(t => t.MetodoPago == "Transferencia").Sum(t => t.Abono), 2)
                });
            }
        }

        private static void AjustarEstadosHabitaciones()
        {
            DateTime hoy = DateTime.Today;
            var libresHoy = Habitaciones
                .Where(h => h.Estado == EstadoHabitacion.Disponible)
                .Where(h => !Reservas.Any(r => r.HabitacionId == h.Id &&
                                               r.Estado != EstadoReserva.Cancelada &&
                                               r.Estado != EstadoReserva.NoShow &&
                                               r.FechaCheckinEsperado.Date <= hoy &&
                                               r.FechaCheckoutEsperado.Date > hoy))
                .ToList();

            int indice = 0;
            foreach (var hab in libresHoy)
            {
                if (indice < 3) hab.Estado = EstadoHabitacion.Limpieza;
                else if (indice == 3) hab.Estado = EstadoHabitacion.Mantenimiento;
                else if (indice == 4) hab.Estado = EstadoHabitacion.FueraDeServicio;
                indice++;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Frontend.Views.Modules;
using Crown__.Frontend.Views.Sections;

namespace Crown__.Frontend.Views
{
    /// <summary>
    /// Contenedor del panel: barra lateral agrupada por rol, encabezado y host de vistas.
    /// La visibilidad por rol es solo de presentación; no sustituye permisos reales.
    /// </summary>
    public partial class DashboardView : Page
    {
        private static readonly Dictionary<string, Rol[]> Permisos = new Dictionary<string, Rol[]>
        {
            { "funcionamiento", new[] { Rol.Gerente, Rol.Recepcionista } },
            { "reservas", new[] { Rol.Gerente, Rol.Recepcionista } },
            { "checkin", new[] { Rol.Recepcionista } },
            { "checkout", new[] { Rol.Recepcionista } },
            { "habitaciones", new[] { Rol.Gerente, Rol.Recepcionista } },
            { "ingresos", new[] { Rol.Gerente } },
            { "huespedes", new[] { Rol.Recepcionista } },
            { "inventario", new[] { Rol.Administrador, Rol.Gerente } },
            { "estancias", new[] { Rol.Recepcionista } },
            { "facturacion", new[] { Rol.Gerente, Rol.Recepcionista } },
            { "pagos", new[] { Rol.Gerente, Rol.Recepcionista } },
            { "reservasadmin", new[] { Rol.Gerente, Rol.Recepcionista } },
            { "notificaciones", new[] { Rol.Administrador, Rol.Gerente } },
            { "tarifas", new[] { Rol.Gerente } },
            { "convenios", new[] { Rol.Gerente } },
            { "limpieza", new[] { Rol.Servicios, Rol.Gerente } },
            { "consumos", new[] { Rol.Servicios, Rol.Recepcionista, Rol.Gerente } },
            { "configuracion", new[] { Rol.Administrador } },
            { "seguridad", new[] { Rol.Administrador } }
        };

        private bool cargando = true;
        private readonly Dictionary<string, UserControl> vistas = new Dictionary<string, UserControl>();

        private FuncionamientoView VistaFuncionamiento { get { return ObtenerVista<FuncionamientoView>("funcionamiento"); } }
        private ReservasView VistaReservas { get { return ObtenerVista<ReservasView>("reservas"); } }
        private HabitacionesView VistaHabitaciones { get { return ObtenerVista<HabitacionesView>("habitaciones"); } }
        private IngresosView VistaIngresos { get { return ObtenerVista<IngresosView>("ingresos"); } }
        private HuespedesView VistaHuespedes { get { return ObtenerVista<HuespedesView>("huespedes"); } }
        private HabitacionesGestionView VistaInventario { get { return ObtenerVista<HabitacionesGestionView>("inventario"); } }
        private EstanciasView VistaEstancias { get { return ObtenerVista<EstanciasView>("estancias"); } }
        private FacturacionFoliosView VistaFacturacion { get { return ObtenerVista<FacturacionFoliosView>("facturacion"); } }
        private PagosCajaView VistaPagos { get { return ObtenerVista<PagosCajaView>("pagos"); } }
        private ReservasGestionView VistaReservasAdmin { get { return ObtenerVista<ReservasGestionView>("reservasadmin"); } }
        private NotificacionesReportesView VistaNotificaciones { get { return ObtenerVista<NotificacionesReportesView>("notificaciones"); } }
        private TarifasView VistaTarifas { get { return ObtenerVista<TarifasView>("tarifas"); } }
        private ConveniosView VistaConvenios { get { return ObtenerVista<ConveniosView>("convenios"); } }
        private LimpiezaView VistaLimpieza { get { return ObtenerVista<LimpiezaView>("limpieza"); } }
        private ConsumosView VistaConsumos { get { return ObtenerVista<ConsumosView>("consumos"); } }
        private ConfiguracionHotelView VistaConfiguracion { get { return ObtenerVista<ConfiguracionHotelView>("configuracion"); } }
        private SeguridadView VistaSeguridad { get { return ObtenerVista<SeguridadView>("seguridad"); } }

        public DashboardView()
        {
            InitializeComponent();
        }

        private T ObtenerVista<T>(string clave) where T : UserControl, new()
        {
            UserControl vista;
            if (!vistas.TryGetValue(clave, out vista))
            {
                vista = new T { Visibility = Visibility.Collapsed };
                vistas.Add(clave, vista);
                ViewHost.Children.Add(vista);
                var reservas = vista as ReservasView;
                if (reservas != null) reservas.CambiosRealizados += OnCambiosRealizados;
            }
            return (T)vista;
        }

        private void AplicarSesion()
        {
            TxtSesionUsuario.Text = Sesion.NombreRol;
            TxtSesionDetalle.Text = Sesion.NombreUsuario ?? "Sin sesión";
            TxtGrupoNegocio.Text = Sesion.Rol == Rol.Administrador ? "INFRAESTRUCTURA Y SISTEMA" : "GESTIÓN DEL HOTEL";

            AplicarPermisos();
        }

        private void AplicarPermisos()
        {
            AplicarPermiso("funcionamiento", NavFuncionamiento);
            AplicarPermiso("reservas", NavReservas);
            AplicarPermiso("checkin", NavCheckIn);
            AplicarPermiso("checkout", NavCheckOut);
            AplicarPermiso("habitaciones", NavHabitaciones);
            AplicarPermiso("ingresos", NavIngresos);

            AplicarPermiso("huespedes", NavHuespedes);
            AplicarPermiso("inventario", NavInventario);
            AplicarPermiso("estancias", NavEstancias);
            AplicarPermiso("facturacion", NavFacturacion);
            AplicarPermiso("pagos", NavPagos);
            AplicarPermiso("reservasadmin", NavReservasAdmin);
            AplicarPermiso("notificaciones", NavNotificaciones);

            AplicarPermiso("tarifas", NavTarifas);
            AplicarPermiso("convenios", NavConvenios);
            AplicarPermiso("limpieza", NavLimpieza);
            AplicarPermiso("consumos", NavConsumos);
            AplicarPermiso("configuracion", NavConfiguracion);
            AplicarPermiso("seguridad", NavSeguridad);

            GrupoRecepcion.Visibility = VisibleGroup(NavReservas, NavCheckIn, NavCheckOut, NavHabitaciones,
                NavHuespedes, NavEstancias, NavFacturacion, NavPagos, NavReservasAdmin);
            GrupoServicios.Visibility = VisibleGroup(NavLimpieza, NavConsumos);
            GrupoNegocio.Visibility = VisibleGroup(NavIngresos, NavInventario, NavTarifas, NavConvenios, NavNotificaciones);
            GrupoConfiguracion.Visibility = VisibleGroup(NavConfiguracion, NavSeguridad);
        }

        private bool Puede(string seccion)
        {
            Rol[] roles;
            return Permisos.TryGetValue(seccion, out roles) && Array.IndexOf(roles, Sesion.Rol) >= 0;
        }

        private void AplicarPermiso(string seccion, RadioButton boton)
        {
            boton.Visibility = Puede(seccion) ? Visibility.Visible : Visibility.Collapsed;
        }

        private static Visibility VisibleGroup(params RadioButton[] botones)
        {
            foreach (var boton in botones)
            {
                if (boton.Visibility == Visibility.Visible)
                    return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        private string SeccionInicial()
        {
            if (Sesion.Rol == Rol.Administrador)
                return "configuracion";
            if (Sesion.Rol == Rol.Recepcionista)
                return "checkin";
            if (Sesion.Rol == Rol.Servicios)
                return "limpieza";
            if (Puede("funcionamiento"))
                return "funcionamiento";
            return "reservas";
        }

        private RadioButton NavDe(string seccion)
        {
            switch (seccion)
            {
                case "reservas": return NavReservas;
                case "checkin": return NavCheckIn;
                case "checkout": return NavCheckOut;
                case "habitaciones": return NavHabitaciones;
                case "ingresos": return NavIngresos;
                case "huespedes": return NavHuespedes;
                case "inventario": return NavInventario;
                case "estancias": return NavEstancias;
                case "facturacion": return NavFacturacion;
                case "pagos": return NavPagos;
                case "reservasadmin": return NavReservasAdmin;
                case "notificaciones": return NavNotificaciones;
                case "tarifas": return NavTarifas;
                case "convenios": return NavConvenios;
                case "limpieza": return NavLimpieza;
                case "consumos": return NavConsumos;
                case "configuracion": return NavConfiguracion;
                case "seguridad": return NavSeguridad;
                default: return NavFuncionamiento;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AplicarSesion();

            TxtFecha.Text = Formato.DiaDeSemana(DateTime.Today);

            string inicial = SeccionInicial();
            NavDe(inicial).IsChecked = true;

            cargando = false;
            MostrarSeccion(inicial);
        }

        private void OnCambiosRealizados(object sender, EventArgs e)
        {
            Recargar(VistaFuncionamiento.Cargar);
            Recargar(VistaHabitaciones.Cargar);
            Recargar(VistaIngresos.Cargar);
        }

        // Una sección que no pudo leer la base avisa una sola vez en lugar de dejar la pantalla en blanco.
        private bool avisoDeConexion;

        private void Recargar(Action cargar)
        {
            try
            {
                cargar();
                avisoDeConexion = false;
            }
            catch (Exception ex)
            {
                if (avisoDeConexion) return;
                avisoDeConexion = true;
                MessageBox.Show("No se pudieron leer los datos de la base." + Environment.NewLine + ex.Message, "Conexión",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnNavChecked(object sender, RoutedEventArgs e)
        {
            if (cargando)
                return;

            var opcion = sender as RadioButton;
            if (opcion == null)
                return;

            MostrarSeccion(opcion.Tag as string);
        }

        private void MostrarSeccion(string seccion)
        {
            if (string.IsNullOrEmpty(seccion))
                return;

            string claveVista = seccion == "checkin" || seccion == "checkout" ? "estancias" : seccion;
            bool vistaYaCreada = vistas.ContainsKey(claveVista);

            foreach (var vista in vistas.Values)
                vista.Visibility = Visibility.Collapsed;

            UserControl activa = ObtenerPorClave(seccion);
            activa.Visibility = Visibility.Visible;

            var estancias = activa as EstanciasView;
            if (estancias != null && seccion == "checkin") estancias.EnfocarLlegadas();
            if (estancias != null && seccion == "checkout") estancias.EnfocarSalidas();

            switch (seccion)
            {
                case "funcionamiento":
                    RecargarSiNecesario(vistaYaCreada, true, VistaFuncionamiento.Cargar);
                    break;
                case "reservas":
                    Titulo("Reservas", "Consulta, crea y opera las reservas del hotel.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaReservas.Cargar);
                    break;
                case "checkin":
                    Titulo("Check-in · llegadas", "Atiende las llegadas previstas y registra la entrada de cada huésped.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaEstancias.Cargar);
                    break;
                case "checkout":
                    Titulo("Check-out · salidas", "Revisa saldos y llaves para cerrar cada estancia.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaEstancias.Cargar);
                    break;
                case "habitaciones":
                    Titulo("Habitaciones disponibles", "Disponibilidad por fechas, tipo de capacidad y calidad.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaHabitaciones.Cargar);
                    break;
                case "ingresos":
                    Titulo("Cuentas de ingreso", "Resumen diario, semanal y trimestral de las cuentas del hotel.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaIngresos.Cargar);
                    break;
                case "huespedes":
                    Titulo("Huéspedes", "Perfiles de huéspedes y consentimientos de datos.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaHuespedes.Cargar);
                    break;
                case "inventario":
                    Titulo("Inventario de habitaciones", "Habitaciones, pisos, tipos, historial y bloqueos.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaInventario.Cargar);
                    break;
                case "estancias":
                    Titulo("Estancias y llaves", "Consulta huéspedes alojados, llaves y cambios de habitación.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaEstancias.Cargar);
                    break;
                case "facturacion":
                    Titulo("Facturación", "Folios, cargos, facturas y notas de crédito.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaFacturacion.Cargar);
                    break;
                case "pagos":
                    Titulo("Pagos y caja", "Cobros, devoluciones, turnos y cierre de caja.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaPagos.Cargar);
                    break;
                case "reservasadmin":
                    Titulo("Reservas · administración", "Canales, grupos, lista de espera y solicitudes especiales.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaReservasAdmin.Cargar);
                    break;
                case "notificaciones":
                    Titulo("Notificaciones y reportes", "Avisos internos y reportes programados.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaNotificaciones.Cargar);
                    break;
                case "tarifas":
                    Titulo("Tarifas y promociones", "Tarifas por tipo de habitación, temporadas, paquetes y promociones.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaTarifas.Cargar);
                    break;
                case "convenios":
                    Titulo("Convenios corporativos", "Empresas, tarifas preferenciales y límites de crédito.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaConvenios.Cargar);
                    break;
                case "limpieza":
                    Titulo("Limpieza y mantenimiento", "Tareas, inspecciones, incidencias y objetos olvidados.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaLimpieza.Cargar);
                    break;
                case "consumos":
                    Titulo("Consumos y servicios", "Productos, inventario, consumos y servicios del hotel.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaConsumos.Cargar);
                    break;
                case "configuracion":
                    Titulo("Configuración del hotel", "Datos del hotel, parámetros, políticas, impuestos y módulos.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaConfiguracion.Cargar);
                    break;
                case "seguridad":
                    Titulo("Seguridad y personal", "Empleados, usuarios, roles y asignaciones.");
                    RecargarSiNecesario(vistaYaCreada, false, VistaSeguridad.Cargar);
                    break;
                default:
                    Titulo("Funcionamiento", "Ocupación, llegadas, salidas y movimientos del día.");
                    break;
            }
        }

        private void RecargarSiNecesario(bool vistaYaCreada, bool noTieneCargaAutomatica, Action cargar)
        {
            // New module controls load their initial data from their WPF Loaded event.
            // Reused controls refresh explicitly when the user returns to them.
            if (vistaYaCreada || noTieneCargaAutomatica)
                Recargar(cargar);
        }

        private UserControl ObtenerPorClave(string clave)
        {
            switch (clave)
            {
                case "reservas": return VistaReservas;
                case "habitaciones": return VistaHabitaciones;
                case "ingresos": return VistaIngresos;
                case "huespedes": return VistaHuespedes;
                case "inventario": return VistaInventario;
                case "estancias":
                case "checkin":
                case "checkout": return VistaEstancias;
                case "facturacion": return VistaFacturacion;
                case "pagos": return VistaPagos;
                case "reservasadmin": return VistaReservasAdmin;
                case "notificaciones": return VistaNotificaciones;
                case "tarifas": return VistaTarifas;
                case "convenios": return VistaConvenios;
                case "limpieza": return VistaLimpieza;
                case "consumos": return VistaConsumos;
                case "configuracion": return VistaConfiguracion;
                case "seguridad": return VistaSeguridad;
                default: return VistaFuncionamiento;
            }
        }

        private void Titulo(string titulo, string subtitulo)
        {
            TxtTitulo.Text = titulo;
            TxtSubtitulo.Text = subtitulo;
        }

        private void OnCerrarSesion(object sender, RoutedEventArgs e)
        {
            Sesion.Cerrar();
            Sesion.Rol = Rol.Recepcionista;
            NavigationService.Navigate(new Uri("/Frontend/Views/LoginView.xaml", UriKind.Relative));
        }
    }
}

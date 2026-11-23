using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
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
            { "funcionamiento", new[] { Rol.Administrador, Rol.Gerente, Rol.Recepcionista } },
            { "reservas", new[] { Rol.Administrador, Rol.Gerente, Rol.Recepcionista } },
            { "habitaciones", new[] { Rol.Administrador, Rol.Gerente, Rol.Recepcionista } },
            { "ingresos", new[] { Rol.Administrador, Rol.Gerente } },
            { "huespedes", new[] { Rol.Administrador, Rol.Gerente, Rol.Recepcionista } },
            { "inventario", new[] { Rol.Administrador, Rol.Gerente } },
            { "estancias", new[] { Rol.Administrador, Rol.Gerente, Rol.Recepcionista } },
            { "facturacion", new[] { Rol.Administrador, Rol.Gerente, Rol.Recepcionista } },
            { "pagos", new[] { Rol.Administrador, Rol.Gerente, Rol.Recepcionista } },
            { "reservasadmin", new[] { Rol.Administrador, Rol.Gerente, Rol.Recepcionista } },
            { "notificaciones", new[] { Rol.Administrador, Rol.Gerente } },
            { "tarifas", new[] { Rol.Administrador, Rol.Gerente } },
            { "convenios", new[] { Rol.Administrador, Rol.Gerente } },
            { "limpieza", new[] { Rol.Administrador, Rol.Servicios } },
            { "consumos", new[] { Rol.Administrador, Rol.Recepcionista, Rol.Servicios } },
            { "configuracion", new[] { Rol.Administrador } },
            { "seguridad", new[] { Rol.Administrador } }
        };

        private bool cargando = true;

        public DashboardView()
        {
            InitializeComponent();
        }

        private void AplicarSesion()
        {
            TxtSesionUsuario.Text = Sesion.NombreRol;
            TxtSesionDetalle.Text = Sesion.NombreUsuario ?? "Sin sesión";

            AplicarPermisos();
        }

        private void AplicarPermisos()
        {
            AplicarPermiso("funcionamiento", NavFuncionamiento);
            AplicarPermiso("reservas", NavReservas);
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

            GrupoAdministracion.Visibility = VisibleGroup(NavHuespedes, NavInventario, NavEstancias,
                NavFacturacion, NavPagos, NavReservasAdmin, NavNotificaciones);
            GrupoProducto.Visibility = VisibleGroup(NavTarifas, NavConvenios);
            GrupoServicios.Visibility = VisibleGroup(NavLimpieza, NavConsumos);
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
            if (Puede("funcionamiento"))
                return "funcionamiento";
            if (Puede("limpieza"))
                return "limpieza";
            if (Puede("consumos"))
                return "consumos";
            return "reservas";
        }

        private RadioButton NavDe(string seccion)
        {
            switch (seccion)
            {
                case "reservas": return NavReservas;
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

            VistaReservas.CambiosRealizados += OnCambiosRealizados;

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

            VistaFuncionamiento.Visibility = Visible(seccion, "funcionamiento");
            VistaReservas.Visibility = Visible(seccion, "reservas");
            VistaHabitaciones.Visibility = Visible(seccion, "habitaciones");
            VistaIngresos.Visibility = Visible(seccion, "ingresos");

            VistaHuespedes.Visibility = Visible(seccion, "huespedes");
            VistaInventario.Visibility = Visible(seccion, "inventario");
            VistaEstancias.Visibility = Visible(seccion, "estancias");
            VistaFacturacion.Visibility = Visible(seccion, "facturacion");
            VistaPagos.Visibility = Visible(seccion, "pagos");
            VistaReservasAdmin.Visibility = Visible(seccion, "reservasadmin");
            VistaNotificaciones.Visibility = Visible(seccion, "notificaciones");

            VistaTarifas.Visibility = Visible(seccion, "tarifas");
            VistaConvenios.Visibility = Visible(seccion, "convenios");
            VistaLimpieza.Visibility = Visible(seccion, "limpieza");
            VistaConsumos.Visibility = Visible(seccion, "consumos");
            VistaConfiguracion.Visibility = Visible(seccion, "configuracion");
            VistaSeguridad.Visibility = Visible(seccion, "seguridad");

            switch (seccion)
            {
                case "funcionamiento":
                    Recargar(VistaFuncionamiento.Cargar);
                    break;
                case "reservas":
                    Titulo("Reservas", "Consulta, crea y opera las reservas del hotel.");
                    Recargar(VistaReservas.Cargar);
                    break;
                case "habitaciones":
                    Titulo("Habitaciones disponibles", "Disponibilidad por fechas, tipo de capacidad y calidad.");
                    Recargar(VistaHabitaciones.Cargar);
                    break;
                case "ingresos":
                    Titulo("Cuentas de ingreso", "Resumen diario, semanal y trimestral de las cuentas del hotel.");
                    Recargar(VistaIngresos.Cargar);
                    break;
                case "huespedes":
                    Titulo("Huéspedes", "Perfiles de huéspedes y consentimientos de datos.");
                    Recargar(VistaHuespedes.Cargar);
                    break;
                case "inventario":
                    Titulo("Inventario de habitaciones", "Habitaciones, pisos, tipos, historial y bloqueos.");
                    Recargar(VistaInventario.Cargar);
                    break;
                case "estancias":
                    Titulo("Estancias y llaves", "Check-in, check-out y control de llaves.");
                    Recargar(VistaEstancias.Cargar);
                    break;
                case "facturacion":
                    Titulo("Facturación", "Folios, cargos, facturas y notas de crédito.");
                    Recargar(VistaFacturacion.Cargar);
                    break;
                case "pagos":
                    Titulo("Pagos y caja", "Cobros, devoluciones, turnos y cierre de caja.");
                    Recargar(VistaPagos.Cargar);
                    break;
                case "reservasadmin":
                    Titulo("Reservas · administración", "Canales, grupos, lista de espera y solicitudes especiales.");
                    Recargar(VistaReservasAdmin.Cargar);
                    break;
                case "notificaciones":
                    Titulo("Notificaciones y reportes", "Avisos internos y reportes programados.");
                    Recargar(VistaNotificaciones.Cargar);
                    break;
                case "tarifas":
                    Titulo("Tarifas y promociones", "Tarifas por tipo de habitación, temporadas, paquetes y promociones.");
                    Recargar(VistaTarifas.Cargar);
                    break;
                case "convenios":
                    Titulo("Convenios corporativos", "Empresas, tarifas preferenciales y límites de crédito.");
                    Recargar(VistaConvenios.Cargar);
                    break;
                case "limpieza":
                    Titulo("Limpieza y mantenimiento", "Tareas, inspecciones, incidencias y objetos olvidados.");
                    Recargar(VistaLimpieza.Cargar);
                    break;
                case "consumos":
                    Titulo("Consumos y servicios", "Productos, inventario, consumos y servicios del hotel.");
                    Recargar(VistaConsumos.Cargar);
                    break;
                case "configuracion":
                    Titulo("Configuración del hotel", "Datos del hotel, parámetros, políticas, impuestos y módulos.");
                    Recargar(VistaConfiguracion.Cargar);
                    break;
                case "seguridad":
                    Titulo("Seguridad y personal", "Empleados, usuarios, roles y asignaciones.");
                    Recargar(VistaSeguridad.Cargar);
                    break;
                default:
                    Titulo("Funcionamiento", "Ocupación, llegadas, salidas y movimientos del día.");
                    break;
            }
        }

        private static Visibility Visible(string seccion, string clave)
        {
            return seccion == clave ? Visibility.Visible : Visibility.Collapsed;
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

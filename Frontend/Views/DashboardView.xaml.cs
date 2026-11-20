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
            TxtSesionDetalle.Text = Sesion.Rol == Rol.Administrador
                ? "Acceso total al sistema"
                : "Vista de demostración";

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

            TxtFecha.Text = DateTime.Today.ToString("dd MMM yyyy", new System.Globalization.CultureInfo("es-BO"));

            VistaReservas.CambiosRealizados -= OnCambiosRealizados;
            VistaReservas.CambiosRealizados += OnCambiosRealizados;
            VistaFuncionamiento.Cargar();
            VistaHabitaciones.Cargar();
            VistaIngresos.Cargar();

            string inicial = SeccionInicial();
            NavDe(inicial).IsChecked = true;

            cargando = false;
            MostrarSeccion(inicial);
        }

        private void OnCambiosRealizados(object sender, EventArgs e)
        {
            VistaFuncionamiento.Cargar();
            VistaHabitaciones.Cargar();
            VistaIngresos.Cargar();
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
                case "reservas":
                    Titulo("Reservas", "Consulta, crea y opera las reservas del hotel.");
                    break;
                case "habitaciones":
                    Titulo("Habitaciones disponibles", "Disponibilidad por fechas, tipo de capacidad y calidad.");
                    break;
                case "ingresos":
                    Titulo("Cuentas de ingreso", "Resumen diario, semanal y trimestral de las cuentas del hotel.");
                    break;
                case "huespedes":
                    Titulo("Huéspedes", "Perfiles de huéspedes y consentimientos de datos.");
                    break;
                case "inventario":
                    Titulo("Inventario de habitaciones", "Habitaciones, pisos, tipos, historial y bloqueos.");
                    break;
                case "estancias":
                    Titulo("Estancias y llaves", "Check-in, check-out y control de llaves.");
                    break;
                case "facturacion":
                    Titulo("Facturación", "Folios, cargos, facturas y notas de crédito.");
                    break;
                case "pagos":
                    Titulo("Pagos y caja", "Cobros, devoluciones, turnos y cierre de caja.");
                    break;
                case "reservasadmin":
                    Titulo("Reservas · administración", "Canales, grupos, lista de espera y solicitudes especiales.");
                    break;
                case "notificaciones":
                    Titulo("Notificaciones y reportes", "Avisos internos y reportes programados.");
                    break;
                case "tarifas":
                    Titulo("Tarifas y promociones", "Tarifas por tipo de habitación, temporadas, paquetes y promociones.");
                    break;
                case "convenios":
                    Titulo("Convenios corporativos", "Empresas, tarifas preferenciales y límites de crédito.");
                    break;
                case "limpieza":
                    Titulo("Limpieza y mantenimiento", "Tareas, inspecciones, incidencias y objetos olvidados.");
                    break;
                case "consumos":
                    Titulo("Consumos y servicios", "Productos, inventario, consumos y servicios del hotel.");
                    break;
                case "configuracion":
                    Titulo("Configuración del hotel", "Datos del hotel, parámetros, políticas, impuestos y módulos.");
                    break;
                case "seguridad":
                    Titulo("Seguridad y personal", "Empleados, usuarios, roles y asignaciones.");
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
            Sesion.Rol = Rol.Recepcionista;
            NavigationService.Navigate(new Uri("/Frontend/Views/LoginView.xaml", UriKind.Relative));
        }
    }
}

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Notificaciones por rol, reportes programados y su historial de ejecuciones.</summary>
    public partial class NotificacionesReportesView : UserControl
    {
        private readonly AdministracionService servicio = new AdministracionService();

        public NotificacionesReportesView()
        {
            InitializeComponent();
            CmbTipo.ItemsSource = new[] { "Ocupación", "Ingresos", "Housekeeping", "Mantenimiento", "Caja" };
            CmbFormato.ItemsSource = new[] { "PDF", "Excel" };
            CmbFrecuencia.ItemsSource = new[] { "Diaria", "Semanal", "Mensual" };
            CmbEstadoEjecucion.ItemsSource = new[] { "Todos", "EXITOSA", "ERROR" };
            CmbTipo.SelectedIndex = CmbFormato.SelectedIndex = CmbFrecuencia.SelectedIndex = CmbEstadoEjecucion.SelectedIndex = 0;
        }

        private string Actor { get { return Sesion.UsuarioId.ToString(); } }

        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }

        public void Cargar()
        {
            try
            {
                int id = (ListaNotificaciones.SelectedItem as NotificacionVista) == null ? 0 : ((NotificacionVista)ListaNotificaciones.SelectedItem).Id;
                var notificaciones = servicio.Notificaciones();
                ListaNotificaciones.ItemsSource = notificaciones;
                ListaNotificaciones.SelectedItem = notificaciones.FirstOrDefault(n => n.Id == id);
                CmbRol.ItemsSource = servicio.Roles_();
                if (CmbRol.SelectedItem == null) CmbRol.SelectedIndex = 0;

                var reportes = servicio.Reportes();
                GridReportes.ItemsSource = reportes;
                CmbReporteEjecucion.ItemsSource = new object[] { "Todos" }.Concat(reportes).ToList();
                if (CmbReporteEjecucion.SelectedItem == null) CmbReporteEjecucion.SelectedIndex = 0;
                OnBuscarEjecuciones(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo leer la base de datos.\n" + ex.Message, "Notificaciones", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnSeleccion(object sender, SelectionChangedEventArgs e)
        {
            var n = ListaNotificaciones.SelectedItem as NotificacionVista;
            TxtTitulo.Text = n == null ? string.Empty : n.Titulo;
            TxtMensaje.Text = n == null ? string.Empty : n.Mensaje;
            try { GridDestinatarios.ItemsSource = servicio.Destinatarios(n); }
            catch (Exception ex) { MessageBox.Show("No se pudieron leer los destinatarios.\n" + ex.Message); }
        }

        private void Aplicar(Func<string> operacion)
        {
            string error;
            try { error = operacion(); }
            catch (Exception ex) { error = "No se pudo guardar.\n" + ex.Message; }

            if (error != null) { MessageBox.Show(error, "Notificaciones", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            Cargar();
        }

        private void OnEnviar(object sender, RoutedEventArgs e)
        {
            Aplicar(() => servicio.EnviarNotificacion(Actor, TxtNuevoTitulo.Text, TxtNuevoMensaje.Text, CmbRol.SelectedItem as RolVista));
            TxtNuevoTitulo.Clear();
            TxtNuevoMensaje.Clear();
        }

        private void OnGuardarReporte(object sender, RoutedEventArgs e)
        {
            Aplicar(() => servicio.NuevoReporte(Actor, TxtReporteNombre.Text, CmbTipo.SelectedItem as string, CmbFormato.SelectedItem as string,
                                                CmbFrecuencia.SelectedItem as string, TxtCorreo.Text, ChkActivo.IsChecked == true));
            TxtReporteNombre.Clear();
            TxtCorreo.Clear();
        }

        private void OnBuscarEjecuciones(object sender, RoutedEventArgs e)
        {
            string estado = CmbEstadoEjecucion.SelectedItem as string;
            try
            {
                GridEjecuciones.ItemsSource = servicio.Ejecuciones(CmbReporteEjecucion.SelectedItem as ReporteVista, estado == "Todos" ? null : estado, DpDesde.SelectedDate, DpHasta.SelectedDate);
            }
            catch (Exception ex) { MessageBox.Show("No se pudieron leer las ejecuciones.\n" + ex.Message); }
        }
    }
}

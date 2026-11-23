using System;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Modules
{
    public partial class NotificacionesReportesView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        public NotificacionesReportesView()
        {
            InitializeComponent();
            CmbRol.ItemsSource = new[] { "Administrador", "Gerente", "Recepcionista", "Personal de servicios" }; CmbRol.SelectedIndex = 0;
            CmbTipoReporte.ItemsSource = new[] { "Funcionamiento", "Reservas", "Ingresos", "Inventario" }; CmbTipoReporte.SelectedIndex = 0;
            CmbFormato.ItemsSource = new[] { "CSV" }; CmbFormato.SelectedIndex = 0;
            CmbFrecuencia.ItemsSource = new[] { "Diario", "Semanal", "Mensual", "Trimestral" }; CmbFrecuencia.SelectedIndex = 0;
        }
        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }
        private void Cargar()
        {
            LstNotificaciones.ItemsSource = null; LstNotificaciones.ItemsSource = DatosHotel.Notificaciones;
            GridReportes.ItemsSource = null; GridReportes.ItemsSource = DatosHotel.ReportesProgramados;
            CmbEjecutarReporte.ItemsSource = null; CmbEjecutarReporte.ItemsSource = DatosHotel.ReportesProgramados;
            CmbEjecutarReporte.DisplayMemberPath = "Nombre";
            GridEjecuciones.ItemsSource = DatosHotel.EjecucionesReportes;
        }
        private void OnSeleccionNotificacion(object sender, SelectionChangedEventArgs e)
        {
            var n = LstNotificaciones.SelectedItem as NotificacionDemo;
            TxtTituloNotificacion.Text = n == null ? "" : n.Titulo;
            TxtMensajeNotificacion.Text = n == null ? "" : n.Mensaje;
            GridDestinatarios.ItemsSource = n == null ? null : new[] { n };
        }
        private void OnEnviar(object sender, RoutedEventArgs e)
        {
            try { servicio.EnviarNotificacion(TxtNuevaNotificacion.Text, CmbRol.SelectedItem as string); TxtNuevaNotificacion.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Notificaciones", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnGuardarReporte(object sender, RoutedEventArgs e)
        {
            try { servicio.GuardarReporteProgramado(TxtNombreReporte.Text, CmbTipoReporte.SelectedItem as string, CmbFormato.SelectedItem as string,
                CmbFrecuencia.SelectedItem as string, TxtCorreoReporte.Text, ChkReporteActivo.IsChecked == true); TxtNombreReporte.Clear(); TxtCorreoReporte.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Reportes", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnSeleccionReporte(object sender, SelectionChangedEventArgs e)
        {
            var r = GridReportes.SelectedItem as ReporteProgramadoDemo;
            if (r != null) CmbEjecutarReporte.SelectedItem = r;
        }
        private void OnEjecutarReporte(object sender, RoutedEventArgs e)
        {
            var r = CmbEjecutarReporte.SelectedItem as ReporteProgramadoDemo;
            if (r == null) { MessageBox.Show("Selecciona un reporte programado activo.", "Reportes", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.EjecutarReporte(r.Nombre); Cargar(); TabsNotificaciones.SelectedIndex = 2; }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Reportes", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
    }
}

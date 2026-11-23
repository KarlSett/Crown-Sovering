using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Controls;

namespace Crown__.Frontend.Views.Modules
{
    public partial class ConveniosView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        private ConvenioDemo convenioActual;
        public ConveniosView() { InitializeComponent(); DpVence.SelectedDate = DateTime.Today.AddYears(1); }
        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }
        private void Cargar() { ListaConvenios.ItemsSource = DatosHotel.Convenios.Where(c => c.Activo).ToList(); GridSaldos.ItemsSource = DatosHotel.Convenios.ToList(); }
        private void OnNuevo(object sender, RoutedEventArgs e) { TabsConvenios.SelectedIndex = 2; TxtNombre.Focus(); }
        private void OnActualizar(object sender, RoutedEventArgs e) { Cargar(); }
        private void OnGuardar(object sender, RoutedEventArgs e)
        {
            decimal tarifa, limite;
            if (!decimal.TryParse(TxtTarifa.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out tarifa) || !decimal.TryParse(TxtLimite.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out limite))
            { MessageBox.Show("Indica tarifa y límite válidos.", "Convenios", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.CrearConvenio(TxtNombre.Text, TxtEmpresa.Text, tarifa, limite, DpVence.SelectedDate ?? DateTime.Today.AddYears(1)); TxtNombre.Clear(); TxtEmpresa.Clear(); TxtTarifa.Clear(); TxtLimite.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Convenios", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnEditar(object sender, RoutedEventArgs e)
        {
            var c = (sender as Button)?.Tag as ConvenioDemo; if (c == null) return;
            var d = DialogoRapido.Mostrar(Window.GetWindow(this), "Editar convenio", "Tarifa preferencial (" + c.TarifaPreferencial + ")", "Límite de crédito (" + c.LimiteCredito + ")", "Vencimiento (AAAA-MM-DD)");
            if (d == null) return;
            decimal tarifa, limite; DateTime fecha;
            if (!decimal.TryParse(d[0], NumberStyles.Number, CultureInfo.CurrentCulture, out tarifa) || !decimal.TryParse(d[1], NumberStyles.Number, CultureInfo.CurrentCulture, out limite) || !DateTime.TryParse(d[2], out fecha))
            { MessageBox.Show("Los valores no son válidos.", "Convenios", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.ActualizarConvenio(c.Id, tarifa, limite, fecha, true); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Convenios", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnReservas(object sender, RoutedEventArgs e)
        {
            var c = (sender as Button)?.Tag as ConvenioDemo; if (c == null) return;
            convenioActual = c;
            GridReservas.ItemsSource = DatosHotel.Reservas.Where(r => !r.ConvenioId.HasValue || r.ConvenioId.Value == c.Id).ToList();
            TabsConvenios.SelectedIndex = 0;
        }
        private void OnAsociarReserva(object sender, RoutedEventArgs e)
        {
            var reserva = GridReservas.SelectedItem as Reserva;
            if (convenioActual == null || reserva == null) { MessageBox.Show("Selecciona un convenio y una reserva.", "Convenios", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.AsociarReservaConvenio(convenioActual.Id, reserva.Id); OnReservasSeleccionConvenio(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Convenios", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnReservasSeleccionConvenio()
        {
            if (convenioActual == null) return;
            GridReservas.ItemsSource = DatosHotel.Reservas.Where(r => !r.ConvenioId.HasValue || r.ConvenioId.Value == convenioActual.Id).ToList();
        }
    }
}

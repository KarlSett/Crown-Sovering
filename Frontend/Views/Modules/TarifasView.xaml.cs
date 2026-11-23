using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Data;
using Crown__.Backend.Services;
using Crown__.Frontend.Controls;

namespace Crown__.Frontend.Views.Modules
{
    public partial class TarifasView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        public TarifasView()
        {
            InitializeComponent();
            DpTemporadaInicio.SelectedDate = DateTime.Today; DpTemporadaFin.SelectedDate = DateTime.Today.AddMonths(1);
            DpPromoInicio.SelectedDate = DateTime.Today; DpPromoFin.SelectedDate = DateTime.Today.AddMonths(1);
        }
        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }
        private void Cargar()
        {
            GridTemporadas.ItemsSource = DatosHotel.Temporadas;
            GridPaquetes.ItemsSource = DatosHotel.Paquetes;
            GridPromociones.ItemsSource = DatosHotel.Promociones;
        }
        private void OnNuevaTarifa(object sender, RoutedEventArgs e)
        {
            var d = DialogoRapido.Mostrar(Window.GetWindow(this), "Actualizar tarifa base", "Tipo de habitación (Sencilla, Doble, Familiar o Suite)", "Nueva tarifa por noche");
            if (d == null) return;
            decimal tarifa;
            if (!decimal.TryParse(d[1], NumberStyles.Number, CultureInfo.CurrentCulture, out tarifa)) { MessageBox.Show("La tarifa no es válida.", "Tarifas", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.ActualizarTarifaBase(d[0], tarifa); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Tarifas", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnNuevaTemporada(object sender, RoutedEventArgs e) { TabsTarifas.SelectedIndex = 0; TxtTemporada.Focus(); }
        private void OnNuevaPromocion(object sender, RoutedEventArgs e) { TabsTarifas.SelectedIndex = 2; TxtPromocion.Focus(); }
        private void OnGuardarTemporada(object sender, RoutedEventArgs e)
        {
            if (!DpTemporadaInicio.SelectedDate.HasValue || !DpTemporadaFin.SelectedDate.HasValue) return;
            try { servicio.RegistrarTemporada(TxtTemporada.Text, DpTemporadaInicio.SelectedDate.Value, DpTemporadaFin.SelectedDate.Value); TxtTemporada.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Temporadas", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnGuardarPaquete(object sender, RoutedEventArgs e)
        {
            decimal precio;
            if (!decimal.TryParse(TxtPrecioPaquete.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out precio)) { MessageBox.Show("Escribe un precio válido.", "Paquetes", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.RegistrarPaquete(TxtPaquete.Text, TxtDescripcionPaquete.Text, precio); TxtPaquete.Clear(); TxtDescripcionPaquete.Clear(); TxtPrecioPaquete.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Paquetes", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnGuardarPromocion(object sender, RoutedEventArgs e)
        {
            decimal descuento;
            if (!decimal.TryParse(TxtDescuento.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out descuento) || !DpPromoInicio.SelectedDate.HasValue || !DpPromoFin.SelectedDate.HasValue)
            { MessageBox.Show("Completa descuento y fechas.", "Promociones", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.RegistrarPromocion(TxtPromocion.Text, descuento, DpPromoInicio.SelectedDate.Value, DpPromoFin.SelectedDate.Value, ChkPromoActiva.IsChecked == true); TxtPromocion.Clear(); TxtDescuento.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Promociones", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
    }
}

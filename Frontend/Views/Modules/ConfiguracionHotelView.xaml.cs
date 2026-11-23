using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Data;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Modules
{
    public partial class ConfiguracionHotelView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        public ConfiguracionHotelView()
        {
            InitializeComponent();
            CmbCategoria.ItemsSource = new[] { 3, 4, 5 }; CmbMoneda.ItemsSource = new[] { "USD", "BOB", "EUR" };
            GridModulos.ItemsSource = new[] { "Funcionamiento", "Reservas", "Inventario", "Facturación", "Caja", "Servicios", "Reportes" };
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            TxtNombreHotel.Text = DatosHotel.NombreHotel; TxtDireccion.Text = DatosHotel.Direccion;
            TxtTelefono.Text = DatosHotel.TelefonoHotel; TxtCorreo.Text = DatosHotel.EmailHotel;
            CmbCategoria.SelectedItem = DatosHotel.Categoria; CmbMoneda.SelectedItem = DatosHotel.Moneda;
            Cargar();
        }
        private void Cargar()
        {
            GridConfiguraciones.ItemsSource = DatosHotel.Configuraciones.OrderBy(c => c.Clave).ToList();
            GridPoliticas.ItemsSource = DatosHotel.Politicas.ToList(); GridImpuestos.ItemsSource = DatosHotel.Impuestos.ToList();
            GridAuditoria.ItemsSource = DatosHotel.Auditoria.OrderByDescending(a => a.Fecha).ToList();
        }
        private void OnGuardarHotel(object sender, RoutedEventArgs e)
        {
            try { servicio.ActualizarHotel(TxtNombreHotel.Text, CmbCategoria.SelectedItem == null ? 0 : (int)CmbCategoria.SelectedItem,
                TxtDireccion.Text, TxtTelefono.Text, TxtCorreo.Text, CmbMoneda.SelectedItem as string); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Configuración del hotel", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnGuardarConfiguracion(object sender, RoutedEventArgs e)
        {
            try { servicio.GuardarConfiguracion(TxtClave.Text, TxtValor.Text); TxtClave.Clear(); TxtValor.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Parámetros", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnGuardarPolitica(object sender, RoutedEventArgs e)
        {
            try { servicio.GuardarPolitica(TxtPolitica.Text, TxtValorPolitica.Text, TxtDescripcionPolitica.Text); TxtPolitica.Clear(); TxtValorPolitica.Clear(); TxtDescripcionPolitica.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Políticas", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnGuardarImpuesto(object sender, RoutedEventArgs e)
        {
            decimal porcentaje;
            if (!decimal.TryParse(TxtPorcentaje.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out porcentaje)) { MessageBox.Show("El porcentaje no es válido.", "Impuestos", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.GuardarImpuesto(TxtImpuesto.Text, porcentaje, ChkImpuestoActivo.IsChecked == true); TxtImpuesto.Clear(); TxtPorcentaje.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Impuestos", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnBuscarAuditoria(object sender, RoutedEventArgs e)
        {
            string q = (TxtFiltroAudit.Text ?? "").Trim();
            GridAuditoria.ItemsSource = DatosHotel.Auditoria.Where(a => q.Length == 0 || a.Usuario.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0 || a.Cambio.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0).OrderByDescending(a => a.Fecha).ToList();
        }
    }
}

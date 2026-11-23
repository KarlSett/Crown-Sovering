using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    public partial class HuespedesView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        private bool cargando;
        public HuespedesView()
        {
            InitializeComponent();
            CmbConsentimiento.ItemsSource = new[] { "Tratamiento de datos", "Comunicaciones", "Marketing" };
            CmbConsentimiento.SelectedIndex = 0;
        }
        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }
        private void Cargar()
        {
            var actual = LstHuespedes.SelectedItem as Huesped;
            cargando = true;
            LstHuespedes.ItemsSource = servicio.ListarHuespedes().Where(h => string.IsNullOrWhiteSpace(TxtBuscar.Text) ||
                h.NombreCompleto.IndexOf(TxtBuscar.Text, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                (h.NumeroDocumento ?? "").IndexOf(TxtBuscar.Text, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
            if (actual != null) LstHuespedes.SelectedItem = LstHuespedes.Items.Cast<Huesped>().FirstOrDefault(h => h.Id == actual.Id);
            cargando = false;
            MostrarSeleccion();
        }
        private void OnBuscar(object sender, TextChangedEventArgs e) { if (IsLoaded) Cargar(); }
        private void OnBuscarClick(object sender, RoutedEventArgs e) { Cargar(); }
        private void OnSeleccionHuesped(object sender, SelectionChangedEventArgs e) { if (!cargando) MostrarSeleccion(); }
        private void MostrarSeleccion()
        {
            var h = LstHuespedes.SelectedItem as Huesped;
            TxtNombres.Text = h == null ? "" : h.Nombres;
            TxtApellidos.Text = h == null ? "" : h.Apellidos;
            TxtDocumento.Text = h == null ? "" : h.NumeroDocumento;
            TxtCorreo.Text = h == null ? "" : h.Correo;
            TxtTelefono.Text = h == null ? "" : h.Telefono;
            DpNacimiento.SelectedDate = h == null ? (DateTime?)null : h.FechaNacimiento;
            GridConsentimientos.ItemsSource = h == null ? null : servicio.ListarConsentimientos(h.Id);
        }
        private void OnNuevoHuesped(object sender, RoutedEventArgs e)
        {
            var w = new NuevoHuespedWindow { Owner = Window.GetWindow(this) };
            if (w.ShowDialog() == true) { Cargar(); LstHuespedes.SelectedItem = LstHuespedes.Items.Cast<Huesped>().FirstOrDefault(h => h.Id == w.HuespedCreado.Id); }
        }
        private void OnEditar(object sender, RoutedEventArgs e)
        {
            var h = LstHuespedes.SelectedItem as Huesped;
            if (h == null) { MessageBox.Show("Selecciona un huésped.", "Huéspedes", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.ActualizarHuesped(h.Id, TxtNombres.Text, TxtApellidos.Text, TxtDocumento.Text, TxtCorreo.Text, TxtTelefono.Text, DpNacimiento.SelectedDate); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Huéspedes", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnGuardarConsentimiento(object sender, RoutedEventArgs e)
        {
            var h = LstHuespedes.SelectedItem as Huesped;
            if (h == null) { MessageBox.Show("Selecciona un huésped.", "Consentimientos", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.RegistrarConsentimiento(h.Id, CmbConsentimiento.SelectedItem as string, ChkConsentimiento.IsChecked == true); GridConsentimientos.ItemsSource = servicio.ListarConsentimientos(h.Id); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Consentimientos", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
    }
}

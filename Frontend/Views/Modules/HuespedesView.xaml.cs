using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Huéspedes y consentimientos de datos. El alta y la edición se hacen en una ventana emergente.</summary>
    public partial class HuespedesView : UserControl
    {
        private readonly ClientesService servicio = new ClientesService();
        private List<ClienteVista> todos = new List<ClienteVista>();

        public HuespedesView()
        {
            InitializeComponent();
            CmbConsentimiento.ItemsSource = ClientesService.TiposDeConsentimiento;
            CmbConsentimiento.SelectedIndex = 0;
            DpConsentimiento.SelectedDate = DateTime.Today;
        }

        private ClienteVista Seleccionado
        {
            get { return ListaHuespedes.SelectedItem as ClienteVista; }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Cargar();
        }

        public void Cargar()
        {
            try
            {
                todos = servicio.Listar();
                TxtAviso.Text = string.Empty;
            }
            catch (Exception ex)
            {
                todos = new List<ClienteVista>();
                TxtAviso.Text = "No se pudo leer la base de datos: " + ex.Message;
            }
            Filtrar();
        }

        private void Filtrar()
        {
            var anterior = Seleccionado;
            string buscado = (TxtBuscar.Text ?? string.Empty).Trim().ToLowerInvariant();
            ListaHuespedes.ItemsSource = todos
                .Where(c => buscado.Length == 0 || c.ToString().ToLowerInvariant().Contains(buscado)
                            || (c.Telefono ?? string.Empty).Contains(buscado) || (c.Email ?? string.Empty).ToLowerInvariant().Contains(buscado))
                .ToList();
            if (anterior != null)
                ListaHuespedes.SelectedItem = ((IEnumerable<ClienteVista>)ListaHuespedes.ItemsSource).FirstOrDefault(c => c.Id == anterior.Id);
            if (ListaHuespedes.SelectedItem == null)
                MostrarFicha(null);
        }

        private void OnBuscar(object sender, RoutedEventArgs e) { Filtrar(); }
        private void OnBuscarCambia(object sender, TextChangedEventArgs e) { if (IsLoaded) Filtrar(); }

        private void OnSeleccion(object sender, SelectionChangedEventArgs e)
        {
            MostrarFicha(Seleccionado);
        }

        private void MostrarFicha(ClienteVista c)
        {
            TxtNombre.Text = c == null ? string.Empty : c.Nombre;
            TxtApellidos.Text = c == null ? string.Empty : c.Apellidos;
            TxtDocumento.Text = c == null ? string.Empty : c.Documento;
            TxtCorreo.Text = c == null ? string.Empty : c.Email;
            TxtTelefono.Text = c == null ? string.Empty : c.Telefono;
            TxtNacimiento.Text = c == null || !c.FechaNacimiento.HasValue ? string.Empty : c.FechaNacimiento.Value.ToString("dd/MM/yyyy");
            BtnEditar.IsEnabled = c != null;
            BtnEliminar.IsEnabled = c != null;
            BtnGuardarConsentimiento.IsEnabled = c != null;
            CargarConsentimientos(c);
        }

        private void CargarConsentimientos(ClienteVista c)
        {
            if (c == null) { GridConsentimientos.ItemsSource = null; return; }
            try { GridConsentimientos.ItemsSource = servicio.Consentimientos(c.Id); }
            catch (Exception ex) { TxtAviso.Text = "No se pudieron leer los consentimientos: " + ex.Message; }
        }

        private void OnNuevoHuesped(object sender, RoutedEventArgs e)
        {
            Abrir(null);
        }

        private void OnEditar(object sender, RoutedEventArgs e)
        {
            if (Seleccionado != null) Abrir(Seleccionado);
        }

        private void Abrir(ClienteVista cliente)
        {
            var ventana = new NuevoHuespedWindow(cliente) { Owner = Window.GetWindow(this) };
            if (ventana.ShowDialog() != true) return;

            Cargar();
            if (ventana.IdGuardado > 0)
                ListaHuespedes.SelectedItem = ((IEnumerable<ClienteVista>)ListaHuespedes.ItemsSource).FirstOrDefault(c => c.Id == ventana.IdGuardado);
            TxtAviso.Text = "Huésped guardado.";
        }

        private void OnEliminar(object sender, RoutedEventArgs e)
        {
            var c = Seleccionado;
            if (c == null) return;
            if (MessageBox.Show("¿Eliminar a " + c.NombreCompleto + "? Esta acción no se puede deshacer.", "Eliminar huésped",
                                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            string error;
            try { error = servicio.Eliminar(Sesion.UsuarioId.ToString(), c); }
            catch (Exception ex) { error = "No se pudo eliminar.\n" + ex.Message; }

            if (error != null) { MessageBox.Show(error, "Eliminar huésped", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            Cargar();
            TxtAviso.Text = "Huésped eliminado.";
        }

        private void OnGuardarConsentimiento(object sender, RoutedEventArgs e)
        {
            var c = Seleccionado;
            if (c == null) return;

            string error;
            try
            {
                error = servicio.GuardarConsentimiento(Sesion.UsuarioId.ToString(), c.Id, CmbConsentimiento.SelectedItem as string,
                                                       ChkAutorizado.IsChecked == true, DpConsentimiento.SelectedDate ?? DateTime.Today);
            }
            catch (Exception ex) { error = "No se pudo guardar.\n" + ex.Message; }

            if (error != null) { MessageBox.Show(error, "Consentimiento", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            CargarConsentimientos(c);
            TxtAviso.Text = "Consentimiento registrado.";
        }
    }
}

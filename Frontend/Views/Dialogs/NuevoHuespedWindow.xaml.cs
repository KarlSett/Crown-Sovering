using System;
using System.Windows;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Dialogs
{
    /// <summary>Alta y edición de un huésped. Con un huésped existente, edita sus datos.</summary>
    public partial class NuevoHuespedWindow : Window
    {
        private readonly ClienteVista existente;

        /// <summary>Identificador del huésped guardado (0 si no se guardó).</summary>
        public int IdGuardado { get; private set; }

        public NuevoHuespedWindow() : this(null)
        {
        }

        public NuevoHuespedWindow(ClienteVista huesped)
        {
            InitializeComponent();
            existente = huesped;
            if (huesped == null) return;

            Title = "Editar huésped";
            TxtNombre.Text = huesped.Nombre;
            TxtApellidos.Text = huesped.Apellidos;
            TxtDocumento.Text = huesped.Documento;
            TxtCorreo.Text = huesped.Email;
            TxtTelefono.Text = huesped.Telefono;
            DpNacimiento.SelectedDate = huesped.FechaNacimiento;
            ChkConsentimiento.Visibility = Visibility.Collapsed;
        }

        private void OnGuardar(object sender, RoutedEventArgs e)
        {
            var datos = new ClienteVista
            {
                Id = existente == null ? 0 : existente.Id,
                Nombre = TxtNombre.Text,
                Apellidos = TxtApellidos.Text,
                Documento = TxtDocumento.Text,
                Email = TxtCorreo.Text,
                Telefono = TxtTelefono.Text,
                FechaNacimiento = DpNacimiento.SelectedDate
            };

            var servicio = new ClientesService();
            string error;
            int id = 0;
            try
            {
                error = servicio.Guardar(Sesion.UsuarioId.ToString(), datos, out id);
                if (error == null && existente == null && ChkConsentimiento.IsChecked == true)
                    error = servicio.GuardarConsentimiento(Sesion.UsuarioId.ToString(), id, ClientesService.TiposDeConsentimiento[0], true, DateTime.Today);
            }
            catch (Exception ex)
            {
                error = "No se pudo guardar. " + ex.Message;
            }

            if (error != null)
            {
                TxtError.Text = error;
                return;
            }

            IdGuardado = id;
            DialogResult = true;
        }

        private void OnCancelar(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

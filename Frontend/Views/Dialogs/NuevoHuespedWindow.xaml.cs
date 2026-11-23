using System.Windows;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Dialogs
{
    /// <summary>Formulario emergente de alta de huésped en la demo activa.</summary>
    public partial class NuevoHuespedWindow : Window
    {
        public Huesped HuespedCreado { get; private set; }
        public NuevoHuespedWindow()
        {
            InitializeComponent();
        }

        private void OnGuardar(object sender, RoutedEventArgs e)
        {
            try
            {
                HuespedCreado = ServicioHotel.Instancia.CrearHuesped(TxtNombres.Text, TxtApellidos.Text,
                    TxtDocumento.Text, TxtCorreo.Text, TxtTelefono.Text, DpNacimiento.SelectedDate,
                    ChkAutoriza.IsChecked == true);
                DialogResult = true;
            }
            catch (System.InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Nuevo huésped", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnCancelar(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

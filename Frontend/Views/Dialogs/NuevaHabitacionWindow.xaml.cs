using System.Windows;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend;

namespace Crown__.Frontend.Views.Dialogs
{
    /// <summary>Formulario emergente de alta de habitación en PostgreSQL.</summary>
    public partial class NuevaHabitacionWindow : Window
    {
        private readonly AdministracionService servicio = new AdministracionService();
        public HabitacionGestion HabitacionCreada { get; private set; }

        public NuevaHabitacionWindow()
        {
            InitializeComponent();
            CmbPiso.ItemsSource = servicio.Pisos();
            CmbPiso.SelectedIndex = 0;
            CmbTipo.ItemsSource = servicio.Tipos();
            CmbTipo.SelectedIndex = 0;
        }

        private void OnGuardar(object sender, RoutedEventArgs e)
        {
            try
            {
                string error = servicio.NuevaHabitacion(Sesion.UsuarioId.ToString(),
                    CmbPiso.SelectedItem as PisoVista, CmbTipo.SelectedItem as TipoHabitacionVista, TxtNumero.Text);
                if (error != null)
                {
                    MessageBox.Show(error, "Nueva habitación", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                DialogResult = true;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Nueva habitación", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnCancelar(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

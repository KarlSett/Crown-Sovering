using System.Windows;
using System.Linq;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Dialogs
{
    /// <summary>Formulario emergente de alta de habitación en la demo activa.</summary>
    public partial class NuevaHabitacionWindow : Window
    {
        public Habitacion HabitacionCreada { get; private set; }

        public NuevaHabitacionWindow()
        {
            InitializeComponent();
            CmbPiso.ItemsSource = ServicioHotel.Instancia.ListarPisos();
            CmbPiso.SelectedIndex = 0;
            CmbTipo.ItemsSource = ServicioHotel.Instancia.ListarTiposHabitacion().Select(t => t.Nombre).ToList();
            CmbTipo.SelectedIndex = 0;
            CmbCalidad.ItemsSource = new[] { "Estándar", "Superior", "Deluxe", "Presidencial" };
            CmbCalidad.SelectedIndex = 0;
        }

        private void OnGuardar(object sender, RoutedEventArgs e)
        {
            try
            {
                HabitacionCreada = ServicioHotel.Instancia.CrearHabitacion(TxtNumero.Text,
                    (int)CmbPiso.SelectedItem, CmbTipo.SelectedItem as string, CmbCalidad.SelectedItem as string, 100m);
                DialogResult = true;
            }
            catch (System.InvalidOperationException ex)
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

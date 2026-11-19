using System.Windows;
using System.Windows.Controls;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Reservas de administración. El alta se hace en una ventana emergente.</summary>
    public partial class ReservasGestionView : UserControl
    {
        public ReservasGestionView()
        {
            InitializeComponent();
        }

        private void OnNuevaReserva(object sender, RoutedEventArgs e)
        {
            var ventana = new NuevaReservaWindow { Owner = Window.GetWindow(this) };
            ventana.ShowDialog();
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Inventario de habitaciones. El alta se hace en una ventana emergente.</summary>
    public partial class HabitacionesGestionView : UserControl
    {
        public HabitacionesGestionView()
        {
            InitializeComponent();
        }

        private void OnNuevaHabitacion(object sender, RoutedEventArgs e)
        {
            var ventana = new NuevaHabitacionWindow { Owner = Window.GetWindow(this) };
            ventana.ShowDialog();
        }
    }
}

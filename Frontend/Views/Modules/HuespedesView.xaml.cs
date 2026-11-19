using System.Windows;
using System.Windows.Controls;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Huéspedes y consentimientos. El alta se hace en una ventana emergente.</summary>
    public partial class HuespedesView : UserControl
    {
        public HuespedesView()
        {
            InitializeComponent();
        }

        private void OnNuevoHuesped(object sender, RoutedEventArgs e)
        {
            var ventana = new NuevoHuespedWindow { Owner = Window.GetWindow(this) };
            ventana.ShowDialog();
        }
    }
}

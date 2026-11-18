using System.Windows;

namespace Crown__.Frontend.Views.Dialogs
{
    /// <summary>Formulario emergente de alta de reserva. Sin persistencia todavía.</summary>
    public partial class NuevaReservaWindow : Window
    {
        public NuevaReservaWindow()
        {
            InitializeComponent();
        }

        private void OnGuardar(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnCancelar(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

using System.Windows;

namespace Crown__.Frontend.Views.Dialogs
{
    /// <summary>Formulario emergente de alta de huésped. Sin persistencia todavía.</summary>
    public partial class NuevoHuespedWindow : Window
    {
        public NuevoHuespedWindow()
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

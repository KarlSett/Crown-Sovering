using System.Windows;

namespace Crown__.Frontend.Views.Dialogs
{
    /// <summary>Formulario emergente de alta de habitación. Sin persistencia todavía.</summary>
    public partial class NuevaHabitacionWindow : Window
    {
        public NuevaHabitacionWindow()
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

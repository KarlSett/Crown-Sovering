using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Inventario de habitaciones. El alta se hace en una ventana emergente.</summary>
    public partial class HabitacionesGestionView : UserControl
    {
        public HabitacionesGestionView()
        {
            InitializeComponent();
            CmbTipoHabitacion.ItemsSource = new[] { "Sencilla", "Doble", "Familiar", "Suite" };
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (ListaPisos.SelectedIndex < 0) ListaPisos.SelectedIndex = 0;
            else CargarPiso();
        }

        private void OnPisoSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ListaHabitaciones != null) CargarPiso();
        }

        private void CargarPiso()
        {
            var rooms = ServicioHotel.Instancia.ListarDisponibilidad(DateTime.Today, DateTime.Today.AddDays(1), "Todas", "Todas")
                .Where(room => room.Piso == (ListaPisos.SelectedIndex + 1).ToString()).ToList();
            ListaHabitaciones.ItemsSource = rooms;
            MostrarDetalle(null);
        }

        private void OnRoomSelected(object sender, RoutedEventArgs e)
        {
            var radio = sender as RadioButton;
            MostrarDetalle(radio == null ? null : radio.Tag as HabitacionVista);
            if (SystemParameters.ClientAreaAnimation)
                DetalleHabitacion.BeginAnimation(OpacityProperty, new DoubleAnimation(0.65, 1, TimeSpan.FromMilliseconds(160)));
        }

        private void MostrarDetalle(HabitacionVista room)
        {
            TxtNumeroHabitacion.Text = room == null ? string.Empty : room.Numero;
            CmbTipoHabitacion.SelectedItem = room == null ? null : room.Tipo;
            DetalleHabitacion.Header = room == null ? "Selecciona una habitación para ver su detalle" :
                "Habitación " + room.Numero + " · " + room.Estado + " · " + room.Tarifa + " / noche";
        }

        private void OnNuevaHabitacion(object sender, RoutedEventArgs e)
        {
            var ventana = new NuevaHabitacionWindow { Owner = Window.GetWindow(this) };
            ventana.ShowDialog();
        }
    }
}

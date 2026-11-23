using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Dialogs
{
    public partial class NuevaReservaWindow : Window
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        private sealed class OpcionHabitacion { public Habitacion Habitacion { get; set; } public string Texto { get; set; } }
        public Reserva ReservaCreada { get; private set; }
        public NuevaReservaWindow()
        {
            InitializeComponent();
            CmbHuesped.ItemsSource = servicio.ListarHuespedes(); CmbHuesped.DisplayMemberPath = "NombreCompleto";
            if (CmbHuesped.Items.Count > 0) CmbHuesped.SelectedIndex = 0;
            CmbCanal.ItemsSource = Enum.GetValues(typeof(CanalReserva)); CmbCanal.SelectedItem = CanalReserva.Mostrador;
            DpEntrada.SelectedDate = DateTime.Today.AddDays(1); DpSalida.SelectedDate = DateTime.Today.AddDays(2);
            CargarHabitaciones();
        }
        private void OnFechasCambian(object sender, SelectionChangedEventArgs e) { if (IsLoaded) CargarHabitaciones(); }
        private void CargarHabitaciones()
        {
            if (DpEntrada.SelectedDate == null || DpSalida.SelectedDate == null) return;
            CmbHabitacion.ItemsSource = servicio.HabitacionesLibres(DpEntrada.SelectedDate.Value, DpSalida.SelectedDate.Value)
                .Select(h => new OpcionHabitacion { Habitacion = h, Texto = h.NumeroHabitacion + " · " + h.CapacidadTipo + " · " + h.Calidad }).ToList();
            CmbHabitacion.DisplayMemberPath = "Texto";
            if (CmbHabitacion.Items.Count > 0) CmbHabitacion.SelectedIndex = 0; else TxtTarifa.Clear();
        }
        private void OnHabitacion(object sender, SelectionChangedEventArgs e)
        {
            var o = CmbHabitacion.SelectedItem as OpcionHabitacion;
            TxtTarifa.Text = o == null ? "" : o.Habitacion.TarifaBase.ToString("0.00", CultureInfo.CurrentCulture);
        }
        private void OnGuardar(object sender, RoutedEventArgs e)
        {
            var huesped = CmbHuesped.SelectedItem as Huesped;
            var habitacion = CmbHabitacion.SelectedItem as OpcionHabitacion;
            int personas;
            if (huesped == null || habitacion == null || CmbCanal.SelectedItem == null || DpEntrada.SelectedDate == null || DpSalida.SelectedDate == null || !int.TryParse(TxtPersonas.Text, out personas) || personas < 1 || personas > habitacion.Habitacion.CapacidadPersonas)
            { MessageBox.Show("Completa los datos y verifica que el número de personas respete la capacidad.", "Nueva reserva", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try
            {
                ReservaCreada = servicio.CrearReserva(huesped.Id, habitacion.Habitacion.Id, DpEntrada.SelectedDate.Value, DpSalida.SelectedDate.Value,
                    (CanalReserva)CmbCanal.SelectedItem, habitacion.Habitacion.TarifaBase, TxtObservaciones.Text);
                ReservaCreada.CantidadPersonas = personas;
                DialogResult = true;
            }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Nueva reserva", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnCancelar(object sender, RoutedEventArgs e) { Close(); }
    }
}

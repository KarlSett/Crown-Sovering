using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Reservas de administración. El alta se hace en una ventana emergente.</summary>
    public partial class ReservasGestionView : UserControl
    {
        private bool ready;

        public sealed class BookingPresentation
        {
            public ReservaVista Reservation { get; set; }
            public bool IsSelected { get; set; }
            public string ShortCode { get { return "CS-" + Reservation.Codigo.Substring(Math.Max(0, Reservation.Codigo.Length - 4)); } }
            public string ShortName
            {
                get
                {
                    string[] words = Reservation.Huesped.Split(' ');
                    return words.Length < 3 ? Reservation.Huesped : words[0] + " " + words[words.Length - 2];
                }
            }
            public string RoomSummary { get { return "Hab. " + Reservation.Habitacion + " · " + Reservation.Noches + " noches"; } }
            public string AccessibleName { get { return Reservation.Codigo + ", " + Reservation.Huesped + ", " + RoomSummary + ", " + Reservation.Estado; } }
        }

        public sealed class DayPresentation
        {
            public string Name { get; set; }
            public string DayNumber { get; set; }
            public bool IsToday { get; set; }
            public List<BookingPresentation> Bookings { get; set; }
            public string CountLabel { get { return Bookings.Count == 0 ? "Sin reservas" : Bookings.Count + (Bookings.Count == 1 ? " reserva" : " reservas"); } }
        }

        public ReservasGestionView()
        {
            InitializeComponent();
            CmbCanalAgenda.ItemsSource = new[] { "Todos", "Mostrador", "Telefono", "Web", "Agencia", "Convenio", "OTA" };
            CmbEstadoAgenda.ItemsSource = new[] { "Todas", "Confirmada", "EnCurso", "Finalizada", "Cancelada", "NoShow" };
            CmbCanalAgenda.SelectedIndex = 0;
            CmbEstadoAgenda.SelectedIndex = 0;
            CmbCanalDetalle.ItemsSource = Enum.GetNames(typeof(CanalReserva));
            AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler(OnBookingSelected));
        }

        private void OnLoaded(object sender, RoutedEventArgs e) { ready = true; CargarAgenda(); }
        private void OnWorkspaceSizeChanged(object sender, SizeChangedEventArgs e)
        {
            DetailRow.Height = new GridLength(e.NewSize.Height >= 570 ? 300 : 230);
        }
        private void OnRefresh(object sender, RoutedEventArgs e) { CargarAgenda(); }
        private void OnFilterChanged(object sender, SelectionChangedEventArgs e) { if (ready) CargarAgenda(); }

        private void CargarAgenda()
        {
            // Read the existing demo data; selection never changes a reservation.
            DateTime start = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek + 6) % 7);
            var culture = CultureInfo.GetCultureInfo("es-BO");
            TxtSemana.Text = start.ToString("dd", culture) + "–" + start.AddDays(6).ToString("dd MMM", culture);
            var reservations = ServicioHotel.Instancia.ListarReservas(string.Empty,
                CmbEstadoAgenda.SelectedItem as string, CmbCanalAgenda.SelectedItem as string);
            var days = Enumerable.Range(0, 7).Select(offset =>
            {
                DateTime date = start.AddDays(offset);
                return new DayPresentation
                {
                    Name = culture.TextInfo.ToTitleCase(date.ToString("dddd", culture)),
                    DayNumber = date.ToString("dd", culture), IsToday = date == DateTime.Today,
                    Bookings = reservations.Where(item => item.Reserva.FechaCheckinEsperado.Date == date)
                        .Select(item => new BookingPresentation { Reservation = item }).ToList()
                };
            }).ToList();
            var first = days.Where(day => day.IsToday).SelectMany(day => day.Bookings).FirstOrDefault()
                ?? days.SelectMany(day => day.Bookings).FirstOrDefault();
            if (first != null) first.IsSelected = true;
            AgendaSemana.ItemsSource = days;
            MostrarDetalle(first == null ? null : first.Reservation);
        }

        private void OnBookingSelected(object sender, RoutedEventArgs e)
        {
            var radio = e.OriginalSource as RadioButton;
            var booking = radio == null ? null : radio.DataContext as BookingPresentation;
            if (booking != null)
            {
                MostrarDetalle(booking.Reservation);
                if (SystemParameters.ClientAreaAnimation && (radio.IsKeyboardFocusWithin || radio.IsMouseCaptureWithin))
                    CardReservaDetalle.BeginAnimation(OpacityProperty, new DoubleAnimation(0.8, 1, TimeSpan.FromMilliseconds(140)));
            }
        }

        private void MostrarDetalle(ReservaVista reservation)
        {
            CardReservaDetalle.Header = reservation == null ? "Sin reservas con estos filtros" : "Reserva " + reservation.Codigo;
            CmbHuespedDetalle.ItemsSource = reservation == null ? new string[0] : new[] { reservation.Huesped };
            CmbHuespedDetalle.SelectedIndex = reservation == null ? -1 : 0;
            CmbCanalDetalle.SelectedItem = reservation == null ? null : reservation.Canal;
            DpEntradaDetalle.SelectedDate = reservation == null ? (DateTime?)null : reservation.Reserva.FechaCheckinEsperado;
            DpSalidaDetalle.SelectedDate = reservation == null ? (DateTime?)null : reservation.Reserva.FechaCheckoutEsperado;
            GridHuespedesDetalle.ItemsSource = reservation == null ? null : new[] { new { Nombre = reservation.Huesped, Principal = "Principal" } };
            GridHabitacionesDetalle.ItemsSource = reservation == null ? null : new[] { reservation };
        }

        private void OnNuevaReserva(object sender, RoutedEventArgs e)
        {
            var ventana = new NuevaReservaWindow { Owner = Window.GetWindow(this) };
            ventana.ShowDialog();
        }
    }
}

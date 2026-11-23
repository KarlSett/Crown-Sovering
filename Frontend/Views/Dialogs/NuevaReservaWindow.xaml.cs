using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Dialogs
{
    /// <summary>Alta de una reserva para un huésped ya registrado, en una habitación libre.</summary>
    public partial class NuevaReservaWindow : Window
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;

        public string CodigoCreado { get; private set; }

        public NuevaReservaWindow()
        {
            InitializeComponent();
            CmbCanal.ItemsSource = Enum.GetNames(typeof(CanalReserva));
            CmbCanal.SelectedIndex = 0;
            DpEntrada.SelectedDate = DateTime.Today;
            DpSalida.SelectedDate = DateTime.Today.AddDays(1);

            try
            {
                CmbHuesped.ItemsSource = servicio.ListarHuespedes();
                if (CmbHuesped.Items.Count > 0) CmbHuesped.SelectedIndex = 0;
                RefrescarHabitaciones();
            }
            catch (Exception ex)
            {
                TxtError.Text = "No se pudo leer la base de datos: " + ex.Message;
            }
        }

        private void OnFechasCambian(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) RefrescarHabitaciones();
        }

        private void RefrescarHabitaciones()
        {
            if (!DpEntrada.SelectedDate.HasValue || !DpSalida.SelectedDate.HasValue) return;
            var libres = servicio.HabitacionesLibres(DpEntrada.SelectedDate.Value, DpSalida.SelectedDate.Value);
            CmbHabitacion.ItemsSource = libres.Select(h => new HabitacionOpcion
            {
                Id = h.Id,
                Texto = h.NumeroHabitacion + " · " + h.CapacidadTipo + " · " + Formato.Moneda(h.TarifaBase)
            }).ToList();
            CmbHabitacion.DisplayMemberPath = "Texto";
            if (CmbHabitacion.Items.Count > 0) CmbHabitacion.SelectedIndex = 0;
            else TxtTarifa.Text = string.Empty;
        }

        private void OnHabitacionCambia(object sender, SelectionChangedEventArgs e)
        {
            var opcion = CmbHabitacion.SelectedItem as HabitacionOpcion;
            var habitacion = opcion == null ? null : servicio.BuscarHabitacion(opcion.Id);
            TxtTarifa.Text = habitacion == null ? string.Empty : Formato.Moneda(habitacion.TarifaBase);
        }

        private void OnGuardar(object sender, RoutedEventArgs e)
        {
            var huesped = CmbHuesped.SelectedItem as Huesped;
            var habitacion = CmbHabitacion.SelectedItem as HabitacionOpcion;
            int personas;
            if (huesped == null) { TxtError.Text = "Selecciona el huésped principal."; return; }
            if (habitacion == null) { TxtError.Text = "No hay una habitación libre para esas fechas."; return; }
            if (!DpEntrada.SelectedDate.HasValue || !DpSalida.SelectedDate.HasValue) { TxtError.Text = "Selecciona las fechas."; return; }
            if (!int.TryParse(TxtPersonas.Text, out personas) || personas < 1) { TxtError.Text = "Escribe el número de personas."; return; }

            CanalReserva canal;
            Enum.TryParse(CmbCanal.SelectedItem as string, out canal);

            try
            {
                var creada = servicio.CrearReserva(huesped.Id, habitacion.Id, DpEntrada.SelectedDate.Value, DpSalida.SelectedDate.Value,
                                                   canal, 1m, TxtObservaciones.Text, personas);
                CodigoCreado = creada.CodigoReserva;
                DialogResult = true;
            }
            catch (InvalidOperationException ex)
            {
                TxtError.Text = ex.Message;
            }
            catch (Exception ex)
            {
                TxtError.Text = "No se pudo guardar. " + ex.Message;
            }
        }

        private void OnCancelar(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

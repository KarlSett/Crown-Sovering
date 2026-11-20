using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Sections
{
    /// <summary>Listado y operación de reservas, con alta en panel emergente.</summary>
    public partial class ReservasView : UserControl
    {
        private readonly DispatcherTimer searchDelay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        private bool cargando = true;
        private ReservaVista seleccion;

        private sealed class RoomChoice : HabitacionOpcion
        {
            public string Titulo { get; set; }
            public string Calidad { get; set; }
            public string Tarifa { get; set; }
        }

        /// <summary>Se dispara cuando una reserva cambia y el panel debe refrescarse.</summary>
        public event EventHandler CambiosRealizados;

        public ReservasView()
        {
            InitializeComponent();
            searchDelay.Tick += delegate { searchDelay.Stop(); CargarReservas(); };
            Unloaded += delegate { searchDelay.Stop(); };
            LlenarFiltros();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            cargando = false;
            Cargar();
        }

        public void Cargar()
        {
            CargarReservas();
        }

        private void LlenarFiltros()
        {
            CmbEstadoReserva.ItemsSource = new List<string>
            {
                "Todas", "Confirmada", "EnCurso", "Finalizada", "Cancelada", "NoShow"
            };
            CmbEstadoReserva.SelectedIndex = 0;

            CmbCanalReserva.ItemsSource = new List<string>
            {
                "Todos", "Mostrador", "Telefono", "Web", "Agencia", "Convenio", "OTA"
            };
            CmbCanalReserva.SelectedIndex = 0;

            CmbCanalForm.ItemsSource = new List<string>
            {
                "Mostrador", "Telefono", "Web", "Agencia", "Convenio", "OTA"
            };
            CmbCanalForm.SelectedIndex = 0;
        }

        private void CargarReservas()
        {
            if (GridReservas == null)
                return;

            string texto = TxtBuscarReserva.Text ?? string.Empty;
            string estado = CmbEstadoReserva.SelectedItem as string;
            string canal = CmbCanalReserva.SelectedItem as string;

            GridReservas.ItemsSource = servicio.ListarReservas(texto, estado, canal);
            GridReservas.SelectedItem = null;
            ActualizarSeleccion(null);
        }

        private void OnBuscarTextoChanged(object sender, TextChangedEventArgs e)
        {
            if (!cargando) { searchDelay.Stop(); searchDelay.Start(); }
        }

        private void OnFiltroComboChanged(object sender, SelectionChangedEventArgs e)
        {
            searchDelay.Stop();
            if (!cargando)
                CargarReservas();
        }

        private void OnSeleccionReserva(object sender, SelectionChangedEventArgs e)
        {
            ActualizarSeleccion(GridReservas.SelectedItem as ReservaVista);
        }

        private void ActualizarSeleccion(ReservaVista reserva)
        {
            seleccion = reserva;

            if (reserva == null)
            {
                TxtSeleccion.Text = "Selecciona una reserva para operar.";
                BtnCheckIn.IsEnabled = false;
                BtnCheckOut.IsEnabled = false;
                BtnCancelarReserva.IsEnabled = false;
                return;
            }

            TxtSeleccion.Text = reserva.Codigo + " · " + reserva.Huesped + " · Hab. " + reserva.Habitacion +
                                " · " + reserva.Noches + " noches · " + reserva.Total;
            BtnCheckIn.IsEnabled = reserva.EstadoCrudo == "Confirmada";
            BtnCheckOut.IsEnabled = reserva.EstadoCrudo == "EnCurso";
            BtnCancelarReserva.IsEnabled = reserva.EstadoCrudo == "Confirmada" || reserva.EstadoCrudo == "EnCurso";
        }

        private void OnCheckIn(object sender, RoutedEventArgs e)
        {
            if (seleccion == null)
                return;

            if (MessageBox.Show("¿Registrar la llegada de " + seleccion.Huesped + " en la habitación " +
                                seleccion.Habitacion + "?", "Registrar llegada",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                servicio.CheckIn(seleccion.Reserva.Id);
                TxtAviso.Text = "Llegada registrada · " + seleccion.Codigo + " · Hab. " + seleccion.Habitacion;
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Registrar llegada", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            NotificarCambio();
        }

        private void OnCheckOut(object sender, RoutedEventArgs e)
        {
            if (seleccion == null)
                return;

            if (MessageBox.Show("¿Registrar la salida de " + seleccion.Huesped + "? Se cobrará el saldo pendiente y la habitación pasará a limpieza.",
                                "Registrar salida", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                servicio.CheckOut(seleccion.Reserva.Id);
                TxtAviso.Text = "Salida registrada · " + seleccion.Codigo + " · Hab. " + seleccion.Habitacion;
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Registrar salida", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            NotificarCambio();
        }

        private void OnCancelarReserva(object sender, RoutedEventArgs e)
        {
            if (seleccion == null)
                return;

            if (MessageBox.Show("¿Cancelar la reserva " + seleccion.Codigo + "?", "Cancelar reserva",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                servicio.Cancelar(seleccion.Reserva.Id);
                TxtAviso.Text = "Reserva cancelada · " + seleccion.Codigo;
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Cancelar reserva", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            NotificarCambio();
        }

        private void NotificarCambio()
        {
            CargarReservas();

            var manejador = CambiosRealizados;
            if (manejador != null)
                manejador(this, EventArgs.Empty);
        }

        // ------------------------------------------------------------ Nueva reserva

        private void OnNuevaReserva(object sender, RoutedEventArgs e)
        {
            CmbHuespedForm.ItemsSource = servicio.ListarHuespedes();
            if (CmbHuespedForm.Items.Count > 0)
                CmbHuespedForm.SelectedIndex = 0;

            DpEntrada.SelectedDate = DateTime.Today.AddDays(1);
            DpSalida.SelectedDate = DateTime.Today.AddDays(2);
            CmbCanalForm.SelectedIndex = 0;
            TxtObservacionesForm.Text = string.Empty;
            TxtMensajeForm.Text = string.Empty;
            TxtTarifaForm.Text = string.Empty;
            TxtTotalForm.Text = string.Empty;

            OverlayNueva.Visibility = Visibility.Visible;
            RefrescarHabitacionesFormulario();
        }

        private void OnCerrarOverlay(object sender, RoutedEventArgs e)
        {
            OverlayNueva.Visibility = Visibility.Collapsed;
        }

        private void OnFechasReservaCambian(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && OverlayNueva.Visibility == Visibility.Visible)
                RefrescarHabitacionesFormulario();
        }

        private void RefrescarHabitacionesFormulario()
        {
            DateTime? entrada = DpEntrada.SelectedDate;
            DateTime? salida = DpSalida.SelectedDate;
            if (!entrada.HasValue || !salida.HasValue)
                return;

            DateTime inicio = entrada.Value;
            DateTime fin = salida.Value <= inicio ? inicio.AddDays(1) : salida.Value;

            var opciones = servicio.HabitacionesLibres(inicio, fin)
                .Select(h => new RoomChoice
                {
                    Id = h.Id,
                    Titulo = h.NumeroHabitacion + " · " + h.CapacidadTipo,
                    Calidad = h.Calidad + " · " + h.CapacidadPersonas + " personas",
                    Tarifa = Formato.Moneda(h.TarifaBase) + " / noche",
                    Texto = h.NumeroHabitacion + " · " + h.CapacidadTipo + " · " + h.Calidad + " · " +
                            Formato.Moneda(h.TarifaBase)
                })
                .ToList();

            CmbHabitacionForm.ItemsSource = opciones;
            if (opciones.Count > 0)
            {
                CmbHabitacionForm.SelectedIndex = 0;
                ActualizarTarifaFormulario();
                TxtMensajeForm.Text = string.Empty;
            }
            else
            {
                TxtTarifaForm.Text = string.Empty;
                TxtTotalForm.Text = string.Empty;
                TxtMensajeForm.Text = "No hay habitaciones libres en ese rango. Ajusta las fechas o filtra la disponibilidad.";
            }
        }

        private void OnHabitacionFormCambia(object sender, SelectionChangedEventArgs e)
        {
            ActualizarTarifaFormulario();
        }

        private void ActualizarTarifaFormulario()
        {
            var opcion = CmbHabitacionForm.SelectedItem as HabitacionOpcion;
            if (opcion == null)
                return;

            var habitacion = servicio.BuscarHabitacion(opcion.Id);
            if (habitacion != null)
                TxtTarifaForm.Text = habitacion.TarifaBase.ToString("0.00", CultureInfo.InvariantCulture);

            ActualizarTotalFormulario();
        }

        private void ActualizarTotalFormulario()
        {
            DateTime? entrada = DpEntrada.SelectedDate;
            DateTime? salida = DpSalida.SelectedDate;
            decimal tarifa;

            if (!entrada.HasValue || !salida.HasValue || salida.Value <= entrada.Value ||
                !LeerTarifa(TxtTarifaForm.Text, out tarifa))
            {
                TxtTotalForm.Text = string.Empty;
                return;
            }

            int noches = (int)(salida.Value - entrada.Value).TotalDays;
            TxtTotalForm.Text = "Total estimado: " + Formato.Moneda(tarifa * noches) + " · " +
                                noches + (noches == 1 ? " noche" : " noches");
        }

        private static bool LeerTarifa(string texto, out decimal tarifa)
        {
            tarifa = 0;
            if (string.IsNullOrWhiteSpace(texto))
                return false;

            return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out tarifa) ||
                   decimal.TryParse(texto, NumberStyles.Number, CultureInfo.GetCultureInfo("es-EC"), out tarifa);
        }

        private void OnGuardarReserva(object sender, RoutedEventArgs e)
        {
            if (!DpEntrada.SelectedDate.HasValue || !DpSalida.SelectedDate.HasValue)
            {
                TxtMensajeForm.Text = "Selecciona las fechas de entrada y salida.";
                return;
            }

            var huesped = CmbHuespedForm.SelectedItem as Huesped;
            if (huesped == null)
            {
                TxtMensajeForm.Text = "Selecciona el huésped de la reserva.";
                return;
            }

            var opcion = CmbHabitacionForm.SelectedItem as HabitacionOpcion;
            if (opcion == null)
            {
                TxtMensajeForm.Text = "Selecciona una habitación disponible.";
                return;
            }

            DateTime entrada = DpEntrada.SelectedDate.Value;
            DateTime salida = DpSalida.SelectedDate.Value;
            if (salida <= entrada)
            {
                TxtMensajeForm.Text = "La fecha de salida debe ser posterior a la de entrada.";
                return;
            }

            decimal tarifa;
            if (!LeerTarifa(TxtTarifaForm.Text, out tarifa) || tarifa <= 0)
            {
                TxtMensajeForm.Text = "Ingresa una tarifa válida por noche.";
                return;
            }

            CanalReserva canal;
            string textoCanal = CmbCanalForm.SelectedItem as string;
            if (!Enum.TryParse(textoCanal, out canal))
                canal = CanalReserva.Mostrador;

            try
            {
                var nueva = servicio.CrearReserva(huesped.Id, opcion.Id, entrada, salida, canal, tarifa,
                                                  TxtObservacionesForm.Text);
                OverlayNueva.Visibility = Visibility.Collapsed;
                TxtAviso.Text = "Reserva " + nueva.CodigoReserva + " creada · " + huesped.NombreCompleto +
                                " · Hab. " + ServicioHotel.NumeroHabitacion(opcion.Id);
                NotificarCambio();
            }
            catch (InvalidOperationException ex)
            {
                TxtMensajeForm.Text = ex.Message;
            }
        }
    }
}

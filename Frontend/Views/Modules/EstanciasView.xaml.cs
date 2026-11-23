using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Controls;

namespace Crown__.Frontend.Views.Modules
{
    public partial class EstanciasView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        private bool cargando;
        private sealed class OpcionReserva { public Reserva Reserva { get; set; } public string Texto { get; set; } }
        private OpcionReserva Seleccion { get; set; }
        public EstanciasView() { InitializeComponent(); }
        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }
        private void OnBuscar(object sender, RoutedEventArgs e) { if (IsLoaded) Cargar(); }
        private void Cargar()
        {
            string q = (TxtBuscar.Text ?? "").Trim();
            var opciones = DatosHotel.Reservas.Where(r => r.Estado == EstadoReserva.Confirmada || r.Estado == EstadoReserva.EnCurso)
                .Where(r => q.Length == 0 || r.CodigoReserva.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || ServicioHotel.NombreHuesped(r.HuespedId).IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0 || ServicioHotel.NumeroHabitacion(r.HabitacionId).Contains(q))
                .Select(r => new OpcionReserva { Reserva = r, Texto = r.CodigoReserva + " · " + ServicioHotel.NombreHuesped(r.HuespedId) + " · Hab. " + ServicioHotel.NumeroHabitacion(r.HabitacionId) }).ToList();
            cargando = true;
            LstLlegadas.ItemsSource = opciones.Where(o => o.Reserva.Estado == EstadoReserva.Confirmada).ToList();
            LstEnCasa.ItemsSource = opciones.Where(o => o.Reserva.Estado == EstadoReserva.EnCurso).ToList();
            LstSalidas.ItemsSource = opciones.Where(o => o.Reserva.Estado == EstadoReserva.EnCurso && o.Reserva.FechaCheckoutEsperado.Date <= DateTime.Today).ToList();
            cargando = false;
            MostrarSeleccion();
        }
        private void OnSeleccion(object sender, SelectionChangedEventArgs e)
        {
            if (cargando) return;
            var source = sender as ListBox;
            Seleccion = source == null ? null : source.SelectedItem as OpcionReserva;
            cargando = true;
            if (source != LstLlegadas) LstLlegadas.SelectedItem = null;
            if (source != LstEnCasa) LstEnCasa.SelectedItem = null;
            if (source != LstSalidas) LstSalidas.SelectedItem = null;
            cargando = false; MostrarSeleccion();
        }
        private void MostrarSeleccion()
        {
            var r = Seleccion == null ? null : Seleccion.Reserva;
            TxtReserva.Text = r == null ? "" : r.CodigoReserva + " · " + ServicioHotel.NombreHuesped(r.HuespedId) + " · Hab. " + ServicioHotel.NumeroHabitacion(r.HabitacionId) + " · " + ServicioHotel.EtiquetaEstado(r.Estado);
            GridLlaves.ItemsSource = r == null ? null : DatosHotel.Llaves.Where(k => k.ReservaId == r.Id).OrderByDescending(k => k.Emitida).ToList();
        }
        private void Operar(bool llegada)
        {
            var r = Seleccion == null ? null : Seleccion.Reserva;
            if (r == null) { MessageBox.Show("Selecciona una reserva del tablero.", "Estancias", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try
            {
                if (llegada) servicio.CheckIn(r.Id); else servicio.CheckOut(r.Id);
                Seleccion = null; Cargar();
            }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Estancias", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnCheckIn(object sender, RoutedEventArgs e) { Operar(true); }
        private void OnCheckOut(object sender, RoutedEventArgs e) { Operar(false); }
        private void OnEmitirLlave(object sender, RoutedEventArgs e)
        {
            if (Seleccion == null) return;
            try { servicio.EmitirLlave(Seleccion.Reserva.Id); MostrarSeleccion(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Llaves", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnDevolverLlave(object sender, RoutedEventArgs e)
        {
            var llave = GridLlaves.SelectedItem as LlaveDemo;
            if (llave == null) { MessageBox.Show("Selecciona una llave.", "Llaves", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.DevolverLlave(llave.Id); MostrarSeleccion(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Llaves", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnMoverHabitacion(object sender, RoutedEventArgs e)
        {
            var r = Seleccion == null ? null : Seleccion.Reserva;
            if (r == null) return;
            var datos = DialogoRapido.Mostrar(Window.GetWindow(this), "Mover de habitación", "Número de habitación destino");
            if (datos == null) return;
            var h = DatosHotel.Habitaciones.FirstOrDefault(x => x.NumeroHabitacion == datos[0]);
            if (h == null) { MessageBox.Show("No se encontró esa habitación.", "Estancias", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.CambiarHabitacionReserva(r.Id, h.Id); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Estancias", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnImprimir(object sender, RoutedEventArgs e)
        {
            if (Seleccion == null) return;
            var r = Seleccion.Reserva;
            MessageBox.Show(r.CodigoReserva + "\n" + ServicioHotel.NombreHuesped(r.HuespedId) + "\nHabitación " + ServicioHotel.NumeroHabitacion(r.HabitacionId) + "\nEntrada " + r.FechaCheckinEsperado.ToString("d") + " · Salida " + r.FechaCheckoutEsperado.ToString("d"), "Resumen de estancia", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

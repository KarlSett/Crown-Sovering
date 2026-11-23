using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Controls;

namespace Crown__.Frontend.Views.Modules
{
    public partial class ConsumosView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        private sealed class ReservaOpcion { public Reserva Reserva { get; set; } public string Texto { get; set; } }
        private sealed class ProductoOpcion { public ProductoDemo Producto { get; set; } public string Texto { get; set; } }
        public ConsumosView() { InitializeComponent(); CmbTipoServicio.ItemsSource = new[] { "Spa", "Lavandería", "Traslado", "Gimnasio", "Otro" }; CmbTipoServicio.SelectedIndex = 0; }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CmbEstancia.ItemsSource = DatosHotel.Reservas.Where(r => r.Estado == EstadoReserva.EnCurso)
                .Select(r => new ReservaOpcion { Reserva = r, Texto = r.CodigoReserva + " · " + ServicioHotel.NombreHuesped(r.HuespedId) + " · Hab. " + ServicioHotel.NumeroHabitacion(r.HabitacionId) }).ToList();
            if (CmbEstancia.Items.Count > 0) CmbEstancia.SelectedIndex = 0;
            CargarProductos(); CargarConsumos();
            GridServicios.ItemsSource = DatosHotel.Servicios;
            GridCitas.ItemsSource = DatosHotel.CitasServicios;
            GridTraslados.ItemsSource = DatosHotel.Traslados;
        }
        private void CargarProductos()
        {
            string q = (TxtBuscar.Text ?? "").Trim();
            LstProductos.ItemsSource = DatosHotel.Productos.Where(p => p.Activo && (q.Length == 0 || p.Nombre.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0))
                .Select(p => new ProductoOpcion { Producto = p, Texto = p.Nombre + " · " + p.Precio.ToString("0.00") + " · stock " + p.Stock }).ToList();
        }
        private void CargarConsumos()
        {
            var r = (CmbEstancia.SelectedItem as ReservaOpcion)?.Reserva;
            var consumos = r == null ? new System.Collections.Generic.List<ConsumoDemo>() : DatosHotel.Consumos.Where(c => c.ReservaId == r.Id).ToList();
            GridConsumos.ItemsSource = consumos;
            decimal subtotal = consumos.Sum(c => c.Total);
            TxtSubtotal.Text = subtotal.ToString("0.00"); TxtImpuesto.Text = "0.00"; TxtTotal.Text = subtotal.ToString("0.00");
        }
        private void OnBuscar(object sender, TextChangedEventArgs e) { if (IsLoaded) CargarProductos(); }
        private void OnEstancia(object sender, SelectionChangedEventArgs e) { if (IsLoaded) CargarConsumos(); }
        private void OnProducto(object sender, SelectionChangedEventArgs e) { }
        private void OnNuevoProducto(object sender, RoutedEventArgs e)
        {
            var d = DialogoRapido.Mostrar(Window.GetWindow(this), "Nuevo producto", "Nombre", "Precio unitario", "Stock inicial");
            if (d == null) return;
            decimal precio; int stock;
            if (!decimal.TryParse(d[1], NumberStyles.Number, CultureInfo.CurrentCulture, out precio) || !int.TryParse(d[2], out stock))
            { MessageBox.Show("Precio o stock no válidos.", "Productos", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.CrearProducto(d[0], precio, stock); CargarProductos(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Productos", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnRegistrarConsumo(object sender, RoutedEventArgs e)
        {
            var r = (CmbEstancia.SelectedItem as ReservaOpcion)?.Reserva;
            var p = (LstProductos.SelectedItem as ProductoOpcion)?.Producto;
            if (r == null || p == null) { MessageBox.Show("Selecciona una estancia y un producto.", "Consumos", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var d = DialogoRapido.Mostrar(Window.GetWindow(this), "Registrar consumo", "Cantidad (disponible: " + p.Stock + ")");
            if (d == null) return;
            int cantidad;
            if (!int.TryParse(d[0], out cantidad)) { MessageBox.Show("La cantidad no es válida.", "Consumos", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.RegistrarConsumo(r.Id, p.Id, cantidad); CargarProductos(); CargarConsumos(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Consumos", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnCobrar(object sender, RoutedEventArgs e)
        {
            var r = (CmbEstancia.SelectedItem as ReservaOpcion)?.Reserva;
            if (r == null) return;
            if (!DatosHotel.TurnoCajaAbierto) { MessageBox.Show("Abre un turno de caja antes de cobrar.", "Consumos", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var folio = DatosHotel.Folios.FirstOrDefault(f => f.ReservaId == r.Id && f.Estado == "Abierto");
            if (folio == null || folio.SaldoPendiente <= 0) { MessageBox.Show("El folio no tiene saldo pendiente.", "Consumos", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var d = DialogoRapido.Mostrar(Window.GetWindow(this), "Cobrar folio", "Método de pago (Efectivo, Tarjeta, Transferencia)");
            if (d == null) return;
            try { servicio.RegistrarPago(folio.Id, folio.SaldoPendiente, d[0]); MessageBox.Show("Pago registrado.", "Consumos", MessageBoxButton.OK, MessageBoxImage.Information); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Consumos", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnGuardarServicio(object sender, RoutedEventArgs e)
        {
            decimal precio;
            if (!decimal.TryParse(TxtPrecioServicio.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out precio)) { MessageBox.Show("El precio no es válido.", "Servicios", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.CrearServicio(TxtNombreServicio.Text, CmbTipoServicio.SelectedItem as string, precio, ChkServicioActivo.IsChecked == true); TxtNombreServicio.Clear(); TxtPrecioServicio.Clear(); GridServicios.ItemsSource = null; GridServicios.ItemsSource = DatosHotel.Servicios; }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Servicios", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnProgramarCita(object sender, RoutedEventArgs e)
        {
            var d = DialogoRapido.Mostrar(Window.GetWindow(this), "Programar cita", "Código de estancia activa", "Servicio", "Fecha y hora (AAAA-MM-DD HH:MM)", "Duración en minutos");
            if (d == null) return;
            DateTime fecha; int minutos;
            if (!DateTime.TryParse(d[2], CultureInfo.CurrentCulture, DateTimeStyles.None, out fecha) || !int.TryParse(d[3], out minutos))
            { MessageBox.Show("Fecha u duración inválida.", "Citas", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.ProgramarCitaServicio(d[0], d[1], fecha, minutos); GridCitas.ItemsSource = null; GridCitas.ItemsSource = DatosHotel.CitasServicios.ToList(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Citas", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnProgramarTraslado(object sender, RoutedEventArgs e)
        {
            var d = DialogoRapido.Mostrar(Window.GetWindow(this), "Programar traslado", "Código de reserva", "Origen", "Destino", "Fecha y hora");
            if (d == null) return;
            DateTime fecha;
            if (!DateTime.TryParse(d[3], CultureInfo.CurrentCulture, DateTimeStyles.None, out fecha))
            { MessageBox.Show("La fecha no es válida.", "Traslados", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.ProgramarTraslado(d[0], d[1], d[2], fecha); GridTraslados.ItemsSource = null; GridTraslados.ItemsSource = DatosHotel.Traslados.ToList(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Traslados", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
    }
}

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
    public partial class PagosCajaView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        private sealed class FolioOpcion { public Folio Folio { get; set; } public string Texto { get; set; } }
        public PagosCajaView()
        {
            InitializeComponent();
            CmbMetodo.ItemsSource = DatosHotel.MetodosPago;
            CmbMetodo.SelectedIndex = 0;
            Cargar();
        }
        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }
        private void Cargar()
        {
            TxtUsuario.Text = "Demo";
            TxtEstadoTurno.Text = DatosHotel.TurnoCajaAbierto ? "Abierto" : "Cerrado";
            TxtApertura.Text = DatosHotel.AperturaCaja.HasValue ? DatosHotel.AperturaCaja.Value.ToString("g") : "—";
            var abiertos = DatosHotel.Folios.Where(f => f.Estado == "Abierto").ToList();
            CmbFolio.ItemsSource = abiertos.Select(f => new FolioOpcion { Folio = f,
                Texto = ServicioHotel.NombreHuesped(f.HuespedId) + " · saldo " + f.SaldoPendiente.ToString("0.00") }).ToList();
            if (CmbFolio.Items.Count > 0 && CmbFolio.SelectedIndex < 0) CmbFolio.SelectedIndex = 0;
            var movimientos = DatosHotel.Transacciones.OrderByDescending(t => t.Fecha).ToList();
            GridMovimientos.ItemsSource = movimientos;
            LstPagos.ItemsSource = movimientos.Where(t => t.Abono > 0).ToList();
            GridMetodos.ItemsSource = DatosHotel.MetodosPago.ToList();
            GridDevoluciones.ItemsSource = DatosHotel.Devoluciones.OrderByDescending(d => d.Fecha).ToList();
            decimal esperado = movimientos.Where(t => t.Fecha.Date == DateTime.Today).Sum(t => t.Abono);
            TxtEsperado.Text = esperado.ToString("0.00");
            ActualizarDiferencia();
        }
        private void OnAbrirTurno(object sender, RoutedEventArgs e)
        {
            if (DatosHotel.TurnoCajaAbierto) { MessageBox.Show("Ya existe un turno abierto.", "Caja", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            DatosHotel.TurnoCajaAbierto = true; DatosHotel.AperturaCaja = DateTime.Now; Cargar();
        }
        private void OnCerrarTurno(object sender, RoutedEventArgs e)
        {
            if (!DatosHotel.TurnoCajaAbierto) { MessageBox.Show("Abre un turno antes de cerrarlo.", "Caja", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var pagos = DatosHotel.Transacciones.Where(t => t.Fecha.Date == DateTime.Today && t.Abono > 0).ToList();
            DatosHotel.CierresCaja.Add(new CierreCaja { Id = DatosHotel.CierresCaja.Count + 1, Fecha = DateTime.Now,
                TotalEfectivo = pagos.Where(t => t.MetodoPago == "Efectivo").Sum(t => t.Abono),
                TotalTarjeta = pagos.Where(t => t.MetodoPago == "Tarjeta").Sum(t => t.Abono),
                TotalTransferencia = pagos.Where(t => t.MetodoPago == "Transferencia").Sum(t => t.Abono) });
            DatosHotel.TurnoCajaAbierto = false; Cargar();
        }
        private void OnContadoChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) ActualizarDiferencia(); }
        private void ActualizarDiferencia()
        {
            decimal esperado, contado;
            if (TxtEsperado == null || TxtDiferencia == null) return;
            decimal.TryParse(TxtEsperado.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out esperado);
            decimal.TryParse(TxtContado.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out contado);
            TxtDiferencia.Text = (contado - esperado).ToString("0.00");
        }
        private void OnRegistrarPago(object sender, RoutedEventArgs e)
        {
            if (!DatosHotel.TurnoCajaAbierto) { MessageBox.Show("Abre el turno de caja antes de registrar pagos.", "Caja", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var opcion = CmbFolio.SelectedItem as FolioOpcion;
            decimal monto;
            if (opcion == null || !decimal.TryParse(TxtMonto.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out monto))
            { MessageBox.Show("Selecciona un folio y escribe un monto válido.", "Registrar pago", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.RegistrarPago(opcion.Folio.Id, monto, CmbMetodo.SelectedItem as string); TxtMonto.Clear(); TxtMotivo.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Registrar pago", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnGuardarMetodo(object sender, RoutedEventArgs e)
        {
            string metodo = TxtMetodoNuevo.Text.Trim();
            if (metodo.Length == 0) { MessageBox.Show("Escribe el nombre del método.", "Métodos de pago", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            if (!DatosHotel.MetodosPago.Any(m => string.Equals(m, metodo, StringComparison.OrdinalIgnoreCase))) DatosHotel.MetodosPago.Add(metodo);
            TxtMetodoNuevo.Clear(); CmbMetodo.ItemsSource = null; CmbMetodo.ItemsSource = DatosHotel.MetodosPago; CmbMetodo.SelectedIndex = 0; Cargar();
        }
        private void OnNuevaDevolucion(object sender, RoutedEventArgs e)
        {
            var pago = GridMovimientos.SelectedItem as Transaccion;
            if (pago == null || pago.Abono <= 0) { MessageBox.Show("Selecciona un pago en Movimientos de caja.", "Devoluciones", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var d = DialogoRapido.Mostrar(Window.GetWindow(this), "Registrar devolución", "Monto (pago original: " + pago.Abono.ToString("0.00") + ")", "Motivo");
            if (d == null) return;
            decimal monto;
            if (!decimal.TryParse(d[0], NumberStyles.Number, CultureInfo.CurrentCulture, out monto)) { MessageBox.Show("El monto no es válido.", "Devoluciones", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.CrearDevolucion(pago.Id, monto, d[1]); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Devoluciones", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
    }
}

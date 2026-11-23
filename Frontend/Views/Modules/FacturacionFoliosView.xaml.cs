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
    public partial class FacturacionFoliosView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        private sealed class OpcionFolio { public Folio Folio { get; set; } public string Texto { get; set; } }
        public FacturacionFoliosView()
        {
            InitializeComponent(); CmbConcepto.ItemsSource = DatosHotel.ConceptosCargos.Select(c => c.Nombre).ToList(); CmbConcepto.SelectedIndex = 0;
        }
        private OpcionFolio Seleccion { get { return LstFolios.SelectedItem as OpcionFolio; } }
        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }
        private void OnBuscar(object sender, RoutedEventArgs e) { if (IsLoaded) Cargar(); }
        private void Cargar()
        {
            var anterior = Seleccion == null ? (Guid?)null : Seleccion.Folio.Id;
            string busqueda = TxtBuscar.Text == null ? "" : TxtBuscar.Text.Trim();
            var opciones = DatosHotel.Folios.Where(f => f.Estado == "Abierto" ||
                    DatosHotel.Transacciones.Any(t => t.FolioId == f.Id && t.Cargo > 0))
                .Where(f => ServicioHotel.NombreHuesped(f.HuespedId).IndexOf(busqueda, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    f.Id.ToString().IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(f => f.Estado == "Abierto").Select(f => new OpcionFolio { Folio = f,
                    Texto = ServicioHotel.NombreHuesped(f.HuespedId) + " · " + f.Estado + " · " + f.SaldoPendiente.ToString("0.00") }).ToList();
            LstFolios.ItemsSource = opciones;
            if (anterior.HasValue) LstFolios.SelectedItem = opciones.FirstOrDefault(o => o.Folio.Id == anterior.Value);
            if (LstFolios.SelectedItem == null && opciones.Count > 0) LstFolios.SelectedIndex = 0;
            GridFacturas.ItemsSource = DatosHotel.Facturas;
            GridNotas.ItemsSource = DatosHotel.NotasCredito;
            GridConceptos.ItemsSource = DatosHotel.ConceptosCargos;
            var conceptoSeleccionado = CmbConcepto.SelectedItem as string;
            CmbConcepto.ItemsSource = DatosHotel.ConceptosCargos.Select(c => c.Nombre).ToList();
            if (!string.IsNullOrWhiteSpace(conceptoSeleccionado)) CmbConcepto.SelectedItem = conceptoSeleccionado;
            if (CmbConcepto.SelectedIndex < 0 && CmbConcepto.Items.Count > 0) CmbConcepto.SelectedIndex = 0;
            CmbFactura.ItemsSource = DatosHotel.Facturas;
            MostrarSeleccion();
        }
        private void OnSeleccionFolio(object sender, SelectionChangedEventArgs e) { MostrarSeleccion(); }
        private void MostrarSeleccion()
        {
            var o = Seleccion;
            TxtHuesped.Text = o == null ? "" : ServicioHotel.NombreHuesped(o.Folio.HuespedId);
            TxtEstadoFolio.Text = o == null ? "" : o.Folio.Estado;
            TxtSaldo.Text = o == null ? "" : o.Folio.SaldoPendiente.ToString("0.00");
            GridCargos.ItemsSource = o == null ? null : DatosHotel.Transacciones.Where(t => t.FolioId == o.Folio.Id && t.Cargo > 0).OrderByDescending(t => t.Fecha).ToList();
        }
        private void OnNuevoFolio(object sender, RoutedEventArgs e)
        {
            var entrada = DialogoRapido.Mostrar(Window.GetWindow(this), "Nuevo folio", "Código de reserva (p. ej. CS-2026-0001)");
            if (entrada == null) return;
            var reserva = DatosHotel.Reservas.FirstOrDefault(r => string.Equals(r.CodigoReserva, entrada[0], StringComparison.OrdinalIgnoreCase));
            if (reserva == null) { MessageBox.Show("No se encontró esa reserva.", "Nuevo folio", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            servicio.CrearFolio(reserva.Id); Cargar();
        }
        private void OnAgregarCargo(object sender, RoutedEventArgs e)
        {
            var o = Seleccion; decimal precio; int cantidad;
            if (o == null || !decimal.TryParse(TxtPrecio.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out precio) ||
                !int.TryParse(TxtCantidad.Text, out cantidad) || cantidad < 1)
            { MessageBox.Show("Selecciona un folio abierto e indica cantidad y precio válidos.", "Cargos", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            string concepto = (CmbConcepto.SelectedItem as string) ?? "Cargo";
            string detalle = string.IsNullOrWhiteSpace(TxtDescripcionCargo.Text) ? concepto : concepto + " · " + TxtDescripcionCargo.Text.Trim();
            try { servicio.RegistrarCargo(o.Folio.Id, detalle, precio * cantidad, cantidad); TxtDescripcionCargo.Clear(); TxtPrecio.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Cargos", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnEmitirFactura(object sender, RoutedEventArgs e)
        {
            var o = Seleccion;
            if (o == null) { MessageBox.Show("Selecciona un folio.", "Facturación", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var factura = servicio.EmitirFactura(o.Folio.Id); Cargar(); CmbFactura.SelectedItem = factura;
        }
        private void OnGuardarConcepto(object sender, RoutedEventArgs e)
        {
            try { servicio.CrearConceptoCargo(TxtNuevoConcepto.Text, TxtCategoriaConcepto.Text); TxtNuevoConcepto.Clear(); TxtCategoriaConcepto.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Conceptos de cargo", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnCrearNota(object sender, RoutedEventArgs e)
        {
            if (CmbFactura.Items.Count == 0) { MessageBox.Show("Emite una factura antes de crear una nota de crédito.", "Nota de crédito", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            CmbFactura.SelectedIndex = CmbFactura.Items.Count - 1;
            MessageBox.Show("Selecciona la factura, indica motivo y monto en la pestaña Notas de crédito y pulsa Crear.", "Nota de crédito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void OnGuardarNota(object sender, RoutedEventArgs e)
        {
            var factura = CmbFactura.SelectedItem as FacturaDemo; decimal monto;
            if (factura == null || !decimal.TryParse(TxtMontoNC.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out monto))
            { MessageBox.Show("Selecciona factura e indica monto válido.", "Nota de crédito", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.CrearNotaCredito(factura.Numero, TxtMotivoNC.Text, monto); TxtMotivoNC.Clear(); TxtMontoNC.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Nota de crédito", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnTransferirCargo(object sender, RoutedEventArgs e)
        {
            var cargo = GridCargos.SelectedItem as Transaccion; var origen = Seleccion;
            if (cargo == null || origen == null) { MessageBox.Show("Selecciona un cargo del folio.", "Transferir cargo", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var destino = DatosHotel.Folios.Where(f => f.Estado == "Abierto" && f.Id != origen.Folio.Id).ToList();
            if (destino.Count == 0) { MessageBox.Show("No hay otro folio abierto. Crea uno para la reserva de destino.", "Transferir cargo", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var ventana = new Window { Title = "Folio de destino", Width = 360, Height = 150, Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize };
            var panel = new StackPanel { Margin = new Thickness(12) }; var combo = new ComboBox { ItemsSource = destino, DisplayMemberPath = "TipoFolio", Margin = new Thickness(0, 0, 0, 10) };
            combo.SelectedIndex = 0; var btn = new Button { Content = "Transferir", IsDefault = true, MinWidth = 90, HorizontalAlignment = HorizontalAlignment.Right };
            btn.Click += (s, a) => ventana.DialogResult = true; panel.Children.Add(combo); panel.Children.Add(btn); ventana.Content = panel;
            if (ventana.ShowDialog() == true) { try { servicio.TransferirCargo(cargo.Id, ((Folio)combo.SelectedItem).Id); Cargar(); } catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Transferir cargo", MessageBoxButton.OK, MessageBoxImage.Information); } }
        }
    }
}

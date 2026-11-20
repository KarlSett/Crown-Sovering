using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Turno de caja, cobros, movimientos, cierre y métodos de pago.</summary>
    public partial class PagosCajaView : UserControl
    {
        private readonly CajaService caja = new CajaService();
        private readonly FacturacionService facturacion = new FacturacionService();
        private readonly FolioService folios = new FolioService();
        private TurnoVista turno;

        public PagosCajaView()
        {
            InitializeComponent();
            CmbTipoMovimiento.ItemsSource = new[] { "INGRESO", "EGRESO" };
            CmbTipoMovimiento.SelectedIndex = 0;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Cargar();
        }

        public void Cargar()
        {
            try
            {
                turno = caja.TurnoAbierto(Sesion.UsuarioId.ToString());
                TxtUsuario.Text = Sesion.NombreUsuario;
                TxtApertura.Text = turno == null ? "—" : turno.Apertura.ToString("dd/MM/yyyy HH:mm");
                TxtEstadoTurno.Text = turno == null ? "Sin turno abierto" : "Abierto";
                BtnAbrirTurno.IsEnabled = turno == null;
                BtnCerrarTurno.IsEnabled = turno != null;
                TxtEsperado.Text = turno == null ? string.Empty : Formato.Moneda(turno.Esperado);

                ListaPagos.ItemsSource = caja.Pagos(turno);
                GridMovimientos.ItemsSource = caja.Movimientos(turno);
                GridMetodos.ItemsSource = caja.Metodos();
                CmbMetodo.ItemsSource = caja.Metodos().Where(m => m.Activo).ToList();
                if (CmbMetodo.Items.Count > 0) CmbMetodo.SelectedIndex = 0;
                GridDevoluciones.ItemsSource = caja.Devoluciones();
                CmbFolio.ItemsSource = facturacion.Folios().Where(f => f.Estado == "Abierto" && f.Saldo > 0).ToList();
                TxtAviso.Text = string.Empty;
            }
            catch (Exception ex)
            {
                TxtAviso.Text = "No se pudo leer la base de datos: " + ex.Message;
            }
            OnContadoCambia(null, null);
        }

        private static bool Numero(string texto, out decimal valor)
        {
            return decimal.TryParse((texto ?? string.Empty).Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
        }

        private void Aplicar(Func<string> operacion, string exito)
        {
            string error;
            try { error = operacion(); }
            catch (Exception ex) { error = "No se pudo guardar.\n" + ex.Message; }

            if (error != null) { MessageBox.Show(error, "Pagos y caja", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            Backend.Data.DatosHotel.Refrescar();
            Cargar();
            TxtAviso.Text = exito;
        }

        private void OnAbrirTurno(object sender, RoutedEventArgs e)
        {
            Aplicar(() => caja.AbrirTurno(Sesion.UsuarioId.ToString()), "Turno abierto.");
        }

        private void OnContadoCambia(object sender, TextChangedEventArgs e)
        {
            decimal contado;
            if (turno == null || !Numero(TxtContado.Text, out contado)) { TxtDiferencia.Text = string.Empty; return; }
            TxtDiferencia.Text = Formato.Moneda(contado - turno.Esperado);
        }

        private void OnCerrarTurno(object sender, RoutedEventArgs e)
        {
            decimal contado;
            if (!Numero(TxtContado.Text, out contado))
            {
                MessageBox.Show("Escribe el efectivo contado en el cierre de caja.", "Pagos y caja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show("¿Cerrar el turno con Bs " + contado.ToString("0.00", CultureInfo.InvariantCulture) + " contados?", "Pagos y caja",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            var cerrado = turno;
            Aplicar(() => caja.CerrarTurno(Sesion.UsuarioId.ToString(), cerrado, contado), "Turno cerrado.");
            TxtContado.Clear();
        }

        private void OnRegistrarPago(object sender, RoutedEventArgs e)
        {
            decimal monto;
            var folio = CmbFolio.SelectedItem as FolioItem;
            var metodo = CmbMetodo.SelectedItem as MetodoPagoVista;
            if (folio == null || metodo == null || !Numero(TxtMontoPago.Text, out monto))
            {
                MessageBox.Show("Elige el folio, el método de pago y escribe el monto.", "Pagos y caja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Aplicar(() => folios.RegistrarPago(Sesion.UsuarioId.ToString(), folio.Reserva, monto, metodo.Nombre, TxtReferencia.Text), "Pago registrado.");
            TxtMontoPago.Clear();
            TxtReferencia.Clear();
        }

        private void OnGuardarMetodo(object sender, RoutedEventArgs e)
        {
            Aplicar(() => caja.GuardarMetodo(Sesion.UsuarioId.ToString(), TxtMetodoNombre.Text, ChkMetodoActivo.IsChecked == true), "Método guardado.");
            TxtMetodoNombre.Clear();
        }

        private void OnRegistrarMovimiento(object sender, RoutedEventArgs e)
        {
            decimal monto;
            if (!Numero(TxtMontoMovimiento.Text, out monto))
            {
                MessageBox.Show("Escribe el monto como número, por ejemplo 50.", "Pagos y caja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Aplicar(() => caja.RegistrarMovimiento(Sesion.UsuarioId.ToString(), turno, CmbTipoMovimiento.SelectedItem as string, monto, TxtMotivoMovimiento.Text), "Movimiento registrado.");
            TxtMontoMovimiento.Clear();
            TxtMotivoMovimiento.Clear();
        }
    }
}

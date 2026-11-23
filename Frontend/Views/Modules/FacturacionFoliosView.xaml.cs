using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Folios de las estancias: cargos, conceptos, facturas y notas de crédito.</summary>
    public partial class FacturacionFoliosView : UserControl
    {
        private readonly FacturacionService servicio = new FacturacionService();
        private List<FolioItem> todos = new List<FolioItem>();

        public FacturacionFoliosView()
        {
            InitializeComponent();
        }

        private FolioItem Folio { get { return ListaFolios.SelectedItem as FolioItem; } }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Cargar();
        }

        public void Cargar()
        {
            try
            {
                todos = servicio.Folios();
                var conceptos = servicio.Conceptos();
                CmbConcepto.ItemsSource = conceptos;
                GridConceptos.ItemsSource = conceptos;
                TxtAviso.Text = string.Empty;
            }
            catch (Exception ex)
            {
                todos = new List<FolioItem>();
                TxtAviso.Text = "No se pudo leer la base de datos: " + ex.Message;
            }
            Filtrar();
        }

        private void Filtrar()
        {
            var anterior = Folio;
            string buscado = (TxtBuscar.Text ?? string.Empty).Trim().ToLowerInvariant();
            ListaFolios.ItemsSource = todos.Where(f => buscado.Length == 0 || f.ToString().ToLowerInvariant().Contains(buscado)).ToList();
            if (anterior != null)
                ListaFolios.SelectedItem = ((IEnumerable<FolioItem>)ListaFolios.ItemsSource).FirstOrDefault(f => f.FolioId == anterior.FolioId);
            if (ListaFolios.SelectedItem == null) Mostrar(null);
        }

        private void OnBuscar(object sender, RoutedEventArgs e) { Filtrar(); }
        private void OnBuscarCambia(object sender, TextChangedEventArgs e) { if (IsLoaded) Filtrar(); }
        private void OnSeleccionFolio(object sender, SelectionChangedEventArgs e) { Mostrar(Folio); }

        private void Mostrar(FolioItem folio)
        {
            TxtHuesped.Text = folio == null ? string.Empty : folio.Huesped;
            TxtEstado.Text = folio == null ? string.Empty : folio.Estado;
            TxtSaldo.Text = folio == null ? string.Empty : Formato.Moneda(folio.Saldo);
            bool abierto = folio != null && folio.Estado == "Abierto";
            BtnAgregarCargo.IsEnabled = abierto;
            BtnEmitirFactura.IsEnabled = folio != null;
            BtnNuevoFolio.IsEnabled = abierto;
            BtnTransferir.IsEnabled = abierto;

            if (folio == null)
            {
                GridCargos.ItemsSource = GridFacturas.ItemsSource = GridNotas.ItemsSource = CmbFacturaNota.ItemsSource = null;
                BtnNotaCredito.IsEnabled = BtnCrearNota.IsEnabled = false;
                return;
            }

            try
            {
                GridCargos.ItemsSource = servicio.Cargos(folio.FolioId);
                var facturas = servicio.Facturas(folio.FolioId);
                GridFacturas.ItemsSource = facturas;
                CmbFacturaNota.ItemsSource = facturas;
                if (facturas.Count > 0) CmbFacturaNota.SelectedIndex = 0;
                GridNotas.ItemsSource = servicio.NotasCredito(folio.FolioId);
                BtnNotaCredito.IsEnabled = BtnCrearNota.IsEnabled = facturas.Count > 0;
            }
            catch (Exception ex)
            {
                TxtAviso.Text = "No se pudo leer el folio: " + ex.Message;
            }
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

            if (error != null) { MessageBox.Show(error, "Facturación", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            Backend.Data.DatosHotel.Refrescar();
            TxtAviso.Text = exito;
            var actual = Folio;
            int id = actual == null ? 0 : actual.FolioId;
            Cargar();
            if (id > 0) ListaFolios.SelectedItem = ((IEnumerable<FolioItem>)ListaFolios.ItemsSource).FirstOrDefault(f => f.FolioId == id);
        }

        private void OnNuevoFolio(object sender, RoutedEventArgs e)
        {
            var folio = Folio;
            if (folio == null) return;
            var v = FormularioWindow.Pedir(Window.GetWindow(this), "Nuevo folio · " + folio.Reserva, Campo.Texto("Nombre del folio", "Empresa"));
            if (v == null) return;
            Aplicar(() => servicio.NuevoFolio(Sesion.UsuarioId.ToString(), folio, (string)v[0]), "Folio creado.");
        }

        private void OnTransferir(object sender, RoutedEventArgs e)
        {
            var origen = Folio;
            var cargo = GridCargos.SelectedItem as CargoVista;
            if (origen == null || cargo == null)
            {
                MessageBox.Show("Selecciona un cargo de la tabla de cargos.", "Facturación", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var destinos = todos.Where(f => f.Reserva == origen.Reserva && f.FolioId != origen.FolioId && f.Estado == "Abierto").Cast<object>().ToList();
            if (destinos.Count == 0)
            {
                MessageBox.Show("La estancia no tiene otro folio abierto. Crea uno con Nuevo folio.", "Facturación", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var v = FormularioWindow.Pedir(Window.GetWindow(this), "Transferir " + cargo.Concepto + " " + cargo.ImporteTexto, Campo.Lista("Folio de destino", destinos));
            if (v == null) return;
            Aplicar(() => servicio.TransferirCargo(Sesion.UsuarioId.ToString(), origen, cargo, v[0] as FolioItem), "Cargo transferido.");
        }

        private void OnAgregarCargo(object sender, RoutedEventArgs e)
        {
            decimal cantidad, precio;
            if (!Numero(TxtCantidad.Text, out cantidad) || !Numero(TxtPrecio.Text, out precio))
            {
                MessageBox.Show("Escribe la cantidad y el precio como números, por ejemplo 2 y 35.50.", "Facturación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Aplicar(() => servicio.AgregarCargo(Sesion.UsuarioId.ToString(), Folio, CmbConcepto.SelectedItem as ConceptoVista, TxtDescripcion.Text, cantidad, precio), "Cargo registrado.");
            TxtDescripcion.Clear();
            TxtPrecio.Clear();
        }

        private void OnGuardarConcepto(object sender, RoutedEventArgs e)
        {
            string error;
            try { error = servicio.GuardarConcepto(Sesion.UsuarioId.ToString(), TxtConceptoNombre.Text, TxtConceptoCategoria.Text); }
            catch (Exception ex) { error = "No se pudo guardar.\n" + ex.Message; }

            if (error != null) { MessageBox.Show(error, "Conceptos", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            TxtConceptoNombre.Clear();
            TxtConceptoCategoria.Clear();
            Cargar();
            TxtAviso.Text = "Concepto guardado.";
        }

        private void OnEmitirFactura(object sender, RoutedEventArgs e)
        {
            var folio = Folio;
            if (folio == null) return;
            if (MessageBox.Show("¿Emitir la factura de los cargos pendientes del folio de " + folio.Huesped + "?", "Facturación",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            Aplicar(() => servicio.EmitirFactura(Sesion.UsuarioId.ToString(), folio), "Factura emitida.");
        }

        private void OnNotaCredito(object sender, RoutedEventArgs e)
        {
            decimal monto;
            if (!Numero(TxtMontoNota.Text, out monto))
            {
                MessageBox.Show("Escribe el monto de la nota de crédito en la pestaña Notas de crédito.", "Facturación", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Aplicar(() => servicio.CrearNotaCredito(Sesion.UsuarioId.ToString(), CmbFacturaNota.SelectedItem as FacturaVista, TxtMotivoNota.Text, monto), "Nota de crédito creada.");
            TxtMotivoNota.Clear();
            TxtMontoNota.Clear();
        }
    }
}

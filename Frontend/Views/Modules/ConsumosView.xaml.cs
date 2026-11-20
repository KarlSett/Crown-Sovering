using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Consumos de productos a la estancia, catálogo de servicios, citas, traslados e inventario.</summary>
    public partial class ConsumosView : UserControl
    {
        private const decimal TasaImpuesto = 0.13m;

        private readonly ServicioService servicio = new ServicioService();
        private List<Producto> productos = new List<Producto>();
        private decimal totalConsumo;

        public ConsumosView()
        {
            InitializeComponent();
            CmbServicioTipo.ItemsSource = new[] { "Spa", "Gimnasio", "Transporte", "Lavandería", "Otro" };
            CmbServicioTipo.SelectedIndex = 0;
            BtnNuevoProducto.Visibility = Sesion.Rol == Rol.Administrador || Sesion.Rol == Rol.Gerente ? Visibility.Visible : Visibility.Collapsed;
        }

        private ReservaRegistro Estancia { get { return CmbEstancia.SelectedItem as ReservaRegistro; } }
        private Producto Producto { get { return ListaProductos.SelectedItem as Producto; } }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Cargar();
        }

        public void Cargar()
        {
            try
            {
                int anterior = Estancia == null ? 0 : Estancia.Id;
                var alojados = new ReservaService().Listar().Where(r => r.Estado == "Check-In").ToList();
                CmbEstancia.ItemsSource = alojados;
                CmbEstancia.SelectedItem = alojados.FirstOrDefault(r => r.Id == anterior) ?? alojados.FirstOrDefault();

                // los insumos de cocina no se cobran a un huésped
                productos = servicio.Productos().Where(p => p.Categoria != "Insumo").ToList();
                FiltrarProductos();
                GridServicios.ItemsSource = servicio.Servicios();
                GridCitas.ItemsSource = servicio.Citas();
                GridTraslados.ItemsSource = new TrasladoService().Todos();
                GridInventario.ItemsSource = servicio.MovimientosInventario();
                TxtAviso.Text = alojados.Count == 0 ? "No hay huéspedes alojados: los consumos se cargan a estancias en curso." : string.Empty;
            }
            catch (Exception ex)
            {
                TxtAviso.Text = "No se pudo leer la base de datos: " + ex.Message;
            }
            MostrarConsumo();
        }

        private void FiltrarProductos()
        {
            string buscado = (TxtBuscar.Text ?? string.Empty).Trim().ToLowerInvariant();
            ListaProductos.ItemsSource = productos.Where(p => buscado.Length == 0 || p.Nombre.ToLowerInvariant().Contains(buscado)).ToList();
            ListaProductos.DisplayMemberPath = null;
        }

        private void OnBuscarCambia(object sender, TextChangedEventArgs e) { if (IsLoaded) FiltrarProductos(); }
        private void OnEstanciaCambia(object sender, SelectionChangedEventArgs e) { MostrarConsumo(); }

        private void MostrarConsumo()
        {
            var estancia = Estancia;
            totalConsumo = 0;
            GridConsumo.ItemsSource = null;
            if (estancia != null)
            {
                try
                {
                    var lineas = servicio.Consumos(estancia.Id);
                    GridConsumo.ItemsSource = lineas;
                    totalConsumo = lineas.Sum(l => l.Total);
                }
                catch (Exception ex) { TxtAviso.Text = "No se pudo leer el consumo: " + ex.Message; }
            }

            decimal impuesto = Math.Round(totalConsumo * TasaImpuesto / (1 + TasaImpuesto), 2);
            TxtSubtotal.Text = Formato.Moneda(totalConsumo - impuesto);
            TxtImpuesto.Text = Formato.Moneda(impuesto);
            TxtTotal.Text = Formato.Moneda(totalConsumo);
            BtnCobrar.IsEnabled = estancia != null && totalConsumo > 0;
        }

        private void Aplicar(Func<string> operacion, string exito)
        {
            string error;
            try { error = operacion(); }
            catch (Exception ex) { error = "No se pudo guardar.\n" + ex.Message; }

            if (error != null) { MessageBox.Show(error, "Consumos y servicios", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            Backend.Data.DatosHotel.Refrescar();
            Cargar();
            TxtAviso.Text = exito;
        }

        private void OnRegistrarConsumo(object sender, RoutedEventArgs e)
        {
            int cantidad;
            if (Estancia == null || Producto == null || !int.TryParse(TxtCantidad.Text, out cantidad))
            {
                MessageBox.Show("Elige la estancia, un producto del catálogo y escribe la cantidad.", "Consumos y servicios", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var estancia = Estancia;
            var producto = Producto;
            Aplicar(() => servicio.ConsumirProducto(Sesion.UsuarioId.ToString(), estancia.Codigo, producto.Id, cantidad),
                    "Consumo cargado al folio · " + producto.Nombre + " x" + cantidad);
        }

        // El consumo ya está en el folio; cobrar registra el pago en efectivo de ese importe.
        private void OnCobrar(object sender, RoutedEventArgs e)
        {
            var estancia = Estancia;
            if (estancia == null) return;
            if (MessageBox.Show("¿Cobrar Bs " + totalConsumo.ToString("0.00", CultureInfo.InvariantCulture) + " en efectivo?", "Consumos y servicios",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            decimal total = totalConsumo;
            Aplicar(() => servicio.CobrarConsumo(Sesion.UsuarioId.ToString(), estancia.Codigo, total), "Cobro registrado.");
        }

        private void OnNuevoProducto(object sender, RoutedEventArgs e)
        {
            var ventana = new Window { Title = "Nuevo producto", Width = 340, Height = 330, ResizeMode = ResizeMode.NoResize,
                                       WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Window.GetWindow(this) };
            var panel = new StackPanel { Margin = new Thickness(16) };
            var nombre = new TextBox(); var categoria = new TextBox(); var precio = new TextBox(); var stock = new TextBox { Text = "0" };
            foreach (var par in new[] { Tuple.Create("Nombre", nombre), Tuple.Create("Categoría", categoria), Tuple.Create("Precio (Bs)", precio), Tuple.Create("Stock inicial", stock) })
            {
                panel.Children.Add(new TextBlock { Text = par.Item1 });
                panel.Children.Add(par.Item2);
            }
            var guardar = new Button { Content = "Guardar", IsDefault = true, Margin = new Thickness(0, 10, 0, 0) };
            guardar.Click += (s, a) => ventana.DialogResult = true;
            panel.Children.Add(guardar);
            ventana.Content = panel;
            if (ventana.ShowDialog() != true) return;

            decimal valor;
            int existencias;
            if (!decimal.TryParse(precio.Text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out valor) || !int.TryParse(stock.Text, out existencias))
            {
                MessageBox.Show("Escribe el precio y el stock como números.", "Nuevo producto", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Aplicar(() => servicio.NuevoProducto(Sesion.UsuarioId.ToString(), nombre.Text, categoria.Text, valor, existencias), "Producto creado.");
        }

        private void OnGuardarServicio(object sender, RoutedEventArgs e)
        {
            decimal precio;
            if (!decimal.TryParse((TxtServicioPrecio.Text ?? string.Empty).Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out precio))
            {
                MessageBox.Show("Escribe el precio como número, por ejemplo 65.", "Servicios", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Aplicar(() => servicio.NuevoServicio(Sesion.UsuarioId.ToString(), TxtServicioNombre.Text, CmbServicioTipo.SelectedItem as string, precio, ChkServicioActivo.IsChecked == true), "Servicio guardado.");
            TxtServicioNombre.Clear();
            TxtServicioPrecio.Clear();
        }
    }
}

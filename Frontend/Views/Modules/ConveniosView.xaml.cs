using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Convenios corporativos, sus reservas y su historial de saldos.</summary>
    public partial class ConveniosView : UserControl
    {
        private readonly AdministracionService servicio = new AdministracionService();

        public ConveniosView()
        {
            InitializeComponent();
        }

        private string Actor { get { return Sesion.UsuarioId.ToString(); } }
        private ConvenioVista Convenio { get { return GridConvenios.SelectedItem as ConvenioVista; } }

        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }
        private void OnActualizar(object sender, RoutedEventArgs e) { Cargar(); }

        public void Cargar()
        {
            try
            {
                int id = Convenio == null ? 0 : Convenio.Id;
                var lista = servicio.Convenios();
                GridConvenios.ItemsSource = lista;
                GridConvenios.SelectedItem = lista.Find(c => c.Id == id);
                TxtAviso.Text = string.Empty;
            }
            catch (Exception ex)
            {
                TxtAviso.Text = "No se pudo leer la base de datos: " + ex.Message;
            }
            Mostrar();
        }

        private void OnSeleccion(object sender, SelectionChangedEventArgs e) { Mostrar(); }

        private void Mostrar()
        {
            try
            {
                GridReservas.ItemsSource = servicio.ReservasDelConvenio(Convenio);
                GridSaldos.ItemsSource = servicio.SaldosDelConvenio(Convenio);
            }
            catch (Exception ex) { TxtAviso.Text = "No se pudo leer el convenio: " + ex.Message; }
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

            if (error != null) { MessageBox.Show(error, "Convenios", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            Cargar();
            TxtAviso.Text = exito;
        }

        private void OnNuevo(object sender, RoutedEventArgs e) { Abrir(null); }

        private void OnEditar(object sender, RoutedEventArgs e)
        {
            if (Convenio == null) { MessageBox.Show("Selecciona un convenio de la tabla.", "Convenios", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            Abrir(Convenio);
        }

        private void Abrir(ConvenioVista c)
        {
            var estados = new object[] { "Activo", "Inactivo" };
            var v = FormularioWindow.Pedir(Window.GetWindow(this), c == null ? "Nuevo convenio" : "Editar convenio",
                Campo.Texto("Nombre", c == null ? "" : c.Nombre), Campo.Texto("Empresa", c == null ? "" : c.Empresa),
                Campo.Texto("Tarifa preferencial por noche (Bs)", c == null ? "0" : c.Tarifa.ToString("0.00", CultureInfo.InvariantCulture)),
                Campo.Texto("Límite de crédito (Bs)", c == null ? "0" : c.Limite.ToString("0.00", CultureInfo.InvariantCulture)),
                Campo.Fecha("Vencimiento", c != null && c.Vence.HasValue ? c.Vence.Value : DateTime.Today.AddYears(1)),
                Campo.Lista("Estado", estados));
            if (v == null) return;

            decimal tarifa, limite;
            if (!Numero((string)v[2], out tarifa) || !Numero((string)v[3], out limite))
            {
                MessageBox.Show("Escribe la tarifa y el límite como números.", "Convenios", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var datos = new ConvenioVista
            {
                Id = c == null ? 0 : c.Id, Nombre = (string)v[0], Empresa = (string)v[1], Tarifa = tarifa, Limite = limite,
                Vence = (DateTime?)v[4], Estado = (string)v[5]
            };
            Aplicar(() => servicio.GuardarConvenio(Actor, datos), "Convenio guardado.");
        }

        private void OnRegistrarSaldo(object sender, RoutedEventArgs e)
        {
            var c = Convenio;
            if (c == null) { MessageBox.Show("Selecciona un convenio de la tabla.", "Convenios", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var v = FormularioWindow.Pedir(Window.GetWindow(this), "Registrar saldo · " + c.Nombre, Campo.Texto("Saldo actual (Bs)"), Campo.Texto("Descripción"));
            decimal saldo;
            if (v == null) return;
            if (!Numero((string)v[0], out saldo)) { MessageBox.Show("Escribe el saldo como número.", "Convenios", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            Aplicar(() => servicio.RegistrarSaldo(Actor, c, saldo, (string)v[1]), "Saldo registrado.");
        }
    }
}

using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Datos del hotel, parámetros, políticas, impuestos, módulos y auditoría.</summary>
    public partial class ConfiguracionHotelView : UserControl
    {
        private readonly SeguridadService servicio = new SeguridadService();
        private HotelVista hotel;

        public ConfiguracionHotelView()
        {
            InitializeComponent();
            CmbCategoria.ItemsSource = new[] { 1, 2, 3, 4, 5 };
            CmbMoneda.ItemsSource = new[] { "BOB", "USD" };
        }

        private string Actor { get { return Sesion.UsuarioId.ToString(); } }

        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }

        public void Cargar()
        {
            try
            {
                hotel = servicio.DatosHotel();
                if (hotel != null)
                {
                    TxtNombre.Text = hotel.Nombre;
                    TxtDireccion.Text = hotel.Direccion;
                    TxtTelefono.Text = hotel.Telefono;
                    TxtCorreo.Text = hotel.Correo;
                    CmbCategoria.SelectedItem = hotel.Categoria;
                }
                string moneda = servicio.Moneda();
                CmbMoneda.SelectedItem = string.IsNullOrEmpty(moneda) ? "BOB" : moneda;
                GridParametros.ItemsSource = servicio.Parametros();
                GridPoliticas.ItemsSource = servicio.Politicas();
                GridImpuestos.ItemsSource = servicio.Impuestos();
                GridModulos.ItemsSource = servicio.Modulos();
                OnBuscarAuditoria(null, null);
                TxtAviso.Text = hotel == null ? "No hay un hotel registrado en la base." : string.Empty;
            }
            catch (Exception ex)
            {
                TxtAviso.Text = "No se pudo leer la base de datos: " + ex.Message;
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

            if (error != null) { MessageBox.Show(error, "Configuración", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            Cargar();
            TxtAviso.Text = exito;
        }

        private void OnGuardarHotel(object sender, RoutedEventArgs e)
        {
            if (hotel == null) return;
            var datos = new HotelVista
            {
                Id = hotel.Id, Nombre = TxtNombre.Text, Direccion = TxtDireccion.Text, Telefono = TxtTelefono.Text, Correo = TxtCorreo.Text,
                Categoria = CmbCategoria.SelectedItem is int ? (int)CmbCategoria.SelectedItem : 0
            };
            Aplicar(() => servicio.GuardarHotel(Actor, datos, CmbMoneda.SelectedItem as string), "Datos del hotel guardados.");
        }

        private void OnGuardarParametro(object sender, RoutedEventArgs e)
        {
            Aplicar(() => servicio.GuardarParametro(Actor, TxtClave.Text, TxtValor.Text), "Parámetro guardado.");
            TxtClave.Clear(); TxtValor.Clear();
        }

        private void OnGuardarPolitica(object sender, RoutedEventArgs e)
        {
            Aplicar(() => servicio.GuardarPolitica(Actor, TxtPoliticaNombre.Text, TxtPoliticaValor.Text, TxtPoliticaDescripcion.Text), "Política guardada.");
            TxtPoliticaNombre.Clear(); TxtPoliticaValor.Clear(); TxtPoliticaDescripcion.Clear();
        }

        private void OnGuardarImpuesto(object sender, RoutedEventArgs e)
        {
            decimal porcentaje;
            if (!Numero(TxtImpuestoPorcentaje.Text, out porcentaje))
            {
                MessageBox.Show("Escribe el porcentaje como número, por ejemplo 13.", "Impuestos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Aplicar(() => servicio.GuardarImpuesto(Actor, TxtImpuestoNombre.Text, porcentaje, ChkImpuestoActivo.IsChecked == true), "Impuesto guardado.");
            TxtImpuestoNombre.Clear(); TxtImpuestoPorcentaje.Clear();
        }

        private void CambiarModulo(bool activo)
        {
            var m = GridModulos.SelectedItem as ModuloVista;
            if (m == null) { MessageBox.Show("Selecciona un módulo de la tabla.", "Módulos", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            Aplicar(() => servicio.CambiarModulo(Actor, m, activo), "Módulo actualizado.");
        }

        private void OnActivarModulo(object sender, RoutedEventArgs e) { CambiarModulo(true); }
        private void OnDesactivarModulo(object sender, RoutedEventArgs e) { CambiarModulo(false); }

        private void OnBuscarAuditoria(object sender, RoutedEventArgs e)
        {
            try { GridAuditoria.ItemsSource = servicio.Auditoria(TxtAuditoriaUsuario.Text, DpAuditoriaDesde.SelectedDate, DpAuditoriaHasta.SelectedDate); }
            catch (Exception ex) { TxtAviso.Text = "No se pudo leer la auditoría: " + ex.Message; }
        }
    }
}

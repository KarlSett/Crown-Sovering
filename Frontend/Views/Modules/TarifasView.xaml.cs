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
    /// <summary>Tarifas por tipo de habitación, temporadas, paquetes, promociones y precios por canal.</summary>
    public partial class TarifasView : UserControl
    {
        private readonly AdministracionService servicio = new AdministracionService();
        private List<TarifaVista> tarifas = new List<TarifaVista>();
        private List<TemporadaVista> temporadas = new List<TemporadaVista>();
        private List<TipoHabitacionVista> tipos = new List<TipoHabitacionVista>();
        private List<CanalVista> canales = new List<CanalVista>();

        public TarifasView()
        {
            InitializeComponent();
        }

        private string Actor { get { return Sesion.UsuarioId.ToString(); } }

        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }

        public void Cargar()
        {
            try
            {
                tarifas = servicio.Tarifas();
                temporadas = servicio.Temporadas();
                tipos = servicio.Tipos();
                canales = servicio.Canales();
                GridTarifas.ItemsSource = tarifas;
                GridTemporadas.ItemsSource = temporadas;
                GridPaquetes.ItemsSource = servicio.Paquetes();
                GridPromociones.ItemsSource = servicio.Promociones();
                GridCanales.ItemsSource = servicio.PreciosPorCanal();
                TxtAviso.Text = string.Empty;
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

            if (error != null) { MessageBox.Show(error, "Tarifas", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            Backend.Data.DatosHotel.Refrescar();
            Cargar();
            TxtAviso.Text = exito;
        }

        private bool LeerNumero(string texto, string campo, out decimal valor)
        {
            if (Numero(texto, out valor)) return true;
            MessageBox.Show("Escribe " + campo + " como número, por ejemplo 120.50.", "Tarifas", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private void OnNuevaTarifa(object sender, RoutedEventArgs e)
        {
            var v = FormularioWindow.Pedir(Window.GetWindow(this), "Nueva tarifa", Campo.Lista("Tipo de habitación", tipos), Campo.Texto("Nombre", "Tarifa especial"), Campo.Texto("Precio base por noche (Bs)"));
            decimal precio;
            if (v == null || !LeerNumero((string)v[2], "el precio", out precio)) return;
            Aplicar(() => servicio.NuevaTarifa(Actor, v[0] as TipoHabitacionVista, (string)v[1], precio), "Tarifa creada.");
        }

        private void OnCambiarPrecio(object sender, RoutedEventArgs e)
        {
            var tarifa = GridTarifas.SelectedItem as TarifaVista;
            if (tarifa == null) { MessageBox.Show("Selecciona una tarifa de la tabla.", "Tarifas", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var v = FormularioWindow.Pedir(Window.GetWindow(this), "Cambiar precio · " + tarifa, Campo.Texto("Nuevo precio por noche (Bs)", tarifa.PrecioBase.ToString("0.00", CultureInfo.InvariantCulture)));
            decimal precio;
            if (v == null || !LeerNumero((string)v[0], "el precio", out precio)) return;
            Aplicar(() => servicio.CambiarPrecio(Actor, tarifa, precio), "Precio actualizado.");
        }

        private void OnNuevaTemporada(object sender, RoutedEventArgs e)
        {
            var v = FormularioWindow.Pedir(Window.GetWindow(this), "Nueva temporada", Campo.Texto("Nombre"), Campo.Fecha("Inicio", DateTime.Today), Campo.Fecha("Fin", DateTime.Today.AddMonths(1)));
            if (v == null) return;
            Aplicar(() => servicio.NuevaTemporada(Actor, (string)v[0], (DateTime?)v[1], (DateTime?)v[2]), "Temporada creada.");
        }

        private void OnNuevoPaquete(object sender, RoutedEventArgs e)
        {
            var v = FormularioWindow.Pedir(Window.GetWindow(this), "Nuevo paquete", Campo.Texto("Nombre"), Campo.Texto("Descripción"), Campo.Texto("Precio (Bs)"));
            decimal precio;
            if (v == null || !LeerNumero((string)v[2], "el precio", out precio)) return;
            Aplicar(() => servicio.NuevoPaquete(Actor, (string)v[0], (string)v[1], precio), "Paquete creado.");
        }

        private void OnNuevaPromocion(object sender, RoutedEventArgs e)
        {
            var v = FormularioWindow.Pedir(Window.GetWindow(this), "Nueva promoción", Campo.Texto("Nombre"), Campo.Texto("Descuento (%)"),
                Campo.Fecha("Inicio", DateTime.Today), Campo.Fecha("Fin", DateTime.Today.AddMonths(1)), Campo.Lista("Estado", new object[] { "Activa", "Inactiva" }));
            decimal descuento;
            if (v == null || !LeerNumero((string)v[1], "el descuento", out descuento)) return;
            Aplicar(() => servicio.NuevaPromocion(Actor, (string)v[0], descuento, (DateTime?)v[2], (DateTime?)v[3], (string)v[4] == "Activa"), "Promoción creada.");
        }

        private void OnNuevoPrecioCanal(object sender, RoutedEventArgs e)
        {
            var v = FormularioWindow.Pedir(Window.GetWindow(this), "Nuevo precio por canal", Campo.Lista("Tarifa", tarifas), Campo.Lista("Temporada", temporadas),
                Campo.Lista("Canal", canales.Select(c => (object)c.Nombre)), Campo.Texto("Precio por noche (Bs)"));
            decimal precio;
            if (v == null || !LeerNumero((string)v[3], "el precio", out precio)) return;
            var canal = canales.FirstOrDefault(c => c.Nombre == (string)v[2]);
            Aplicar(() => servicio.NuevoPrecioCanal(Actor, v[0] as TarifaVista, v[1] as TemporadaVista, canal, precio), "Precio por canal guardado.");
        }
    }
}

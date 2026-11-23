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
    /// <summary>Inventario: pisos, habitaciones, estados, bloqueos y tipos de habitación.</summary>
    public partial class HabitacionesGestionView : UserControl
    {
        private readonly AdministracionService servicio = new AdministracionService();
        private List<PisoVista> pisos = new List<PisoVista>();
        private List<TipoHabitacionVista> tipos = new List<TipoHabitacionVista>();
        private List<HabitacionGestion> habitaciones = new List<HabitacionGestion>();
        private HabitacionGestion actual;

        public HabitacionesGestionView()
        {
            InitializeComponent();
        }

        private string Actor { get { return Sesion.UsuarioId.ToString(); } }
        private PisoVista Piso { get { return ListaPisos.SelectedItem as PisoVista; } }

        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }

        public void Cargar()
        {
            try
            {
                int pisoActual = Piso == null ? 0 : Piso.Id;
                int habitacionActual = actual == null ? 0 : actual.Id;
                pisos = servicio.Pisos();
                tipos = servicio.Tipos();
                habitaciones = servicio.Habitaciones();
                ListaPisos.ItemsSource = pisos;
                ListaPisos.SelectedItem = pisos.FirstOrDefault(p => p.Id == pisoActual) ?? pisos.FirstOrDefault();
                CmbTipo.ItemsSource = tipos;
                GridTipos.ItemsSource = tipos;
                CmbHabitacionBloqueo.ItemsSource = habitaciones.Select(h => h.Numero).ToList();
                GridBloqueos.ItemsSource = servicio.Bloqueos();
                MostrarHabitaciones();
                Elegir(habitaciones.FirstOrDefault(h => h.Id == habitacionActual));
                TxtAviso.Text = string.Empty;
            }
            catch (Exception ex)
            {
                TxtAviso.Text = "No se pudo leer la base de datos: " + ex.Message;
            }
        }

        private void MostrarHabitaciones()
        {
            PanelHabitaciones.Children.Clear();
            string buscado = (TxtBuscar.Text ?? string.Empty).Trim().ToLowerInvariant();
            var lista = habitaciones.Where(h => (Piso == null || h.PisoId == Piso.Id) &&
                (buscado.Length == 0 || h.ToString().ToLowerInvariant().Contains(buscado)));
            foreach (var h in lista)
            {
                var habitacion = h;
                var boton = new Button { Content = h.Numero + " · " + h.Tipo + "\n" + h.Estado, Width = 140, Height = 60 };
                boton.Click += (s, e) => Elegir(habitacion);
                PanelHabitaciones.Children.Add(boton);
            }
        }

        private void Elegir(HabitacionGestion h)
        {
            actual = h;
            TxtNumero.Text = h == null ? string.Empty : h.Numero;
            TxtEstado.Text = h == null ? string.Empty : h.Estado;
            CmbTipo.SelectedItem = h == null ? null : tipos.FirstOrDefault(t => t.Id == h.TipoId);
            BtnGuardar.IsEnabled = BtnEstado.IsEnabled = h != null;
            CargarHistorial();
        }

        private void CargarHistorial()
        {
            try { GridHistorial.ItemsSource = servicio.Historial(actual, DpHistDesde.SelectedDate, DpHistHasta.SelectedDate); }
            catch (Exception ex) { TxtAviso.Text = "No se pudo leer el historial: " + ex.Message; }
        }

        private void OnBuscar(object sender, RoutedEventArgs e) { MostrarHabitaciones(); }
        private void OnBuscarCambia(object sender, TextChangedEventArgs e) { if (IsLoaded) MostrarHabitaciones(); }
        private void OnPisoCambia(object sender, SelectionChangedEventArgs e) { if (IsLoaded) MostrarHabitaciones(); }
        private void OnBuscarHistorial(object sender, RoutedEventArgs e) { CargarHistorial(); }
        private void OnIrABloqueos(object sender, RoutedEventArgs e)
        {
            Pestanas.SelectedItem = PestanaBloqueos;
            if (actual != null) CmbHabitacionBloqueo.SelectedItem = actual.Numero;
        }

        private void Aplicar(Func<string> operacion, string exito)
        {
            string error;
            try { error = operacion(); }
            catch (Exception ex) { error = "No se pudo guardar.\n" + ex.Message; }

            if (error != null) { MessageBox.Show(error, "Inventario", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            Backend.Data.DatosHotel.Refrescar();
            Cargar();
            TxtAviso.Text = exito;
        }

        private void OnAgregarPiso(object sender, RoutedEventArgs e)
        {
            Aplicar(() => servicio.NuevoPiso(Actor, TxtPiso.Text), "Piso agregado.");
            TxtPiso.Clear();
        }

        private void OnNuevaHabitacion(object sender, RoutedEventArgs e)
        {
            var v = FormularioWindow.Pedir(Window.GetWindow(this), "Nueva habitación",
                Campo.Lista("Piso", pisos), Campo.Lista("Tipo", tipos), Campo.Texto("Número"));
            if (v == null) return;
            Aplicar(() => servicio.NuevaHabitacion(Actor, v[0] as PisoVista, v[1] as TipoHabitacionVista, (string)v[2]), "Habitación creada.");
        }

        private void OnGuardarTipo(object sender, RoutedEventArgs e)
        {
            var h = actual;
            Aplicar(() => servicio.CambiarTipo(Actor, h, CmbTipo.SelectedItem as TipoHabitacionVista), "Tipo actualizado.");
        }

        private void OnCambiarEstado(object sender, RoutedEventArgs e)
        {
            var h = actual;
            var siguientes = EstadosHabitacion.Siguientes(h.Estado);
            if (siguientes.Count() == 0)
            {
                MessageBox.Show("La habitación está " + h.Estado.ToLowerInvariant() + ": no tiene un estado siguiente desde aquí.", "Inventario", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var v = FormularioWindow.Pedir(Window.GetWindow(this), "Cambiar estado · hab. " + h.Numero, Campo.Lista("Nuevo estado", siguientes.Cast<object>()));
            if (v == null) return;
            Aplicar(() => servicio.CambiarEstado(Actor, h, (string)v[0]), "Estado actualizado.");
        }

        private void OnBloquear(object sender, RoutedEventArgs e)
        {
            var h = habitaciones.FirstOrDefault(x => x.Numero == (CmbHabitacionBloqueo.SelectedItem as string));
            Aplicar(() => servicio.NuevoBloqueo(Actor, h, DpBloqueoDesde.SelectedDate, DpBloqueoHasta.SelectedDate, TxtMotivoBloqueo.Text), "Habitación bloqueada.");
            TxtMotivoBloqueo.Clear();
        }

        private void OnGuardarTipoNuevo(object sender, RoutedEventArgs e)
        {
            int capacidad;
            if (!int.TryParse(TxtTipoCapacidad.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out capacidad))
            {
                MessageBox.Show("Escribe la capacidad como número entero.", "Tipos de habitación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Aplicar(() => servicio.NuevoTipo(Actor, TxtTipoNombre.Text, capacidad), "Tipo guardado.");
            TxtTipoNombre.Clear();
        }
    }
}

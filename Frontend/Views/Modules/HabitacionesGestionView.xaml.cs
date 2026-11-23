using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    public partial class HabitacionesGestionView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        private bool cargando;
        public HabitacionesGestionView()
        {
            InitializeComponent();
            CmbTipo.ItemsSource = servicio.ListarTiposHabitacion().Select(t => t.Nombre).ToList();
            LstPisos.ItemsSource = servicio.ListarPisos();
            if (LstPisos.Items.Count > 0) LstPisos.SelectedIndex = 0;
        }
        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }
        private void Cargar()
        {
            int piso = LstPisos.SelectedItem is int ? (int)LstPisos.SelectedItem : -1;
            cargando = true;
            GridHabitaciones.ItemsSource = servicio.ListarHabitaciones().Where(h => (piso < 0 || h.Piso == piso) &&
                (string.IsNullOrWhiteSpace(TxtBuscar.Text) || h.NumeroHabitacion.IndexOf(TxtBuscar.Text, StringComparison.OrdinalIgnoreCase) >= 0 || h.CapacidadTipo.IndexOf(TxtBuscar.Text, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            CmbHabitacionBloqueo.ItemsSource = servicio.ListarHabitaciones();
            GridBloqueos.ItemsSource = servicio.ListarBloqueos();
            GridTipos.ItemsSource = servicio.ListarTiposHabitacion();
            CmbTipo.ItemsSource = servicio.ListarTiposHabitacion().Select(t => t.Nombre).ToList();
            cargando = false;
        }
        private void OnBuscar(object sender, RoutedEventArgs e) { if (IsLoaded) Cargar(); }
        private void OnPiso(object sender, SelectionChangedEventArgs e) { if (!cargando && IsLoaded) Cargar(); }
        private void OnHabitacion(object sender, SelectionChangedEventArgs e)
        {
            var h = GridHabitaciones.SelectedItem as Habitacion;
            TxtNumero.Text = h == null ? "" : h.NumeroHabitacion;
            CmbTipo.SelectedItem = h == null ? null : h.CapacidadTipo;
            GridHistorial.ItemsSource = h == null ? null : servicio.ListarHistorialHabitaciones(h.Id);
        }
        private void OnNuevaHabitacion(object sender, RoutedEventArgs e)
        {
            var w = new NuevaHabitacionWindow { Owner = Window.GetWindow(this) };
            if (w.ShowDialog() == true) { LstPisos.ItemsSource = servicio.ListarPisos(); LstPisos.SelectedItem = w.HabitacionCreada.Piso; Cargar(); }
        }
        private void OnCambiarEstado(object sender, RoutedEventArgs e)
        {
            var h = GridHabitaciones.SelectedItem as Habitacion;
            if (h == null) { MessageBox.Show("Selecciona una habitación.", "Inventario", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            servicio.CambiarEstadoHabitacion(h.Id); Cargar();
        }
        private void OnGuardarTipo(object sender, RoutedEventArgs e)
        {
            var h = GridHabitaciones.SelectedItem as Habitacion;
            if (h == null) { MessageBox.Show("Selecciona una habitación.", "Inventario", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.CambiarTipoHabitacion(h.Id, CmbTipo.SelectedItem as string); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Inventario", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnAgregarPiso(object sender, RoutedEventArgs e)
        {
            int nuevo = servicio.AgregarPiso(); LstPisos.ItemsSource = servicio.ListarPisos(); LstPisos.SelectedItem = nuevo;
            MessageBox.Show("Piso " + nuevo + " agregado. Crea una habitación para registrarlo en el inventario.", "Inventario", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void OnBloquear(object sender, RoutedEventArgs e)
        {
            var h = CmbHabitacionBloqueo.SelectedItem as Habitacion;
            if (h == null || !DpBloqueoInicio.SelectedDate.HasValue || !DpBloqueoFin.SelectedDate.HasValue)
            { MessageBox.Show("Selecciona habitación y fechas.", "Bloqueos", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.BloquearHabitacion(h.Id, DpBloqueoInicio.SelectedDate.Value, DpBloqueoFin.SelectedDate.Value, TxtMotivoBloqueo.Text); TxtMotivoBloqueo.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Bloqueos", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnBuscarHistorial(object sender, RoutedEventArgs e)
        {
            var h = GridHabitaciones.SelectedItem as Habitacion;
            if (h == null) { MessageBox.Show("Selecciona una habitación.", "Historial", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            GridHistorial.ItemsSource = servicio.ListarHistorialHabitaciones(h.Id).Where(x =>
                (!DpHistorialDesde.SelectedDate.HasValue || x.Inicio.Date >= DpHistorialDesde.SelectedDate.Value.Date) &&
                (!DpHistorialHasta.SelectedDate.HasValue || x.Inicio.Date <= DpHistorialHasta.SelectedDate.Value.Date)).ToList();
        }
        private void OnGuardarTipoHabitacion(object sender, RoutedEventArgs e)
        {
            int capacidad;
            if (!int.TryParse(TxtTipoCapacidad.Text, out capacidad)) { MessageBox.Show("La capacidad debe ser un número entero.", "Tipos de habitación", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.RegistrarTipoHabitacion(TxtTipoNombre.Text, capacidad); TxtTipoNombre.Clear(); TxtTipoCapacidad.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Tipos de habitación", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    public partial class ReservasGestionView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        private ReservaVista seleccion;
        public ReservasGestionView()
        {
            InitializeComponent(); CmbEstado.ItemsSource = new List<string> { "Todas", "Confirmada", "EnCurso", "En curso", "Finalizada", "Cancelada", "NoShow" }; CmbEstado.SelectedIndex = 0;
        }
        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }
        private void Cargar()
        {
            string estado = CmbEstado.SelectedItem as string;
            if (estado == "En curso") estado = "EnCurso";
            GridReservas.ItemsSource = servicio.ListarReservas(TxtBuscar.Text, estado, "Todos");
            GridReservas.SelectedItem = null; seleccion = null; Mostrar();
        }
        private void OnBuscar(object sender, RoutedEventArgs e) { if (IsLoaded) Cargar(); }
        private void OnActualizar(object sender, RoutedEventArgs e) { Cargar(); }
        private void OnSeleccion(object sender, SelectionChangedEventArgs e) { seleccion = GridReservas.SelectedItem as ReservaVista; Mostrar(); }
        private void Mostrar() { TxtDetalle.Text = seleccion == null ? "Selecciona una reserva para operar." : seleccion.Codigo + " · " + seleccion.Huesped + " · " + seleccion.Observaciones; }
        private void OnNuevaReserva(object sender, RoutedEventArgs e) { var w = new NuevaReservaWindow { Owner = Window.GetWindow(this) }; if (w.ShowDialog() == true) Cargar(); }
        private void Ejecutar(string accion)
        {
            if (seleccion == null) { MessageBox.Show("Selecciona una reserva.", "Reservas", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try
            {
                if (accion == "Check-in") servicio.CheckIn(seleccion.Reserva.Id);
                else if (accion == "Check-out") servicio.CheckOut(seleccion.Reserva.Id);
                else servicio.Cancelar(seleccion.Reserva.Id);
                Cargar();
            }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Reservas", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnCheckIn(object sender, RoutedEventArgs e) { Ejecutar("Check-in"); }
        private void OnCheckOut(object sender, RoutedEventArgs e) { Ejecutar("Check-out"); }
        private void OnCancelar(object sender, RoutedEventArgs e) { Ejecutar("Cancelar"); }
    }
}

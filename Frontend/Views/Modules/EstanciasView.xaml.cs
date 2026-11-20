using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Modules
{
    public partial class EstanciasView : UserControl
    {
        private bool ready;
        public EstanciasView()
        {
            InitializeComponent();
            CmbEntradaEstancia.ItemsSource = new[] { "Individual", "Grupo" };
            CmbSalidaEstancia.ItemsSource = new[] { "Regular", "Express" };
            CmbEstadoEstancia.ItemsSource = new[] { "Confirmada", "En curso", "Finalizada", "Cancelada", "No presentado" };
            CmbEntradaEstancia.SelectedIndex = 0; CmbSalidaEstancia.SelectedIndex = 0;
        }
        private void OnLoaded(object sender, RoutedEventArgs e) { ready = true; Cargar(); }
        private void OnSearchChanged(object sender, TextChangedEventArgs e) { if (ready) Cargar(); }
        private void Cargar()
        {
            var reservations = ServicioHotel.Instancia.ListarReservas(TxtBuscarEstancia.Text, "Todas", "Todos");
            ListaLlegadas.ItemsSource = reservations.Where(item => item.EstadoCrudo == "Confirmada" && item.Reserva.FechaCheckinEsperado.Date == DateTime.Today).ToList();
            ListaEnCasa.ItemsSource = reservations.Where(item => item.EstadoCrudo == "EnCurso").ToList();
            ListaSalidas.ItemsSource = reservations.Where(item => item.EstadoCrudo == "EnCurso" && item.Reserva.FechaCheckoutEsperado.Date == DateTime.Today).ToList();
            TxtReservaEstancia.Text = string.Empty; CmbEstadoEstancia.SelectedItem = null;
            if (ListaLlegadas.Items.Count > 0) ListaLlegadas.SelectedIndex = 0;
            else if (ListaEnCasa.Items.Count > 0) ListaEnCasa.SelectedIndex = 0;
        }
        private void OnStaySelected(object sender, SelectionChangedEventArgs e)
        {
            var list = (ListBox)sender;
            var reservation = list.SelectedItem as ReservaVista;
            if (reservation == null) return;
            foreach (var other in new[] { ListaLlegadas, ListaEnCasa, ListaSalidas })
                if (other != list) other.SelectedIndex = -1;
            TxtReservaEstancia.Text = reservation.Codigo + " · Hab. " + reservation.Habitacion;
            CmbEstadoEstancia.SelectedItem = reservation.Estado;
        }
    }
}

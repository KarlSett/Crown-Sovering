using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Sections
{
    /// <summary>Disponibilidad de habitaciones por fechas, tipo y calidad.</summary>
    public partial class HabitacionesView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;

        public HabitacionesView()
        {
            InitializeComponent();

            CmbCapacidad.ItemsSource = new List<string> { "Todas", "Sencilla", "Doble", "Familiar", "Suite" };
            CmbCapacidad.SelectedIndex = 0;

            CmbCalidadHabitacion.ItemsSource = new List<string>
            {
                "Todas", "Estándar", "Superior", "Deluxe", "Presidencial"
            };
            CmbCalidadHabitacion.SelectedIndex = 0;

            DpInicio.SelectedDate = DateTime.Today;
            DpFin.SelectedDate = DateTime.Today.AddDays(1);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Cargar();
        }

        public void Cargar()
        {
            DateTime inicio = DpInicio.SelectedDate ?? DateTime.Today;
            DateTime fin = DpFin.SelectedDate ?? inicio.AddDays(1);
            if (fin <= inicio)
                fin = inicio.AddDays(1);

            var lista = servicio.ListarDisponibilidad(inicio, fin,
                CmbCapacidad.SelectedItem as string,
                CmbCalidadHabitacion.SelectedItem as string);

            GridHabitaciones.ItemsSource = lista;
            int libres = lista.Count(h => h.EstaDisponible);
            TxtResumenHabitaciones.Text = libres + " de " + lista.Count + " habitaciones disponibles del " +
                                          Formato.Fecha(inicio) + " al " + Formato.Fecha(fin) + ".";
        }

        private void OnBuscarDisponibilidad(object sender, RoutedEventArgs e)
        {
            if (DpInicio.SelectedDate.HasValue && DpFin.SelectedDate.HasValue &&
                DpFin.SelectedDate.Value <= DpInicio.SelectedDate.Value)
            {
                MessageBox.Show("La fecha de salida debe ser posterior a la de entrada.",
                                "Buscar disponibilidad", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Cargar();
        }
    }
}

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
    /// <summary>Tablero semanal de reservas, detalle, canales, grupos y lista de espera.</summary>
    public partial class ReservasGestionView : UserControl
    {
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-EC");
        private static readonly string[] Estados = { "Todos", "Confirmada", "Check-In", "Check-Out", "Cancelada" };

        private readonly ReservaService reservas = new ReservaService();
        private readonly ReservasAvanzadasService avanzadas = new ReservasAvanzadasService();
        private DateTime inicioSemana;
        private bool cambiando;
        private ListBox[] listas;
        private GroupBox[] cajas;

        public ReservasGestionView()
        {
            InitializeComponent();
            listas = new[] { Lista0, Lista1, Lista2, Lista3, Lista4, Lista5, Lista6 };
            cajas = new[] { Dia0, Dia1, Dia2, Dia3, Dia4, Dia5, Dia6 };
            CmbEstado.ItemsSource = Estados;
            CmbEstado.SelectedIndex = 0;
            inicioSemana = LunesDe(DateTime.Today);
        }

        private static DateTime LunesDe(DateTime fecha)
        {
            return fecha.Date.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Cargar();
        }

        public void Cargar()
        {
            try
            {
                var canales = avanzadas.Canales();
                CmbCanal.ItemsSource = new[] { "Todos" }.Concat(canales.Select(c => c.Nombre)).ToList();
                if (CmbCanal.SelectedItem == null) CmbCanal.SelectedIndex = 0;
                GridCanales.ItemsSource = canales;
                GridGrupos.ItemsSource = avanzadas.Grupos();
                GridEspera.ItemsSource = avanzadas.ListaEspera();
                TxtAviso.Text = string.Empty;
                MostrarSemana();
            }
            catch (Exception ex)
            {
                TxtAviso.Text = "No se pudo leer la base de datos: " + ex.Message;
            }
        }

        private void MostrarSemana()
        {
            string canal = CmbCanal.SelectedItem as string ?? "Todos";
            string estado = CmbEstado.SelectedItem as string ?? "Todos";
            var todas = reservas.Listar().Where(r => (canal == "Todos" || r.Canal == canal) && (estado == "Todos" || r.Estado == estado)).ToList();

            cambiando = true;
            TxtSemana.Text = inicioSemana.ToString("d MMM", Cultura) + " – " + inicioSemana.AddDays(6).ToString("d MMM yyyy", Cultura);
            for (int i = 0; i < 7; i++)
            {
                DateTime dia = inicioSemana.AddDays(i);
                string nombre = dia.ToString("dddd d", Cultura);
                cajas[i].Header = char.ToUpper(nombre[0], Cultura) + nombre.Substring(1);
                listas[i].ItemsSource = todas.Where(r => r.CheckIn.Date == dia).ToList();
            }
            cambiando = false;
            Mostrar(null);
        }

        private void OnActualizar(object sender, RoutedEventArgs e) { Cargar(); }
        private void OnSemanaAnterior(object sender, RoutedEventArgs e) { inicioSemana = inicioSemana.AddDays(-7); Cargar(); }
        private void OnSemanaSiguiente(object sender, RoutedEventArgs e) { inicioSemana = inicioSemana.AddDays(7); Cargar(); }

        private ReservaRegistro seleccionada;

        private void OnSeleccion(object sender, SelectionChangedEventArgs e)
        {
            if (cambiando) return;
            var origen = (ListBox)sender;
            var elegida = origen.SelectedItem as ReservaRegistro;
            cambiando = true;
            foreach (var lista in listas) if (lista != origen) lista.SelectedItem = null;
            cambiando = false;
            Mostrar(elegida);
        }

        private void Mostrar(ReservaRegistro r)
        {
            seleccionada = r;
            TxtHuesped.Text = r == null ? string.Empty : r.Huesped;
            TxtCanal.Text = r == null ? string.Empty : r.Canal;
            TxtEntrada.Text = r == null ? string.Empty : r.CheckIn.ToString("dd/MM/yyyy");
            TxtSalida.Text = r == null ? string.Empty : r.CheckOut.ToString("dd/MM/yyyy");
            TxtEstado.Text = r == null ? string.Empty : r.Estado;
            if (r == null)
            {
                ListaHuespedes.ItemsSource = ListaHabitaciones.ItemsSource = ListaSolicitudes.ItemsSource = ListaRecursos.ItemsSource = null;
                return;
            }

            try
            {
                var detalle = avanzadas.Detalle(r.Id);
                ListaHuespedes.ItemsSource = detalle.Huespedes;
                ListaHabitaciones.ItemsSource = detalle.Habitaciones;
                ListaSolicitudes.ItemsSource = detalle.Solicitudes;
                ListaRecursos.ItemsSource = detalle.Recursos;
            }
            catch (Exception ex) { TxtAviso.Text = "No se pudo leer el detalle: " + ex.Message; }
        }

        private void OnNuevaReserva(object sender, RoutedEventArgs e)
        {
            var ventana = new NuevaReservaWindow { Owner = Window.GetWindow(this) };
            if (ventana.ShowDialog() != true) return;

            Backend.Data.DatosHotel.Refrescar();
            Cargar();
            TxtAviso.Text = "Reserva " + ventana.CodigoCreado + " creada.";
        }

        private void Aplicar(Func<string> operacion, string exito)
        {
            string error;
            try { error = operacion(); }
            catch (Exception ex) { error = "No se pudo guardar.\n" + ex.Message; }

            if (error != null) { MessageBox.Show(error, "Reservas", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var actual = seleccionada;
            Cargar();
            TxtAviso.Text = exito;
            if (actual != null) Mostrar(actual);
        }

        private void OnGuardarCanal(object sender, RoutedEventArgs e)
        {
            decimal comision;
            if (!decimal.TryParse((TxtCanalComision.Text ?? string.Empty).Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out comision))
            {
                MessageBox.Show("Escribe la comisión como número, por ejemplo 10.", "Canales", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Aplicar(() => avanzadas.GuardarCanal(Sesion.UsuarioId.ToString(), TxtCanalNombre.Text, comision), "Canal guardado.");
            TxtCanalNombre.Clear();
        }

        private void OnGuardarGrupo(object sender, RoutedEventArgs e)
        {
            Aplicar(() => avanzadas.GuardarGrupo(Sesion.UsuarioId.ToString(), TxtGrupoNombre.Text), "Grupo guardado.");
            TxtGrupoNombre.Clear();
        }

        private void OnSolicitud(object sender, RoutedEventArgs e)
        {
            var r = seleccionada;
            Aplicar(() => avanzadas.GuardarSolicitud(Sesion.UsuarioId.ToString(), r == null ? 0 : r.Id, TxtSolicitud.Text), "Solicitud registrada.");
            TxtSolicitud.Clear();
        }
    }
}

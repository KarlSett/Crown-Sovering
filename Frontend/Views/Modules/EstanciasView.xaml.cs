using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Crown__.Backend.Models;
using Crown__.Backend.Repositories;
using Crown__.Backend.Services;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Llegadas, huéspedes en casa y salidas del día, con el detalle de la estancia y sus llaves.</summary>
    public partial class EstanciasView : UserControl
    {
        private readonly ReservaService reservas = new ReservaService();
        private readonly EstanciaService estancias = new EstanciaService();
        private List<ReservaRegistro> todas = new List<ReservaRegistro>();
        private ReservaRegistro actual;
        private EstanciaDetalle detalle;
        private bool cambiandoSeleccion;

        private class Item
        {
            public ReservaRegistro Reserva;
            public string Texto;
            public override string ToString() { return Texto; }
        }

        public EstanciasView()
        {
            InitializeComponent();
            CmbCheckin.ItemsSource = EstanciaService.TiposCheckin;
            CmbCheckout.ItemsSource = EstanciaService.TiposCheckout;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Cargar();
        }

        public void Cargar()
        {
            try
            {
                todas = reservas.Listar();
                TxtAviso.Text = string.Empty;
            }
            catch (Exception ex)
            {
                todas = new List<ReservaRegistro>();
                TxtAviso.Text = "No se pudo leer la base de datos: " + ex.Message;
            }
            Mostrar();
        }

        public void EnfocarLlegadas()
        {
            TabsFlujo.SelectedIndex = 0;
        }

        public void EnfocarSalidas()
        {
            TabsFlujo.SelectedIndex = 2;
        }

        private bool Coincide(ReservaRegistro r)
        {
            string buscado = (TxtBuscar.Text ?? string.Empty).Trim().ToLowerInvariant();
            return buscado.Length == 0 || r.Codigo.ToLowerInvariant().Contains(buscado) || r.Huesped.ToLowerInvariant().Contains(buscado)
                   || (r.Habitacion ?? string.Empty).ToLowerInvariant().Contains(buscado);
        }

        private void Mostrar()
        {
            DateTime hoy = DateTime.Today;
            cambiandoSeleccion = true;
            ListaLlegadas.ItemsSource = todas.Where(r => r.Estado == "Confirmada" && r.CheckIn.Date <= hoy && Coincide(r))
                .Select(r => new Item { Reserva = r, Texto = r.Codigo + " · " + r.Huesped + " · Hab. " + r.Habitacion }).ToList();
            ListaEnCasa.ItemsSource = todas.Where(r => r.Estado == "Check-In" && Coincide(r))
                .Select(r => new Item { Reserva = r, Texto = "Hab. " + r.Habitacion + " · " + r.Huesped + " · sale " + r.CheckOut.ToString("dd/MM") }).ToList();
            ListaSalidas.ItemsSource = todas.Where(r => r.Estado == "Check-In" && r.CheckOut.Date <= hoy && Coincide(r))
                .Select(r => new Item { Reserva = r, Texto = "Hab. " + r.Habitacion + " · " + r.Huesped }).ToList();
            cambiandoSeleccion = false;
            Elegir(null);
        }

        private void OnBuscar(object sender, RoutedEventArgs e) { Mostrar(); }
        private void OnBuscarCambia(object sender, TextChangedEventArgs e) { if (IsLoaded) Mostrar(); }

        private void OnSeleccionLlegada(object sender, SelectionChangedEventArgs e) { Seleccion(ListaLlegadas); }
        private void OnSeleccionEnCasa(object sender, SelectionChangedEventArgs e) { Seleccion(ListaEnCasa); }
        private void OnSeleccionSalida(object sender, SelectionChangedEventArgs e) { Seleccion(ListaSalidas); }

        // Una sola reserva seleccionada a la vez entre las tres listas.
        private void Seleccion(ListBox origen)
        {
            if (cambiandoSeleccion) return;
            var item = origen.SelectedItem as Item;
            cambiandoSeleccion = true;
            foreach (var lista in new[] { ListaLlegadas, ListaEnCasa, ListaSalidas })
                if (lista != origen) lista.SelectedItem = null;
            cambiandoSeleccion = false;
            Elegir(item == null ? null : item.Reserva);
        }

        private void Elegir(ReservaRegistro reserva)
        {
            actual = reserva;
            detalle = null;
            GridLlaves.ItemsSource = null;
            TxtReserva.Text = reserva == null ? string.Empty : reserva.Codigo + " · " + reserva.Huesped + " · Hab. " + reserva.Habitacion;
            CmbCheckin.SelectedItem = null;
            CmbCheckout.SelectedItem = null;
            TxtEstado.Text = reserva == null ? string.Empty : reserva.Estado;

            BtnCheckIn.IsEnabled = reserva != null && reserva.Estado == "Confirmada";
            BtnCheckOut.IsEnabled = reserva != null && reserva.Estado == "Check-In";

            if (reserva != null)
            {
                try { detalle = estancias.Detalle(reserva.Id); }
                catch (Exception ex) { TxtAviso.Text = "No se pudo leer la estancia: " + ex.Message; }
            }

            bool enCurso = detalle != null && detalle.Estado == "ALOJADO";
            if (detalle != null)
            {
                CmbCheckin.SelectedItem = detalle.TipoCheckin;
                CmbCheckout.SelectedItem = detalle.TipoCheckout;
                CargarLlaves();
            }
            BtnGuardarTipos.IsEnabled = enCurso;
            BtnEmitirLlave.IsEnabled = enCurso;
            BtnImprimir.IsEnabled = reserva != null;
            BtnMover.IsEnabled = enCurso;
            BtnDevolverLlave.IsEnabled = enCurso;
        }

        private void CargarLlaves()
        {
            try { GridLlaves.ItemsSource = estancias.Llaves(detalle.EstanciaId); }
            catch (Exception ex) { TxtAviso.Text = "No se pudieron leer las llaves: " + ex.Message; }
        }

        private void OnCheckIn(object sender, RoutedEventArgs e) { Operar("registrar la llegada de", actual, a => estancias.HacerCheckIn(Sesion.UsuarioId.ToString(), a.Codigo)); }

        private void OnCheckOut(object sender, RoutedEventArgs e)
        {
            Operar("registrar la salida de", actual, a =>
            {
                // Si queda saldo se cobra en efectivo antes de la salida, igual que en Reservas.
                try { ServicioHotel.Instancia.CheckOut(a.Id); return null; }
                catch (InvalidOperationException ex) { return ex.Message; }
            });
        }

        private void Operar(string accion, ReservaRegistro reserva, Func<ReservaRegistro, string> operacion)
        {
            if (reserva == null) return;
            if (MessageBox.Show("¿Desea " + accion + " " + reserva.Huesped + " (hab. " + reserva.Habitacion + ")?", "Estancias",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            string error;
            try { error = operacion(reserva); }
            catch (Exception ex) { error = "No se pudo completar la operación.\n" + ex.Message; }

            if (error != null) { MessageBox.Show(error, "Estancias", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            Backend.Data.DatosHotel.Refrescar();
            TxtAviso.Text = "Operación registrada · " + reserva.Codigo;
            Cargar();
        }

        private void OnGuardarTipos(object sender, RoutedEventArgs e)
        {
            Aplicar(() => estancias.ActualizarTipos(Sesion.UsuarioId.ToString(), detalle, CmbCheckin.SelectedItem as string, CmbCheckout.SelectedItem as string), "Tipos guardados.");
        }

        private void OnEmitirLlave(object sender, RoutedEventArgs e)
        {
            Aplicar(() => estancias.EmitirLlave(Sesion.UsuarioId.ToString(), detalle, TxtCodigoLlave.Text), "Llave emitida.");
            TxtCodigoLlave.Clear();
        }

        private void OnDevolverLlave(object sender, RoutedEventArgs e)
        {
            Aplicar(() => estancias.DevolverLlave(Sesion.UsuarioId.ToString(), GridLlaves.SelectedItem as LlaveVista), "Llave devuelta.");
        }

        private void Aplicar(Func<string> operacion, string mensaje)
        {
            string error;
            try { error = operacion(); }
            catch (Exception ex) { error = "No se pudo guardar.\n" + ex.Message; }

            if (error != null) { MessageBox.Show(error, "Estancias", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            TxtAviso.Text = mensaje;
            if (detalle != null) CargarLlaves();
        }

        private void OnMover(object sender, RoutedEventArgs e)
        {
            var reserva = actual;
            if (reserva == null) return;

            var libres = ServicioHotel.Instancia.HabitacionesLibres(DateTime.Today, reserva.CheckOut.Date > DateTime.Today ? reserva.CheckOut : DateTime.Today.AddDays(1))
                .Where(h => h.NumeroHabitacion != reserva.Habitacion).ToList();
            if (libres.Count == 0)
            {
                MessageBox.Show("No hay habitaciones libres para el resto de la estancia.", "Estancias", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var v = FormularioWindow.Pedir(Window.GetWindow(this), "Mover a " + reserva.Huesped + " (hab. " + reserva.Habitacion + ")",
                Campo.Lista("Nueva habitación", libres.Select(h => (object)(h.NumeroHabitacion + " · " + h.CapacidadTipo))));
            if (v == null) return;

            string numero = ((string)v[0]).Split(' ')[0];
            var nueva = libres.First(h => h.NumeroHabitacion == numero);
            Operar("pasar a otra habitación a", reserva, a => estancias.MoverHabitacion(Sesion.UsuarioId.ToString(), a.Codigo, nueva.Id));
        }

        private void OnImprimir(object sender, RoutedEventArgs e)
        {
            if (actual == null) return;

            var documento = new FlowDocument { PagePadding = new Thickness(48), FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
            documento.Blocks.Add(new Paragraph(new Run("Crown & Sovereign · Estancia " + actual.Codigo)) { FontSize = 20, FontWeight = FontWeights.Bold });
            documento.Blocks.Add(new Paragraph(new Run("Huésped: " + actual.Huesped + "\nHabitación: " + actual.Habitacion + " (" + actual.Tipo + ")" +
                "\nEntrada: " + actual.CheckIn.ToString("dd/MM/yyyy") + "\nSalida: " + actual.CheckOut.ToString("dd/MM/yyyy") + "\nEstado: " + actual.Estado)));
            if (detalle != null)
                foreach (var llave in estancias.Llaves(detalle.EstanciaId))
                    documento.Blocks.Add(new Paragraph(new Run("Llave " + llave.Codigo + " · " + llave.Estado + " · emitida " + llave.EmitidaTexto)));

            var impresion = new PrintDialog();
            if (impresion.ShowDialog() == true)
                impresion.PrintDocument(((IDocumentPaginatorSource)documento).DocumentPaginator, "Estancia " + actual.Codigo);
        }
    }
}

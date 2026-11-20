using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Huéspedes y consentimientos. El alta se hace en una ventana emergente.</summary>
    public partial class HuespedesView : UserControl
    {
        private readonly DispatcherTimer searchDelay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        private List<GuestPresentation> guests;
        public sealed class GuestPresentation
        {
            public Huesped Guest { get; set; }
            public string Initials { get { return Guest.Nombres.Substring(0, 1) + Guest.Apellidos.Substring(0, 1); } }
        }

        public HuespedesView()
        {
            InitializeComponent();
            searchDelay.Tick += delegate { searchDelay.Stop(); FilterGuests(); };
            Unloaded += delegate { searchDelay.Stop(); };
            guests = ServicioHotel.Instancia.ListarHuespedes().Select(guest => new GuestPresentation { Guest = guest }).ToList();
            FilterGuests();
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e) { searchDelay.Stop(); searchDelay.Start(); }
        private void FilterGuests()
        {
            var search = new BusquedaTolerante(TxtBuscarHuesped.Text);
            ListaHuespedes.ItemsSource = guests.Select(item => new { Item = item, Score = search.Puntuar(item.Guest.NombreCompleto, item.Guest.NumeroDocumento, item.Guest.Correo) })
                .Where(item => item.Score >= 0).OrderBy(item => item.Score).Select(item => item.Item).ToList();
            ListaHuespedes.SelectedIndex = ListaHuespedes.Items.Count > 0 ? 0 : -1;
        }

        private void OnNuevoHuesped(object sender, RoutedEventArgs e)
        {
            var ventana = new NuevoHuespedWindow { Owner = Window.GetWindow(this) };
            ventana.ShowDialog();
        }
    }
}

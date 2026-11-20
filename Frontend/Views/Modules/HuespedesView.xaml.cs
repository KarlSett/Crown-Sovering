using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Huéspedes y consentimientos. El alta se hace en una ventana emergente.</summary>
    public partial class HuespedesView : UserControl
    {
        private List<GuestPresentation> guests;
        public sealed class GuestPresentation
        {
            public Huesped Guest { get; set; }
            public string Initials { get { return Guest.Nombres.Substring(0, 1) + Guest.Apellidos.Substring(0, 1); } }
        }

        public HuespedesView()
        {
            InitializeComponent();
            guests = ServicioHotel.Instancia.ListarHuespedes().Select(guest => new GuestPresentation { Guest = guest }).ToList();
            FilterGuests();
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e) { if (guests != null) FilterGuests(); }
        private void FilterGuests()
        {
            string search = (TxtBuscarHuesped.Text ?? string.Empty).Trim().ToLowerInvariant();
            ListaHuespedes.ItemsSource = guests.Where(item => (item.Guest.NombreCompleto + " " + item.Guest.NumeroDocumento + " " + item.Guest.Correo).ToLowerInvariant().Contains(search)).ToList();
            ListaHuespedes.SelectedIndex = ListaHuespedes.Items.Count > 0 ? 0 : -1;
        }

        private void OnNuevoHuesped(object sender, RoutedEventArgs e)
        {
            var ventana = new NuevoHuespedWindow { Owner = Window.GetWindow(this) };
            ventana.ShowDialog();
        }
    }
}

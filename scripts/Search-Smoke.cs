using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Crown__.Backend.Services;

internal static class SearchSmoke
{
    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }
    [STAThread] static int Main()
    {
        try
        {
            Check(new BusquedaTolerante("Zofia").Puntuar("Sofia Rios") >= 0, "One-letter substitution");
            Check(new BusquedaTolerante("Sofai").Puntuar("Sofia Rios") >= 0, "Adjacent transposition");
            Check(new BusquedaTolerante("Sofi").Puntuar("Sofia Rios") >= 0, "Partial name");
            Check(new BusquedaTolerante("Sofiya").Puntuar("Sofia Rios") >= 0, "Insertion");
            Check(new BusquedaTolerante("Sfia").Puntuar("Sofia Rios") >= 0, "Deletion");
            Check(new BusquedaTolerante("  SOFIA   RIOS  ").Puntuar("Sofía Ríos") >= 0, "Accents, case and spaces");
            Check(new BusquedaTolerante("Rios Zofia").Puntuar("Sofía Ríos") >= 0, "Reordered name and surname");
            Check(new BusquedaTolerante("Zofia").Puntuar("Sofia") > new BusquedaTolerante("Zofia").Puntuar("Zofia"), "Exact results first");
            Check(new BusquedaTolerante("Sofzz").Puntuar("Sofia Rios") < 0, "Excessive name errors rejected");
            Check(new BusquedaTolerante("Ana").Puntuar("Ada") < 0, "No fuzzy matches on short names");
            Check(new BusquedaTolerante("Sofia Vega").Puntuar("Sofia Rios") < 0, "Every name word must match");
            Check(new BusquedaTolerante("101").Puntuar("Sofia", "102") < 0, "No approximate room numbers");
            Check(new BusquedaTolerante("12345678").Puntuar("Sofia", "12345679") < 0, "No approximate documents");
            Check(new BusquedaTolerante("CS-0002").Puntuar("Sofia", "CS-0003") < 0, "No approximate reservation codes");
            Check(new BusquedaTolerante("zofia@hotel.com").Puntuar("Sofia", "sofia@hotel.com") < 0, "No approximate emails");
            Check(new BusquedaTolerante(null).Puntuar(null) == 0, "Empty query");
            var service = ServicioHotel.Instancia;
            var exact = service.ListarReservas("Sofia", "Todas", "Todos");
            var typo = service.ListarReservas("Zofia", "Todas", "Todos");
            Check(exact.Count > 0 && typo.Any(item => exact.Any(other => other.Codigo == item.Codigo)), "Real demo reservations tolerate Zofia");
            Check(service.ListarReservas("Zofia", "Confirmada", "Todos").All(item => item.EstadoCrudo == "Confirmada"), "Status filter retained");
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            foreach (string resource in new[] { "Frontend/Themes/HotelPalette.xaml", "Frontend/Styles/ModernControls.xaml", "Frontend/Styles/LiquidGlassStyles.xaml", "Frontend/Styles/DashboardStyles.xaml", "Frontend/Styles/OperationsStyles.xaml" })
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Crown &;component/" + resource, UriKind.Relative) });
            var view = new Crown__.Frontend.Views.Modules.HuespedesView();
            var input = (TextBox)view.FindName("TxtBuscarHuesped");
            var results = (ListBox)view.FindName("ListaHuespedes");
            input.Text = "zzzz-no-match"; input.Text = "Zofia";
            System.Threading.Thread.Sleep(220);
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
            Check(results.Items.Cast<Crown__.Frontend.Views.Modules.HuespedesView.GuestPresentation>().Any(item => item.Guest.Nombres.StartsWith("Sof", StringComparison.OrdinalIgnoreCase)), "Guest textbox, debounce and Sofia result");
            input.Text = "zzzz-no-match";
            System.Threading.Thread.Sleep(220);
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
            Check(results.Items.Count == 0, "Guest textbox rejects unrelated input");
            Console.WriteLine("PASS: typo tolerance, accents, ranking, strict identifiers and reservation integration.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}

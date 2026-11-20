using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Threading;
using Crown__.Frontend.Themes;
using Crown__.Frontend.Views;
using Crown__.Frontend.Views.Dialogs;
using Crown__.Backend.Models;

internal static class PreviewFrontend
{
    static void Pump() { Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { })); }
    static void Layout(FrameworkElement element, int width, int height)
    {
        element.Measure(new Size(width, height)); element.Arrange(new Rect(0, 0, width, height)); element.UpdateLayout(); Pump(); element.UpdateLayout();
    }
    static void Capture(FrameworkElement element, string path, int width, int height)
    {
        Layout(element, width, height);
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(element);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
        using (var stream = File.Create(path)) png.Save(stream);
    }
    static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T) yield return (T)child;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    static void ShowSection(DashboardView page, string section)
    {
        var nav = (RadioButton)typeof(DashboardView).GetMethod("NavDe", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(page, new object[] { section });
        nav.IsChecked = true;
        typeof(DashboardView).GetMethod("MostrarSeccion", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(page, new object[] { section });
    }
    static double Luminance(Color color)
    {
        Func<byte, double> channel = value => value / 255.0 <= 0.04045 ? value / 255.0 / 12.92 : Math.Pow((value / 255.0 + 0.055) / 1.055, 2.4);
        return 0.2126 * channel(color.R) + 0.7152 * channel(color.G) + 0.0722 * channel(color.B);
    }
    static void VerifyContrast(string foreground, string background)
    {
        double a = Luminance(((SolidColorBrush)Application.Current.FindResource("UI." + foreground)).Color);
        double b = Luminance(((SolidColorBrush)Application.Current.FindResource("UI." + background)).Color);
        double ratio = (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        if (ratio < 4.5) throw new Exception("Contrast below 4.5:1: " + foreground + "/" + background);
    }
    static void VerifyInteractions(string theme, string output)
    {
        var page = new DashboardView();
        var parameters = new HwndSourceParameters("Crown frontend verification") { Width = 1200, Height = 760, PositionX = -20000, PositionY = -20000, WindowStyle = unchecked((int)0x80000000) };
        using (var source = new HwndSource(parameters))
        {
            source.RootVisual = page; Pump(); Layout(page, 1172, 702);
            ShowSection(page, "habitaciones"); Layout(page, 1172, 702);
            var rooms = (FrameworkElement)page.FindName("VistaHabitaciones");
            var grid = (DataGrid)rooms.FindName("GridHabitaciones");
            ((ComboBox)rooms.FindName("CmbCapacidad")).SelectedItem = "Suite"; Pump();
            if (!page.IsLoaded || grid.Items.Count == 0 || grid.Items.Cast<HabitacionVista>().Any(room => room.Tipo != "Suite"))
                throw new Exception("Room type filter did not refresh the displayed availability.");
            grid.SelectedIndex = 0;
            Capture(page, Path.Combine(output, "room-selected-" + theme + ".png"), 1172, 702);
            ShowSection(page, "inventario"); Layout(page, 1172, 702);
            var inventory = (FrameworkElement)page.FindName("VistaInventario");
            ((ListBox)inventory.FindName("ListaPisos")).SelectedIndex = 1; Layout(page, 1172, 702);
            var tiles = (ItemsControl)inventory.FindName("ListaHabitaciones");
            var radio = Descendants<RadioButton>(tiles).First(); radio.IsChecked = true; Pump();
            if (!((TextBox)inventory.FindName("TxtNumeroHabitacion")).Text.StartsWith("2")) throw new Exception("Room selection did not update the existing detail panel.");
            System.Threading.Thread.Sleep(180); Pump();
            Capture(page, Path.Combine(output, "inventory-selected-" + theme + ".png"), 1172, 702);
            ShowSection(page, "habitaciones"); Layout(page, 1172, 702);
            var date = (DatePicker)rooms.FindName("DpInicio"); date.ApplyTemplate();
            ((Button)date.Template.FindName("PART_Button", date)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Pump();
            if (!date.IsDropDownOpen) throw new Exception("Calendar button did not open the native picker.");
            var popup = (Popup)date.Template.FindName("PART_Popup", date);
            var calendar = popup.Child as Calendar ?? Descendants<Calendar>(popup.Child).First();
            calendar.SelectedDate = DateTime.Today.AddDays(-1); Pump();
            if (date.SelectedDate != calendar.SelectedDate) throw new Exception("Calendar selection did not update the date field.");
            Capture(calendar, Path.Combine(output, "calendar-" + theme + ".png"), 300, 280);
            date.IsDropDownOpen = false;
            ShowSection(page, "reservasadmin"); Layout(page, 1172, 702);
            var advanced = (FrameworkElement)page.FindName("VistaReservasAdmin");
            var agenda = (ItemsControl)advanced.FindName("AgendaSemana");
            var bookingRadio = Descendants<RadioButton>(agenda).FirstOrDefault(button => button.IsChecked != true);
            if (bookingRadio == null) throw new Exception("Calendar reservation cards did not load.");
            bookingRadio.IsChecked = true; Pump();
            var booking = (Crown__.Frontend.Views.Modules.ReservasGestionView.BookingPresentation)bookingRadio.DataContext;
            if ((string)((ComboBox)advanced.FindName("CmbHuespedDetalle")).SelectedItem != booking.Reservation.Huesped)
                throw new Exception("Calendar selection did not update the guest detail.");
            ((ComboBox)advanced.FindName("CmbEstadoAgenda")).SelectedItem = "Confirmada"; Pump();
            var days = agenda.Items.Cast<Crown__.Frontend.Views.Modules.ReservasGestionView.DayPresentation>();
            if (days.SelectMany(day => day.Bookings).Any(item => item.Reservation.EstadoCrudo != "Confirmada"))
                throw new Exception("Calendar status filter did not refresh.");
            ShowSection(page, "huespedes"); Layout(page, 1172, 702);
            var guests = (FrameworkElement)page.FindName("VistaHuespedes");
            ((TextBox)guests.FindName("TxtBuscarHuesped")).Text = "zzzz-no-match"; Pump();
            if (((ListBox)guests.FindName("ListaHuespedes")).Items.Count != 0) throw new Exception("Guest filter did not refresh.");
            source.RootVisual = null;
        }
    }
    [STAThread] static int Main(string[] args)
    {
        try
        {
            var app = new Application();
            foreach (string resource in new[] { "Frontend/Themes/HotelPalette.xaml", "Frontend/Styles/ModernControls.xaml", "Frontend/Styles/LiquidGlassStyles.xaml", "Frontend/Styles/DashboardStyles.xaml", "Frontend/Styles/OperationsStyles.xaml" })
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Crown &;component/" + resource, UriKind.Relative) });
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            string output = args.Length > 0 ? args[0] : ".design-preview/modern";
            Directory.CreateDirectory(output);
            if (!args.Contains("--themes-only")) foreach (string theme in new[] { "Light", "Dark", "Brand" })
            {
                ThemePresentation.Apply(theme);
                VerifyContrast("Text", "Surface"); VerifyContrast("Muted", "Surface"); VerifyContrast("Muted", "Page"); VerifyContrast("OnAccent", "Accent");
                var login = new LoginView();
                Capture(login, Path.Combine(output, "login-" + theme + ".png"), 1172, 702);
                if ((string)login.Tag != theme) throw new Exception("Login theme failed.");
                Capture(login, Path.Combine(output, "login-" + theme + "-minimum.png"), 892, 602);
                var page = new DashboardView(); Layout(page, 1172, 702);
                page.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                foreach (string viewName in new[] { "VistaFuncionamiento", "VistaReservas", "VistaHabitaciones", "VistaIngresos", "VistaInventario", "VistaReservasAdmin", "VistaEstancias" })
                    ((FrameworkElement)page.FindName(viewName)).RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                if ((string)page.Tag != theme) throw new Exception("Dashboard theme failed.");
                foreach (string section in new[] { "funcionamiento", "habitaciones", "inventario", "reservas", "huespedes", "configuracion", "tarifas", "limpieza", "seguridad", "pagos", "estancias", "consumos", "facturacion", "notificaciones", "convenios", "reservasadmin", "ingresos" })
                {
                    ShowSection(page, section);
                    Capture(page, Path.Combine(output, section + "-" + theme + ".png"), 1172, 702);
                    Capture(page, Path.Combine(output, section + "-" + theme + "-minimum.png"), 892, 602);
                    if (section == "reservasadmin" || section == "inventario" || section == "funcionamiento")
                        Capture(page, Path.Combine(output, section + "-" + theme + "-wide.png"), 1536, 824);
                    if (section == "reservasadmin")
                    {
                        var workspace = (FrameworkElement)page.FindName("VistaReservasAdmin");
                        var detailGrid = (DataGrid)workspace.FindName("GridHuespedesDetalle");
                        if (detailGrid.ActualHeight < 80) throw new Exception("The reservation detail cannot show its first guest row at wide size: " + detailGrid.ActualHeight);
                    }
                }
                var reserves = (FrameworkElement)page.FindName("VistaReservas");
                typeof(DashboardView).GetMethod("MostrarSeccion", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(page, new object[] { "reservas" });
                reserves.GetType().GetMethod("OnNuevaReserva", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(reserves, new object[] { null, new RoutedEventArgs() });
                Capture(page, Path.Combine(output, "new-reservation-" + theme + ".png"), 1172, 702);
                Capture(page, Path.Combine(output, "new-reservation-" + theme + "-minimum.png"), 892, 602);
                var saveReservation = Descendants<Button>(reserves).First(button => button.Content as string == "Guardar reserva");
                var savePosition = saveReservation.TranslatePoint(new Point(0, 0), page);
                if (savePosition.Y + saveReservation.ActualHeight > 602 || savePosition.Y < 0)
                    throw new Exception("Reservation save action is outside the minimum window.");
                foreach (Window dialog in new Window[] { new NuevaHabitacionWindow(), new NuevaReservaWindow(), new NuevoHuespedWindow() })
                {
                    Capture((FrameworkElement)dialog.Content, Path.Combine(output, dialog.GetType().Name + "-" + theme + ".png"), (int)dialog.Width - 24, (int)dialog.Height - 40);
                    dialog.Close();
                }
                VerifyInteractions(theme, output);
                Console.WriteLine("PASS " + theme + ": all 17 sections at both sizes, three dialogs, room filters, floor selection, calendar and contrast.");
            }
            ThemePresentation.Apply("Light");
            var togglePage = new LoginView(); Layout(togglePage, 1172, 702);
            var toggle = (ButtonBase)togglePage.FindName("ThemeToggle");
            var existingDashboard = new DashboardView(); Layout(existingDashboard, 1172, 702);
            // Application resource invalidation traverses its Window collection.
            // The windows remain unshown, but own the same logical tree as the live app.
            var themeHost = new Window { Content = togglePage };
            var dashboardHost = new Window { Content = existingDashboard }; Pump();
            foreach (string expected in new[] { "Dark", "Brand", "Light" })
            {
                toggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Pump();
                if (ThemePresentation.Current != expected) throw new Exception("Theme cycle failed.");
                var newDashboard = new DashboardView(); Layout(newDashboard, 1172, 702);
                if ((string)existingDashboard.Tag != expected || (string)togglePage.Tag != expected || (string)newDashboard.Tag != expected)
                    throw new Exception("Theme did not propagate: expected " + expected + ", dashboard=" + existingDashboard.Tag + ", login=" + togglePage.Tag + ", new=" + newDashboard.Tag);
            }
            themeHost.Close(); dashboardHost.Close();
            Console.WriteLine("PASS shared three-theme cycle.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}

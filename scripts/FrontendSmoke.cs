using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

// Compiled separately; exercises real WPF templates and routed events without showing a window.
internal static class FrontendSmoke
{
    private static int checks;
    private static readonly string[] Modules = { "Inicio", "Reservas", "Habitaciones", "CheckInOut", "Huespedes", "Convenios", "FNB", "GymSpa", "Lavanderia", "Finanzas", "Reportes", "Config" };
    [STAThread]
    private static int Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs e) {
            return e.Name.StartsWith("Crown &,") ? Assembly.LoadFrom(Environment.GetEnvironmentVariable("CROWN_FRONTEND_ASSEMBLY")) : null;
        };
        try { return Run(); } catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static int Run()
    {
        var assembly = Assembly.LoadFrom(Environment.GetEnvironmentVariable("CROWN_FRONTEND_ASSEMBLY"));
        var app = (Application)Activator.CreateInstance(assembly.GetType("Crown__.App")); Call(app, "InitializeComponent");
        var dashboardType = assembly.GetType("Crown__.Frontend.Views.DashboardView");
        foreach (string theme in new[] { "Light", "Dark", "Brand" })
        {
            var page = (Page)Activator.CreateInstance(dashboardType, new object[] { theme });
            foreach (int width in new[] { 892, 1172, 1885 })
            {
                int height = width == 892 ? 602 : width == 1172 ? 702 : 1000;
                foreach (string module in Modules)
                {
                    Call(page, "ShowModule", module); Layout(page, width, height);
                    Assert(((Panel)page.FindName("ModuleContent")).Children.Count > 0, "Module has no content: " + module);
                    Assert(((TextBlock)page.FindName("PageTitle")).ActualHeight > 20, "Title is clipped: " + module);
                    Assert(page.DesiredSize.Width <= width, "Page overflows minimum width: " + module);
                    if (width == 1172 || (width == 892 && (module == "Inicio" || module == "Habitaciones" || module == "Config")))
                        Save(page, "dashboard-" + module + "-" + theme + "-" + width + ".png", width, height);
                }
            }
            var nav = (Panel)page.FindName("NavigationItems");
            Assert(nav.Children.Count == 12, "Missing navigation module.");
            foreach (var path in Descendants<System.Windows.Shapes.Path>(nav))
                Assert(path.Stroke is SolidColorBrush && ((SolidColorBrush)path.Stroke).Color == ((SolidColorBrush)page.Foreground).Color, "Navigation icon does not match theme.");
            foreach (string module in Modules.Where(m => m != "Inicio" && m != "Config"))
            {
                Call(page, "ShowModule", module); Layout(page, 1172, 702);
                var search = (TextBox)Field(page, "searchBox"); var state = (ComboBox)Field(page, "stateFilter");
                search.Text = "no-such-record-987"; Pump();
                Assert(((TextBlock)Field(page, "resultCount")).Text.StartsWith("0 "), "Empty filter failed: " + module);
                search.Clear(); state.SelectedIndex = 1; Pump();
                Assert(!((TextBlock)Field(page, "resultCount")).Text.StartsWith("0 "), "State filter failed: " + module);
                state.SelectedIndex = 0;
                if (module == "Huespedes") { search.Text = "maria"; Assert(((TextBlock)Field(page, "resultCount")).Text.StartsWith("1 "), "Accent-insensitive search failed."); search.Clear(); }
                Call(page, "OpenForm"); Layout(page, 1172, 702);
                Click((Button)page.FindName("DialogSubmit"));
                Layout(page, 892, 602);
                var submitPosition = ((Button)page.FindName("DialogSubmit")).TransformToAncestor(page).Transform(new Point(0, 0));
                Assert(submitPosition.Y >= 0 && submitPosition.Y + ((Button)page.FindName("DialogSubmit")).ActualHeight <= 602, "Form actions clipped at minimum size: " + module);
                if (module == "Reservas") Save(page, "reservation-errors-minimum-" + theme + ".png", 892, 602);
                Layout(page, 1172, 702);
                Assert(((TextBlock)page.FindName("DialogError")).Visibility == Visibility.Visible, "Empty form accepted: " + module);
                var fields = ((IEnumerable)Field(page, "formFields")).Cast<object>().ToArray();
                int dateIndex = 0;
                foreach (var field in fields)
                {
                    object definition = Field(field, "Definition"); string kind = (string)Field(definition, "Kind");
                    Control input = (Control)Field(field, "Input");
                    if (input is ComboBox) ((ComboBox)input).SelectedIndex = 1;
                    else ((TextBox)input).Text = kind == "date" ? (dateIndex++ == 0 ? "31/10/2026" : "03/11/2026") : kind == "time" ? "14:30" : kind == "email" ? "maria@example.com" : kind == "phone" ? "+591 7000 0123" : kind == "integer" ? "2" : kind == "amount" ? "125,50" : "María de Prueba";
                    if (input is TextBox) {
                        input.ApplyTemplate();
                        var placeholder = input.Template.FindName("Placeholder", input) as TextBlock;
                        Assert(placeholder == null || placeholder.Visibility == Visibility.Collapsed, "Hint overlaps typed text: " + module);
                    }
                }
                Click((Button)page.FindName("DialogSubmit")); Layout(page, 1172, 702);
                Assert((bool)Field(page, "reviewingDraft"), "Valid draft rejected: " + module);
                if (module == "Reservas") Save(page, "reservation-draft-" + theme + ".png", 1172, 702);
                Click((Button)page.FindName("DialogSubmit"));
                Assert(!(bool)Field(page, "reviewingDraft"), "Return to editing failed.");
                foreach (var field in fields)
                {
                    string kind = (string)Field(Field(field, "Definition"), "Kind"); var input = Field(field, "Input") as TextBox;
                    if (input == null || !new[] { "date", "time", "email", "phone", "integer", "amount" }.Contains(kind)) continue;
                    string valid = input.Text;
                    input.Text = kind == "date" ? "31/02/2026" : kind == "time" ? "25:61" : kind == "email" ? "no-email" : kind == "phone" ? "abc" : kind == "integer" ? "0" : "-20";
                    Click((Button)page.FindName("DialogSubmit"));
                    Assert(!(bool)Field(page, "reviewingDraft"), "Invalid " + kind + " accepted: " + module);
                    input.Text = valid;
                }
                if (module == "Reservas" || module == "Reportes") {
                    var dates = fields.Where(f => (string)Field(Field(f, "Definition"), "Kind") == "date").ToArray();
                    ((TextBox)Field(dates[1], "Input")).Text = "30/10/2026"; Click((Button)page.FindName("DialogSubmit"));
                    Assert(!(bool)Field(page, "reviewingDraft"), "Inverted date range accepted.");
                }
                Call(page, "OnCloseDialog", null, new RoutedEventArgs());
                Assert(((Grid)page.FindName("Workspace")).IsEnabled, "Workspace left disabled.");
            }
            Call(page, "ShowModule", "Reservas");
            var persistentSearch = (TextBox)Field(page, "searchBox"); persistentSearch.Text = "María";
            for (int i = 0; i < 3; i++) {
                Click((Button)page.FindName("ThemeButton")); Layout(page, 1172, 702);
                Assert(persistentSearch.Text == "María", "Theme switch reset user input.");
                foreach (var path in Descendants<System.Windows.Shapes.Path>(nav))
                    Assert(path.Stroke is SolidColorBrush && ((SolidColorBrush)path.Stroke).Color == ((SolidColorBrush)page.Foreground).Color, "Icon failed to update after theme switch.");
            }
            Console.WriteLine("PASS: " + theme + ", 12 modules at 3 sizes, filters, forms, validation and draft editing.");
        }
        foreach (string theme in new[] { "Light", "Dark", "Brand" })
        {
            var login = (Page)Activator.CreateInstance(assembly.GetType("Crown__.Frontend.Views.LoginView"), new object[] { theme }); Layout(login, 1172, 702);
            Assert(((SolidColorBrush)((Button)login.FindName("ThemeButton")).Foreground).Color == ((SolidColorBrush)login.Foreground).Color, "Login theme label contrast regression.");
            var user = (TextBox)login.FindName("TxtUsuario");
            foreach (string value in new[] { "", "Tu Usuario", "áéíóú prueba", "", "admin" }) {
                user.Text = value; Pump(); user.ApplyTemplate();
                var placeholder = (TextBlock)user.Template.FindName("Placeholder", user);
                Assert(placeholder.Visibility == (value.Length == 0 ? Visibility.Visible : Visibility.Collapsed), "Login user hint regression.");
            }
            var password = (PasswordBox)login.FindName("TxtContrasena"); var reveal = (TextBox)login.FindName("RevealBox"); var eye = (ToggleButton)login.FindName("EyeToggle");
            password.Password = "Contraseña 123"; Assert(((TextBlock)login.FindName("PassPlaceholder")).Visibility == Visibility.Collapsed, "Password hint overlaps text.");
            eye.IsChecked = true; eye.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Assert(reveal.Text == password.Password, "Password reveal lost value.");
            reveal.Text = "Nueva 456"; eye.IsChecked = false; eye.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Assert(password.Password == "Nueva 456", "Password hide lost edited value.");
            password.Clear(); Assert(((TextBlock)login.FindName("PassPlaceholder")).Visibility == Visibility.Visible, "Password hint did not return.");
            Save(login, "login-current-" + theme + ".png", 1172, 702);
            Layout(login, 892, 602); Save(login, "login-minimum-" + theme + ".png", 892, 602);
        }
        Console.WriteLine("PASS: " + checks + " assertions. Compiled WPF renders saved to .design-preview. No interactive-window screenshots.");
        return 0;
    }
    private static object Field(object target, string name) { return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(target); }
    private static object Call(object target, string method, params object[] args) { return target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(target, args); }
    private static void Click(Button button) { button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Pump(); }
    private static void Assert(bool success, string message) { checks++; if (!success) throw new Exception(message); }
    private static void Pump() { Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { })); }
    private static void Layout(FrameworkElement view, int width, int height) { view.Measure(new Size(width, height)); view.Arrange(new Rect(0, 0, width, height)); view.UpdateLayout(); Pump(); view.UpdateLayout(); }
    private static IEnumerable<T> Descendants<T>(DependencyObject node) where T : DependencyObject {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) { var child = VisualTreeHelper.GetChild(node, i); if (child is T) yield return (T)child; foreach (var descendant in Descendants<T>(child)) yield return descendant; }
    }
    private static void Save(FrameworkElement view, string name, int width, int height) {
        Layout(view, width, height); var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(view);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using (var stream = File.Create(Path.Combine(".design-preview", name))) encoder.Save(stream);
    }
}

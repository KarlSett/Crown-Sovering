using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

// Compiled separately; exercises real WPF templates and routed events without showing a window.
internal static class FrontendSmoke
{
    private static int checks;
    private static readonly string[] Modules = {
        "Inicio", "Alertas", "Preferencias",
        "Reservas", "Habitaciones", "CheckInOut", "Folios", "Operacion",
        "Housekeeping", "Mantenimiento", "FNB", "Inventario", "GymSpa", "Lavanderia", "Transporte",
        "Eventos", "Convenios", "Tarifas", "Finanzas", "Reportes",
        "Usuarios", "Auditoria", "Config"
    };

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
        var contextType = assembly.GetType("Crown__.Frontend.Security.FrontendAccessContext");
        var limitsType = assembly.GetType("Crown__.Frontend.Views.HotelEventsAndLimits");
        Func<string, object> demo = role => contextType.GetMethod("CreateDemonstration").Invoke(null, new object[]{role});
        var allPermissions = new List<string>();
        foreach (string m in Modules)
            foreach (string a in new[] { "View", "Write", "Cancel", "Approve", "Export", "Manage", "Privacy", "Limit" })
                allPermissions.Add(m + "." + a);
        Func<object> fullTestContext = () => Activator.CreateInstance(contextType, new object[] { "smoke-all", "Verificación", "Verificación", allPermissions });

        foreach (string theme in new[] { "Light", "Dark", "Brand" })
        {
            limitsType.GetMethod("ResetServiceLimits", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            var page = (Page)Activator.CreateInstance(dashboardType, new object[] { theme, fullTestContext() });
            foreach (int width in new[] { 892, 1172, 1885 })
            {
                int height = width == 892 ? 602 : width == 1172 ? 702 : 1000;
                foreach (string module in Modules)
                {
                    Call(page, "ShowModule", module); Layout(page, width, height);
                    Assert(((Panel)page.FindName("ModuleContent")).Children.Count > 0, "Module has no content: " + module);
                    Assert(((TextBlock)page.FindName("PageTitle")).ActualHeight > 20, "Title is clipped: " + module);
                    Assert(page.DesiredSize.Width <= width + 1, "Page overflows minimum width: " + module + ", desired " + page.DesiredSize.Width);
                    if (width == 1172 || (width == 892 && (module == "Inicio" || module == "Habitaciones" || module == "Config" || module == "Preferencias" || module == "Eventos" || module == "CheckInOut")))
                        Save(page, "dashboard-" + module + "-" + theme + "-" + width + ".png", width, height);
                }
            }

            var nav = (Panel)page.FindName("NavigationItems");
            Assert(Descendants<Button>(nav).Count() == Modules.Length, "Missing navigation module.");
            Assert(!Descendants<Button>(nav).Any(b => AutomationProperties.GetAutomationId(b) == "NavHuespedes"), "Separate Huespedes menu was not unified.");
            foreach (var path in Descendants<System.Windows.Shapes.Path>(nav))
                Assert(path.Stroke is SolidColorBrush && ((SolidColorBrush)path.Stroke).Color == Colors.White, "Navigation icon does not match theme.");

            if (theme == "Brand")
            {
                var brandPageBrush = (SolidColorBrush)page.Resources["Ui.Page"];
                var brandNavBrush = (SolidColorBrush)page.Resources["Ui.Nav"];
                var brandPrimaryBrush = (SolidColorBrush)page.Resources["Ui.Primary"];
                Assert(brandPageBrush.Color == Color.FromRgb(0xEB, 0xFE, 0xF7), "Brand page background must be #EBFEF7.");
                Assert(brandNavBrush.Color == Color.FromRgb(0x00, 0x30, 0x29), "Brand sidebar background must be #003029.");
                Assert(brandPrimaryBrush.Color == Color.FromRgb(0x00, 0x4D, 0x40), "Brand primary color must be #004D40.");
            }

            // Verify unified CheckInOut + Huespedes sub-views + RF-27 fuzzy & external directory search
            Call(page, "ShowModule", "CheckInOut"); Layout(page, 1172, 702);
            var tabHuespedes = Descendants<Button>((Panel)page.FindName("ModuleContent")).FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "TabHuespedes");
            Assert(tabHuespedes != null, "Unified guest directory tab missing.");
            Click(tabHuespedes); Layout(page, 1172, 702);
            var guestSearch = (TextBox)Field(page, "searchBox");
            Assert(((TextBlock)Field(page, "resultCount")).Text.StartsWith("5 "), "Guest directory count incorrect.");
            guestSearch.Text = "maria"; Pump();
            Assert(((TextBlock)Field(page, "resultCount")).Text.StartsWith("1 "), "Accent-insensitive guest search failed.");
            guestSearch.Text = "fernadnez"; Pump();
            Assert(((TextBlock)Field(page, "resultCount")).Text.StartsWith("1 "), "RF-27 typo-tolerant fuzzy guest search failed.");
            guestSearch.Text = "inexistente-xyz-999"; Pump();
            var extBtn = Descendants<Button>((Panel)page.FindName("ModuleContent")).FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "BtnDirectorioExterno");
            Assert(extBtn != null, "RF-27 external directory button missing on empty guest search.");
            Click(extBtn); Layout(page, 1172, 702);
            Assert(((TextBlock)Field(page, "resultCount")).Text.Contains("RF-27"), "RF-27 external directory results did not load.");
            guestSearch.Clear();
            if (theme == "Light" || theme == "Brand") Save(page, "dashboard-CheckInOut-Guests-" + theme + "-1172.png", 1172, 702);
            var tabMovimientos = Descendants<Button>((Panel)page.FindName("ModuleContent")).First(b => AutomationProperties.GetAutomationId(b) == "TabMovimientos");
            Click(tabMovimientos); Layout(page, 1172, 702);
            Assert(((TextBlock)Field(page, "resultCount")).Text.StartsWith("7 "), "Check-in/out movements count incorrect.");

            // Verify Preferencias ComboBox + Alt+T shortcut cycle
            Call(page, "ShowModule", "Preferencias"); Layout(page, 1172, 702);
            var themeCombo = Descendants<ComboBox>((Panel)page.FindName("ModuleContent")).FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == "ThemeComboBox");
            Assert(themeCombo != null && themeCombo.Items.Count == 3 && themeCombo.ItemTemplate != null, "Preferences theme ComboBox missing or unstyled.");
            int initialThemeIdx = themeCombo.SelectedIndex;
            Call(page, "CycleTheme"); Layout(page, 1172, 702);
            Assert(themeCombo.SelectedIndex == (initialThemeIdx + 1) % 3, "Alt+T CycleTheme did not sync Preferences ComboBox.");
            themeCombo.SelectedIndex = initialThemeIdx; Layout(page, 1172, 702);
            Assert((string)page.Tag == theme, "Selecting theme via ComboBox did not restore active theme.");

            // Verify Ctrl+Shift+Up / Ctrl+Shift+Down relative menu navigation
            Call(page, "ShowModule", "Inicio");
            Call(page, "NavigateRelativeMenu", 1);
            Assert((string)Field(Field(page, "currentModule"), "Key") == "Alertas", "Ctrl+Shift+Down failed to advance menu.");
            Call(page, "NavigateRelativeMenu", 1);
            Assert((string)Field(Field(page, "currentModule"), "Key") == "Preferencias", "Ctrl+Shift+Down failed to reach Preferencias.");
            Call(page, "NavigateRelativeMenu", -1);
            Assert((string)Field(Field(page, "currentModule"), "Key") == "Alertas", "Ctrl+Shift+Up failed to step back.");
            Call(page, "ShowModule", "Inicio");
            Call(page, "NavigateRelativeMenu", -1);
            Assert((string)Field(Field(page, "currentModule"), "Key") == "Config", "Ctrl+Shift+Up wrap-around failed.");

            // Verify Eventos upcoming calendar & prefilled discount draft
            Call(page, "ShowModule", "Eventos"); Layout(page, 1172, 702);
            var eventBtn = Descendants<Button>((Panel)page.FindName("ModuleContent")).FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "EventDay_EVT-202_12");
            Assert(eventBtn != null, "Upcoming event calendar button missing.");
            Click(eventBtn); Layout(page, 1172, 702);
            var prepEventBtn = Descendants<Button>((Panel)page.FindName("DialogBody")).FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "EventPrepareDiscount");
            Assert(prepEventBtn != null, "Event contextual discount button missing.");
            Click(prepEventBtn); Layout(page, 1172, 702);
            var evtFields = ((IEnumerable)Field(page, "formFields")).Cast<object>().ToArray();
            Assert(((TextBox)Field(evtFields[0], "Input")).Text.Contains("Congreso"), "Event discount form did not prefill event title.");
            Assert(((TextBox)Field(evtFields[3], "Input")).Text == "12", "Event discount form did not prefill suggested percentage.");
            Call(page, "OnCloseDialog", null, new RoutedEventArgs());

            foreach (string module in Modules.Where(m => m != "Inicio" && m != "Preferencias" && m != "Config"))
            {
                Call(page, "ShowModule", module); Layout(page, 1172, 702);
                var search = (TextBox)Field(page, "searchBox"); var state = (ComboBox)Field(page, "stateFilter");
                search.Text = "no-such-record-987"; Pump();
                Assert(((TextBlock)Field(page, "resultCount")).Text.StartsWith("0 "), "Empty filter failed: " + module);
                search.Clear(); state.SelectedIndex = 1; Pump();
                Assert(!((TextBlock)Field(page, "resultCount")).Text.StartsWith("0 "), "State filter failed: " + module);
                state.SelectedIndex = 0;
                if (module == "FNB" || module == "GymSpa")
                {
                    var sub = (ComboBox)Field(page, "subAreaFilter");
                    Assert(sub != null, "Sub-area filter missing for " + module);
                    sub.SelectedIndex = 1; Pump();
                    Assert(!((TextBlock)Field(page, "resultCount")).Text.StartsWith("0 "), "Sub-area filter failed: " + module);
                    sub.SelectedIndex = 0;
                }
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
                    else ((TextBox)input).Text = kind == "date" ? (dateIndex++ == 0 ? "31/10/2026" : "03/11/2026") : kind == "time" ? "14:30" : kind == "email" ? "maria@example.com" : kind == "phone" ? "+591 7000 0123" : kind == "integer" ? "2" : kind == "nonnegative" ? "0" : kind == "amount" ? "125,50" : "María de Prueba";
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
                if (module == "Reservas")
                {
                    // RNF-02: simulated network loss blocks submission while preserving typed inputs
                    Call(page, "SetSimulatedOffline", true);
                    Assert(((Border)page.FindName("OfflineBanner")).Visibility == Visibility.Visible, "RNF-02 OfflineBanner did not appear.");
                    Click((Button)page.FindName("DialogSubmit")); Layout(page, 1172, 702);
                    Assert(!(bool)Field(page, "reviewingDraft") && ((TextBlock)page.FindName("DialogError")).Visibility == Visibility.Visible, "RNF-02 offline guard did not block critical submission.");
                    Assert(((TextBox)Field(fields[1], "Input")).Text == "María de Prueba", "RNF-02 offline guard lost typed form input.");
                    Call(page, "SetSimulatedOffline", false);
                    Assert(((Border)page.FindName("OfflineBanner")).Visibility == Visibility.Collapsed, "RNF-02 OfflineBanner did not hide on network restore.");
                }
                foreach (var field in fields)
                {
                    string kind = (string)Field(Field(field, "Definition"), "Kind"); var input = Field(field, "Input") as TextBox;
                    if (input == null || !new[] { "date", "time", "email", "phone", "integer", "amount", "nonnegative" }.Contains(kind)) continue;
                    string valid = input.Text;
                    input.Text = kind == "date" ? "31/02/2026" : kind == "time" ? "25:61" : kind == "email" ? "no-email" : kind == "phone" ? "abc" : kind == "integer" ? "0" : "-20";
                    Click((Button)page.FindName("DialogSubmit"));
                    Assert(!(bool)Field(page, "reviewingDraft"), "Invalid " + kind + " accepted: " + module);
                    input.Text = valid;
                }
                if (module == "Reservas" || module == "Reportes" || module == "Eventos") {
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
                Call(page, "CycleTheme"); Layout(page, 1172, 702);
                Assert(persistentSearch.Text == "María", "Theme switch reset user input.");
                foreach (var path in Descendants<System.Windows.Shapes.Path>(nav))
                    Assert(path.Stroke is SolidColorBrush && ((SolidColorBrush)path.Stroke).Color == Colors.White, "Icon failed to update after theme switch.");
            }
            VerifyWorkflows(page, assembly, theme);
            Console.WriteLine("PASS: " + theme + ", 23 modules at 3 sizes, unified CheckInOut/Guests, Eventos, Preferencias, shortcuts, workflows and validation.");
        }

        VerifyRoles(assembly, dashboardType, contextType, limitsType, demo);
        VerifyImports(assembly);

        foreach (string theme in new[] { "Light", "Dark", "Brand" })
        {
            var login = (Page)Activator.CreateInstance(assembly.GetType("Crown__.Frontend.Views.LoginView"), new object[] { theme }); Layout(login, 1172, 702);
            var roleCombo = (ComboBox)login.FindName("CmbRolDemo");
            var loginThemeBtn = (Button)login.FindName("ThemeButton");
            Assert(roleCombo != null && roleCombo.Items.Count == 3, "Login role selector ComboBox must have exactly 3 roles (Administrador, Gerente, Recepcionista).");
            Assert(loginThemeBtn != null, "Original Login ThemeButton missing.");
            string beforeLabel = (string)loginThemeBtn.Content;
            Call(login, "CycleTheme"); Layout(login, 1172, 702);
            Assert((string)loginThemeBtn.Content != beforeLabel, "Login Alt+T CycleTheme did not update ThemeButton.");
            Call(login, "CycleTheme"); Call(login, "CycleTheme"); Layout(login, 1172, 702);
            Assert((string)login.Tag == theme && (string)loginThemeBtn.Content == beforeLabel, "Theme cycle did not restore initial Login theme.");

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

    private static void VerifyWorkflows(Page page, Assembly assembly, string theme)
    {
        var catalog = assembly.GetType("Crown__.Frontend.Views.OperationsCatalog");
        var workflows = ((IEnumerable)catalog.GetField("Workflows", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)).Cast<object>().ToArray();
        foreach (var workflow in workflows) {
            string module = (string)Field(workflow, "Module"), key = (string)Field(workflow, "Key");
            Call(page, "ShowModule", module); Call(page, "SelectWorkflow", key); Layout(page, 892, 602);
            Assert(Field(page, "currentWorkflow") == workflow, "Workflow missing: " + key);
            Assert(page.DesiredSize.Width <= 893, "Workflow overflows minimum width: " + key);
            if (new[]{"Calendario", "Importar", "Roles"}.Contains(key)) {
                if (theme == "Light") Save(page, "workflow-" + key + "-minimum.png", 892, 602);
                continue;
            }
            Call(page, "OpenForm"); Layout(page, 892, 602);
            Assert(((Grid)page.FindName("Overlay")).Visibility == Visibility.Visible, "Workflow form did not open: " + key);
            if (key == "CheckIn")
            {
                var autoBtn = Descendants<Button>((Panel)page.FindName("DialogBody")).FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "AutocompleteGuest_H-001");
                Assert(autoBtn != null, "RF-17/RF-18 guest history autocomplete button missing in CheckIn.");
                Click(autoBtn); Layout(page, 892, 602);
                var prefilledFields = ((IEnumerable)Field(page, "formFields")).Cast<object>().ToArray();
                Assert(((TextBox)Field(prefilledFields[2], "Input")).Text == "María Fernández", "RF-17 autocomplete did not prefill Huésped titular.");
            }
            Click((Button)page.FindName("DialogSubmit"));
            Assert(!(bool)Field(page, "reviewingDraft"), "Empty workflow accepted: " + key);
            var fields = ((IEnumerable)Field(page, "formFields")).Cast<object>().ToArray(); int date = 0;
            foreach (var field in fields) {
                var input = (Control)Field(field, "Input"); string kind = (string)Field(Field(field, "Definition"), "Kind");
                if (input is ComboBox) ((ComboBox)input).SelectedIndex = 1;
                else ((TextBox)input).Text = kind == "date" ? (++date == 1 ? "31/10/2026" : "03/11/2026") : kind == "time" ? "14:30" : kind == "email" ? "maria@example.com" : kind == "amount" ? "125.50" : kind == "nonnegative" ? "0" : kind == "integer" ? "2" : "Referencia de muestra";
            }
            if (key == "CheckOut") ((TextBox)Field(fields.First(f => (string)Field(Field(f, "Definition"), "Label") == "Referencia de pago"), "Input")).Clear();
            Call(page, "OnHelpClick", null, new RoutedEventArgs());
            Assert(((IEnumerable)Field(page, "formFields")).Cast<object>().Count() == fields.Length, "Help destroyed the draft: " + key);
            for (int i = 0; i < 4 && !(bool)Field(page, "reviewingDraft"); i++) Click((Button)page.FindName("DialogSubmit"));
            Assert((bool)Field(page, "reviewingDraft"), "Valid workflow rejected: " + key);
            Click((Button)page.FindName("DialogSubmit"));
            Assert(!(bool)Field(page, "reviewingDraft"), "Workflow could not return to editor: " + key);
            if (key == "CheckIn" || key == "CheckOut") Save(page, "workflow-" + key + "-" + theme + ".png", 892, 602);
            Call(page, "OnCloseDialog", null, new RoutedEventArgs());
        }
        Call(page, "ShowModule", "Reservas"); Call(page, "SelectWorkflow", "Calendario");
        var period = Descendants<ComboBox>((Panel)page.FindName("ModuleContent")).First(c => AutomationProperties.GetAutomationId(c) == "CalendarPeriod");
        period.SelectedIndex = 2; Layout(page, 892, 602);
        Assert(page.DesiredSize.Width <= 893, "Monthly calendar overflows minimum width.");
        var stay = Descendants<Button>((Panel)page.FindName("ModuleContent")).FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "CalendarStay204");
        Assert(stay != null, "Calendar stay button was not found."); Click(stay);
        var prepare = Descendants<Button>((Panel)page.FindName("DialogBody")).FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "CalendarPrepare");
        Assert(prepare != null, "Calendar contextual prepare button was not found."); Click(prepare);
        var calendarFields = ((IEnumerable)Field(page,"formFields")).Cast<object>().ToArray();
        Assert(((TextBox)Field(calendarFields.First(f => (string)Field(Field(f,"Definition"),"Label") == "Reserva"),"Input")).Text == "RES-1048", "Calendar lost selected reservation.");
        Assert(((TextBox)Field(calendarFields.First(f => (string)Field(Field(f,"Definition"),"Label") == "Habitación preferida"),"Input")).Text == "204", "Calendar lost selected room.");
        Assert(((TextBox)Field(calendarFields.First(f => (string)Field(Field(f,"Definition"),"Label") == "Nueva salida"),"Input")).Text == "03/11/2026", "Calendar lost selected dates.");
        Call(page, "OnCloseDialog", null, new RoutedEventArgs());
        Call(page, "SelectWorkflow", "Calendario"); Layout(page, 892, 602);
        Click(Descendants<Button>((Panel)page.FindName("ModuleContent")).First(b => AutomationProperties.GetAutomationId(b).StartsWith("CalendarFree204-")));
        Layout(page, 892, 602);
        Click(Descendants<Button>((Panel)page.FindName("DialogBody")).First(b => AutomationProperties.GetAutomationId(b) == "CalendarPrepare"));
        var freeFields = ((IEnumerable)Field(page,"formFields")).Cast<object>().ToArray();
        Assert(((TextBox)Field(freeFields.First(f => (string)Field(Field(f,"Definition"),"Label") == "Habitación preferida"),"Input")).Text == "204", "Free calendar cell lost selected room.");
        Call(page, "OnCloseDialog", null, new RoutedEventArgs());
    }

    private static void VerifyRoles(Assembly assembly, Type dashboardType, Type contextType, Type limitsType, Func<string, object> demo)
    {
        limitsType.GetMethod("ResetServiceLimits", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        var demoRolesField = (string[])contextType.GetField("DemoRoles", BindingFlags.Static | BindingFlags.Public).GetValue(null);
        Assert(demoRolesField.Length == 3 && demoRolesField[0] == "Administrador" && demoRolesField[1] == "Gerente" && demoRolesField[2] == "Recepcionista",
            "DemoRoles must contain only Administrador, Gerente and Recepcionista.");

        var expected = new Dictionary<string, string[]> {
            {"Administrador", new[]{"Inicio","Alertas","Preferencias","Eventos","Tarifas","Convenios","Finanzas","Reportes","FNB","GymSpa","Lavanderia","Transporte","Inventario","Usuarios","Auditoria","Config"}},
            {"Gerente", new[]{"Inicio","Alertas","Preferencias","Eventos","Tarifas","Convenios","Finanzas","Reportes","FNB","GymSpa","Lavanderia","Transporte","Inventario"}},
            {"Recepcionista", new[]{"Inicio","Alertas","Preferencias","Reservas","Habitaciones","CheckInOut","Folios","Operacion","FNB","GymSpa","Housekeeping","Mantenimiento","Lavanderia","Transporte"}},
            {"Huesped", new string[0]}, {"desconocido", new string[0]}
        };
        foreach (var pair in expected) {
            var page = (Page)Activator.CreateInstance(dashboardType, new object[]{"Light", demo(pair.Key)}); Layout(page, 892, 602);
            var ids = Descendants<Button>((Panel)page.FindName("NavigationItems")).Select(b => AutomationProperties.GetAutomationId(b)).ToArray();
            Assert(ids.Length == pair.Value.Length && pair.Value.All(key => ids.Contains("Nav" + key)), "Role navigation incorrect: " + pair.Key);

            // Admin and Gerente must NOT see Reservas; only Recepcionista sees Reservas
            bool shouldSeeReservas = pair.Key == "Recepcionista";
            Assert(ids.Contains("NavReservas") == shouldSeeReservas, "Reservas visibility violated for role: " + pair.Key);

            // Eventos must ONLY be visible to Administrador and Gerente
            bool shouldSeeEventos = pair.Key == "Administrador" || pair.Key == "Gerente";
            Assert(ids.Contains("NavEventos") == shouldSeeEventos, "Eventos visibility violated for role: " + pair.Key);

            foreach (string forbidden in Modules.Except(pair.Value)) {
                object before = Field(page, "currentModule"); Call(page, "ShowModule", forbidden);
                Assert(Field(page, "currentModule") == before, "Direct navigation bypass: " + pair.Key + "/" + forbidden);
                Assert(((TextBlock)page.FindName("DialogTitle")).Text == "Acceso no disponible", "Missing denial feedback.");
                Call(page, "OnCloseDialog", null, new RoutedEventArgs());
            }

            // Gerente and Administrador can limit services; Recepcionista cannot
            if (pair.Value.Contains("GymSpa"))
            {
                Call(page, "ShowModule", "GymSpa"); Layout(page, 1172, 702);
                var limitBtn = Descendants<Button>((Panel)page.FindName("ModuleContent")).FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "LimitServiceBtn_SRV-SPA-1");
                if (pair.Key == "Administrador" || pair.Key == "Gerente")
                {
                    Assert(limitBtn != null, "Manager/Admin missing service limit button in GymSpa: " + pair.Key);
                    if (pair.Key == "Gerente")
                    {
                        Click(limitBtn); Layout(page, 1172, 702);
                        var limitFields = ((IEnumerable)Field(page, "formFields")).Cast<object>().ToArray();
                        ((ComboBox)Field(limitFields[0], "Input")).SelectedItem = "Horario limitado";
                        ((TextBox)Field(limitFields[1], "Input")).Text = "10:00–19:00";
                        Click((Button)page.FindName("DialogSubmit")); Layout(page, 1172, 702);
                        Assert((bool)Field(page, "reviewingDraft"), "Gerente service limit form failed to review.");
                        Call(page, "OnCloseDialog", null, new RoutedEventArgs());
                    }
                }
                else
                {
                    Assert(limitBtn == null, "Recepcionista exposed to service limit button: " + pair.Key);
                    Call(page, "SelectWorkflow", "LimitarGymSpa");
                    Assert(Field(page, "currentWorkflow") == null, "Unauthorized service limit workflow opened: " + pair.Key);
                    Call(page, "OnCloseDialog", null, new RoutedEventArgs());
                }
            }

            if (pair.Key == "Recepcionista") {
                Call(page, "ShowModule", "Folios"); Call(page, "SelectWorkflow", "Transferir");
                Assert(Field(page, "currentWorkflow") == null, "Unauthorized transfer opened for Recepcionista.");
                Call(page, "OnCloseDialog", null, new RoutedEventArgs());
            }

            Call(page, "ShowModule", pair.Value.Length > 0 ? "Inicio" : "Usuarios");
            Layout(page, 1172, 702); Save(page, "role-" + pair.Key + ".png", 1172, 702);
            if (pair.Key == "Administrador" || pair.Key == "Gerente")
                Assert(!Descendants<TextBlock>(page).Any(t => t.Text == "Reservas" || t.Text == "Nueva reserva"), "Reservas leaked onto Administrative/Managerial home: " + pair.Key);
        }
        var empty = (Page)Activator.CreateInstance(dashboardType, new object[]{"Light", null});
        Assert(!Descendants<Button>((Panel)empty.FindName("NavigationItems")).Any(), "Null access context must deny access.");
        var anonymous = Activator.CreateInstance(contextType, new object[]{"", "Invitado", "Administrador", new[]{"Usuarios.View","Usuarios.Manage"}});
        Assert(!(bool)contextType.GetMethod("Allows").Invoke(anonymous, new object[]{"Usuarios","Manage"}), "Blank identity accepted.");
        var custom = Activator.CreateInstance(contextType, new object[]{"u-1", "Operador", "Administrador", new[]{"Inicio.View","Reservas.View"}});
        Assert(!(bool)contextType.GetMethod("Allows").Invoke(custom, new object[]{"Usuarios","Manage"}), "Role label granted privileges.");
        var readOnly = (Page)Activator.CreateInstance(dashboardType, new object[]{"Light", custom});
        Call(readOnly, "ShowModule", "Reservas"); Call(readOnly, "OpenForm");
        Assert(((TextBlock)readOnly.FindName("DialogTitle")).Text == "Acceso no disponible", "Read-only profile opened write form.");
        Console.WriteLine("PASS: 3 roles only (Administrador, Gerente, Recepcionista), Admin/Gerente without Reservas, Eventos & service limits enforced.");
    }

    private static void VerifyImports(Assembly assembly)
    {
        var reader = assembly.GetType("Crown__.Frontend.Views.ReservationImportPreview");
        string csv = "huesped,llegada,salida,tipo,tarifa,canal\r\n\"Pérez, María\",31/10/2026,03/11/2026,Doble,125.50,Correo\r\nCarlos,03/11/2026,01/11/2026,Suite,-1,Correo\r\n";
        var parsed = reader.GetMethod("ParseCsv", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,new object[]{csv});
        var report = reader.GetMethod("ValidateRows", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,new[]{parsed});
        Assert((int)Field(report,"ValidRows") == 1, "CSV valid count incorrect.");
        Assert(((IList)Field(report,"Errors")).Count == 1, "CSV invalid row was not reported.");
        Assert(((string)((IList)Field(report,"Errors"))[0]).StartsWith("Fila 3:"), "CSV row number incorrect.");
        string path = Path.GetFullPath(Path.Combine(".design-preview", "import-smoke.xlsx"));
        using (var stream = File.Create(path)) using (var zip = new ZipArchive(stream, ZipArchiveMode.Create)) {
            WritePart(zip, "xl/workbook.xml", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Reservas\" sheetId=\"1\" r:id=\"rId3\" /></sheets></workbook>");
            WritePart(zip, "xl/_rels/workbook.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId3\" Target=\"worksheets/reservas.xml\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" /></Relationships>");
            var xml = new System.Text.StringBuilder("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            var rows = new[]{new[]{"huesped","llegada","salida","tipo","tarifa","canal"},new[]{"Maria","31/10/2026","03/11/2026","Doble","125.50","Correo"},new[]{"Carlos","03/11/2026","01/11/2026","Suite","-1","Correo"}};
            for (int r = 0; r < rows.Length; r++) {
                xml.Append("<row r=\"" + (r+1) + "\">");
                for (int c = 0; c < 6; c++) xml.Append("<c r=\"" + (char)('A'+c) + (r+1) + "\" t=\"inlineStr\"><is><t>" + rows[r][c] + "</t></is></c>");
                xml.Append("</row>");
            }
            xml.Append("</sheetData></worksheet>"); WritePart(zip,"xl/worksheets/reservas.xml",xml.ToString());
        }
        var excel = reader.GetMethod("Read",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{path});
        Assert((int)Field(excel,"ValidRows") == 1 && ((IList)Field(excel,"Errors")).Count == 1, "XLSX row preview incorrect.");
        Assert(((string)((IList)Field(excel,"Errors"))[0]).StartsWith("Fila 3:"), "XLSX error row incorrect.");
        Console.WriteLine("PASS: CSV quoting and XLSX worksheet relationship, dates, tariff validation and row errors.");
    }

    private static void WritePart(ZipArchive zip, string name, string value) { using (var writer = new StreamWriter(zip.CreateEntry(name).Open())) writer.Write(value); }
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

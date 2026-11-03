using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Crown__.Frontend.Security;

namespace Crown__.Frontend.Views
{
    // UI state and illustrative fixtures only. Backend owns authorization, availability,
    // pricing, transactions, persistence and all real business validation.
    public partial class DashboardView : Page
    {
        private static readonly string[] Themes = { "Light", "Dark", "Brand" };
        private static readonly string[] ThemeLabels = { "Claro", "Oscuro", "Marca" };
        private readonly Dictionary<string, Button> navigation = new Dictionary<string, Button>();
        private readonly List<string> navigationOrder = new List<string>();
        private readonly List<FormField> formFields = new List<FormField>();
        private ModuleDefinition currentModule;
        private int themeIndex;
        private bool updatingThemeUi;
        private ComboBox activeThemeCombo;
        private TextBox searchBox;
        private ComboBox stateFilter, floorFilter, subAreaFilter;
        private StackPanel results;
        private TextBlock resultCount;
        private FrameworkElement previousFocus;
        private StackPanel formEditor, draftPreview;
        private bool reviewingDraft;
        private readonly FrontendAccessContext access;
        private WorkflowDefinition currentWorkflow;
        private string formModule, formPermission;
        private int formStep;
        private Button stepBack;
        private TextBlock stepLabel;
        private int receptionViewMode; // 0 = Movimientos Check-in/out, 1 = Directorio de huéspedes
        private Action onDraftConfirmedCallback;
        private bool simulatedOffline;
        private CheckBox offlinePreferenceCheck;
        private Border guestHistoryBanner;

        private sealed class FormField
        {
            internal FieldDefinition Definition;
            internal Control Input;
            internal TextBlock Error;
            internal string Value { get { return Input is TextBox ? ((TextBox)Input).Text.Trim() : (string)((ComboBox)Input).SelectedItem ?? ""; } }
        }

        public DashboardView() : this("Light") { }
        public DashboardView(string theme) : this(theme, FrontendAccessContext.CreateDemonstration("Recepcionista")) { }
        public DashboardView(string theme, FrontendAccessContext accessContext)
        {
            access = accessContext ?? new FrontendAccessContext(null, null, null, null);
            InitializeComponent();
            themeIndex = Math.Max(0, Array.IndexOf(Themes, theme));
            ApplyTheme();
            RoleName.Text = access.RoleLabel;
            SessionNotice.Text = access.IsDemonstration
                ? "Demostración con datos de muestra · Alt+T cambia tema · Ctrl+Shift+↑/↓ cambia menú."
                : "Datos de muestra. Las operaciones reales aún no están conectadas.";
            string lastGroup = null;
            foreach (var module in NavigationGroups.SelectMany(g => g.Skip(1)).Select(key => ModuleCatalog.All.First(m => m.Key == key)).Where(m => access.Allows(m.Key, "View")))
            {
                string group = ModuleGroup(module.Key);
                if (group != lastGroup)
                {
                    var groupLabel = Text(group, 11, true); groupLabel.Margin = new Thickness(12, 18, 0, 6);
                    groupLabel.SetResourceReference(TextBlock.ForegroundProperty, "Ui.NavMuted");
                    NavigationItems.Children.Add(groupLabel); lastGroup = group;
                }
                var button = new Button { Tag = module.Key, Style = (Style)FindResource("Nav"), ToolTip = module.Title + " (Ctrl+Shift+↑/↓)" };
                AutomationProperties.SetAutomationId(button, "Nav" + module.Key);
                AutomationProperties.SetName(button, module.Title);
                var content = new StackPanel { Orientation = Orientation.Horizontal };
                var icon = new Path { Data = Geometry.Parse(module.Icon), Width = 18, Height = 18, Stretch = Stretch.Uniform,
                    StrokeThickness = 1.5, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, Margin = new Thickness(0, 0, 11, 0) };
                icon.SetBinding(Shape.StrokeProperty, new Binding("Foreground") { Source = button });
                content.Children.Add(icon);
                var label = Text(NavTitle(module), 12);
                label.SetBinding(TextBlock.ForegroundProperty, new Binding("Foreground") { Source = button });
                content.Children.Add(label);
                button.Content = content;
                button.Click += OnModuleClick;
                navigation.Add(module.Key, button);
                navigationOrder.Add(module.Key);
                NavigationItems.Children.Add(button);
            }
            if (navigation.Count > 0) ShowModule(navigation.ContainsKey("Inicio") ? "Inicio" : navigationOrder[0]);
            else { PageTitle.Text = "Sin acceso asignado"; PageDescription.Text = "Solicita al administrador un perfil de personal autorizado."; }
        }

        private void OnDashboardLoaded(object sender, RoutedEventArgs e)
        {
            if (currentModule != null && navigation.ContainsKey(currentModule.Key) && !IsKeyboardFocusWithin)
                navigation[currentModule.Key].Focus();
        }

        public void CycleTheme()
        {
            SetThemeIndex((themeIndex + 1) % Themes.Length);
        }

        private void OnThemeCycle(object sender, RoutedEventArgs e)
        {
            CycleTheme();
        }

        public void SetThemeIndex(int index)
        {
            if (index < 0 || index >= Themes.Length) return;
            themeIndex = index;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            Tag = Themes[themeIndex];
            string prefix = themeIndex == 2 ? "ThemeBrand" : Themes[themeIndex];
            foreach (var name in new[] { "Page", "Ink", "Secondary", "Field", "Border", "Primary", "OnPrimary", "Focus" })
                Resources["Ui." + name] = FindResource(prefix + "." + name);
            bool dark = themeIndex == 1;
            bool brand = themeIndex == 2;
            Resources["Ui.Page"] = Brush(dark ? "#14232F" : brand ? "#EBFEF7" : "#F3F6F8");
            Resources["Ui.Border"] = Brush(dark ? "#506674" : brand ? "#64F1C6" : "#CBD5DC");
            Resources["Ui.Nav"] = Brush(dark ? "#10202C" : brand ? "#003029" : "#17384F");
            Resources["Ui.NavSelected"] = Brush(brand ? "#006653" : "#31566D");
            Resources["Ui.NavMuted"] = Brush(brand ? "#A1F9D9" : "#C1D0DB");
            Resources["Ui.NavAccent"] = Brush(brand ? "#27E0AE" : "#D9BB79");
            Resources["Ui.Selection"] = Brush(brand ? "#64F1C6" : "#D9BB79");
            Resources["Ui.Soft"] = Brush(dark ? "#2A3D4C" : brand ? "#CEFDE9" : "#EDF0F2");
            Resources["Ui.Good"] = Brush(dark ? "#9AD7B3" : brand ? "#008269" : "#276447");
            Resources["Ui.Warning"] = Brush(dark ? "#E5C780" : "#805C1C");
            Resources["Ui.Danger"] = Brush(dark ? "#FFAAAA" : "#AE3535");
            if (CloudStatusDot != null)
                CloudStatusDot.SetResourceReference(Shape.FillProperty, simulatedOffline ? "Ui.Danger" : "Ui.Good");
            if (ThemeButton != null)
                ThemeButton.Content = "Tema: " + ThemeLabels[themeIndex];
            if (activeThemeCombo != null && activeThemeCombo.SelectedIndex != themeIndex)
            {
                updatingThemeUi = true;
                activeThemeCombo.SelectedIndex = themeIndex;
                updatingThemeUi = false;
            }
        }

        public void SetSimulatedOffline(bool offline)
        {
            simulatedOffline = offline;
            if (OfflineBanner != null)
                OfflineBanner.Visibility = offline ? Visibility.Visible : Visibility.Collapsed;
            if (CloudStatusDot != null)
                CloudStatusDot.SetResourceReference(Shape.FillProperty, offline ? "Ui.Danger" : "Ui.Good");
            if (CloudStatusText != null)
                CloudStatusText.Text = offline ? "Sin conexión (RNF-02)" : "Nube conectada";
            if (offlinePreferenceCheck != null && offlinePreferenceCheck.IsChecked != offline)
                offlinePreferenceCheck.IsChecked = offline;
        }

        private void OnRestoreNetworkClick(object sender, RoutedEventArgs e)
        {
            SetSimulatedOffline(false);
            if (Overlay.Visibility == Visibility.Visible && DialogError.Visibility == Visibility.Visible && DialogError.Text.Contains("RNF-02"))
                DialogError.Visibility = Visibility.Collapsed;
        }

        private string CurrentDemoRoleKey()
        {
            if (access != null && access.UserId != null && access.UserId.StartsWith("demo-", StringComparison.Ordinal))
                return access.UserId.Substring(5);
            return "Recepcionista";
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService == null) return;
            while (NavigationService.CanGoBack) NavigationService.RemoveBackEntry();
            NavigationService.Navigate(new LoginView(Themes[themeIndex], CurrentDemoRoleKey()));
        }

        private void OnModuleClick(object sender, RoutedEventArgs e) { ShowModule((string)((Button)sender).Tag); }

        public void NavigateRelativeMenu(int delta)
        {
            if (Overlay.Visibility == Visibility.Visible || navigationOrder.Count == 0) return;
            int current = currentModule == null ? 0 : Math.Max(0, navigationOrder.IndexOf(currentModule.Key));
            int next = (current + delta % navigationOrder.Count + navigationOrder.Count) % navigationOrder.Count;
            string targetKey = navigationOrder[next];
            ShowModule(targetKey);
            if (navigation.ContainsKey(targetKey))
                navigation[targetKey].BringIntoView();
        }

        private void ShowModule(string key)
        {
            if (!access.Allows(key, "View")) { DenyAccess(); return; }
            var target = ModuleCatalog.All.FirstOrDefault(m => m.Key == key);
            if (target == null) { DenyAccess(); return; }
            Overlay.Visibility = Visibility.Collapsed; Workspace.IsEnabled = true; formFields.Clear();
            bool sameModule = currentModule != null && currentModule.Key == key;
            currentModule = target; currentWorkflow = null;
            if (!sameModule) receptionViewMode = 0;
            PageTitle.Text = currentModule.Title;
            PageDescription.Text = currentModule.Description;
            foreach (var item in navigation)
            {
                if (item.Key == key)
                    item.Value.SetResourceReference(BackgroundProperty, "Ui.NavSelected");
                else
                    item.Value.Background = Brushes.Transparent;
                item.Value.FontWeight = item.Key == key ? FontWeights.SemiBold : FontWeights.Normal;
                AutomationProperties.SetHelpText(item.Value, item.Key == key ? "Sección actual" : "Abrir sección");
            }
            ModuleContent.Children.Clear();
            activeThemeCombo = null; offlinePreferenceCheck = null; searchBox = null; stateFilter = null; floorFilter = null; subAreaFilter = null; results = null;
            ContentScroll.ScrollToTop();
            if (key == "Inicio") BuildRoleHome();
            else if (key == "Preferencias") BuildPreferences();
            else BuildWorkspace();
        }

        private UIElement HomeRow(string time, string title, string subtitle, string status, Action open)
        {
            var grid = new Grid { Margin = new Thickness(16, 12, 16, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(68) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var hour = Text(time, 13, true); hour.SetResourceReference(TextBlock.ForegroundProperty, StatusColor(status));
            grid.Children.Add(hour);
            var stack = new StackPanel { Margin = new Thickness(6, 0, 12, 0) };
            stack.Children.Add(Text(title, 13, true)); stack.Children.Add(Muted(subtitle));
            Grid.SetColumn(stack, 1); grid.Children.Add(stack);
            var button = Action("Revisar", open); button.Margin = new Thickness(0); Grid.SetColumn(button, 2); grid.Children.Add(button);
            var border = new Border { BorderThickness = new Thickness(0, 0, 0, 1), Child = grid };
            border.SetResourceReference(Border.BorderBrushProperty, "Ui.Border"); return border;
        }

        private void AddMetrics(string[] metrics)
        {
            if (metrics == null || metrics.Length == 0) return;
            var wrap = new System.Windows.Controls.Primitives.UniformGrid { Columns = metrics.Length == 4 ? 4 : 3, Margin = new Thickness(0, 0, -10, 18) };
            foreach (string metric in metrics)
            {
                string[] value = metric.Split('|');
                var stack = new StackPanel(); stack.Children.Add(Muted(value[1])); stack.Children.Add(Text(value[0], 25, true));
                var panel = Panel(stack, 16); panel.Margin = new Thickness(0, 0, 10, 10); wrap.Children.Add(panel);
            }
            ModuleContent.Children.Add(wrap);
        }

        private string[] ActiveColumns()
        {
            if (currentModule != null && currentModule.Key == "CheckInOut" && receptionViewMode == 1)
                return HotelEventsAndLimits.GuestColumns;
            return currentModule.Columns;
        }

        private string ActiveActionLabel()
        {
            if (currentModule != null && currentModule.Key == "CheckInOut" && receptionViewMode == 1)
                return "Nuevo huésped";
            return currentModule.Action;
        }

        private FieldDefinition[] ActiveBaseFields()
        {
            if (currentModule != null && currentModule.Key == "CheckInOut" && receptionViewMode == 1)
                return HotelEventsAndLimits.GuestFields;
            return currentModule.Fields;
        }

        private void BuildModuleList()
        {
            var toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
            string targetLabel = currentModule.Key == "CheckInOut" && receptionViewMode == 1 ? "directorio de huéspedes" : currentModule.Title.ToLowerInvariant();
            searchBox = new TextBox { Width = 264, Tag = "Buscar en " + targetLabel, Margin = new Thickness(0, 0, 10, 8) };
            AutomationProperties.SetName(searchBox, "Buscar en " + targetLabel);
            toolbar.Children.Add(searchBox);
            var states = new[] { "Todos los estados" }.Concat(VisibleRows().Select(r => r[4]).Distinct()).ToArray();
            stateFilter = Choice(states, "Filtrar por estado"); stateFilter.Width = 172; stateFilter.Margin = new Thickness(0, 0, 10, 8);
            toolbar.Children.Add(stateFilter);
            if (currentModule.Key == "Habitaciones")
            {
                floorFilter = Choice(new[] { "Todos los pisos", "Piso 1", "Piso 2", "Piso 3", "Piso 4" }, "Filtrar por piso");
                floorFilter.Width = 148; floorFilter.Margin = new Thickness(0, 0, 10, 8); toolbar.Children.Add(floorFilter);
            }
            else if (currentModule.Key == "FNB")
            {
                subAreaFilter = Choice(new[] { "Todos los servicios", "Desayuno", "Restaurante", "Room service", "Minibar" }, "Filtrar por servicio de comidas");
                subAreaFilter.Width = 168; subAreaFilter.Margin = new Thickness(0, 0, 10, 8); toolbar.Children.Add(subAreaFilter);
            }
            else if (currentModule.Key == "GymSpa")
            {
                subAreaFilter = Choice(new[] { "Todos los espacios", "Spa", "Gimnasio" }, "Filtrar por espacio de bienestar");
                subAreaFilter.Width = 164; subAreaFilter.Margin = new Thickness(0, 0, 10, 8); toolbar.Children.Add(subAreaFilter);
            }
            if (!string.IsNullOrEmpty(ActiveActionLabel()) && access.Allows(currentModule.Key, BasePermission(currentModule.Key)))
                toolbar.Children.Add(Action(ActiveActionLabel(), OpenForm, true));
            ModuleContent.Children.Add(toolbar);
            resultCount = Muted(""); resultCount.Margin = new Thickness(0, 0, 0, 12); AutomationProperties.SetLiveSetting(resultCount, AutomationLiveSetting.Polite);
            ModuleContent.Children.Add(resultCount);
            results = new StackPanel(); ModuleContent.Children.Add(results);
            searchBox.TextChanged += (s, e) => RefreshResults();
            stateFilter.SelectionChanged += (s, e) => RefreshResults();
            if (floorFilter != null) floorFilter.SelectionChanged += (s, e) => RefreshResults();
            if (subAreaFilter != null) subAreaFilter.SelectionChanged += (s, e) => RefreshResults();
            RefreshResults();
        }

        private static string Normalize(string text)
        {
            var result = new StringBuilder();
            foreach (char character in text.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark) result.Append(char.ToLowerInvariant(character));
            return result.ToString().Normalize(NormalizationForm.FormC);
        }

        private static bool IsWithinOneEditOrSwap(string a, string b)
        {
            if (Math.Abs(a.Length - b.Length) > 1) return false;
            if (a.Length == b.Length)
            {
                int diffs = 0, first = -1, second = -1;
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] == b[i]) continue;
                    diffs++;
                    if (diffs == 1) first = i;
                    else if (diffs == 2) second = i;
                    else return false;
                }
                if (diffs <= 1) return true;
                return second == first + 1 && a[first] == b[second] && a[second] == b[first];
            }
            string shorter = a.Length < b.Length ? a : b;
            string longer = a.Length < b.Length ? b : a;
            int si = 0, li = 0, edits = 0;
            while (si < shorter.Length && li < longer.Length)
            {
                if (shorter[si] == longer[li]) { si++; li++; }
                else { if (++edits > 1) return false; li++; }
            }
            return true;
        }

        private bool RowMatchesTerm(string[] row, string term)
        {
            if (string.IsNullOrEmpty(term)) return true;
            string full = Normalize(string.Join(" ", row));
            if (full.Contains(term)) return true;
            if (currentModule != null && currentModule.Key == "CheckInOut" && term.Length >= 5 && !term.Contains("-"))
            {
                foreach (string token in Regex.Split(full, @"[^a-z0-9]+"))
                    if (token.Length >= 5 && IsWithinOneEditOrSwap(token, term))
                        return true;
            }
            return false;
        }

        private void ShowExternalDirectoryResults()
        {
            if (results == null || currentModule == null || currentModule.Key != "CheckInOut") return;
            results.Children.Clear();
            resultCount.Text = HotelEventsAndLimits.ExternalDirectoryContacts.Length + " contactos en directorio externo configurable (RF-27)";
            var table = new StackPanel { MinWidth = 760 };
            table.Children.Add(TableRow(HotelEventsAndLimits.GuestColumns, true));
            foreach (var ext in HotelEventsAndLimits.ExternalDirectoryContacts)
            {
                var captured = ext;
                var grid = new Grid { Margin = new Thickness(14, 13, 14, 13) };
                double[] widths = { 1.0, 2.2, 1.65, 1.45, 1.3, 1.1 };
                foreach (double width in widths) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width, GridUnitType.Star) });
                for (int i = 0; i < 5; i++)
                {
                    var cell = i == 4 ? Status(ext[i]) : Text(ext[i], 13, i == 0);
                    cell.Margin = new Thickness(0, 0, 12, 0);
                    Grid.SetColumn(cell, i); grid.Children.Add(cell);
                }
                if (access.Allows("CheckInOut", "Write"))
                {
                    var importBtn = Action("Usar ficha", () =>
                    {
                        receptionViewMode = 1;
                        BuildWorkspace();
                        OpenForm();
                        string[] nameParts = captured[1].Split('·');
                        string[] contactParts = captured[2].Split('·');
                        string[] docParts = captured[0].Split('·');
                        Prefill("Nombre completo", nameParts[0].Trim());
                        Prefill("Documento de identidad", docParts.Length > 1 ? docParts[1].Trim() : captured[0]);
                        Prefill("Correo electrónico", contactParts[0].Trim());
                        if (contactParts.Length > 1) Prefill("Teléfono", contactParts[1].Trim());
                        if (nameParts.Length > 1) Prefill("Empresa o agencia", nameParts[1].Trim());
                        Prefill("Segmento", "Corporativo");
                    });
                    importBtn.Padding = new Thickness(8, 5, 8, 5);
                    importBtn.Margin = new Thickness(0);
                    AutomationProperties.SetAutomationId(importBtn, "UseExternal_" + ext[0].Split(' ')[0]);
                    Grid.SetColumn(importBtn, 5);
                    grid.Children.Add(importBtn);
                }
                var border = new Border { Child = grid, BorderThickness = new Thickness(0, 0, 0, 1) };
                border.SetResourceReference(Border.BorderBrushProperty, "Ui.Border");
                table.Children.Add(border);
            }
            var scroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = table, Focusable = false };
            results.Children.Add(Panel(scroll, 0));
            AddHelp(results, "Resultados simulados del directorio externo configurable (RF-27). Usa “Restablecer” en el buscador para volver al listado interno.");
        }

        private void RefreshResults()
        {
            if (results == null) return;
            string term = Normalize(searchBox.Text.Trim());
            string state = stateFilter.SelectedIndex > 0 ? (string)stateFilter.SelectedItem : null;
            string floor = floorFilter != null && floorFilter.SelectedIndex > 0 ? (string)floorFilter.SelectedItem : null;
            string subArea = subAreaFilter != null && subAreaFilter.SelectedIndex > 0 ? Normalize((string)subAreaFilter.SelectedItem) : null;
            var rows = VisibleRows().Where(r =>
                RowMatchesTerm(r, term) &&
                (state == null || r[4] == state) &&
                (floor == null || r[2] == floor) &&
                (subArea == null || Normalize(r[2]).Contains(subArea))).ToArray();
            results.Children.Clear();
            resultCount.Text = rows.Length + (rows.Length == 1 ? " registro de muestra" : " registros de muestra") + (currentModule.Key == "Habitaciones" ? " · Vista parcial del hotel" : "");
            if (rows.Length == 0)
            {
                var empty = new StackPanel { Margin = new Thickness(6, 14, 6, 14) };
                empty.Children.Add(Text("No hay coincidencias", 18, true));
                empty.Children.Add(Muted(currentModule.Key == "CheckInOut"
                    ? "No se halló al huésped en la base local (incluso con tolerancia tipográfica RF-27). Puedes restablecer filtros o consultar el directorio externo."
                    : "Prueba con otro nombre, habitación o estado, o restablece los filtros."));
                var actions = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
                actions.Children.Add(Action("Restablecer filtros", () => {
                    searchBox.Clear(); stateFilter.SelectedIndex = 0;
                    if (floorFilter != null) floorFilter.SelectedIndex = 0;
                    if (subAreaFilter != null) subAreaFilter.SelectedIndex = 0;
                }));
                if (currentModule.Key == "CheckInOut")
                {
                    var extBtn = Action("Consultar directorio externo (RF-27)", ShowExternalDirectoryResults, true);
                    AutomationProperties.SetAutomationId(extBtn, "BtnDirectorioExterno");
                    actions.Children.Add(extBtn);
                }
                empty.Children.Add(actions);
                results.Children.Add(Panel(empty)); return;
            }
            if (currentModule.Key == "Habitaciones")
            {
                var rooms = new WrapPanel { Margin = new Thickness(0, 0, -10, 0) };
                foreach (var row in rows)
                {
                    var captured = row;
                    var stack = new StackPanel();
                    stack.Children.Add(Text(row[0], 26, true)); stack.Children.Add(Muted(row[1]));
                    stack.Children.Add(Status(row[4])); stack.Children.Add(Muted(row[2] + " · " + row[3]));
                    stack.Children.Add(Action("Ver habitación", () => ShowDetail(captured)));
                    var room = Panel(stack, 16); room.Width = 184; room.Margin = new Thickness(0, 0, 10, 10); rooms.Children.Add(room);
                }
                results.Children.Add(rooms); return;
            }
            var table = new StackPanel { MinWidth = 760 };
            table.Children.Add(TableRow(ActiveColumns(), true));
            foreach (var row in rows) table.Children.Add(TableRow(row, false));
            var scroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = table, Focusable = false };
            results.Children.Add(Panel(scroll, 0));
            if (currentModule.Key == "CheckInOut")
                AddHelp(results, receptionViewMode == 0
                    ? "Los botones abren la ficha de revisión. Confirmar un check-in/out o liquidar un saldo requiere la integración de backend."
                    : "Directorio unificado con recepción. Usa Área de trabajo para historial, preferencias, consentimiento o duplicados.");
            else if (currentModule.Key == "Reportes")
                AddHelp(results, "Prepara el período y formato desde “Preparar reporte”. La descarga se habilitará con la integración de datos.");
        }

        private UIElement TableRow(string[] values, bool header)
        {
            var grid = new Grid { Margin = new Thickness(14, header ? 10 : 13, 14, header ? 10 : 13) };
            double[] widths = { 1.0, 2.2, 1.65, 1.45, 1.3, 0.8 };
            foreach (double width in widths) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width, GridUnitType.Star) });
            for (int i = 0; i < 5; i++)
            {
                var cell = !header && i == 4 ? Status(values[i]) : Text(values[i], header ? 11 : 13, !header && i == 0);
                cell.Margin = new Thickness(0, 0, 12, 0);
                if (header) cell.SetResourceReference(TextBlock.ForegroundProperty, "Ui.Secondary");
                Grid.SetColumn(cell, i); grid.Children.Add(cell);
            }
            if (!header)
            {
                var captured = values;
                var button = Action("Ver", () => ShowDetail(captured)); button.MinWidth = 48; button.Padding = new Thickness(8, 5, 8, 5); button.Margin = new Thickness(0);
                AutomationProperties.SetName(button, "Ver " + values[0] + ", " + values[1]);
                Grid.SetColumn(button, 5); grid.Children.Add(button);
            }
            var border = new Border { Child = grid, BorderThickness = new Thickness(0, 0, 0, 1), CornerRadius = header ? new CornerRadius(15, 15, 0, 0) : new CornerRadius(0) };
            border.SetResourceReference(Border.BorderBrushProperty, "Ui.Border");
            if (header) border.SetResourceReference(Border.BackgroundProperty, "Ui.Soft");
            return border;
        }

        private void BuildOccupancyChart()
        {
            var stack = new StackPanel();
            stack.Children.Add(Text("Ocupación de la semana", 17, true));
            stack.Children.Add(Muted("25–31 de octubre de 2026 · Porcentaje de habitaciones ocupadas (muestra)"));
            var chart = new System.Windows.Controls.Primitives.UniformGrid { Columns = 7, Height = 145, Margin = new Thickness(0, 16, 0, 0) };
            int[] values = { 64, 71, 68, 76, 80, 85, 82 };
            string[] days = { "Dom 25", "Lun 26", "Mar 27", "Mié 28", "Jue 29", "Vie 30", "Sáb 31" };
            for (int i = 0; i < values.Length; i++)
            {
                var column = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(5, 0, 5, 0) };
                var number = Text(values[i] + "%", 12, true); number.HorizontalAlignment = HorizontalAlignment.Center; column.Children.Add(number);
                var bar = new Border { Height = values[i], Width = 32, CornerRadius = new CornerRadius(4, 4, 0, 0), Margin = new Thickness(0, 5, 0, 5) };
                bar.SetResourceReference(Border.BackgroundProperty, i == 6 ? "Ui.Focus" : "Ui.Primary"); column.Children.Add(bar);
                var day = Muted(days[i]); day.FontSize = 11; day.HorizontalAlignment = HorizontalAlignment.Center; column.Children.Add(day); chart.Children.Add(column);
            }
            stack.Children.Add(chart); var panel = Panel(stack); panel.Margin = new Thickness(0, 0, 0, 22); ModuleContent.Children.Add(panel);
        }

        internal ComboBox CreateStyledThemeComboBox(string accessibleName)
        {
            var combo = new ComboBox
            {
                ItemsSource = HotelEventsAndLimits.ThemeOptions,
                ItemTemplate = (DataTemplate)FindResource("ThemeOptionTemplate"),
                SelectedIndex = themeIndex,
                MinHeight = 52,
                MaxWidth = 420,
                Width = 380,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            AutomationProperties.SetName(combo, accessibleName);
            AutomationProperties.SetAutomationId(combo, "ThemeComboBox");
            combo.SelectionChanged += (s, e) =>
            {
                if (updatingThemeUi || combo.SelectedIndex < 0) return;
                SetThemeIndex(combo.SelectedIndex);
            };
            activeThemeCombo = combo;
            return combo;
        }

        private void BuildPreferences()
        {
            var appearance = new StackPanel();
            AddHeading(appearance, "Tema visual de la interfaz", "Elige el tema con el selector desplegable o alterna desde cualquier pantalla con Alt + T.");
            var themeCombo = CreateStyledThemeComboBox("Seleccionar tema visual");
            var label = new Label { Content = "Paleta activa del puesto", Target = themeCombo, Padding = new Thickness(0, 0, 0, 6), FontWeight = FontWeights.SemiBold };
            label.SetResourceReference(Control.ForegroundProperty, "Ui.Ink");
            appearance.Children.Add(label);
            appearance.Children.Add(themeCombo);
            var tip = Muted("Los tres modos mantienen contraste legible en tablas, calendarios y borradores.");
            tip.Margin = new Thickness(0, 12, 0, 0);
            appearance.Children.Add(tip);
            ModuleContent.Children.Add(Panel(appearance));

            var shortcuts = new StackPanel();
            AddHeading(shortcuts, "Atajos de teclado", "Navegación rápida habilitada para operar sin soltar el teclado.");
            var shortcutItems = new[] {
                new[] { "Alt + T", "Cambiar tema visual", "Alterna entre Claro, Oscuro y Marca en login y puesto." },
                new[] { "Ctrl + Shift + ↑", "Menú anterior", "Sube a la sección previa visible para tu rol." },
                new[] { "Ctrl + Shift + ↓", "Menú siguiente", "Avanza a la siguiente sección visible para tu rol." },
                new[] { "Ctrl + F", "Buscar en el listado", "Enfoca el buscador del área operativa abierta." },
                new[] { "F1 / Esc", "Ayuda y cierre", "Abre la guía contextual o cierra el diálogo activo." }
            };
            foreach (var sc in shortcutItems)
            {
                var row = new Grid { Margin = new Thickness(0, 6, 0, 6) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(148) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var badgeText = Text(sc[0], 11, true);
                var badge = new Border { Child = badgeText, CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 5, 10, 5), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
                badge.SetResourceReference(Border.BackgroundProperty, "Ui.Soft");
                badge.SetResourceReference(Border.BorderBrushProperty, "Ui.Border");
                row.Children.Add(badge);
                var desc = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
                desc.Children.Add(Text(sc[1], 13, true));
                desc.Children.Add(Muted(sc[2]));
                Grid.SetColumn(desc, 1);
                row.Children.Add(desc);
                shortcuts.Children.Add(row);
            }
            ModuleContent.Children.Add(Panel(shortcuts));

            var sessionPrefs = new StackPanel();
            AddHeading(sessionPrefs, "Preferencias regionales y estado cloud", "Opciones locales de muestra para idioma (RNF-25), moneda única y prueba de desconexión (RNF-02).");
            AddSetting(sessionPrefs, "Idioma de interfaz y plantillas (RNF-25)", Choice(new[] { "Español (interfaz y documentos)", "Español + Inglés (plantillas bilingües)" }, "Idioma de interfaz y documentos"));
            AddSetting(sessionPrefs, "Moneda principal única (Alcance)", Choice(new[] { "USD · Dólar estadounidense (única)", "BOB · Boliviano (única)" }, "Moneda principal única"));
            AddSetting(sessionPrefs, "Formato de hora operativo", Choice(new[] { "24 horas (HH:mm)", "12 horas (AM / PM)" }, "Formato de hora operativo"));
            foreach (string check in new[] { "Destacar límites activos en comidas, spa y gimnasio", "Mostrar avisos de alta prioridad al abrir Mi puesto", "Conservar texto buscado al alternar de tema" })
                sessionPrefs.Children.Add(new CheckBox { Content = check, IsChecked = true });
            offlinePreferenceCheck = new CheckBox
            {
                Content = "Simular caída de red cloud para verificar bloqueo preventivo sin pérdida de datos (RNF-02)",
                IsChecked = simulatedOffline
            };
            AutomationProperties.SetAutomationId(offlinePreferenceCheck, "ChkSimularOffline");
            offlinePreferenceCheck.Checked += (s, e) => SetSimulatedOffline(true);
            offlinePreferenceCheck.Unchecked += (s, e) => SetSimulatedOffline(false);
            sessionPrefs.Children.Add(offlinePreferenceCheck);
            var savePreview = Action("Revisar preferencias", () => OpenInfo("Preferencias aplicadas en sesión", "Tema activo: " + ThemeLabels[themeIndex] + "\nRed cloud simulada: " + (simulatedOffline ? "Sin conexión (bloqueo preventivo RNF-02)" : "Conectada") + "\nAtajos de teclado: activos (Alt+T y Ctrl+Shift+↑/↓).\n\nEstas preferencias operan localmente en el frontend de demostración."), true);
            savePreview.Margin = new Thickness(0, 12, 0, 0);
            sessionPrefs.Children.Add(savePreview);
            ModuleContent.Children.Add(Panel(sessionPrefs));
        }

        private void AddSetting(StackPanel host, string title, Control input)
        {
            AutomationProperties.SetName(input, title);
            var label = new Label { Content = title, Target = input, Padding = new Thickness(0, 0, 0, 6) };
            label.SetResourceReference(Control.ForegroundProperty, "Ui.Ink"); host.Children.Add(label);
            input.MaxWidth = 360; input.HorizontalAlignment = HorizontalAlignment.Left; input.Width = 340; input.Margin = new Thickness(0, 0, 0, 14); host.Children.Add(input);
        }

        private void ShowDetail(string[] row)
        {
            if (currentModule == null || !access.Allows(currentModule.Key, "View") || !VisibleRows().Any(r => r.SequenceEqual(row))) { DenyAccess(); return; }
            var cols = ActiveColumns();
            OpenDialog(currentModule.Key == "Habitaciones" ? "Habitación " + row[0] : row[0], currentModule.Title + " · Ficha de muestra");
            for (int i = 0; i < cols.Length; i++)
            {
                DialogBody.Children.Add(Muted(cols[i]));
                var value = Text(row[i], 15, true); value.Margin = new Thickness(0, 3, 0, 16); DialogBody.Children.Add(value);
            }
            if (currentModule.Key == "CheckInOut" && receptionViewMode == 0)
                AddHelp(DialogBody, row[4] == "Llegada" ? "Antes del check-in: verificar identidad, reserva, ocupantes y disponibilidad de la habitación." : "Antes del check-out: revisar consumos, saldo, factura y devolución de llaves.");
            if (currentModule.Key == "Habitaciones") AddHelp(DialogBody, "La asignación y el cambio de estado se confirmarán con disponibilidad real al integrar backend.");
            DialogSubmit.Visibility = Visibility.Collapsed;
            AddDetailContext(row);
            if (currentModule.Key != "Auditoria" && !string.IsNullOrEmpty(ActiveActionLabel()) && access.Allows(currentModule.Key, BasePermission(currentModule.Key)))
                DialogBody.Children.Add(Action(ActiveActionLabel(), OpenForm, true));
        }

        private void OpenInfo(string title, string body)
        {
            OpenDialog(title, "Previsualización local"); DialogBody.Children.Add(Text(body)); DialogSubmit.Visibility = Visibility.Collapsed;
        }

        private void OpenForm()
        {
            OpenCustomForm(null, null, null, null);
        }

        internal void OpenCustomForm(string customTitle, FieldDefinition[] customDefinitions, string requiredPermission, Action onDraftReviewed)
        {
            if (currentModule == null) { DenyAccess(); return; }
            string permission = requiredPermission ?? (currentWorkflow == null ? BasePermission(currentModule.Key) : currentWorkflow.Permission);
            if (!access.Allows(currentModule.Key, permission)) { DenyAccess(); return; }
            if (customDefinitions == null && currentWorkflow != null && currentWorkflow.Fields.Length == 0) return;
            var definitions = customDefinitions ?? (currentWorkflow == null ? ActiveBaseFields() : currentWorkflow.Fields);
            if (definitions == null) return;
            OpenDialog(customTitle ?? (currentWorkflow == null ? ActiveActionLabel() : currentWorkflow.Title), "Completa los datos y revisa el borrador. Validación en tiempo real activa (RNF-21).");
            onDraftConfirmedCallback = onDraftReviewed;
            formModule = currentModule.Key; formPermission = permission; formStep = 0;
            if (customDefinitions == null && ((currentModule.Key == "CheckInOut" && currentWorkflow != null && currentWorkflow.Key == "CheckIn") || (currentModule.Key == "Reservas" && currentWorkflow == null)))
            {
                var historyStack = new StackPanel();
                historyStack.Children.Add(Text("Historial y autocompletado de huésped (RF-17 · RF-18)", 13, true));
                var historyDetail = Muted("Selecciona un huésped frecuente para autocompletar datos y revisar estancias, preferencias y alergias.");
                historyStack.Children.Add(historyDetail);
                var quickGuests = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
                foreach (var profile in HotelEventsAndLimits.GuestProfiles)
                {
                    var captured = profile;
                    var gBtn = Action(profile.FullName + " (" + profile.Segment + ")", () =>
                    {
                        historyDetail.Text = captured.FullName + " · " + captured.Stays + "\nPreferencias: " + captured.Preferences + " · Alergias: " + captured.Allergies + "\nNotas: " + captured.Notes;
                        Prefill("Nombre del huésped", captured.FullName);
                        Prefill("Huésped titular", captured.FullName);
                        Prefill("Documento de identidad", captured.DocumentId);
                        Prefill("Correo de contacto", captured.Email);
                        Prefill("Preferencias, alergias y notas del historial", captured.Preferences + " · " + captured.Allergies);
                        Prefill("Observaciones", captured.Preferences + " · " + captured.Allergies);
                    });
                    gBtn.Padding = new Thickness(10, 5, 10, 5);
                    gBtn.MinHeight = 32;
                    gBtn.FontSize = 12;
                    AutomationProperties.SetAutomationId(gBtn, "AutocompleteGuest_" + profile.Code);
                    quickGuests.Children.Add(gBtn);
                }
                historyStack.Children.Add(quickGuests);
                guestHistoryBanner = Panel(historyStack, 12);
                guestHistoryBanner.SetResourceReference(Border.BackgroundProperty, "Ui.Soft");
                guestHistoryBanner.Margin = new Thickness(0, 0, 0, 14);
                DialogBody.Children.Add(guestHistoryBanner);
            }
            formEditor = new StackPanel(); draftPreview = new StackPanel { Visibility = Visibility.Collapsed };
            DialogBody.Children.Add(formEditor); DialogBody.Children.Add(draftPreview);
            foreach (var definition in definitions)
            {
                Control input;
                string cleanHint = (definition.Hint ?? "").Trim();
                if (definition.Kind == "choice")
                {
                    var combo = Choice(new[] { "Selecciona una opción" }.Concat(definition.Options).ToArray(), definition.Label);
                    input = combo;
                }
                else
                {
                    var text = new TextBox { Text = string.Empty, Tag = cleanHint, MaxLength = definition.Kind == "notes" || definition.Kind == "optional" ? 500 : 120 };
                    if (definition.Kind == "notes") { text.Height = 86; text.TextWrapping = TextWrapping.Wrap; text.AcceptsReturn = true; text.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; }
                    input = text;
                }
                AutomationProperties.SetName(input, definition.Label);
                AutomationProperties.SetHelpText(input, cleanHint + (definition.Kind == "optional" ? "" : ". Campo obligatorio"));
                var label = new Label { Content = definition.Label + (definition.Kind == "optional" ? " (opcional)" : " *"), Target = input, Padding = new Thickness(0, 0, 0, 5), FontSize = 12.5, FontWeight = FontWeights.SemiBold };
                label.SetResourceReference(Control.ForegroundProperty, "Ui.Ink");
                var error = Text("", 12); error.SetResourceReference(TextBlock.ForegroundProperty, "Ui.Danger"); error.Visibility = Visibility.Collapsed;
                var field = new FormField { Definition = definition, Input = input, Error = error }; formFields.Add(field);
                input.Margin = new Thickness(0, 0, 0, 4);
                formEditor.Children.Add(label); formEditor.Children.Add(input); formEditor.Children.Add(error);
                formEditor.Children.Add(new Border { Height = 10 });
                if (input is TextBox)
                {
                    var tb = (TextBox)input;
                    tb.TextChanged += (s, e) =>
                    {
                        if (field.Error.Visibility == Visibility.Visible)
                        {
                            if (Validate(field) == null) ClearFieldError(field);
                        }
                        else DialogError.Visibility = Visibility.Collapsed;
                    };
                    tb.LostKeyboardFocus += (s, e) =>
                    {
                        if (tb.Text.Trim().Length == 0) return;
                        string err = Validate(field);
                        field.Error.Text = err ?? "";
                        field.Error.Visibility = err == null ? Visibility.Collapsed : Visibility.Visible;
                        field.Input.SetResourceReference(Control.BorderBrushProperty, err == null ? "Ui.Border" : "Ui.Danger");
                    };
                }
                else ((ComboBox)input).SelectionChanged += (s, e) => ClearFieldError(field);
            }
            if (customDefinitions == null && currentWorkflow != null && currentWorkflow.Guided) PrepareGuidedForm();
            if (formFields.Count > 0)
            {
                var firstInput = formFields[0].Input;
                Dispatcher.BeginInvoke(new Action(() => { if (Overlay.IsVisible && firstInput.IsVisible) firstInput.Focus(); }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private void ClearFieldError(FormField field)
        {
            field.Error.Text = "";
            field.Error.Visibility = Visibility.Collapsed; field.Input.SetResourceReference(Control.BorderBrushProperty, "Ui.Border");
            DialogError.Visibility = Visibility.Collapsed;
        }

        private void OpenDialog(string title, string description)
        {
            formModule = null; formPermission = null; stepBack = null; stepLabel = null; onDraftConfirmedCallback = null; guestHistoryBanner = null;
            if (Overlay.Visibility != Visibility.Visible) previousFocus = Keyboard.FocusedElement as FrameworkElement;
            formFields.Clear(); reviewingDraft = false; DialogBody.Children.Clear(); DialogError.Visibility = Visibility.Collapsed;
            DialogTitle.Text = title; DialogDescription.Text = description; DialogSubmit.Visibility = Visibility.Visible; DialogSubmit.Content = "Revisar borrador";
            Workspace.IsEnabled = false; Overlay.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke(new Action(() => Overlay.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void OnCloseDialog(object sender, RoutedEventArgs e)
        {
            Overlay.Visibility = Visibility.Collapsed; Workspace.IsEnabled = true;
            formModule = null; formPermission = null; onDraftConfirmedCallback = null; formFields.Clear();
            if (previousFocus != null && previousFocus.IsVisible && previousFocus.IsEnabled) previousFocus.Focus();
            else if (currentModule != null && navigation.ContainsKey(currentModule.Key)) navigation[currentModule.Key].Focus();
        }

        public void HandleShortcut(KeyEventArgs e)
        {
            if (e.Handled) return;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            ModifierKeys modifiers = Keyboard.Modifiers;

            if (modifiers == ModifierKeys.Alt && key == Key.T)
            {
                CycleTheme();
                e.Handled = true;
                return;
            }

            if (modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && (key == Key.Up || key == Key.Down))
            {
                if (Overlay.Visibility != Visibility.Visible)
                {
                    NavigateRelativeMenu(key == Key.Up ? -1 : 1);
                    e.Handled = true;
                }
                return;
            }

            if (e.Key == Key.Escape && Overlay.Visibility == Visibility.Visible) { OnCloseDialog(this, e); e.Handled = true; }
            else if (e.Key == Key.F1) { OnHelpClick(this, e); e.Handled = true; }
            else if (e.Key == Key.F && modifiers == ModifierKeys.Control && Overlay.Visibility != Visibility.Visible && searchBox != null) { searchBox.Focus(); e.Handled = true; }
        }

        private void OnPageKeyDown(object sender, KeyEventArgs e)
        {
            HandleShortcut(e);
        }

        private void OnReviewDraft(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(formModule) || currentModule == null || currentModule.Key != formModule || !access.Allows(formModule, formPermission)) { DenyAccess(); return; }
            bool isGuided = onDraftConfirmedCallback == null && currentWorkflow != null && currentWorkflow.Guided;
            if (reviewingDraft)
            {
                reviewingDraft = false; draftPreview.Visibility = Visibility.Collapsed; formEditor.Visibility = Visibility.Visible;
                if (guestHistoryBanner != null) guestHistoryBanner.Visibility = Visibility.Visible;
                DialogSubmit.Content = "Revisar borrador"; if (isGuided) UpdateGuidedForm();
                if (formFields.Count > 0) (isGuided ? formFields.First(f => FieldStep(f) == formStep) : formFields[0]).Input.Focus(); return;
            }
            FormField firstInvalid = null;
            foreach (var field in formFields.Where(f => !isGuided || FieldStep(f) <= formStep))
            {
                string error = Validate(field);
                field.Error.Text = error ?? ""; field.Error.Visibility = error == null ? Visibility.Collapsed : Visibility.Visible;
                field.Input.SetResourceReference(Control.BorderBrushProperty, error == null ? "Ui.Border" : "Ui.Danger");
                if (error != null && firstInvalid == null) firstInvalid = field;
            }
            if (firstInvalid == null && formFields.Count(f => f.Definition.Kind == "date") >= 2)
            {
                var dates = formFields.Where(f => f.Definition.Kind == "date").ToArray();
                var start = DateTime.ParseExact(dates[0].Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var end = DateTime.ParseExact(dates[1].Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                if (end < start || ((currentModule.Key == "Reservas" || currentModule.Key == "Tarifas") && currentWorkflow != null && currentWorkflow.Key == "Simulador" && end == start) || (currentModule.Key == "Reservas" && end == start))
                {
                    firstInvalid = dates[1]; firstInvalid.Error.Text = currentModule.Key == "Reservas" ? "La salida debe ser posterior a la llegada." : "La fecha final no puede ser anterior a la inicial.";
                    firstInvalid.Error.Visibility = Visibility.Visible; firstInvalid.Input.SetResourceReference(Control.BorderBrushProperty, "Ui.Danger");
                }
            }
            if (firstInvalid != null)
            {
                DialogError.Text = "Revisa los campos indicados antes de continuar."; DialogError.Visibility = Visibility.Visible;
                firstInvalid.Input.Focus(); firstInvalid.Input.BringIntoView(); return;
            }
            if (isGuided && currentWorkflow.Key == "CheckOut" && formStep >= 1)
            {
                var balance = formFields.First(f => f.Definition.Label == "Saldo de referencia");
                var payment = formFields.First(f => f.Definition.Label == "Pago");
                var reference = formFields.First(f => f.Definition.Label == "Referencia de pago");
                bool paid = decimal.Parse(balance.Value.Replace(',', '.'), CultureInfo.InvariantCulture) > 0;
                if ((paid && payment.Value == "Sin pago pendiente") || (paid && reference.Value.Length == 0))
                {
                    var invalid = payment.Value == "Sin pago pendiente" ? payment : reference;
                    invalid.Error.Text = invalid == payment ? "Selecciona el método para revisar el saldo pendiente." : "Indica una referencia del pago pendiente.";
                    invalid.Error.Visibility = Visibility.Visible; invalid.Input.SetResourceReference(Control.BorderBrushProperty, "Ui.Danger");
                    DialogError.Text = "Revisa el pago del saldo antes de continuar."; DialogError.Visibility = Visibility.Visible; invalid.Input.Focus(); return;
                }
            }
            if (isGuided && formStep < GuidedStepCount - 1) { formStep++; UpdateGuidedForm(); return; }
            if (simulatedOffline)
            {
                DialogError.Text = "Sin conexión con la nube (RNF-02): operación bloqueada preventivamente. Tus datos permanecen intactos en pantalla hasta restablecer la red.";
                DialogError.Visibility = Visibility.Visible;
                return;
            }
            if (onDraftConfirmedCallback != null) onDraftConfirmedCallback();
            else ApplyWorkflowLimitIfApplicable();
            draftPreview.Children.Clear();
            draftPreview.Children.Add(Text("Borrador para revisión", 18, true));
            AddHelp(draftPreview, onDraftConfirmedCallback != null || (currentWorkflow != null && currentWorkflow.Permission == "Limit")
                ? "Simulación aplicada en la vista local de esta sesión. No persiste en base de datos."
                : "Estos datos solo se previsualizan. No se ha registrado ninguna operación.");
            if (currentWorkflow != null && (currentWorkflow.Key == "Simulador" || currentWorkflow.Key == "Factura"))
            {
                decimal total = 0m;
                foreach (var moneyField in formFields.Where(f => f.Definition.Kind == "amount" || f.Definition.Kind == "nonnegative"))
                {
                    decimal parsed;
                    if (decimal.TryParse(moneyField.Value.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsed))
                        total += parsed;
                }
                var calcBox = Panel(Text((currentWorkflow.Key == "Simulador" ? "Precio final estimado (RF-07): $" : "Total factura desglosada (RF-43): $") + total.ToString("0.00", CultureInfo.InvariantCulture), 15, true), 12);
                calcBox.SetResourceReference(Border.BackgroundProperty, "Ui.Soft");
                calcBox.Margin = new Thickness(0, 0, 0, 14);
                draftPreview.Children.Add(calcBox);
            }
            foreach (var field in formFields)
            {
                draftPreview.Children.Add(Muted(field.Definition.Label)); var value = Text(string.IsNullOrEmpty(field.Value) ? "Sin observaciones" : field.Value);
                value.Margin = new Thickness(0, 3, 0, 14); draftPreview.Children.Add(value);
            }
            reviewingDraft = true; formEditor.Visibility = Visibility.Collapsed; draftPreview.Visibility = Visibility.Visible; DialogSubmit.Content = "Volver a editar";
            if (guestHistoryBanner != null) guestHistoryBanner.Visibility = Visibility.Collapsed;
            if (stepBack != null) stepBack.Visibility = Visibility.Collapsed;
        }

        private void ApplyWorkflowLimitIfApplicable()
        {
            if (currentWorkflow == null || currentWorkflow.Permission != "Limit") return;
            string serviceName = null;
            var svcField = formFields.FirstOrDefault(f => f.Definition.Label == "Servicio");
            if (svcField != null) serviceName = svcField.Value;
            else if (currentModule.Key == "Lavanderia") serviceName = "Lavandería y planchado exprés";
            else if (currentModule.Key == "Transporte") serviceName = "Traslados aeropuerto y ciudad";
            if (serviceName == null) return;
            string state = formFields.First(f => f.Definition.Label == "Estado operativo").Value;
            string schedule = formFields.First(f => f.Definition.Label == "Horario permitido").Value;
            string cap = formFields[formFields.Count - 2].Value;
            string reason = formFields.Last().Value;
            HotelEventsAndLimits.ApplySimulatedLimit(serviceName, state, schedule, cap, reason);
        }

        private static string Validate(FormField field)
        {
            string value = field.Value, kind = field.Definition.Kind;
            if (kind == "optional") return null;
            if (value.Length == 0 || (kind == "choice" && ((ComboBox)field.Input).SelectedIndex <= 0)) return "Completa este campo.";
            if (kind == "email")
            {
                try { var email = new MailAddress(value); if (email.Address != value || !value.Split('@').Last().Contains(".")) return "Introduce un correo válido, por ejemplo nombre@correo.com."; }
                catch (FormatException) { return "Introduce un correo válido, por ejemplo nombre@correo.com."; }
            }
            if (kind == "date") { DateTime date; if (!DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) return "Usa una fecha válida en formato dd/MM/aaaa."; }
            if (kind == "time") { DateTime time; if (!DateTime.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time)) return "Usa una hora entre 00:00 y 23:59 (HH:mm)."; }
            if (kind == "amount" || kind == "nonnegative")
            {
                decimal amount;
                if (!Regex.IsMatch(value, @"^\d{1,7}([.,]\d{1,2})?$") || !decimal.TryParse(value.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) || (kind == "nonnegative" ? amount < 0 : amount <= 0))
                    return kind == "nonnegative" ? "Introduce un importe igual o mayor que 0, con hasta 2 decimales y sin separadores de miles." : "Introduce un importe mayor que 0, con hasta 2 decimales y sin separadores de miles.";
            }
            if (kind == "integer") { int count; int max = field.Definition.Label.Contains("huéspedes") ? 6 : 500; if (!int.TryParse(value, out count) || count < 1 || count > max) return "Introduce un número entero entre 1 y " + max + "."; }
            if (kind == "phone" && (!Regex.IsMatch(value, @"^\+?[\d\s()\-]{7,22}$") || value.Count(char.IsDigit) < 7)) return "Introduce un teléfono de al menos 7 dígitos.";
            return null;
        }

        private void AddNotice(string title, string description)
        {
            var stack = new StackPanel(); stack.Children.Add(Text(title, 14, true)); stack.Children.Add(Muted(description));
            var panel = Panel(stack, 16); panel.SetResourceReference(Border.BackgroundProperty, "Ui.Soft"); panel.Margin = new Thickness(0, 0, 0, 18); ModuleContent.Children.Add(panel);
        }
        private void AddHeading(StackPanel target, string title, string subtitle)
        {
            var heading = Text(title, 19, true); heading.Margin = new Thickness(0, 4, 0, 6); target.Children.Add(heading);
            var description = Muted(subtitle); description.Margin = new Thickness(0, 0, 0, 16); target.Children.Add(description);
        }
        private static SolidColorBrush Brush(string color) { return (SolidColorBrush)new BrushConverter().ConvertFromString(color); }
        private static TextBlock Text(string value, double size = 13, bool bold = false)
        {
            return new TextBlock { Text = value, FontSize = size, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
        }
        private TextBlock Muted(string value) { var text = Text(value, 12); text.Style = (Style)FindResource("Muted"); text.Margin = new Thickness(0, 4, 0, 4); return text; }
        private Border Panel(UIElement child, double padding = 20) { return new Border { Style = (Style)FindResource("Panel"), Padding = new Thickness(padding), Child = child, Margin = new Thickness(0, 0, 0, 20) }; }
        private Button Action(string label, Action action, bool primary = false)
        {
            var button = new Button { Content = label, Margin = new Thickness(0, 0, 8, 8) };
            if (primary) button.Style = (Style)FindResource("Primary");
            button.Click += (s, e) => action(); return button;
        }
        private static ComboBox Choice(string[] values, string label)
        {
            var combo = new ComboBox { ItemsSource = values, SelectedIndex = 0 }; AutomationProperties.SetName(combo, label); return combo;
        }
        private static string StatusColor(string status)
        {
            if (new[] { "Cancelada", "Vencido", "Mantenimiento", "Por cobrar", "Alta", "Bloqueada", "Crítica", "Bajo mínimo", "Suspendido temporalmente", "Prioridad" }.Contains(status)) return "Ui.Danger";
            if (new[] { "Por confirmar", "En limpieza", "Por renovar", "Por revisar", "En preparación", "En planchado", "En lavado", "Por facturar", "Pendiente", "Inspección pendiente", "En revisión", "Aforo limitado", "Horario limitado", "Por programar", "Sugerido" }.Contains(status)) return "Ui.Warning";
            return new[] { "Disponible", "Activo", "Activa", "Confirmada", "Completado", "Conciliado", "Finalizada", "Entregada", "Inspeccionada", "Limpia", "Operativo", "Programada" }.Contains(status) ? "Ui.Good" : "Ui.Secondary";
        }
        private static TextBlock Status(string value)
        {
            var text = Text(value, 12, true); text.SetResourceReference(TextBlock.ForegroundProperty, StatusColor(value)); text.Margin = new Thickness(0, 10, 0, 8); return text;
        }
        private void AddHelp(StackPanel host, string value) { var help = Muted(value); help.Margin = new Thickness(0, 12, 0, 16); host.Children.Add(help); }
    }
}

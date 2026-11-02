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

namespace Crown__.Frontend.Views
{
    // UI state and illustrative fixtures only. Backend owns authorization, availability,
    // pricing, transactions, persistence and all real business validation.
    public partial class DashboardView : Page
    {
        private static readonly string[] Themes = { "Light", "Dark", "Brand" };
        private static readonly string[] ThemeLabels = { "Claro", "Oscuro", "Marca" };
        private readonly Dictionary<string, Button> navigation = new Dictionary<string, Button>();
        private readonly List<FormField> formFields = new List<FormField>();
        private ModuleDefinition currentModule;
        private int themeIndex;
        private TextBox searchBox;
        private ComboBox stateFilter, floorFilter;
        private StackPanel results;
        private TextBlock resultCount;
        private FrameworkElement previousFocus;
        private StackPanel formEditor, draftPreview;
        private bool reviewingDraft;

        private sealed class FormField
        {
            internal FieldDefinition Definition;
            internal Control Input;
            internal TextBlock Error;
            internal string Value { get { return Input is TextBox ? ((TextBox)Input).Text.Trim() : (string)((ComboBox)Input).SelectedItem ?? ""; } }
        }

        public DashboardView() : this("Light") { }
        public DashboardView(string theme)
        {
            InitializeComponent();
            themeIndex = Math.Max(0, Array.IndexOf(Themes, theme));
            ApplyTheme();
            foreach (var module in ModuleCatalog.All)
            {
                var button = new Button { Tag = module.Key, Style = (Style)FindResource("Nav"), ToolTip = module.Title };
                AutomationProperties.SetAutomationId(button, "Nav" + module.Key);
                AutomationProperties.SetName(button, module.Title);
                var content = new StackPanel { Orientation = Orientation.Horizontal };
                var icon = new Path { Data = Geometry.Parse(module.Icon), Width = 18, Height = 18, Stretch = Stretch.Uniform,
                    StrokeThickness = 1.5, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, Margin = new Thickness(0, 0, 11, 0) };
                icon.SetBinding(Shape.StrokeProperty, new Binding("Foreground") { Source = button });
                content.Children.Add(icon);
                var label = Text(module.Key == "Inicio" ? "Inicio" : module.Key == "FNB" ? "F & B" : module.Key == "GymSpa" ? "Gym / Spa" : module.Key == "Config" ? "Ajustes" : module.Key == "CheckInOut" ? "Check-in / out" : module.Title);
                label.SetBinding(TextBlock.ForegroundProperty, new Binding("Foreground") { Source = button });
                content.Children.Add(label);
                button.Content = content;
                button.Click += OnModuleClick;
                navigation.Add(module.Key, button);
                NavigationItems.Children.Add(button);
            }
            ShowModule("Inicio");
        }

        private void OnThemeCycle(object sender, RoutedEventArgs e)
        {
            themeIndex = (themeIndex + 1) % Themes.Length;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            Tag = Themes[themeIndex];
            string prefix = themeIndex == 2 ? "ThemeBrand" : Themes[themeIndex];
            foreach (var name in new[] { "Page", "Ink", "Secondary", "Field", "Border", "Primary", "OnPrimary", "Focus" })
                Resources["Ui." + name] = FindResource(prefix + "." + name);
            bool dark = themeIndex == 1;
            Resources["Ui.Soft"] = Brush(dark ? "#2A3D4C" : themeIndex == 2 ? "#E7F1EE" : "#EDF0F2");
            Resources["Ui.Good"] = Brush(dark ? "#9AD7B3" : "#276447");
            Resources["Ui.Warning"] = Brush(dark ? "#E5C780" : "#805C1C");
            Resources["Ui.Danger"] = Brush(dark ? "#FFAAAA" : "#AE3535");
            ThemeButton.Content = "Tema: " + ThemeLabels[themeIndex];
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService == null) return;
            while (NavigationService.CanGoBack) NavigationService.RemoveBackEntry();
            NavigationService.Navigate(new LoginView(Themes[themeIndex]));
        }

        private void OnModuleClick(object sender, RoutedEventArgs e) { ShowModule((string)((Button)sender).Tag); }

        private void ShowModule(string key)
        {
            currentModule = ModuleCatalog.All.First(m => m.Key == key);
            PageTitle.Text = currentModule.Title;
            PageDescription.Text = currentModule.Description;
            foreach (var item in navigation)
            {
                item.Value.SetResourceReference(BackgroundProperty, item.Key == key ? "Ui.Soft" : "Ui.Field");
                item.Value.FontWeight = item.Key == key ? FontWeights.SemiBold : FontWeights.Normal;
                AutomationProperties.SetHelpText(item.Value, item.Key == key ? "Sección actual" : "Abrir sección");
            }
            ModuleContent.Children.Clear();
            searchBox = null; stateFilter = null; floorFilter = null; results = null;
            ContentScroll.ScrollToTop();
            if (key == "Inicio") BuildHome();
            else if (key == "Config") BuildSettings();
            else
            {
                AddMetrics(currentModule.Metrics);
                if (key == "Reportes") BuildOccupancyChart();
                if (key == "GymSpa") AddNotice("Gimnasio · 18 de 20 plazas ocupadas", "Horario de muestra: 06:00–22:00. Spa: 09:00–20:00. Revisa la disponibilidad antes de confirmar una cita.");
                if (key == "CheckInOut") AddNotice("Antes de completar un movimiento", "Check-in: documento, datos y habitación. Check-out: consumos, saldo y devolución de llaves. Esta vista no registra entradas ni salidas.");
                BuildModuleList();
            }
        }

        private void BuildHome()
        {
            AddMetrics(new[] { "82%|Ocupación", "14|Llegadas hoy", "11|Salidas hoy", "$148.50|Tarifa media diaria", "$121.77|Ingreso por habitación", "$3,920.10|Caja del turno" });
            var actions = new WrapPanel { Margin = new Thickness(0, 0, 0, 22) };
            actions.Children.Add(Action("Nueva reserva", () => { ShowModule("Reservas"); OpenForm(); }, true));
            actions.Children.Add(Action("Revisar llegadas y salidas", () => ShowModule("CheckInOut")));
            actions.Children.Add(Action("Ver habitaciones", () => ShowModule("Habitaciones")));
            ModuleContent.Children.Add(actions);
            AddHeading(ModuleContent, "Próximos movimientos", "Llegadas y salidas que requieren atención durante este turno.");
            var reception = ModuleCatalog.All.First(m => m.Key == "CheckInOut");
            var rows = new StackPanel();
            foreach (var row in reception.Rows.Take(3))
            {
                var captured = row;
                rows.Children.Add(HomeRow(row[2], row[1], "Habitación " + row[0] + " · " + row[3], row[4], () => { ShowModule("CheckInOut"); ShowDetail(captured); }));
            }
            ModuleContent.Children.Add(Panel(rows, 0));
            AddHeading(ModuleContent, "Atención del turno", "Pendientes de coordinación entre las áreas del hotel.");
            var alerts = new StackPanel();
            alerts.Children.Add(HomeRow("Alta", "Suite 410 pendiente de limpieza", "Housekeeping · Revisar antes de asignar", "En limpieza", () => ShowModule("Habitaciones")));
            alerts.Children.Add(HomeRow("Media", "Andes Corp. vence en 6 días", "Convenios · Contactar para renovación", "Por renovar", () => ShowModule("Convenios")));
            alerts.Children.Add(HomeRow("Media", "Gimnasio próximo a su aforo", "Bienestar · 18 de 20 plazas ocupadas", "En curso", () => ShowModule("GymSpa")));
            ModuleContent.Children.Add(Panel(alerts, 0));
        }

        private UIElement HomeRow(string time, string title, string subtitle, string status, Action open)
        {
            var grid = new Grid { Margin = new Thickness(16, 12, 16, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
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
            var wrap = new System.Windows.Controls.Primitives.UniformGrid { Columns = metrics.Length == 4 ? 4 : 3, Margin = new Thickness(0, 0, -10, 18) };
            foreach (string metric in metrics)
            {
                string[] value = metric.Split('|');
                var stack = new StackPanel(); stack.Children.Add(Muted(value[1])); stack.Children.Add(Text(value[0], 25, true));
                var panel = Panel(stack, 16); panel.Margin = new Thickness(0, 0, 10, 10); wrap.Children.Add(panel);
            }
            ModuleContent.Children.Add(wrap);
        }

        private void BuildModuleList()
        {
            var toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
            searchBox = new TextBox { Width = 250, Tag = "Buscar en " + currentModule.Title.ToLowerInvariant(), Margin = new Thickness(0, 0, 10, 8) };
            AutomationProperties.SetName(searchBox, "Buscar en " + currentModule.Title);
            toolbar.Children.Add(searchBox);
            var states = new[] { "Todos los estados" }.Concat(currentModule.Rows.Select(r => r[4]).Distinct()).ToArray();
            stateFilter = Choice(states, "Filtrar por estado"); stateFilter.Width = 178; stateFilter.Margin = new Thickness(0, 0, 10, 8);
            toolbar.Children.Add(stateFilter);
            if (currentModule.Key == "Habitaciones")
            {
                floorFilter = Choice(new[] { "Todos los pisos", "Piso 1", "Piso 2", "Piso 3", "Piso 4" }, "Filtrar por piso");
                floorFilter.Width = 150; floorFilter.Margin = new Thickness(0, 0, 10, 8); toolbar.Children.Add(floorFilter);
            }
            toolbar.Children.Add(Action(currentModule.Action, OpenForm, true));
            ModuleContent.Children.Add(toolbar);
            resultCount = Muted(""); resultCount.Margin = new Thickness(0, 0, 0, 12); AutomationProperties.SetLiveSetting(resultCount, AutomationLiveSetting.Polite);
            ModuleContent.Children.Add(resultCount);
            results = new StackPanel(); ModuleContent.Children.Add(results);
            searchBox.TextChanged += (s, e) => RefreshResults();
            stateFilter.SelectionChanged += (s, e) => RefreshResults();
            if (floorFilter != null) floorFilter.SelectionChanged += (s, e) => RefreshResults();
            RefreshResults();
        }

        private static string Normalize(string text)
        {
            var result = new StringBuilder();
            foreach (char character in text.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark) result.Append(char.ToLowerInvariant(character));
            return result.ToString().Normalize(NormalizationForm.FormC);
        }

        private void RefreshResults()
        {
            if (results == null) return;
            string term = Normalize(searchBox.Text.Trim());
            string state = stateFilter.SelectedIndex > 0 ? (string)stateFilter.SelectedItem : null;
            string floor = floorFilter != null && floorFilter.SelectedIndex > 0 ? (string)floorFilter.SelectedItem : null;
            var rows = currentModule.Rows.Where(r => Normalize(string.Join(" ", r)).Contains(term) && (state == null || r[4] == state) && (floor == null || r[2] == floor)).ToArray();
            results.Children.Clear();
            resultCount.Text = rows.Length + (rows.Length == 1 ? " registro de muestra" : " registros de muestra") + (currentModule.Key == "Habitaciones" ? " · Vista parcial del hotel" : "");
            if (rows.Length == 0)
            {
                var empty = new StackPanel { Margin = new Thickness(6, 14, 6, 14) };
                empty.Children.Add(Text("No hay coincidencias", 18, true));
                empty.Children.Add(Muted("Prueba con otro nombre, habitación o estado, o restablece los filtros."));
                empty.Children.Add(Action("Restablecer filtros", () => { searchBox.Clear(); stateFilter.SelectedIndex = 0; if (floorFilter != null) floorFilter.SelectedIndex = 0; }));
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
            table.Children.Add(TableRow(currentModule.Columns, true));
            foreach (var row in rows) table.Children.Add(TableRow(row, false));
            var scroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = table, Focusable = false };
            results.Children.Add(Panel(scroll, 0));
            if (currentModule.Key == "CheckInOut")
                AddHelp(results, "Los botones abren la ficha de revisión. Confirmar un movimiento o liquidar un saldo requiere la integración de backend.");
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
            var border = new Border { Child = grid, BorderThickness = new Thickness(0, 0, 0, 1) };
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

        private void BuildSettings()
        {
            var appearance = new StackPanel();
            AddHeading(appearance, "Apariencia", "El tema se aplica al instante durante esta sesión de muestra.");
            var themes = new WrapPanel();
            for (int i = 0; i < Themes.Length; i++) { int index = i; themes.Children.Add(Action(ThemeLabels[i], () => { themeIndex = index; ApplyTheme(); })); }
            appearance.Children.Add(themes); ModuleContent.Children.Add(Panel(appearance));
            var reception = new StackPanel(); AddHeading(reception, "Preferencias de recepción", "Valores ilustrativos. No modifican la configuración real del hotel.");
            AddSetting(reception, "Idioma de la interfaz", Choice(new[] { "Español" }, "Idioma de la interfaz"));
            AddSetting(reception, "Moneda de referencia", Choice(new[] { "USD · Dólar estadounidense", "BOB · Boliviano" }, "Moneda de referencia"));
            var checkIn = new TextBox { Text = "14:00", MaxLength = 5 }; var checkOut = new TextBox { Text = "11:00", MaxLength = 5 };
            AddSetting(reception, "Hora de check-in (HH:mm)", checkIn); AddSetting(reception, "Hora de check-out (HH:mm)", checkOut);
            var errors = Text(""); errors.SetResourceReference(TextBlock.ForegroundProperty, "Ui.Danger"); reception.Children.Add(errors);
            reception.Children.Add(Action("Revisar preferencias", () => {
                DateTime parsed;
                if (!DateTime.TryParseExact(checkIn.Text.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed) ||
                    !DateTime.TryParseExact(checkOut.Text.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                { errors.Text = "Introduce ambas horas en formato HH:mm, por ejemplo 14:00."; return; }
                errors.Text = ""; OpenInfo("Preferencias de muestra", "Check-in: " + checkIn.Text + "\nCheck-out: " + checkOut.Text + "\n\nLa configuración real se guardará al integrar backend.");
            }, true));
            var receptionPanel = Panel(reception); receptionPanel.Margin = new Thickness(0, 18, 0, 0); ModuleContent.Children.Add(receptionPanel);
            var notifications = new StackPanel(); AddHeading(notifications, "Notificaciones", "Previsualiza las preferencias del puesto de recepción.");
            foreach (string label in new[] { "Llegadas y salidas pendientes", "Habitaciones listas para asignar", "Convenios próximos a vencer", "Consumos y saldos pendientes" })
                notifications.Children.Add(new CheckBox { Content = label, IsChecked = true });
            var notificationPanel = Panel(notifications); notificationPanel.Margin = new Thickness(0, 18, 0, 0); ModuleContent.Children.Add(notificationPanel);
            AddNotice("Puesto de recepción", "Crown & Sovereign · Interfaz de muestra. Usuarios, permisos y conexiones serán administrados por backend.");
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
            OpenDialog(currentModule.Key == "Habitaciones" ? "Habitación " + row[0] : row[0], currentModule.Title + " · Ficha de muestra");
            for (int i = 0; i < currentModule.Columns.Length; i++)
            {
                DialogBody.Children.Add(Muted(currentModule.Columns[i]));
                var value = Text(row[i], 15, true); value.Margin = new Thickness(0, 3, 0, 16); DialogBody.Children.Add(value);
            }
            if (currentModule.Key == "CheckInOut")
                AddHelp(DialogBody, row[4] == "Llegada" ? "Antes del check-in: verificar identidad, reserva, ocupantes y disponibilidad de la habitación." : "Antes del check-out: revisar consumos, saldo, factura y devolución de llaves.");
            if (currentModule.Key == "Habitaciones") AddHelp(DialogBody, "La asignación y el cambio de estado se confirmarán con disponibilidad real al integrar backend.");
            DialogSubmit.Visibility = Visibility.Collapsed;
            DialogBody.Children.Add(Action(currentModule.Action, OpenForm, true));
        }

        private void OpenInfo(string title, string body)
        {
            OpenDialog(title, "Previsualización local"); DialogBody.Children.Add(Text(body)); DialogSubmit.Visibility = Visibility.Collapsed;
        }

        private void OpenForm()
        {
            OpenDialog(currentModule.Action, "Completa los campos para revisar un borrador de muestra. No se enviará ni se guardará.");
            formEditor = new StackPanel(); draftPreview = new StackPanel { Visibility = Visibility.Collapsed };
            DialogBody.Children.Add(formEditor); DialogBody.Children.Add(draftPreview);
            foreach (var definition in currentModule.Fields)
            {
                Control input;
                if (definition.Kind == "choice")
                {
                    var combo = Choice(new[] { "Selecciona una opción" }.Concat(definition.Options).ToArray(), definition.Label);
                    input = combo;
                }
                else
                {
                    var text = new TextBox { Tag = definition.Hint, MaxLength = definition.Kind == "notes" || definition.Kind == "optional" ? 500 : 120 };
                    if (definition.Kind == "notes") { text.Height = 82; text.TextWrapping = TextWrapping.Wrap; text.AcceptsReturn = true; text.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; }
                    input = text;
                }
                AutomationProperties.SetName(input, definition.Label);
                AutomationProperties.SetHelpText(input, definition.Hint + (definition.Kind == "optional" ? "" : ". Campo obligatorio"));
                var label = new Label { Content = definition.Label + (definition.Kind == "optional" ? " (opcional)" : " *"), Target = input, Padding = new Thickness(0, 0, 0, 6) };
                label.SetResourceReference(Control.ForegroundProperty, "Ui.Ink");
                var error = Text("", 12); error.SetResourceReference(TextBlock.ForegroundProperty, "Ui.Danger"); error.Visibility = Visibility.Collapsed;
                var field = new FormField { Definition = definition, Input = input, Error = error }; formFields.Add(field);
                input.Margin = new Thickness(0, 0, 0, 4);
                formEditor.Children.Add(label); formEditor.Children.Add(input); formEditor.Children.Add(error);
                formEditor.Children.Add(new Border { Height = 10 });
                if (input is TextBox) ((TextBox)input).TextChanged += (s, e) => ClearFieldError(field);
                else ((ComboBox)input).SelectionChanged += (s, e) => ClearFieldError(field);
            }
            if (formFields.Count > 0)
            {
                var firstInput = formFields[0].Input;
                Dispatcher.BeginInvoke(new Action(() => { if (Overlay.IsVisible && firstInput.IsVisible) firstInput.Focus(); }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private void ClearFieldError(FormField field)
        {
            field.Error.Visibility = Visibility.Collapsed; field.Input.SetResourceReference(Control.BorderBrushProperty, "Ui.Border");
            DialogError.Visibility = Visibility.Collapsed;
        }

        private void OpenDialog(string title, string description)
        {
            if (Overlay.Visibility != Visibility.Visible) previousFocus = Keyboard.FocusedElement as FrameworkElement;
            formFields.Clear(); reviewingDraft = false; DialogBody.Children.Clear(); DialogError.Visibility = Visibility.Collapsed;
            DialogTitle.Text = title; DialogDescription.Text = description; DialogSubmit.Visibility = Visibility.Visible; DialogSubmit.Content = "Revisar borrador";
            Workspace.IsEnabled = false; Overlay.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke(new Action(() => Overlay.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void OnCloseDialog(object sender, RoutedEventArgs e)
        {
            Overlay.Visibility = Visibility.Collapsed; Workspace.IsEnabled = true;
            if (previousFocus != null && previousFocus.IsVisible && previousFocus.IsEnabled) previousFocus.Focus();
            else navigation[currentModule.Key].Focus();
        }

        private void OnPageKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && Overlay.Visibility == Visibility.Visible) { OnCloseDialog(sender, e); e.Handled = true; }
        }

        private void OnReviewDraft(object sender, RoutedEventArgs e)
        {
            if (reviewingDraft)
            {
                reviewingDraft = false; draftPreview.Visibility = Visibility.Collapsed; formEditor.Visibility = Visibility.Visible;
                DialogSubmit.Content = "Revisar borrador"; if (formFields.Count > 0) formFields[0].Input.Focus(); return;
            }
            FormField firstInvalid = null;
            foreach (var field in formFields)
            {
                string error = Validate(field);
                field.Error.Text = error ?? ""; field.Error.Visibility = error == null ? Visibility.Collapsed : Visibility.Visible;
                field.Input.SetResourceReference(Control.BorderBrushProperty, error == null ? "Ui.Border" : "Ui.Danger");
                if (error != null && firstInvalid == null) firstInvalid = field;
            }
            if (firstInvalid == null && (currentModule.Key == "Reservas" || currentModule.Key == "Reportes"))
            {
                var dates = formFields.Where(f => f.Definition.Kind == "date").ToArray();
                var start = DateTime.ParseExact(dates[0].Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var end = DateTime.ParseExact(dates[1].Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                if (end < start || (currentModule.Key == "Reservas" && end == start))
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
            draftPreview.Children.Clear();
            draftPreview.Children.Add(Text("Borrador para revisión", 18, true));
            AddHelp(draftPreview, "Estos datos solo se previsualizan. No se ha registrado ninguna operación.");
            foreach (var field in formFields)
            {
                draftPreview.Children.Add(Muted(field.Definition.Label)); var value = Text(string.IsNullOrEmpty(field.Value) ? "Sin observaciones" : field.Value);
                value.Margin = new Thickness(0, 3, 0, 14); draftPreview.Children.Add(value);
            }
            reviewingDraft = true; formEditor.Visibility = Visibility.Collapsed; draftPreview.Visibility = Visibility.Visible; DialogSubmit.Content = "Volver a editar";
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
            if (kind == "amount")
            {
                decimal amount;
                if (!Regex.IsMatch(value, @"^\d{1,7}([.,]\d{1,2})?$") || !decimal.TryParse(value.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) || amount <= 0)
                    return "Introduce un importe mayor que 0, con hasta 2 decimales y sin separadores de miles.";
            }
            if (kind == "integer") { int count; int max = field.Definition.Label.Contains("huéspedes") ? 6 : 100; if (!int.TryParse(value, out count) || count < 1 || count > max) return "Introduce un número entero entre 1 y " + max + "."; }
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
            if (new[] { "Cancelada", "Vencido", "Mantenimiento", "Por cobrar", "Alta" }.Contains(status)) return "Ui.Danger";
            if (new[] { "Por confirmar", "En limpieza", "Por renovar", "Por revisar", "En preparación", "En planchado", "En lavado", "Por facturar" }.Contains(status)) return "Ui.Warning";
            return "Ui.Good";
        }
        private static TextBlock Status(string value)
        {
            var text = Text(value, 12, true); text.SetResourceReference(TextBlock.ForegroundProperty, StatusColor(value)); text.Margin = new Thickness(0, 10, 0, 8); return text;
        }
        private void AddHelp(StackPanel host, string value) { var help = Muted(value); help.Margin = new Thickness(0, 12, 0, 16); host.Children.Add(help); }
    }
}

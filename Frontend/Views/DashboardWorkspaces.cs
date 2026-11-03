using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Crown__.Frontend.Security;

namespace Crown__.Frontend.Views
{
    public partial class DashboardView
    {
        private static readonly string[][] NavigationGroups = {
            new[]{"Mi puesto", "Inicio", "Alertas", "Preferencias"},
            new[]{"Recepción", "Reservas", "Habitaciones", "CheckInOut", "Folios", "Operacion"},
            new[]{"Gestión administrativa", "Eventos", "Tarifas", "Convenios", "Finanzas", "Reportes"},
            new[]{"Servicios", "FNB", "GymSpa", "Housekeeping", "Mantenimiento", "Lavanderia", "Transporte", "Inventario"},
            new[]{"Sistema", "Usuarios", "Auditoria", "Config"}
        };
        private static string ModuleGroup(string key) { return NavigationGroups.First(g => g.Skip(1).Contains(key))[0]; }
        private static string NavTitle(ModuleDefinition module)
        {
            switch (module.Key) {
                case "Inicio": return "Mi puesto";
                case "Preferencias": return "Preferencias";
                case "FNB": return "Comidas y bebidas";
                case "GymSpa": return "Spa y gimnasio";
                case "CheckInOut": return "Check-in y huéspedes";
                case "Eventos": return "Eventos y descuentos";
                case "Usuarios": return "Usuarios y permisos";
                default: return module.Title;
            }
        }
        private static string BasePermission(string module)
        {
            if (new[]{"Usuarios", "Config", "Tarifas", "Convenios", "Housekeeping"}.Contains(module)) return "Manage";
            if (module == "Reportes") return "Export";
            if (module == "Auditoria") return "View";
            return "Write";
        }
        private void DenyAccess() { OpenInfo("Acceso no disponible", "Tu perfil no tiene permiso para esta área o acción. Solicita una revisión al administrador."); }

        private IEnumerable<string[]> VisibleRows()
        {
            if (currentModule == null || !access.Allows(currentModule.Key, "View")) return Enumerable.Empty<string[]>();
            if (currentModule.Key == "CheckInOut" && receptionViewMode == 1) return HotelEventsAndLimits.GuestRows;
            if (currentModule.Key != "Alertas") return currentModule.Rows ?? new string[0][];
            var areaModules = new Dictionary<string, string> { {"Housekeeping", "Housekeeping"}, {"Mantenimiento", "Mantenimiento"}, {"Transporte", "Transporte"}, {"Finanzas", "Finanzas"}, {"Recepción", "CheckInOut"}, {"Alimentos y bebidas", "FNB"}, {"Bienestar", "GymSpa"}, {"Lavandería", "Lavanderia"} };
            return currentModule.Rows.Where(r => areaModules.ContainsKey(r[2]) && access.Allows(areaModules[r[2]], "View"));
        }

        private void BuildRoleHome()
        {
            bool isManagement = access.Allows("Eventos", "View");
            PageTitle.Text = isManagement ? "Dashboard gerencial y administrativo" : "Tu jornada en recepción";
            PageDescription.Text = access.RoleLabel + " · Sábado, 31 de octubre de 2026 · Indicadores en tiempo real simulados (RF-55 · RNF-02)";
            var available = ModuleCatalog.All.Where(m => access.Allows(m.Key, "View") && m.Key != "Inicio" && m.Key != "Alertas" && m.Key != "Preferencias").ToArray();
            if (isManagement)
            {
                AddMetrics(new[] {
                    "82%|Ocupación actual",
                    "$148.50|ADR (Tarifa media)",
                    "$121.77|RevPAR",
                    "$3,920.10|Ingresos del día",
                    "14 / 11|Llegadas y salidas hoy",
                    "3|Alertas activas del rol"
                });
            }
            else
            {
                var strip = new Grid { Margin = new Thickness(0, 0, 0, 24) };
                strip.ColumnDefinitions.Add(new ColumnDefinition()); strip.ColumnDefinitions.Add(new ColumnDefinition()); strip.ColumnDefinitions.Add(new ColumnDefinition());
                string[] metrics = access.Allows("CheckInOut", "View")
                    ? new[] { "14|Llegadas hoy", "11|Salidas hoy", "96|Huéspedes in-house" }
                    : new[] { available.Length + "|Áreas de trabajo", "3|Pendientes de muestra", "Turno 1|Jornada actual" };
                for (int i = 0; i < metrics.Length; i++) {
                    var parts = metrics[i].Split('|'); var item = new StackPanel { Margin = new Thickness(i == 0 ? 0 : 18, 0, 10, 0) };
                    item.Children.Add(Muted(parts[1])); item.Children.Add(Text(parts[0], 25, true));
                    var cell = new Border { Child = item, BorderThickness = new Thickness(i == 0 ? 0 : 1, 0, 0, 0) }; cell.SetResourceReference(Border.BorderBrushProperty, "Ui.Border");
                    Grid.SetColumn(cell, i); strip.Children.Add(cell);
                }
                ModuleContent.Children.Add(strip);
            }

            if (isManagement)
            {
                AddHeading(ModuleContent, "Próximos eventos y descuentos sugeridos", "Exclusivo de Administración y Gerencia para anticipar campañas comerciales y ajustar servicios.");
                var upcomingList = new StackPanel();
                foreach (var evt in HotelEventsAndLimits.UpcomingEvents.Take(3))
                {
                    var capturedEvt = evt;
                    string dates = evt.Start.ToString("dd MMM", CultureInfo.GetCultureInfo("es-BO")) + " – " + evt.End.ToString("dd MMM", CultureInfo.GetCultureInfo("es-BO"));
                    upcomingList.Children.Add(HomeRow("-" + evt.SuggestedDiscount, evt.Title + " (" + dates + ")", evt.TargetServices + " · " + evt.DemandLevel, evt.Status, () => {
                        ShowModule("Eventos");
                        OpenEventDiscountContext(capturedEvt);
                    }));
                }
                ModuleContent.Children.Add(Panel(upcomingList, 0));

                AddHeading(ModuleContent, "Gestión administrativa y control de servicios", "Supervisión financiera, tarifas, límites de servicios y parámetros del hotel.");
                var adminTasks = new StackPanel();
                string[] adminPriorityKeys = { "Finanzas", "Tarifas", "FNB", "GymSpa", "Usuarios", "Config" };
                foreach (var module in adminPriorityKeys.Select(k => available.FirstOrDefault(m => m.Key == k)).Where(m => m != null).Take(5))
                {
                    var captured = module;
                    string detail = module.Key == "FNB"
                        ? "Control gerencial de horarios y cupos de comidas y restaurante"
                        : module.Key == "GymSpa"
                        ? "Control gerencial de horarios y aforo máximo de spa y gimnasio"
                        : module.Description;
                    adminTasks.Children.Add(HomeRow("Gestión", NavTitle(module), detail, "Activo", () => ShowModule(captured.Key)));
                }
                ModuleContent.Children.Add(Panel(adminTasks, 0));
            }
            else
            {
                AddHeading(ModuleContent, "Pendientes de tu puesto", "Abre un área para revisar llegadas, reservas, habitaciones y servicios de huéspedes.");
                var tasks = new StackPanel();
                foreach (var module in available.Take(5)) {
                    var captured = module;
                    string detail = module.Key == "CheckInOut" ? "14 llegadas, 11 salidas y directorio de huéspedes unificado" : module.Description;
                    tasks.Children.Add(HomeRow("Turno 1", NavTitle(module), detail, "Pendiente", () => ShowModule(captured.Key)));
                }
                ModuleContent.Children.Add(Panel(tasks, 0));
            }

            var quick = new WrapPanel();
            foreach (var module in available.Take(6)) { string key = module.Key; quick.Children.Add(Action(NavTitle(module), () => ShowModule(key))); }
            quick.Children.Add(Action("Preferencias y tema", () => ShowModule("Preferencias")));
            AddHeading(ModuleContent, "Accesos de trabajo", "Solo aparecen las áreas asignadas a tu perfil."); ModuleContent.Children.Add(quick);
            if (access.Allows("Reservas", "Write")) ModuleContent.Children.Add(Action("Nueva reserva", () => { ShowModule("Reservas"); OpenForm(); }, true));
            if (access.Allows("Reportes", "View")) { AddHeading(ModuleContent, "Ocupación del hotel", "Evolución ilustrativa para la revisión gerencial y administrativa."); BuildOccupancyChart(); }
        }

        private void BuildWorkspace()
        {
            ModuleContent.Children.Clear(); activeThemeCombo = null; offlinePreferenceCheck = null; searchBox = null; stateFilter = null; floorFilter = null; subAreaFilter = null; results = null;
            var flows = OperationsCatalog.Workflows.Where(w => w.Module == currentModule.Key && access.Allows(w.Module, w.Permission)).ToArray();
            if (flows.Length > 0) {
                var tools = new Grid { Margin = new Thickness(0, 0, 0, 22) };
                tools.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                tools.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var names = new[]{"Vista general"}.Concat(flows.Select(w => w.Title)).ToArray();
                var chooser = Choice(names, "Área de trabajo de " + currentModule.Title);
                chooser.MaxWidth = 440; chooser.HorizontalAlignment = HorizontalAlignment.Left; chooser.MinWidth = 280;
                chooser.SelectedIndex = currentWorkflow == null ? 0 : Array.IndexOf(flows, currentWorkflow) + 1;
                AutomationProperties.SetAutomationId(chooser, "WorkspacePicker");
                chooser.SelectionChanged += (s, e) => {
                    SelectWorkflow(chooser.SelectedIndex == 0 ? null : flows[chooser.SelectedIndex - 1].Key);
                };
                tools.Children.Add(chooser);
                var count = Muted(flows.Length + " flujos disponibles"); count.Margin = new Thickness(12, 0, 0, 0); Grid.SetColumn(count, 1); tools.Children.Add(count);
                ModuleContent.Children.Add(tools);
            }
            if (currentWorkflow != null) { BuildWorkflow(); return; }
            if (currentModule.Key == "Config") { BuildAdministration(); return; }
            if (currentModule.Key == "Usuarios") { AddNotice("Acceso de personal", "El huésped no tiene acceso al sistema. Cada usuario recibe permisos por módulo y acción."); }
            if (currentModule.Key == "Alertas") {
                var records = VisibleRows().ToArray();
                AddMetrics(new[]{records.Length + "|Avisos del perfil", records.Count(r => r[4] == "Alta") + "|Prioridad alta", "Turno 1|Jornada de muestra"});
            } else AddMetrics(currentModule.Metrics);
            if (currentModule.Key == "Habitaciones") {
                var legend = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
                foreach (string state in new[]{"Disponible", "Ocupada", "Sucia", "En limpieza", "Limpia", "Inspeccionada", "Fuera de servicio", "Mantenimiento", "Bloqueada"}) { var tag = Status(state); tag.Margin = new Thickness(0, 0, 16, 8); legend.Children.Add(tag); }
                ModuleContent.Children.Add(legend);
            }
            if (currentModule.Key == "Housekeeping") AddNotice("Inspección antes de liberar", "En hoteles de 4, 5 estrellas y lujo, la limpieza diaria y la inspección de supervisión son obligatorias. Una habitación con alerta no debe marcarse inspeccionada.");
            if (new[]{"FNB", "GymSpa", "Lavanderia", "Transporte"}.Contains(currentModule.Key))
                BuildServiceLimitsStrip(currentModule.Key);
            if (currentModule.Key == "Eventos")
                BuildEventsCalendarSection();
            if (currentModule.Key == "Reportes") BuildOccupancyChart();
            if (currentModule.Key == "CheckInOut")
                BuildReceptionSubViewSwitcher();
            BuildModuleList();
        }

        private void BuildReceptionSubViewSwitcher()
        {
            var bar = new WrapPanel { Margin = new Thickness(0, 0, 0, 14) };
            var movBtn = Action("Movimientos Check-in / out (" + currentModule.Rows.Length + ")", () => { receptionViewMode = 0; BuildWorkspace(); }, receptionViewMode == 0);
            AutomationProperties.SetAutomationId(movBtn, "TabMovimientos");
            var guestBtn = Action("Directorio de huéspedes (" + HotelEventsAndLimits.GuestRows.Length + ")", () => { receptionViewMode = 1; BuildWorkspace(); }, receptionViewMode == 1);
            AutomationProperties.SetAutomationId(guestBtn, "TabHuespedes");
            bar.Children.Add(movBtn);
            bar.Children.Add(guestBtn);
            ModuleContent.Children.Add(bar);
            AddNotice(
                receptionViewMode == 0 ? "Recepción unificada · Llegadas y salidas" : "Recepción unificada · Directorio de huéspedes",
                receptionViewMode == 0
                    ? "Consulta llegadas y salidas previstas o cambia a Directorio de huéspedes sin salir de este menú."
                    : "Consulta fichas, estancias y preferencias de atención; usa Área de trabajo para historial, consentimiento o duplicados.");
        }

        private void BuildServiceLimitsStrip(string moduleKey)
        {
            var items = HotelEventsAndLimits.ServiceLimits.Where(s => s.ModuleKey == moduleKey).ToArray();
            if (items.Length == 0) return;
            bool canLimit = access.Allows(moduleKey, "Limit");
            var host = new StackPanel();
            var headerRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var titleStack = new StackPanel();
            titleStack.Children.Add(Text(moduleKey == "FNB" ? "Disponibilidad de comidas y restaurante" : moduleKey == "GymSpa" ? "Horarios y aforo de Spa y Gimnasio" : "Estado operativo del servicio", 16, true));
            titleStack.Children.Add(Muted(canLimit ? "Tu perfil (Administración / Gerencia) puede limitar horarios, cupos o suspender servicios temporalmente." : "Límites operativos vigentes establecidos por Gerencia y Administración."));
            headerRow.Children.Add(titleStack);
            host.Children.Add(headerRow);

            var cards = new System.Windows.Controls.Primitives.UniformGrid { Columns = items.Length > 1 ? 2 : 1, Margin = new Thickness(0, 4, -10, 0) };
            foreach (var item in items)
            {
                var captured = item;
                var cardStack = new StackPanel();
                var topLine = new Grid();
                topLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                topLine.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                topLine.Children.Add(Text(item.ServiceName, 14, true));
                var statusText = Status(item.State);
                statusText.Margin = new Thickness(8, 0, 0, 0);
                Grid.SetColumn(statusText, 1);
                topLine.Children.Add(statusText);
                cardStack.Children.Add(topLine);
                cardStack.Children.Add(Muted("Horario: " + item.Schedule + " · Cupo: " + item.Capacity));
                cardStack.Children.Add(Muted(item.Reason));
                if (canLimit)
                {
                    var limitBtn = Action("Limitar servicio", () => OpenQuickServiceLimitForm(captured));
                    limitBtn.Margin = new Thickness(0, 8, 0, 0);
                    AutomationProperties.SetAutomationId(limitBtn, "LimitServiceBtn_" + item.Code);
                    cardStack.Children.Add(limitBtn);
                }
                var card = Panel(cardStack, 14);
                card.SetResourceReference(Border.BackgroundProperty, "Ui.Soft");
                card.Margin = new Thickness(0, 0, 10, 10);
                cards.Children.Add(card);
            }
            host.Children.Add(cards);
            ModuleContent.Children.Add(Panel(host, 16));
        }

        private void OpenQuickServiceLimitForm(ServiceLimitItem item)
        {
            if (!access.Allows(currentModule.Key, "Limit")) { DenyAccess(); return; }
            var defs = new[] {
                new FieldDefinition("Estado operativo", "choice", "", "Operativo", "Aforo limitado", "Horario limitado", "Suspendido temporalmente"),
                new FieldDefinition("Horario permitido", "text", "Ej. 07:00–21:00"),
                new FieldDefinition("Cupo o aforo máximo", "text", "Ej. 15 plazas"),
                new FieldDefinition("Motivo gerencial", "notes", "Indica la razón del ajuste operativo")
            };
            OpenCustomForm("Limitar · " + item.ServiceName, defs, "Limit", () => {
                string state = formFields[0].Value;
                string schedule = formFields[1].Value;
                string capacity = formFields[2].Value;
                string reason = formFields[3].Value;
                HotelEventsAndLimits.ApplySimulatedLimit(item.ServiceName, state, schedule, capacity, reason);
                BuildWorkspace();
            });
            Prefill("Estado operativo", item.State);
            Prefill("Horario permitido", item.Schedule);
            Prefill("Cupo o aforo máximo", item.Capacity);
            Prefill("Motivo gerencial", item.Reason);
        }

        private int eventMonthOffset; // 0 = Nov 2026, 1 = Dic 2026, 2 = Ene 2027
        private void BuildEventsCalendarSection()
        {
            if (!access.Allows("Eventos", "View")) { DenyAccess(); return; }
            DateTime baseMonth = new DateTime(2026, 11, 1).AddMonths(eventMonthOffset);
            var culture = CultureInfo.GetCultureInfo("es-BO");

            var container = new StackPanel();
            var header = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
            var monthChoices = new[] { "Noviembre 2026", "Diciembre 2026", "Enero 2027" };
            var monthPicker = Choice(monthChoices, "Mes del calendario de eventos");
            monthPicker.Width = 176; monthPicker.SelectedIndex = eventMonthOffset; monthPicker.Margin = new Thickness(0, 0, 8, 8);
            AutomationProperties.SetAutomationId(monthPicker, "EventsMonthPicker");
            monthPicker.SelectionChanged += (s, e) => { if (monthPicker.SelectedIndex >= 0 && monthPicker.SelectedIndex != eventMonthOffset) { eventMonthOffset = monthPicker.SelectedIndex; BuildWorkspace(); } };
            header.Children.Add(monthPicker);
            header.Children.Add(Action("Mes anterior", () => { eventMonthOffset = (eventMonthOffset + 2) % 3; BuildWorkspace(); }));
            header.Children.Add(Action("Mes siguiente", () => { eventMonthOffset = (eventMonthOffset + 1) % 3; BuildWorkspace(); }));
            if (access.Allows("FNB", "Limit"))
                header.Children.Add(Action("Limitar comidas / spa", () => ShowModule("GymSpa")));
            container.Children.Add(header);

            container.Children.Add(Text("Calendario comercial · " + culture.TextInfo.ToTitleCase(baseMonth.ToString("MMMM yyyy", culture)), 17, true));
            container.Children.Add(Muted("Selecciona un día destacado para consultar el impacto previsto y preparar el descuento correspondiente."));

            var calGrid = new Grid { Margin = new Thickness(0, 12, 0, 6) };
            for (int c = 0; c < 7; c++) calGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            string[] weekDays = { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
            calGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < 7; c++)
            {
                var wd = Text(weekDays[c], 11, true);
                wd.Margin = new Thickness(6, 4, 6, 8);
                wd.SetResourceReference(TextBlock.ForegroundProperty, "Ui.Secondary");
                Grid.SetColumn(wd, c); calGrid.Children.Add(wd);
            }

            int daysInMonth = DateTime.DaysInMonth(baseMonth.Year, baseMonth.Month);
            int firstOffset = ((int)baseMonth.DayOfWeek + 6) % 7; // Monday = 0
            int totalCells = firstOffset + daysInMonth;
            int weeks = (totalCells + 6) / 7;
            for (int r = 0; r < weeks; r++) calGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (int day = 1; day <= daysInMonth; day++)
            {
                DateTime date = new DateTime(baseMonth.Year, baseMonth.Month, day);
                int index = firstOffset + day - 1;
                int row = (index / 7) + 1;
                int col = index % 7;
                var evt = HotelEventsAndLimits.UpcomingEvents.FirstOrDefault(ev => date >= ev.Start && date <= ev.End);
                if (evt != null)
                {
                    var capturedEvt = evt;
                    var cellStack = new StackPanel();
                    cellStack.Children.Add(Text(day + " · -" + evt.SuggestedDiscount, 11, true));
                    var shortName = Text(evt.Title.Split('·')[0].Trim(), 10, false);
                    shortName.TextTrimming = TextTrimming.CharacterEllipsis;
                    shortName.MaxHeight = 28;
                    cellStack.Children.Add(shortName);

                    var btn = Action("", () => OpenEventDiscountContext(capturedEvt));
                    btn.Content = cellStack;
                    btn.HorizontalContentAlignment = HorizontalAlignment.Left;
                    btn.Margin = new Thickness(2);
                    btn.Padding = new Thickness(6, 5, 6, 5);
                    btn.MinHeight = 52;
                    btn.SetResourceReference(BackgroundProperty, "Ui.Soft");
                    btn.ToolTip = evt.Title + " (" + evt.SuggestedDiscount + " sugerido · " + evt.TargetServices + ")";
                    AutomationProperties.SetName(btn, day + " " + evt.Title + ", descuento sugerido " + evt.SuggestedDiscount);
                    AutomationProperties.SetAutomationId(btn, "EventDay_" + evt.Code + "_" + day);
                    Grid.SetRow(btn, row); Grid.SetColumn(btn, col);
                    calGrid.Children.Add(btn);
                }
                else
                {
                    var dayText = Muted(day.ToString());
                    dayText.Margin = new Thickness(8, 6, 4, 6);
                    var emptyCell = new Border { Child = dayText, Margin = new Thickness(2), MinHeight = 52, CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(1) };
                    emptyCell.SetResourceReference(Border.BorderBrushProperty, "Ui.Border");
                    Grid.SetRow(emptyCell, row); Grid.SetColumn(emptyCell, col);
                    calGrid.Children.Add(emptyCell);
                }
            }
            container.Children.Add(calGrid);
            ModuleContent.Children.Add(Panel(container, 16));
        }

        private void OpenEventDiscountContext(HotelEventItem evt)
        {
            if (!access.Allows("Eventos", "View")) { DenyAccess(); return; }
            OpenInfo(evt.Title,
                "Código: " + evt.Code + " · " + evt.Category +
                "\nVigencia: " + evt.Start.ToString("dd/MM/yyyy") + " – " + evt.End.ToString("dd/MM/yyyy") +
                "\nPrevisión: " + evt.DemandLevel +
                "\nDescuento recomendado: " + evt.SuggestedDiscount + " (" + evt.TargetServices + ")" +
                "\n\nEstrategia sugerida: " + evt.Notes);
            if (access.Allows("Eventos", "Write"))
            {
                var prepare = Action("Preparar descuento del evento", () => {
                    SelectWorkflow(null);
                    OpenForm();
                    Prefill("Evento asociado", evt.Title);
                    Prefill("Desde", evt.Start.ToString("dd/MM/yyyy"));
                    Prefill("Hasta", evt.End.ToString("dd/MM/yyyy"));
                    Prefill("Descuento sugerido (%)", evt.SuggestedDiscount.TrimEnd('%'));
                    Prefill("Alcance del descuento", evt.TargetServices);
                    Prefill("Condición comercial", evt.Notes);
                }, true);
                prepare.Margin = new Thickness(0, 14, 8, 0);
                AutomationProperties.SetAutomationId(prepare, "EventPrepareDiscount");
                DialogBody.Children.Add(prepare);
            }
        }

        private void SelectWorkflow(string key)
        {
            if (currentModule == null || !access.Allows(currentModule.Key, "View")) { DenyAccess(); return; }
            var workflow = key == null ? null : OperationsCatalog.Workflows.FirstOrDefault(w => w.Module == currentModule.Key && w.Key == key);
            if (key != null && (workflow == null || !access.Allows(workflow.Module, workflow.Permission))) { DenyAccess(); return; }
            currentWorkflow = workflow; BuildWorkspace(); ContentScroll.ScrollToTop();
        }

        private void BuildWorkflow()
        {
            if (!access.Allows(currentWorkflow.Module, currentWorkflow.Permission)) { DenyAccess(); return; }
            string subtitle = string.IsNullOrEmpty(currentWorkflow.Requirements)
                ? currentWorkflow.Description
                : currentWorkflow.Description + " (" + currentWorkflow.Requirements + ")";
            AddHeading(ModuleContent, currentWorkflow.Title, subtitle);
            if (currentWorkflow.Key == "Calendario") { BuildCalendar(); return; }
            if (currentWorkflow.Key == "Importar") { BuildImportPreview(); return; }
            if (currentModule.Key == "Usuarios" && currentWorkflow.Key == "Roles") { BuildPermissionMatrix(); return; }
            var summary = new StackPanel();
            if (currentWorkflow.Key == "Consulta") {
                summary.Children.Add(Muted("Ejemplo AUD-003 · Adriana Pérez · 31 oct 2026, 09:10"));
                summary.Children.Add(Text("Política de salida: 11:00 → 12:00", 17, true));
                summary.Children.Add(Muted("Motivo: ajuste de operación. Este evento es ilustrativo; la auditoría real debe ser inmutable."));
            }
            summary.Children.Add(Text(currentWorkflow.Guided ? "Revisión guiada" : "Datos para la revisión", 17, true));
            var fieldNames = string.Join(" · ", currentWorkflow.Fields.Select(f => f.Label));
            var description = Muted(fieldNames); description.Margin = new Thickness(0, 8, 0, 20); summary.Children.Add(description);
            summary.Children.Add(Action(currentWorkflow.Guided ? "Comenzar revisión" : "Preparar borrador", OpenForm, true));
            ModuleContent.Children.Add(Panel(summary));
            if (new[]{"Approve", "Cancel", "Privacy", "Limit"}.Contains(currentWorkflow.Permission)) AddNotice("Operación supervisada", "El borrador documenta motivo y alcance operativo. La autorización definitiva y el registro de auditoría corresponden al servicio.");
            var related = new StackPanel();
            foreach (var row in VisibleRows().Take(3)) { var captured = row; related.Children.Add(HomeRow(row[0], row[1], row[2] + " · " + row[3], row[4], () => ShowDetail(captured))); }
            if (related.Children.Count > 0) { AddHeading(ModuleContent, "Referencias del área", "Registros de muestra para orientar la revisión."); ModuleContent.Children.Add(Panel(related, 0)); }
        }

        private void BuildAdministration()
        {
            AddHeading(ModuleContent, "Administración del hotel", "Selecciona un flujo para preparar datos, reglas o plantillas. Los cambios se revisan sin guardarse.");
            var appearance = new StackPanel();
            appearance.Children.Add(Text("Apariencia del puesto", 17, true));
            appearance.Children.Add(Muted("También disponible para todos los roles en el menú Preferencias o con el atajo Alt + T."));
            var combo = CreateStyledThemeComboBox("Tema desde administración");
            combo.Margin = new Thickness(0, 12, 0, 0);
            appearance.Children.Add(combo);
            ModuleContent.Children.Add(Panel(appearance));
            foreach (var flow in OperationsCatalog.Workflows.Where(w => w.Module == "Config" && access.Allows(w.Module, w.Permission))) {
                var captured = flow;
                var row = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };
                row.Children.Add(Text(flow.Title, 16, true)); row.Children.Add(Muted(flow.Description));
                row.Children.Add(Action("Revisar " + flow.Title.ToLowerInvariant(), () => { currentWorkflow = captured; BuildWorkspace(); }));
                ModuleContent.Children.Add(row);
            }
        }

        private void AddDetailContext(string[] row)
        {
            switch (currentModule.Key) {
                case "Habitaciones":
                    AddHelp(DialogBody, "Ficha de muestra: cama matrimonial, vista interior, libre de humo. Accesibilidad y cuna sujetas a confirmación. Historial: Sucia → En limpieza → Inspección pendiente; cada cambio requiere responsable, fecha y motivo."); break;
                case "CheckInOut":
                    if (receptionViewMode == 1)
                        AddHelp(DialogBody, "Atención de muestra: prefiere piso alto y habitación sin humo. Alergias: consultar con el huésped. Consentimiento: pendiente de verificar. Historial y segmentación deben recuperarse del servicio.");
                    break;
                case "Eventos":
                    var match = HotelEventsAndLimits.UpcomingEvents.FirstOrDefault(ev => ev.Code == row[0]);
                    if (match != null) AddHelp(DialogBody, " Estrategia comercial sugerida: " + match.Notes + " (" + match.DemandLevel + ").");
                    break;
                case "Folios":
                    AddHelp(DialogBody, "Desglose ilustrativo: alojamiento $445.50; F&B $33.00; abono $300.00. Cada concepto debe mostrar usuario, fecha, impuesto y documento de origen. El saldo de muestra no es un cálculo en tiempo real."); break;
                case "Auditoria":
                    AddHelp(DialogBody, "Ejemplo: valor anterior 11:00; valor nuevo 12:00. Autor: usuario de muestra. Motivo: ajuste de operación. La consulta no permite editar ni eliminar eventos."); break;
                case "Housekeeping":
                    foreach (string item in new[]{"Ropa de cama y toallas", "Baño y superficies", "Minibar y amenities", "Revisión de seguridad"}) DialogBody.Children.Add(new CheckBox { Content = item, IsChecked = true, IsEnabled = false });
                    AddHelp(DialogBody, "Los checks ilustran la ficha. Usa el flujo Limpieza y turndown para preparar la revisión."); break;
            }
        }

        private int GuidedStepCount { get { return currentWorkflow != null && currentWorkflow.Key == "Asistente" ? 4 : 3; } }
        private int FieldStep(FormField field) { return Math.Min(GuidedStepCount - 1, formFields.IndexOf(field) * GuidedStepCount / Math.Max(1, formFields.Count)); }
        private void PrepareGuidedForm()
        {
            stepLabel = Text("", 14, true); stepLabel.Margin = new Thickness(0, 0, 0, 16); formEditor.Children.Insert(0, stepLabel);
            stepBack = Action("Paso anterior", () => { if (formStep > 0) { formStep--; UpdateGuidedForm(); } });
            formEditor.Children.Add(stepBack); UpdateGuidedForm();
        }
        private void UpdateGuidedForm()
        {
            string[] labels = currentWorkflow.Key == "CheckIn" ? new[]{"Identidad", "Estancia y acompañantes", "Llaves y entrega"}
                : currentWorkflow.Key == "CheckOut" ? new[]{"Folio y consumos", "Saldo y pago", "Factura y cierre"} : new[]{"Hotel", "Habitaciones", "Tarifas", "Personal"};
            stepLabel.Text = "Paso " + (formStep + 1) + " de " + GuidedStepCount + ": " + labels[formStep];
            for (int i = 0; i < formFields.Count; i++) {
                var field = formFields[i]; var visible = FieldStep(field) == formStep ? Visibility.Visible : Visibility.Collapsed;
                // Each generated field is label, input, error and spacing; index 0 is the step label.
                for (int child = 0; child < 4; child++) formEditor.Children[1 + i * 4 + child].Visibility = child == 2 && string.IsNullOrEmpty(field.Error.Text) ? Visibility.Collapsed : visible;
            }
            stepBack.Visibility = formStep == 0 ? Visibility.Collapsed : Visibility.Visible;
            DialogSubmit.Content = formStep < GuidedStepCount - 1 ? "Continuar" : "Revisar borrador";
            var target = formFields.FirstOrDefault(f => FieldStep(f) == formStep);
            if (target != null) Dispatcher.BeginInvoke(new Action(() => { if (Overlay.IsVisible) target.Input.Focus(); }));
        }

        private void BuildPermissionMatrix()
        {
            var roleLabels = FrontendAccessContext.DemoRoles.Select(r => FrontendAccessContext.GetRoleDisplayName(r) + " (" + r + ")").ToArray();
            var role = Choice(roleLabels, "Perfil para consultar permisos"); role.MaxWidth = 360; role.HorizontalAlignment = HorizontalAlignment.Left; role.Width = 320;
            var matrix = new StackPanel();
            Action refresh = () => {
                matrix.Children.Clear();
                string selectedRoleKey = FrontendAccessContext.DemoRoles[Math.Max(0, role.SelectedIndex)];
                var context = FrontendAccessContext.CreateDemonstration(selectedRoleKey);
                foreach (var module in ModuleCatalog.All) {
                    var actions = new[]{"View", "Write", "Cancel", "Approve", "Manage", "Limit", "Export", "Privacy"}.Where(a => context.Allows(module.Key, a)).ToArray();
                    if (actions.Length == 0) continue;
                    var translations = new Dictionary<string,string> { {"View","Ver"}, {"Write","Preparar"}, {"Cancel","Cancelar"}, {"Approve","Autorizar"}, {"Manage","Administrar"}, {"Limit","Limitar servicios"}, {"Export","Exportar"}, {"Privacy","Privacidad"} };
                    var line = new StackPanel { Margin = new Thickness(0, 14, 0, 0) }; line.Children.Add(Text(NavTitle(module), 14, true)); line.Children.Add(Muted(string.Join(" · ", actions.Select(a => translations[a])))); matrix.Children.Add(line);
                }
            };
            role.SelectionChanged += (s, e) => refresh(); refresh(); ModuleContent.Children.Add(role); ModuleContent.Children.Add(matrix);
            AddHelp(ModuleContent, "Esta consulta no cambia el perfil de tu sesión. Puedes probar cada perfil directamente desde el selector de rol en el Login.");
        }

        private void OnHelpClick(object sender, RoutedEventArgs e)
        {
            // Help must not replace an unfinished editor or its values.
            if (Overlay.Visibility == Visibility.Visible) return;
            OpenInfo("Ayuda de " + (currentModule == null ? "acceso" : currentModule.Title),
                "1. Elige un área en el menú. Solo aparecen las asignadas a tu perfil.\n\n2. Usa Área de trabajo para consultar los flujos disponibles. Buscar y los filtros actúan sobre la lista visible.\n\n3. Prepara los datos, corrige los campos indicados y revisa el resumen. Los borradores no registran operaciones reales.\n\nAtajos de teclado:\n• Alt+T: cambiar tema visual (Claro, Oscuro, Marca).\n• Ctrl+Shift+↑ / Ctrl+Shift+↓: cambiar al menú anterior o siguiente.\n• Ctrl+F: buscar en el listado actual.\n• F1: abrir esta ayuda · Esc: cerrar diálogo.\n\nSi necesitas otra área o acción, solicita un cambio de permisos al administrador.");
        }

        private DateTime calendarStart = new DateTime(2026, 10, 31);
        private int calendarMode = 1;
        private int calendarRoomTypeIndex;
        private void BuildCalendar()
        {
            var controls = new WrapPanel { Margin = new Thickness(0, 0, 0, 16) };
            var mode = Choice(new[]{"Día", "Semana", "Mes"}, "Período del calendario"); mode.SelectedIndex = calendarMode; mode.Width = 130; mode.Margin = new Thickness(0, 0, 8, 8);
            AutomationProperties.SetAutomationId(mode, "CalendarPeriod");
            mode.SelectionChanged += (s, e) => { calendarMode = mode.SelectedIndex; BuildWorkspace(); };
            controls.Children.Add(mode);
            var roomTypeChoices = new[]{"Todos los tipos", "Doble", "Deluxe", "Triple", "Suite"};
            var typeFilter = Choice(roomTypeChoices, "Filtrar calendario por tipo de habitación");
            typeFilter.SelectedIndex = calendarRoomTypeIndex; typeFilter.Width = 156; typeFilter.Margin = new Thickness(0, 0, 8, 8);
            AutomationProperties.SetAutomationId(typeFilter, "CalendarRoomType");
            typeFilter.SelectionChanged += (s, e) => { calendarRoomTypeIndex = typeFilter.SelectedIndex; BuildWorkspace(); };
            controls.Children.Add(typeFilter);
            controls.Children.Add(Action("Anterior", () => { calendarStart = calendarMode == 2 ? calendarStart.AddMonths(-1) : calendarStart.AddDays(calendarMode == 0 ? -1 : -7); BuildWorkspace(); }));
            controls.Children.Add(Action("Siguiente", () => { calendarStart = calendarMode == 2 ? calendarStart.AddMonths(1) : calendarStart.AddDays(calendarMode == 0 ? 1 : 7); BuildWorkspace(); }));
            ModuleContent.Children.Add(controls);
            var first = calendarMode == 2 ? new DateTime(calendarStart.Year, calendarStart.Month, 1) : calendarStart;
            int days = calendarMode == 2 ? DateTime.DaysInMonth(first.Year, first.Month) : calendarMode == 0 ? 1 : 7;
            ModuleContent.Children.Add(Text(first.ToString("dd MMM", CultureInfo.GetCultureInfo("es-BO")) + (days > 1 ? " – " + first.AddDays(days - 1).ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("es-BO")) : first.ToString(" yyyy")), 17, true));
            var grid = new Grid { MinWidth = 150 + days * (calendarMode == 2 ? 42 : 100), Margin = new Thickness(0, 16, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            for (int day = 0; day < days; day++) grid.ColumnDefinitions.Add(new ColumnDefinition());
            string[] allRooms = { "118 · Doble", "204 · Deluxe", "210 · Deluxe", "307 · Triple", "415 · Suite" };
            int[] allLengths = { 2, 3, 1, 4, 5 };
            string[] allGuests = { "Carlos Rojas", "María Fernández", "Lucía Gómez", "Eco Tours", "Diego Salazar" };
            string[] allReservationIds = { "RES-1049", "RES-1048", "RES-1051", "RES-1050", "RES-1052" };
            string selectedType = calendarRoomTypeIndex > 0 ? roomTypeChoices[calendarRoomTypeIndex] : null;
            var visibleIndices = Enumerable.Range(0, allRooms.Length).Where(idx => selectedType == null || allRooms[idx].Contains(selectedType)).ToArray();
            for (int i = 0; i <= visibleIndices.Length; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(i == 0 ? 42 : 64) });
            AddCalendarCell(grid, "Habitación", 0, 0, true);
            for (int day = 0; day < days; day++) AddCalendarCell(grid, first.AddDays(day).ToString("ddd dd", CultureInfo.GetCultureInfo("es-BO")), 0, day + 1, true);
            for (int rowPos = 0; rowPos < visibleIndices.Length; rowPos++) {
                int room = visibleIndices[rowPos];
                AddCalendarCell(grid, allRooms[room], rowPos + 1, 0, true);
                DateTime arrival = new DateTime(2026, 10, 31), departure = arrival.AddDays(allLengths[room]);
                int capturedRoom = room;
                for (int day = 0; day < days; day++) {
                    DateTime date = first.AddDays(day);
                    if (date >= arrival && date < departure) continue;
                    var button = Action("Libre", () => CalendarContext(allRooms[capturedRoom], date, date.AddDays(1), null, null));
                    button.Margin = new Thickness(1, 8, 1, 8); button.Padding = new Thickness(4); button.FontSize = 11;
                    AutomationProperties.SetName(button, allRooms[room] + ", " + date.ToString("dd/MM/yyyy") + ", Libre");
                    AutomationProperties.SetAutomationId(button, "CalendarFree" + allRooms[room].Split(' ')[0] + "-" + date.ToString("yyyyMMdd"));
                    Grid.SetRow(button, rowPos + 1); Grid.SetColumn(button, day + 1); grid.Children.Add(button);
                }
                int start = Math.Max(0, (arrival - first).Days), end = Math.Min(days, (departure - first).Days);
                if (end > start) {
                    var stay = Action(calendarMode == 2 && end - start < 3 ? "RES" : allGuests[room] + " · " + allReservationIds[room],
                        () => CalendarContext(allRooms[capturedRoom], arrival, departure, allReservationIds[capturedRoom], allGuests[capturedRoom]));
                    stay.Margin = new Thickness(2, 8, 2, 8); stay.Padding = new Thickness(6); stay.FontSize = 11;
                    stay.SetResourceReference(BackgroundProperty, "Ui.Soft");
                    stay.ToolTip = allGuests[room] + " · " + allReservationIds[room] + " · " + arrival.ToString("dd/MM") + " – " + departure.ToString("dd/MM");
                    AutomationProperties.SetName(stay, (string)stay.ToolTip);
                    AutomationProperties.SetAutomationId(stay, "CalendarStay" + allRooms[room].Split(' ')[0]);
                    Grid.SetRow(stay, rowPos + 1); Grid.SetColumn(stay, start + 1); Grid.SetColumnSpan(stay, end - start); grid.Children.Add(stay);
                }
            }
            ModuleContent.Children.Add(new ScrollViewer { Content = grid, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled });
            AddHelp(ModuleContent, "Ocupación ilustrativa por día, semana o mes filtrable por tipo de habitación (RF-04). Selecciona una celda para consultar o preparar una reserva o cambio.");
        }
        private void CalendarContext(string room, DateTime arrival, DateTime departure, string reservation, string guest)
        {
            if (!access.Allows("Reservas", "View")) { DenyAccess(); return; }
            OpenInfo(reservation ?? "Habitación " + room.Split(' ')[0], room + "\n" + arrival.ToString("dd/MM/yyyy") + " – " + departure.ToString("dd/MM/yyyy") + "\n" + (guest ?? "Sin estancia en los datos de muestra."));
            if (access.Allows("Reservas", "Write")) {
                var prepare = Action(reservation == null ? "Preparar reserva" : "Preparar cambio", () => {
                    SelectWorkflow(reservation == null ? null : "Modificar"); OpenForm();
                    Prefill("Reserva", reservation); Prefill(reservation == null ? "Llegada" : "Nueva llegada", arrival.ToString("dd/MM/yyyy"));
                    Prefill(reservation == null ? "Salida" : "Nueva salida", departure.ToString("dd/MM/yyyy"));
                    Prefill("Habitación preferida", room.Split(' ')[0]); Prefill("Modo de asignación", "Manual");
                    Prefill("Modalidad de reserva", "Individual");
                    Prefill("Canal de origen (manual)", "Mostrador");
                    Prefill("Tipo de habitación", room.Contains("Suite") ? "Suite" : room.Contains("Triple") ? "Triple" : "Doble");
                }, true);
                AutomationProperties.SetAutomationId(prepare, "CalendarPrepare"); DialogBody.Children.Add(prepare);
            }
            if (reservation != null && access.Allows("Reservas", "Cancel")) DialogBody.Children.Add(Action("Preparar cancelación / no-show", () => {
                SelectWorkflow("Cancelar"); OpenForm(); Prefill("Reserva", reservation);
            }));
        }
        private void Prefill(string label, string value)
        {
            if (value == null) return;
            var field = formFields.FirstOrDefault(f => f.Definition.Label == label);
            if (field == null) return;
            if (field.Input is TextBox) ((TextBox)field.Input).Text = value;
            else ((ComboBox)field.Input).SelectedItem = value;
        }
        private void AddCalendarCell(Grid grid, string value, int row, int column, bool header)
        {
            var label = Text(value, 11, header); label.Margin = new Thickness(8); Grid.SetColumn(label, column); Grid.SetRow(label, row); grid.Children.Add(label);
        }

        // Import preview is implemented in the separate reader; it never persists or uploads.
        private void BuildImportPreview()
        {
            AddNotice("Columnas esperadas", "huesped, llegada, salida, tipo, tarifa, canal. Fechas dd/MM/aaaa. Archivo CSV UTF-8 o Excel .xlsx, hasta 2 MB y 2.000 filas.");
            var preview = new StackPanel();
            ModuleContent.Children.Add(Action("Elegir archivo CSV / Excel", () => {
                if (!access.Allows("Reservas", "Write")) { DenyAccess(); return; }
                var picker = new Microsoft.Win32.OpenFileDialog { Filter = "Reservas CSV o Excel|*.csv;*.xlsx", Multiselect = false, CheckFileExists = true };
                if (picker.ShowDialog() != true) return;
                preview.Children.Clear();
                try {
                    var report = ReservationImportPreview.Read(picker.FileName);
                    preview.Children.Add(Text(System.IO.Path.GetFileName(picker.FileName), 17, true));
                    preview.Children.Add(Muted(report.ValidRows + " filas válidas · " + report.Errors.Count + " errores. Vista previa: no se importa ninguna reserva."));
                    foreach (string error in report.Errors.Take(30)) { var line = Text(error, 12); line.SetResourceReference(TextBlock.ForegroundProperty, "Ui.Danger"); line.Margin = new Thickness(0, 8, 0, 0); preview.Children.Add(line); }
                    foreach (string line in report.Examples) preview.Children.Add(Muted(line));
                    if (report.Errors.Count > 30) AddHelp(preview, "Se muestran los primeros 30 errores. Corrige el archivo y vuelve a seleccionarlo.");
                } catch (Exception error) when (error is System.IO.IOException || error is System.UnauthorizedAccessException || error is FormatException || error is System.Xml.XmlException || error is System.IO.InvalidDataException) { preview.Children.Add(Text("No se pudo revisar el archivo: " + error.Message)); }
            }, true));
            ModuleContent.Children.Add(preview);
        }
    }
}

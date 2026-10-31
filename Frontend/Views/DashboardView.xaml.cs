using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Crown__.Frontend.Views
{
    // Presentation only. Data shown is illustrative; no backend calls.
    public partial class DashboardView : Page
    {
        private const double FullHeight = 60;
        private const double CompactHeight = 34;

        private static readonly string[] Themes = { "Light", "Dark", "Brand" };
        private static readonly string[] ThemeLabels = { "Claro", "Oscuro", "Marca" };

        // Controla qué se muestra en la barra: expandido (logo + iconos) o compacto (solo nombres).
        public static readonly DependencyProperty IsBarExpandedProperty =
            DependencyProperty.Register(nameof(IsBarExpanded), typeof(bool), typeof(DashboardView),
                new PropertyMetadata(true));

        public bool IsBarExpanded
        {
            get => (bool)GetValue(IsBarExpandedProperty);
            set => SetValue(IsBarExpandedProperty, value);
        }

        private bool atTop = true;
        private bool mouseOverBar;
        private double lastVerticalOffset;
        private Button activeModuleButton;
        private int themeIndex;

        public DashboardView() : this("Light") { }

        // theme: tema heredado del login para que la apariencia se mantenga al navegar.
        public DashboardView(string theme)
        {
            InitializeComponent();
            themeIndex = Array.IndexOf(Themes, theme);
            if (themeIndex < 0) themeIndex = 0;
            ApplyTheme();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            PopulateArrivals();
            PopulateDepartures();
            PopulateAlerts();
            lastVerticalOffset = ContentScroll.VerticalOffset;
            UpdateExpanded();
        }

        // --- Tema cíclico: Claro -> Oscuro -> Marca ---
        private void OnThemeCycle(object sender, RoutedEventArgs e)
        {
            themeIndex = (themeIndex + 1) % Themes.Length;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            Tag = Themes[themeIndex];
            if (ThemeButton != null)
                ThemeButton.Content = ThemeLabels[themeIndex];
        }

        // --- Compresión / expansión de la barra ---
        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            atTop = e.VerticalOffset <= 0;
            lastVerticalOffset = e.VerticalOffset;
            UpdateExpanded();
        }

        private void OnBarEnter(object sender, MouseEventArgs e)
        {
            mouseOverBar = true;
            UpdateExpanded();
        }

        private void OnBarLeave(object sender, MouseEventArgs e)
        {
            mouseOverBar = false;
            UpdateExpanded();
        }

        private void UpdateExpanded()
        {
            bool expanded = atTop || mouseOverBar;
            if (expanded != IsBarExpanded)
            {
                IsBarExpanded = expanded;
                AnimateHeights(expanded ? FullHeight : CompactHeight);
            }
        }

        private void AnimateHeights(double target)
        {
            var anim = new DoubleAnimation(target, TimeSpan.FromSeconds(0.2))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            TopBar.BeginAnimation(FrameworkElement.HeightProperty, anim);
            TopSpacer.BeginAnimation(FrameworkElement.HeightProperty, anim);
        }

        // --- Volver al inicio de sesión (hereda el tema actual del dashboard) ---
        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService == null)
                return;
            // Reemplaza el historial para no acumular páginas login/dashboard.
            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
            NavigationService.Navigate(new LoginView(Themes[themeIndex]));
        }

        // --- Selección de módulo (placeholder, sin navegación a subpantallas) ---
        private void OnModuleClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || btn.Tag == null)
                return;
            string module = btn.Tag.ToString();
            ActiveViewLabel.Text = "Vista actual: " + ModuleLabel(module);
            MarkActiveModule(btn);
        }

        private void MarkActiveModule(Button button)
        {
            if (activeModuleButton != null && activeModuleButton != button)
                SetActiveMark(activeModuleButton, false);
            SetActiveMark(button, true);
            activeModuleButton = button;
        }

        private static void SetActiveMark(Button button, bool active)
        {
            var mark = button.Template?.FindName("ActiveMark", button) as Border;
            if (mark != null)
                mark.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
        }

        private static string ModuleLabel(string tag)
        {
            switch (tag)
            {
                case "Inicio": return "Inicio";
                case "Reservas": return "Reservas";
                case "Habitaciones": return "Habitaciones y housekeeping";
                case "CheckInOut": return "Check-in / check-out";
                case "Huespedes": return "Huéspedes y contactos";
                case "Convenios": return "Convenios y transporte";
                case "FNB": return "Alimentos y bebidas";
                case "GymSpa": return "Gimnasio y spa";
                case "Lavanderia": return "Lavandería";
                case "Finanzas": return "Financiero y facturación";
                case "Reportes": return "Reportes";
                case "Config": return "Configuración";
                default: return tag;
            }
        }

        // --- Datos ilustrativos (sin backend) ---

        private void PopulateArrivals()
        {
            var rows = new[]
            {
                new { Habitacion = "204", Huesped = "María Fernández", Hora = "13:30", Estado = "En camino",      Canal = "Booking"   },
                new { Habitacion = "118", Huesped = "Carlos Rojas",    Hora = "14:00", Estado = "Programada",     Canal = "WhatsApp"  },
                new { Habitacion = "307", Huesped = "Eco Tours S.A.",  Hora = "14:45", Estado = "Grupo · 6 hab.", Canal = "Agencia"   },
                new { Habitacion = "210", Huesped = "L. Gómez + acomp.", Hora = "15:20", Estado = "Recurrente",  Canal = "Teléfono"  },
                new { Habitacion = "415", Huesped = "D. Salazar",      Hora = "16:10", Estado = "VIP",           Canal = "Correo"    },
                new { Habitacion = "122", Huesped = "A. Mendoza",      Hora = "17:00", Estado = "Walk-in posible", Canal = "Mostrador" },
            };
            ArrivalsList.ItemsSource = BuildArrivalRows(rows);
        }

        private void PopulateDepartures()
        {
            var rows = new[]
            {
                new { Habitacion = "201", Huesped = "Familia Herrera", Hora = "11:00", Estado = "Consumos abiertos", Cargo = "$24.00"   },
                new { Habitacion = "305", Huesped = "R. Núñez",        Hora = "12:30", Estado = "Late check-out",    Cargo = "$0.00"    },
                new { Habitacion = "408", Huesped = "J. Vargas",       Hora = "11:45", Estado = "Listo",             Cargo = "$148.50"  },
            };
            DeparturesList.ItemsSource = BuildDepartureRows(rows);
        }

        private void PopulateAlerts()
        {
            var rows = new[]
            {
                new { Prioridad = "Alta",  Texto = "Suite 410 sin limpiar tras horario (5★)",     Origen = "Housekeeping" },
                new { Prioridad = "Media", Texto = "Convenio \"Andes Corp.\" vence en 6 días",    Origen = "Convenios"   },
                new { Prioridad = "Media", Texto = "Aforo de gimnasio al 90% (cap. 20)",          Origen = "Gym/Spa"     },
                new { Prioridad = "Baja",  Texto = "Minibar 305 con stock bajo de bebidas",       Origen = "F&B"         },
                new { Prioridad = "Baja",  Texto = "2 llaves no devueltas del turno anterior",    Origen = "Recepción"   },
            };
            AlertsList.ItemsSource = BuildAlertRows(rows);
        }

        private IEnumerable<UIElement> BuildArrivalRows(dynamic[] rows)
        {
            var list = new List<UIElement>();
            foreach (dynamic r in rows)
            {
                var grid = new Grid { Margin = new Thickness(8, 6, 8, 6) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });

                var hab   = MakeCell(r.Habitacion, true);  Grid.SetColumn(hab, 0);
                var name  = MakeStack(r.Huesped, r.Canal); Grid.SetColumn(name, 1);
                var hora  = MakeCell(r.Hora, false);       Grid.SetColumn(hora, 2); hora.Margin = new Thickness(12, 0, 12, 0);
                var chip  = MakeChip(r.Estado);            Grid.SetColumn(chip, 3);
                var btn   = MakeActionButton("Check-in");  Grid.SetColumn(btn, 4);
                btn.Click += (s, ev) => { ActiveViewLabel.Text = "Vista actual: Check-in / check-out"; };

                grid.Children.Add(hab); grid.Children.Add(name); grid.Children.Add(hora);
                grid.Children.Add(chip); grid.Children.Add(btn);
                list.Add(grid);
                list.Add(MakeDivider(8));
            }
            return list;
        }

        private IEnumerable<UIElement> BuildDepartureRows(dynamic[] rows)
        {
            var list = new List<UIElement>();
            foreach (dynamic r in rows)
            {
                var grid = new Grid { Margin = new Thickness(8, 6, 8, 6) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });

                var hab   = MakeCell(r.Habitacion, true);  Grid.SetColumn(hab, 0);
                var name  = MakeStack(r.Huesped, r.Estado); Grid.SetColumn(name, 1);
                var hora  = MakeCell(r.Hora, false);       Grid.SetColumn(hora, 2); hora.Margin = new Thickness(12, 0, 12, 0);
                var cargo = MakeCell(r.Cargo, false);      Grid.SetColumn(cargo, 3);
                var btn   = MakeActionButton("Check-out"); Grid.SetColumn(btn, 4);
                btn.Click += (s, ev) => { ActiveViewLabel.Text = "Vista actual: Check-in / check-out"; };

                grid.Children.Add(hab); grid.Children.Add(name); grid.Children.Add(hora);
                grid.Children.Add(cargo); grid.Children.Add(btn);
                list.Add(grid);
                list.Add(MakeDivider(8));
            }
            return list;
        }

        private IEnumerable<UIElement> BuildAlertRows(dynamic[] rows)
        {
            var list = new List<UIElement>();
            foreach (dynamic r in rows)
            {
                var grid = new Grid { Margin = new Thickness(10, 8, 10, 8) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var dot = new Ellipse { Width = 9, Height = 9, Fill = PriorityBrush(r.Prioridad),
                                        VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 5, 0, 0) };
                Grid.SetColumn(dot, 0);

                var stack = new StackPanel();
                stack.Children.Add(new TextBlock { Text = r.Texto, FontSize = 12.5, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.Medium });
                stack.Children.Add(new TextBlock { Text = r.Origen, FontSize = 10, Margin = new Thickness(0, 2, 0, 0), Opacity = 0.65 });
                Grid.SetColumn(stack, 1);

                var chip = MakeChip(r.Prioridad);
                chip.VerticalAlignment = VerticalAlignment.Top;
                Grid.SetColumn(chip, 2);

                grid.Children.Add(dot); grid.Children.Add(stack); grid.Children.Add(chip);
                list.Add(grid);
                list.Add(MakeDivider(10));
            }
            return list;
        }

        private static Border MakeDivider(double hMargin)
        {
            return new Border
            {
                Height = 1,
                Background = (Brush)Application.Current.FindResource("Light.Border"),
                Margin = new Thickness(hMargin, 0, hMargin, 0),
                Opacity = 0.5
            };
        }

        private static TextBlock MakeCell(string text, bool bold)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static StackPanel MakeStack(string title, string subtitle)
        {
            var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(new TextBlock { Text = title,    FontSize = 13,   FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            sp.Children.Add(new TextBlock { Text = subtitle, FontSize = 10.5, Margin = new Thickness(0, 2, 0, 0), Opacity = 0.65, TextTrimming = TextTrimming.CharacterEllipsis });
            return sp;
        }

        private static Border MakeChip(string text)
        {
            bool high = text == "Alta";
            return new Border
            {
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(8, 2, 8, 3),
                Background = high
                    ? new SolidColorBrush(Color.FromArgb(0x28, 0xD9, 0x53, 0x4F))
                    : new SolidColorBrush(Color.FromArgb(0x20, 0x17, 0x38, 0x4F)),
                Child = new TextBlock { Text = text, FontSize = 10.5, FontWeight = FontWeights.Medium }
            };
        }

        private static Button MakeActionButton(string text)
        {
            var btn = new Button
            {
                Content = text, Height = 30, FontSize = 11.5,
                FontWeight = FontWeights.SemiBold, Cursor = Cursors.Hand,
                Margin = new Thickness(4, 0, 0, 0)
            };
            btn.SetResourceReference(FrameworkElement.StyleProperty, "HotelLinkButton");
            btn.SetResourceReference(Control.BorderBrushProperty, "Light.Border");
            return btn;
        }

        private static Brush PriorityBrush(string priority)
        {
            switch (priority)
            {
                case "Alta":  return new SolidColorBrush(Color.FromRgb(0xD9, 0x53, 0x4F));
                case "Media": return new SolidColorBrush(Color.FromRgb(0xD9, 0xBB, 0x79));
                default:      return new SolidColorBrush(Color.FromRgb(0x8A, 0xB0, 0x9C));
            }
        }
    }
}

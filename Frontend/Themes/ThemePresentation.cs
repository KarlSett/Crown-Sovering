using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Crown__.Frontend.Themes
{
    /// <summary>Shared, in-memory appearance. Never stores hotel or account data.</summary>
    public static class ThemePresentation
    {
        public static string Current { get; private set; } = "Light";

        public static readonly DependencyProperty CycleProperty = DependencyProperty.RegisterAttached(
            "Cycle", typeof(bool), typeof(ThemePresentation), new PropertyMetadata(false, OnCycleChanged));

        public static void SetCycle(DependencyObject target, bool value) { target.SetValue(CycleProperty, value); }
        public static bool GetCycle(DependencyObject target) { return (bool)target.GetValue(CycleProperty); }

        private static void OnCycleChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            var button = target as ButtonBase;
            if (button == null) return;
            if ((bool)e.NewValue) button.Click += NextTheme;
            else button.Click -= NextTheme;
        }

        public static readonly DependencyProperty SmoothHoverProperty = DependencyProperty.RegisterAttached(
            "SmoothHover", typeof(bool), typeof(ThemePresentation), new PropertyMetadata(false, OnSmoothHoverChanged));
        public static void SetSmoothHover(DependencyObject target, bool value) { target.SetValue(SmoothHoverProperty, value); }
        public static bool GetSmoothHover(DependencyObject target) { return (bool)target.GetValue(SmoothHoverProperty); }
        private static void OnSmoothHoverChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            var button = target as ButtonBase;
            if (button == null) return;
            if ((bool)e.NewValue) { button.MouseEnter += OnHover; button.MouseLeave += OnHover; }
            else { button.MouseEnter -= OnHover; button.MouseLeave -= OnHover; }
        }
        private static void OnHover(object sender, MouseEventArgs e)
        {
            if (!SystemParameters.ClientAreaAnimation) return;
            var button = (ButtonBase)sender;
            var overlay = button.Template.FindName("Hover", button) as Border;
            if (overlay == null || !button.IsEnabled) return;
            overlay.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(
                button.IsMouseOver ? 0 : 0.07, button.IsMouseOver ? 0.07 : 0, TimeSpan.FromMilliseconds(120))
                { FillBehavior = FillBehavior.Stop });
        }

        private static void NextTheme(object sender, RoutedEventArgs e)
        {
            Apply(Current == "Light" ? "Dark" : Current == "Dark" ? "Brand" : "Light");
        }

        public static void Apply(string theme)
        {
            if (theme != "Light" && theme != "Dark" && theme != "Brand")
                throw new ArgumentException("Unknown appearance.", "theme");
            Current = theme;
            string[] keys = { "Page", "Surface", "Inset", "Text", "Muted", "Border", "Hover", "Accent", "OnAccent", "Selection", "Success", "SuccessSurface", "Warning", "WarningSurface", "Danger", "DangerSurface", "Sidebar", "Focus" };
            string[] values = theme == "Dark"
                ? new[] { "#10151D", "#1B2330", "#151D28", "#F1F4F8", "#AFBDCD", "#364355", "#253143", "#DCC18B", "#172431", "#303D50", "#95DDC3", "#1B3C35", "#F2CE92", "#443A29", "#FFB3BF", "#472D38", "#151C26", "#DCC18B" }
                : theme == "Brand"
                // Brand is the light Forest Green appearance; regular Light keeps its original palette.
                ? new[] { "#EFFEF3", "#FFFFFF", "#EFFEF3", "#013213", "#0F6C2F", "#B6FCCC", "#D9FFE5", "#0F6C2F", "#EFFEF3", "#D9FFE5", "#0F6C2F", "#D9FFE5", "#805719", "#FBF1DD", "#A43D52", "#FBEAF0", "#EFFEF3", "#0C9239" }
                : new[] { "#EDF1F6", "#FFFFFF", "#F3F6FA", "#172E44", "#596D83", "#DCE3EC", "#E7EDF4", "#183C55", "#FFFFFF", "#E2ECF5", "#23664D", "#E7F4EC", "#805719", "#FBF1DD", "#A43D52", "#FBEAF0", "#FBFCFE", "#345F83" };
            for (int i = 0; i < keys.Length; i++)
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(values[i]));
                brush.Freeze();
                Application.Current.Resources["UI." + keys[i]] = brush;
            }
            string[] railKeys = { "SidebarText", "SidebarMuted", "SidebarActive", "OnSidebarActive", "SidebarHover", "SidebarBorder", "BrandMark", "BoardToday", "BoardTodayText" };
            string[] railValues = theme == "Light"
                ? new[] { "#334B63", "#5B6F84", "#183C55", "#FFFFFF", "#EAF0F6", "#DCE3EC", "#8B692F", "#E6EEF7", "#183C55" }
                : theme == "Dark"
                ? new[] { "#C2D0DE", "#97AABD", "#DCC18B", "#172431", "#222E3E", "#2E3C4E", "#E1C590", "#303A46", "#F2D89D" }
                : new[] { "#0E5929", "#0F6C2F", "#D9FFE5", "#013213", "#D9FFE5", "#B6FCCC", "#0F6C2F", "#D9FFE5", "#0F6C2F" };
            for (int i = 0; i < railKeys.Length; i++)
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(railValues[i]));
                brush.Freeze();
                Application.Current.Resources["UI." + railKeys[i]] = brush;
            }
            Application.Current.Resources["UI.RowAlternate"] = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(theme == "Brand" ? "#EFFEF3" : theme == "Dark" ? "#10FFFFFF" : "#0817384F"));
            Application.Current.Resources["UI.Overlay"] = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(theme == "Brand" ? "#C8013213" : "#C8112230"));
            Application.Current.Resources["UI.TextSelection"] = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(theme == "Brand" ? "#B6FCCC" : "#D9BB79"));
            Application.Current.Resources["UI.Mode"] = theme;
            Application.Current.Resources["UI.ThemeLabel"] = theme == "Dark" ? "Oscuro" : theme == "Brand" ? "De marca" : "Claro";
        }
    }
}

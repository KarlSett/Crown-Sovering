using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Crown__.Frontend.Controls
{
    /// <summary>Captura breve de datos para operaciones de demostración.</summary>
    public static class DialogoRapido
    {
        public static string[] Mostrar(Window propietario, string titulo, params string[] campos)
        {
            var ventana = new Window { Title = titulo, Width = 390, SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize,
                Owner = propietario };
            var contenido = new StackPanel { Margin = new Thickness(18) };
            var cajas = new List<TextBox>();
            foreach (string campo in campos)
            {
                contenido.Children.Add(new TextBlock { Text = campo, Margin = new Thickness(0, 5, 0, 2) });
                var caja = new TextBox { MinWidth = 300, Padding = new Thickness(7) };
                cajas.Add(caja); contenido.Children.Add(caja);
            }
            var botones = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var cancelar = new Button { Content = "Cancelar", MinWidth = 84, Margin = new Thickness(0, 0, 8, 0), IsCancel = true };
            var guardar = new Button { Content = "Guardar", MinWidth = 84, IsDefault = true };
            guardar.Click += (s, e) => ventana.DialogResult = true;
            botones.Children.Add(cancelar); botones.Children.Add(guardar); contenido.Children.Add(botones);
            ventana.Content = contenido;
            if (ventana.ShowDialog() != true) return null;
            var resultado = new List<string>();
            foreach (var caja in cajas) resultado.Add(caja.Text.Trim());
            return resultado.ToArray();
        }
    }
}

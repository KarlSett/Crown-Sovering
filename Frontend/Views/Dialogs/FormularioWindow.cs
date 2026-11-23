using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Crown__.Frontend.Views.Dialogs
{
    public enum TipoCampo { Texto, Lista, Fecha, Memo, Clave }

    public class Campo
    {
        public string Etiqueta { get; set; }
        public TipoCampo Tipo { get; set; }
        public IEnumerable<object> Opciones { get; set; }
        public object Valor { get; set; }

        public static Campo Texto(string etiqueta, string valor = "") { return new Campo { Etiqueta = etiqueta, Tipo = TipoCampo.Texto, Valor = valor }; }
        public static Campo Clave(string etiqueta) { return new Campo { Etiqueta = etiqueta, Tipo = TipoCampo.Clave }; }
        public static Campo Memo(string etiqueta) { return new Campo { Etiqueta = etiqueta, Tipo = TipoCampo.Memo }; }
        public static Campo Lista(string etiqueta, IEnumerable<object> opciones) { return new Campo { Etiqueta = etiqueta, Tipo = TipoCampo.Lista, Opciones = opciones }; }
        public static Campo Fecha(string etiqueta, DateTime valor) { return new Campo { Etiqueta = etiqueta, Tipo = TipoCampo.Fecha, Valor = valor }; }
    }

    /// <summary>Formulario emergente genérico: se arma con una lista de campos y devuelve sus valores.</summary>
    public static class FormularioWindow
    {
        /// <summary>Devuelve un valor por campo (texto, elemento elegido o fecha), o null si se cancela.</summary>
        public static object[] Pedir(Window propietario, string titulo, params Campo[] campos)
        {
            var ventana = new Window
            {
                Title = titulo, Width = 420, SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = propietario
            };
            var panel = new StackPanel { Margin = new Thickness(16) };
            var controles = new List<Control>();

            foreach (var campo in campos)
            {
                panel.Children.Add(new TextBlock { Text = campo.Etiqueta, Margin = new Thickness(0, 6, 0, 0) });
                Control control;
                switch (campo.Tipo)
                {
                    case TipoCampo.Lista:
                        var combo = new ComboBox { ItemsSource = campo.Opciones };
                        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
                        control = combo;
                        break;
                    case TipoCampo.Fecha:
                        control = new DatePicker { SelectedDate = campo.Valor as DateTime? };
                        break;
                    case TipoCampo.Clave:
                        control = new PasswordBox();
                        break;
                    case TipoCampo.Memo:
                        control = new TextBox { Height = 60, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
                        break;
                    default:
                        control = new TextBox { Text = Convert.ToString(campo.Valor) };
                        break;
                }
                controles.Add(control);
                panel.Children.Add(control);
            }

            var guardar = new Button { Content = "Guardar", IsDefault = true, Margin = new Thickness(0, 12, 0, 0), HorizontalAlignment = HorizontalAlignment.Right };
            guardar.Click += (s, e) => ventana.DialogResult = true;
            panel.Children.Add(guardar);
            ventana.Content = panel;

            if (ventana.ShowDialog() != true) return null;

            var valores = new object[campos.Length];
            for (int i = 0; i < campos.Length; i++)
            {
                var combo = controles[i] as ComboBox;
                var fecha = controles[i] as DatePicker;
                var clave = controles[i] as PasswordBox;
                var texto = controles[i] as TextBox;
                valores[i] = combo != null ? combo.SelectedItem : fecha != null ? (object)fecha.SelectedDate : clave != null ? clave.Password : texto.Text;
            }
            return valores;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Crown__.Frontend.Views.Sections;

namespace Crown__.Frontend.Views.Dialogs
{
    /// <summary>Presentation-only dialog routing. No service calls, save, print or destructive operations.</summary>
    public static class DialogLauncher
    {
        private static readonly Dictionary<string, string[]> Forms = new Dictionary<string, string[]>
        {
            { "new-product", new[] { "Nuevo producto", "Nombre", "Categoría", "Precio", "Existencias" } },
            { "consumption", new[] { "Registrar consumo", "Habitación", "Producto", "Cantidad", "Observaciones" } },
            { "charge", new[] { "Cobrar consumo", "Habitación", "Importe", "Medio de pago" } },
            { "new-agreement", new[] { "Nuevo convenio", "Empresa", "Documento fiscal", "Contacto", "Correo", "Observaciones" } },
            { "edit-agreement", new[] { "Editar convenio", "Empresa", "Contacto", "Correo", "Observaciones" } },
            { "agreement-reservations", new[] { "Reservas del convenio", "Empresa", "Desde", "Hasta" } },
            { "check-in", new[] { "Check-in", "Reserva", "Huésped", "Habitación", "Observaciones" } },
            { "check-out", new[] { "Check-out", "Reserva", "Habitación", "Observaciones" } },
            { "issue-key", new[] { "Emitir llave", "Reserva", "Habitación", "Código de llave" } },
            { "return-key", new[] { "Devolver llave", "Reserva", "Código de llave", "Observaciones" } },
            { "move-room", new[] { "Mover de habitación", "Reserva", "Habitación actual", "Habitación destino", "Motivo" } },
            { "print-stay", new[] { "Vista previa de estancia", "Reserva", "Huésped", "Habitación" } },
            { "new-folio", new[] { "Nuevo folio", "Reserva", "Huésped", "Tipo de folio" } },
            { "transfer-charge", new[] { "Transferir cargo", "Folio origen", "Folio destino", "Importe", "Motivo" } },
            { "invoice", new[] { "Emitir factura", "Folio", "Razón social", "Documento fiscal", "Correo" } },
            { "credit-note", new[] { "Nota de crédito", "Factura", "Importe", "Motivo" } },
            { "block-room", new[] { "Bloquear habitación", "Habitación", "Desde", "Hasta", "Motivo" } },
            { "new-floor", new[] { "Agregar piso", "Nombre del piso", "Descripción" } },
            { "room-state", new[] { "Cambiar estado", "Habitación", "Estado", "Observaciones" } },
            { "edit-guest", new[] { "Editar huésped", "Nombre", "Apellidos", "Documento", "Correo", "Teléfono" } },
            { "delete-guest", new[] { "Eliminar huésped", "Huésped", "Motivo" } },
            { "new-task", new[] { "Nueva tarea", "Habitación", "Responsable", "Fecha", "Descripción" } },
            { "incident", new[] { "Registrar incidencia", "Habitación", "Prioridad", "Descripción" } },
            { "work-order", new[] { "Nueva orden de mantenimiento", "Habitación", "Responsable", "Descripción" } },
            { "open-shift", new[] { "Abrir turno", "Responsable", "Fondo inicial", "Observaciones" } },
            { "close-shift", new[] { "Cerrar turno", "Responsable", "Efectivo contado", "Observaciones" } },
            { "payment", new[] { "Registrar pago", "Folio", "Importe", "Medio de pago" } },
            { "new-rate", new[] { "Nueva tarifa", "Tipo de habitación", "Precio", "Desde", "Hasta" } },
            { "new-season", new[] { "Nueva temporada", "Nombre", "Desde", "Hasta" } },
            { "new-promotion", new[] { "Nueva promoción", "Nombre", "Descuento", "Desde", "Hasta" } },
            { "notification", new[] { "Enviar notificación", "Destinatario", "Asunto", "Mensaje" } },
            { "assign-role", new[] { "Asignar rol", "Usuario", "Rol" } },
            { "availability", new[] { "Buscar disponibilidad" } },
        };
        public static readonly DependencyProperty ActionProperty = DependencyProperty.RegisterAttached(
            "Action", typeof(string), typeof(DialogLauncher), new PropertyMetadata(null, ActionChanged));
        public static string GetAction(DependencyObject target) { return (string)target.GetValue(ActionProperty); }
        public static void SetAction(DependencyObject target, string value) { target.SetValue(ActionProperty, value); }
        private static void ActionChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            var button = target as Button;
            if (button == null) return;
            button.Click -= Open;
            if (e.NewValue != null) button.Click += Open;
        }
        private static void Open(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var window = Create(GetAction(button));
            var owner = Window.GetWindow(button);
            if (owner != null) window.Owner = owner;
            window.ShowDialog();
            e.Handled = true;
        }
        public static Window Create(string action)
        {
            if (action == "new-reservation") return new NuevaReservaWindow();
            string[] fields;
            if (!Forms.TryGetValue(action, out fields)) throw new ArgumentException("Unknown dialog", "action");
            var window = new Window { Title = fields[0], Width = action == "availability" ? 1080 : 520,
                Height = action == "availability" ? 680 : 570, MinWidth = action == "availability" ? 760 : 360,
                MinHeight = 360, WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false, ResizeMode = ResizeMode.CanResize, UseLayoutRounding = true };
            var root = new Grid { Margin = new Thickness(20) };
            root.SetResourceReference(Panel.BackgroundProperty, "UI.Page");
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition());
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var title = new TextBlock { Text = fields[0], FontSize = 22, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,0,0,20), TextWrapping = TextWrapping.Wrap };
            title.SetResourceReference(TextBlock.ForegroundProperty, "UI.Text"); root.Children.Add(title);
            FrameworkElement body;
            if (action == "availability") body = new HabitacionesView();
            else
            {
                var form = new StackPanel();
                for (int i = 1; i < fields.Length; i++)
                {
                    var label = new TextBlock { Text = fields[i], Margin = new Thickness(0,0,0,6) };
                    label.SetResourceReference(TextBlock.ForegroundProperty, "UI.Text"); form.Children.Add(label);
                    Control input;
                    if (fields[i] == "Desde" || fields[i] == "Hasta" || fields[i] == "Fecha") input = new DatePicker();
                    else if (fields[i] == "Observaciones" || fields[i] == "Descripción" || fields[i] == "Mensaje" || fields[i] == "Motivo")
                        input = new TextBox { Height = 82, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                    else input = new TextBox();
                    input.Margin = new Thickness(0,0,0,16);
                    System.Windows.Automation.AutomationProperties.SetName(input, fields[i]);
                    form.Children.Add(input);
                }
                body = new ScrollViewer { Content = form, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            }
            Grid.SetRow(body,1); root.Children.Add(body);
            var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0,16,0,0) };
            var close = new Button { Content = "Cerrar", IsCancel = true, MinWidth = 100 };
            close.Click += delegate { window.Close(); };
            footer.Children.Add(close); Grid.SetRow(footer,2); root.Children.Add(footer);
            window.Content = root;
            return window;
        }
    }
}

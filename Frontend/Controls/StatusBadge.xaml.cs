using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Crown__.Frontend.Controls
{
    /// <summary>Insignia de estado con color, etiqueta y cantidad.</summary>
    public partial class StatusBadge : UserControl
    {
        public static readonly DependencyProperty EtiquetaProperty =
            DependencyProperty.Register("Etiqueta", typeof(string), typeof(StatusBadge),
                                        new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty CantidadProperty =
            DependencyProperty.Register("Cantidad", typeof(int), typeof(StatusBadge),
                                        new PropertyMetadata(0));

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Brush), typeof(StatusBadge),
                                        new PropertyMetadata(Brushes.Transparent));

        public StatusBadge()
        {
            InitializeComponent();
        }

        public string Etiqueta
        {
            get { return (string)GetValue(EtiquetaProperty); }
            set { SetValue(EtiquetaProperty, value); }
        }

        public int Cantidad
        {
            get { return (int)GetValue(CantidadProperty); }
            set { SetValue(CantidadProperty, value); }
        }

        public Brush Color
        {
            get { return (Brush)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }
    }
}

using System.Windows.Controls;
using System.Globalization;
using System.Windows.Media;

namespace Crown__.Frontend.Controls
{
    /// <summary>Tarjeta compacta de indicador usada en el panel operativo.</summary>
    public partial class KpiCard : UserControl
    {
        public KpiCard()
        {
            InitializeComponent();
        }

        public string Titulo
        {
            get { return TxtTitulo.Text; }
            set
            {
                string label = (value ?? string.Empty).ToLower(CultureInfo.GetCultureInfo("es-BO"));
                TxtTitulo.Text = label.Length == 0 ? label : char.ToUpper(label[0]) + label.Substring(1);
                string path = "M2,16 V9 H6 V16 M9,16 V4 H13 V16 M16,16 V1 H20 V16";
                if (label.Contains("llegada")) path = "M2,2 H10 V18 H2 M7,10 H19 M15,6 L19,10 15,14";
                else if (label.Contains("salida")) path = "M18,2 H10 V18 H18 M13,10 H1 M5,6 L1,10 5,14";
                else if (label.Contains("disponible")) path = "M2,10 L8,16 19,3";
                else if (label.Contains("ocupación") || label.Contains("casa")) path = "M2,9 L10,2 18,9 V18 H2 Z M8,18 V12 H12 V18";
                else if (label.Contains("ingreso") || label.Contains("saldo")) path = "M2,4 H18 V17 H2 Z M2,8 H18 M5,13 H9";
                else if (label.Contains("reserva")) path = "M3,4 H17 V18 H3 Z M3,8 H17 M7,1 V6 M13,1 V6 M7,12 L10,15 14,10";
                MetricGlyph.Data = Geometry.Parse(path);
            }
        }

        public string Valor
        {
            get { return TxtValor.Text; }
            set { TxtValor.Text = value; }
        }

        public string Pista
        {
            get { return TxtPista.Text; }
            set { TxtPista.Text = value; }
        }

        public double TamanoValor
        {
            get { return TxtValor.FontSize; }
            set { TxtValor.FontSize = value; }
        }
    }
}

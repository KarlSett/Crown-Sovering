using System.Windows.Controls;

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
            set { TxtTitulo.Text = value; }
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

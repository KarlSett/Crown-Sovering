using System.Windows;
using System.Windows.Controls;

namespace Crown__.Frontend.Controls
{
    /// <summary>
    /// Tarjeta con título y nota opcional. Se comporta como ContentControl para que
    /// el contenido declarado en la vista conserve su namescope (x:Name).
    /// La plantilla vive en Frontend/Styles/DashboardStyles.xaml.
    /// </summary>
    public class SectionCard : ContentControl
    {
        public static readonly DependencyProperty TituloProperty =
            DependencyProperty.Register("Titulo", typeof(string), typeof(SectionCard),
                                        new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty NotaProperty =
            DependencyProperty.Register("Nota", typeof(string), typeof(SectionCard),
                                        new PropertyMetadata(string.Empty));

        public SectionCard()
        {
            // La plantilla vive en Frontend/Styles/DashboardStyles.xaml y se aplica
            // como referencia de recurso para no depender del orden de carga.
            SetResourceReference(StyleProperty, "SectionCardStyle");
        }

        public string Titulo
        {
            get { return (string)GetValue(TituloProperty); }
            set { SetValue(TituloProperty, value); }
        }

        public string Nota
        {
            get { return (string)GetValue(NotaProperty); }
            set { SetValue(NotaProperty, value); }
        }
    }
}

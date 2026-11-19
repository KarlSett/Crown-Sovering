using System;
using System.Globalization;

namespace Crown__.Backend.Models
{
    /// <summary>Formatos de presentación compartidos por las vistas.</summary>
    public static class Formato
    {
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-EC");

        public static string Moneda(decimal valor)
        {
            return "$" + valor.ToString("N2", Cultura);
        }

        public static string MonedaCorto(decimal valor)
        {
            if (Math.Abs(valor) >= 1000)
                return "$" + (valor / 1000m).ToString("0.0", Cultura) + "k";
            return "$" + valor.ToString("N0", Cultura);
        }

        public static string Porcentaje(double valor)
        {
            return (valor * 100).ToString("0", Cultura) + "%";
        }

        public static string Fecha(DateTime fecha)
        {
            return fecha.ToString("dd MMM yyyy", Cultura);
        }

        public static string FechaHora(DateTime fecha)
        {
            return fecha.ToString("dd MMM · HH:mm", Cultura);
        }

        public static string Hora(DateTime fecha)
        {
            return fecha.ToString("HH:mm", Cultura);
        }

        public static string DiaDeSemana(DateTime fecha)
        {
            var texto = fecha.ToString("dddd, d 'de' MMMM 'de' yyyy", Cultura);
            return char.ToUpper(texto[0], Cultura) + texto.Substring(1);
        }
    }
}

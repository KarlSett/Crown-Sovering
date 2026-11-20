using System;

namespace Crown__.Backend.Models
{
    public class ReservaRegistro
    {
        public int Id { get; set; }
        public string Codigo { get { return CodigoDe(Id); } }
        public string Huesped { get; set; }
        public string Canal { get; set; }
        public string Habitacion { get; set; }
        public string Tipo { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public string Estado { get; set; }
        public decimal Tarifa { get; set; }
        public int Personas { get; set; }

        public override string ToString()
        {
            return Codigo + " · " + Huesped + (Habitacion == null ? "" : " · Hab. " + Habitacion);
        }

        public static string CodigoDe(int id)
        {
            return "RES-" + id.ToString("0000");
        }

        // "RES-0007" da 7; devuelve 0 si el texto no tiene ese formato.
        public static int IdDe(string codigo)
        {
            int id;
            if (codigo == null || !codigo.StartsWith("RES-")) return 0;
            return int.TryParse(codigo.Substring(4), out id) ? id : 0;
        }
    }
}

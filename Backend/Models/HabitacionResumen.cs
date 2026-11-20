using System;

namespace Crown__.Backend.Models
{
    public class HabitacionResumen
    {
        public string Numero { get; set; }
        public string Tipo { get; set; }
        public string Piso { get; set; }
        public string Estado { get; set; }
        public string Descripcion { get; set; }
        public int Capacidad { get; set; }
        public string Huesped { get; set; }
        public string Reserva { get; set; }
        public DateTime? Entrada { get; set; }
        public DateTime? Salida { get; set; }
    }
}

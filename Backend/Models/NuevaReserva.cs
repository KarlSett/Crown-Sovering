using System;

namespace Crown__.Backend.Models
{
    public class NuevaReserva
    {
        public string Modalidad { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string TipoDocumento { get; set; }
        public string NumeroDocumento { get; set; }
        public DateTime Llegada { get; set; }
        public DateTime Salida { get; set; }
        public string TipoHabitacion { get; set; }
        public int Huespedes { get; set; }
        public decimal Tarifa { get; set; }
        public string Canal { get; set; }
        public string HabitacionPreferida { get; set; }
        public string Observaciones { get; set; }
    }
}

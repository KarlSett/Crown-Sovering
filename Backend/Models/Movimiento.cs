using System;

namespace Crown__.Backend.Models
{
    public class Movimiento
    {
        public int Id { get; set; }
        public string Concepto { get; set; }
        public string Reserva { get; set; }
        public string Habitacion { get; set; }
        public string Huesped { get; set; }
        public decimal Cargo { get; set; }
        public decimal Abono { get; set; }
        public decimal SaldoFolio { get; set; }
        public DateTime Fecha { get; set; }
    }
}

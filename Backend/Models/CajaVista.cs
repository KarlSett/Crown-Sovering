using System;

namespace Crown__.Backend.Models
{
    public class TurnoVista
    {
        public int Id { get; set; }
        public DateTime Apertura { get; set; }
        public DateTime? Cierre { get; set; }
        public string Estado { get; set; }
        public decimal Esperado { get; set; }
    }

    public class PagoVista
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Reserva { get; set; }
        public string Huesped { get; set; }
        public string Metodo { get; set; }
        public decimal Monto { get; set; }

        public override string ToString()
        {
            return Fecha.ToString("HH:mm") + " · " + Reserva + " · " + Huesped + " · " + Metodo + " · " + Formato.Moneda(Monto);
        }
    }

    public class MovimientoCajaVista
    {
        public string Tipo { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string Pago { get; set; }
        public string MontoTexto { get { return Formato.Moneda(Monto); } }
        public string FechaTexto { get { return Fecha.ToString("dd/MM HH:mm"); } }
    }

    public class MetodoPagoVista
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public bool Activo { get; set; }
        public string ActivoTexto { get { return Activo ? "Sí" : "No"; } }
        public override string ToString() { return Nombre; }
    }

    public class DevolucionVista
    {
        public string Pago { get; set; }
        public string Tipo { get; set; }
        public decimal Monto { get; set; }
        public string Motivo { get; set; }
        public string Estado { get; set; }
        public string MontoTexto { get { return Formato.Moneda(Monto); } }
    }
}

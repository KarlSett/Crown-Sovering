using System;

namespace Crown__.Backend.Models
{
    public class ConsumoLinea
    {
        public string Producto { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Total { get { return Cantidad * Precio; } }
        public string PrecioTexto { get { return Formato.Moneda(Precio); } }
        public string TotalTexto { get { return Formato.Moneda(Total); } }
    }

    public class ServicioVista
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public decimal Precio { get; set; }
        public bool Activo { get; set; }
        public string PrecioTexto { get { return Formato.Moneda(Precio); } }
        public string ActivoTexto { get { return Activo ? "Sí" : "No"; } }
    }

    public class CitaVista
    {
        public string Reserva { get; set; }
        public string Huesped { get; set; }
        public DateTime FechaHora { get; set; }
        public int Duracion { get; set; }
        public string Estado { get; set; }
        public string FechaTexto { get { return FechaHora.ToString("dd/MM HH:mm"); } }
    }

    public class MovimientoInventarioVista
    {
        public string Producto { get; set; }
        public string Tipo { get; set; }
        public int Cantidad { get; set; }
        public string Motivo { get; set; }
        public DateTime Fecha { get; set; }
        public string FechaTexto { get { return Fecha.ToString("dd/MM HH:mm"); } }
    }
}

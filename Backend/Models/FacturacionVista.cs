using System;

namespace Crown__.Backend.Models
{
    public class FolioItem
    {
        public int FolioId { get; set; }
        public string Nombre { get; set; }
        public string Reserva { get; set; }
        public string Huesped { get; set; }
        public string Habitacion { get; set; }
        public string Estado { get; set; }
        public decimal Saldo { get; set; }

        public override string ToString()
        {
            return Reserva + (string.IsNullOrEmpty(Nombre) || Nombre == "Folio principal" ? "" : " [" + Nombre + "]") + " · " + Huesped + (Habitacion == null ? "" : " · Hab. " + Habitacion) + " · " + Formato.Moneda(Saldo);
        }
    }

    public class CargoVista
    {
        public int Id { get; set; }
        public string FechaTexto { get; set; }
        public string Concepto { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Importe { get { return Cantidad * Precio; } }
        public string PrecioTexto { get { return Formato.Moneda(Precio); } }
        public string ImporteTexto { get { return Formato.Moneda(Importe); } }
    }

    public class ConceptoVista
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public override string ToString() { return Nombre; }
    }

    public class FacturaVista
    {
        public int Id { get; set; }
        public string Numero { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public string FechaTexto { get { return Fecha.ToString("dd/MM/yyyy HH:mm"); } }
        public string SubtotalTexto { get { return Formato.Moneda(Subtotal); } }
        public string ImpuestoTexto { get { return Formato.Moneda(Impuesto); } }
        public string TotalTexto { get { return Formato.Moneda(Total); } }
        public override string ToString() { return Numero + " · " + Formato.Moneda(Total); }
    }

    public class NotaCreditoVista
    {
        public string Numero { get; set; }
        public string Factura { get; set; }
        public string Motivo { get; set; }
        public decimal Monto { get; set; }
        public string MontoTexto { get { return Formato.Moneda(Monto); } }
    }
}

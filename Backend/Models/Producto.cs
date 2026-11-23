namespace Crown__.Backend.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public bool Activo { get; set; }

        public override string ToString()
        {
            return Nombre + " · " + Formato.Moneda(Precio) + " · stock " + Stock;
        }
    }
}

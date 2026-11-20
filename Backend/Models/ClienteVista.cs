using System;

namespace Crown__.Backend.Models
{
    public class ClienteVista
    {
        public int Id { get; set; }
        // Nombre completo. Si Apellidos viene informado, Nombre son solo los nombres.
        public string Nombre { get; set; }
        public string Apellidos { get; set; }
        public string Documento { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string[] Estancias { get; set; }

        public string NombreCompleto
        {
            get { return string.IsNullOrWhiteSpace(Apellidos) ? Nombre : Nombre + " " + Apellidos; }
        }

        public override string ToString()
        {
            return NombreCompleto + (string.IsNullOrWhiteSpace(Documento) ? "" : " · " + Documento);
        }
    }

    public class ConsentimientoVista
    {
        public string Tipo { get; set; }
        public bool Autorizado { get; set; }
        public DateTime Fecha { get; set; }
        public string AutorizadoTexto { get { return Autorizado ? "Sí" : "No"; } }
        public string FechaTexto { get { return Fecha.ToString("dd/MM/yyyy"); } }
    }
}

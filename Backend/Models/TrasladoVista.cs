using System;

namespace Crown__.Backend.Models
{
    public class TrasladoVista
    {
        public int Id { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public DateTime FechaHora { get; set; }
        public string Estado { get; set; }
        public string Habitacion { get; set; }
        public string Cliente { get; set; }
    }
}

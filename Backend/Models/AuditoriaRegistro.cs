using System;

namespace Crown__.Backend.Models
{
    public class AuditoriaRegistro
    {
        public int Id { get; set; }
        public string Usuario { get; set; }
        public string Modulo { get; set; }
        public string Accion { get; set; }
        public string Motivo { get; set; }
        public DateTime Fecha { get; set; }
    }
}

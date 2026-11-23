using System;

namespace Crown__.Backend.Models
{
    public class CanalVista
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal Comision { get; set; }
        public string ComisionTexto { get { return Comision.ToString("0.##") + " %"; } }
    }

    public class GrupoVista
    {
        public string Nombre { get; set; }
        public DateTime Creado { get; set; }
        public string CreadoTexto { get { return Creado.ToString("dd/MM/yyyy"); } }
    }

    public class EsperaVista
    {
        public string Huesped { get; set; }
        public string TipoHabitacion { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public string Estado { get; set; }
        public string DesdeTexto { get { return Desde.ToString("dd/MM/yyyy"); } }
        public string HastaTexto { get { return Hasta.ToString("dd/MM/yyyy"); } }
    }

    public class DetalleReservaVista
    {
        public string[] Huespedes { get; set; }
        public string[] Habitaciones { get; set; }
        public string[] Solicitudes { get; set; }
        public string[] Recursos { get; set; }
    }
}

using System;

namespace Crown__.Backend.Models
{
    public class TareaLimpieza
    {
        public int Id { get; set; }
        public int HabitacionId { get; set; }
        public string Habitacion { get; set; }
        public string Tipo { get; set; }
        public DateTime Programada { get; set; }
        public string Estado { get; set; }
        public string Empleado { get; set; }

        public override string ToString()
        {
            return "Hab. " + Habitacion + " · " + Tipo + " · " + Programada.ToString("dd/MM HH:mm") + (string.IsNullOrEmpty(Empleado) ? "" : " · " + Empleado);
        }
    }

    public class EmpleadoVista
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public override string ToString() { return Nombre; }
    }

    public class ChecklistVista
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int Items { get; set; }
        public override string ToString() { return Nombre; }
    }

    public class ChecklistItemVista
    {
        public string Checklist { get; set; }
        public string Descripcion { get; set; }
        public string Completado { get; set; }
    }

    public class InspeccionVista
    {
        public string Tarea { get; set; }
        public string Empleado { get; set; }
        public string Resultado { get; set; }
        public string Observaciones { get; set; }
        public DateTime Fecha { get; set; }
        public string FechaTexto { get { return Fecha.ToString("dd/MM HH:mm"); } }
    }

    public class IncidenciaVista
    {
        public int Id { get; set; }
        public string Habitacion { get; set; }
        public string Tipo { get; set; }
        public string Descripcion { get; set; }
        public string Estado { get; set; }
        public override string ToString() { return "MT-" + Id.ToString("000") + " · Hab. " + Habitacion + " · " + Descripcion; }
    }

    public class OrdenVista
    {
        public int Id { get; set; }
        public int IncidenciaId { get; set; }
        public string Incidencia { get; set; }
        public string Empleado { get; set; }
        public string Estado { get; set; }
        public string OrdenTexto { get { return "OM-" + Id.ToString("000"); } }
        public override string ToString() { return OrdenTexto + " · " + Incidencia; }
    }

    public class ObjetoOlvidadoVista
    {
        public int Id { get; set; }
        public string Habitacion { get; set; }
        public string Descripcion { get; set; }
        public DateTime Encontrado { get; set; }
        public string Estado { get; set; }
        public string EncontradoTexto { get { return Encontrado.ToString("dd/MM/yyyy"); } }
    }
}

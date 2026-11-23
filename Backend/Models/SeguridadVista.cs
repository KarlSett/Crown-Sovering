using System;

namespace Crown__.Backend.Models
{
    public class UsuarioGestion
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Empleado { get; set; }
        public string Roles { get; set; }
        public string Estado { get; set; }
        public override string ToString() { return Username + " · " + Empleado; }
    }

    public class EmpleadoFicha
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public override string ToString() { return Nombre + " " + Apellido; }
    }

    public class RolGestion
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int Usuarios { get; set; }
        public override string ToString() { return Nombre; }
    }

    public class AsignacionVista
    {
        public int UsuarioId { get; set; }
        public int RolId { get; set; }
        public string Usuario { get; set; }
        public string Rol { get; set; }
    }

    public class HotelVista
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Direccion { get; set; }
        public int Categoria { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
    }

    public class ParametroVista
    {
        public string Clave { get; set; }
        public string Valor { get; set; }
        public DateTime Actualizado { get; set; }
        public string ActualizadoTexto { get { return Actualizado.ToString("dd/MM/yyyy HH:mm"); } }
    }

    public class PoliticaVista
    {
        public string Nombre { get; set; }
        public string Valor { get; set; }
        public string Descripcion { get; set; }
    }

    public class ImpuestoVista
    {
        public string Nombre { get; set; }
        public decimal Porcentaje { get; set; }
        public bool Activo { get; set; }
        public string PorcentajeTexto { get { return Porcentaje.ToString("0.##") + " %"; } }
        public string ActivoTexto { get { return Activo ? "Sí" : "No"; } }
    }

    public class ModuloVista
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }
        public string ActivoTexto { get { return Activo ? "Sí" : "No"; } }
    }

    public class AuditoriaFila
    {
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; }
        public string Cambio { get; set; }
        public string Anterior { get; set; }
        public string Nuevo { get; set; }
        public string FechaTexto { get { return Fecha.ToString("dd/MM/yyyy HH:mm"); } }
    }
}

using System;

namespace Crown__.Backend.Models
{
    public class PisoVista
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public override string ToString() { return Nombre; }
    }

    public class TipoHabitacionVista
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int Capacidad { get; set; }
        public override string ToString() { return Nombre; }
    }

    public class HabitacionGestion
    {
        public int Id { get; set; }
        public int PisoId { get; set; }
        public string Numero { get; set; }
        public int TipoId { get; set; }
        public string Tipo { get; set; }
        public string Estado { get; set; }
        public override string ToString() { return Numero + " · " + Tipo + " · " + Estado; }
    }

    public class HistorialEstadoVista
    {
        public string Estado { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime? Fin { get; set; }
        public string Motivo { get; set; }
        public string InicioTexto { get { return Inicio.ToString("dd/MM/yyyy HH:mm"); } }
        public string FinTexto { get { return Fin.HasValue ? Fin.Value.ToString("dd/MM/yyyy HH:mm") : "—"; } }
    }

    public class BloqueoVista
    {
        public string Habitacion { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
        public string Motivo { get; set; }
        public string InicioTexto { get { return Inicio.ToString("dd/MM/yyyy"); } }
        public string FinTexto { get { return Fin.ToString("dd/MM/yyyy"); } }
    }

    public class TarifaVista
    {
        public int Id { get; set; }
        public string Tipo { get; set; }
        public string Nombre { get; set; }
        public decimal PrecioBase { get; set; }
        public string PrecioTexto { get { return Formato.Moneda(PrecioBase); } }
        public override string ToString() { return Tipo + " · " + Nombre; }
    }

    public class TemporadaVista
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
        public string InicioTexto { get { return Inicio.ToString("dd/MM/yyyy"); } }
        public string FinTexto { get { return Fin.ToString("dd/MM/yyyy"); } }
        public override string ToString() { return Nombre; }
    }

    public class PaqueteVista
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public string PrecioTexto { get { return Formato.Moneda(Precio); } }
    }

    public class PromocionVista
    {
        public string Nombre { get; set; }
        public decimal Descuento { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
        public string Estado { get; set; }
        public string DescuentoTexto { get { return Descuento.ToString("0.##") + " %"; } }
        public string InicioTexto { get { return Inicio.ToString("dd/MM/yyyy"); } }
        public string FinTexto { get { return Fin.ToString("dd/MM/yyyy"); } }
    }

    public class PrecioCanalVista
    {
        public string Tarifa { get; set; }
        public string Temporada { get; set; }
        public string Canal { get; set; }
        public decimal Precio { get; set; }
        public string PrecioTexto { get { return Formato.Moneda(Precio); } }
    }

    public class ConvenioVista
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Empresa { get; set; }
        public decimal Tarifa { get; set; }
        public decimal Limite { get; set; }
        public DateTime? Vence { get; set; }
        public string Estado { get; set; }
        public string TarifaTexto { get { return Formato.Moneda(Tarifa); } }
        public string LimiteTexto { get { return Formato.Moneda(Limite); } }
        public string VenceTexto { get { return Vence.HasValue ? Vence.Value.ToString("dd/MM/yyyy") : "—"; } }
        public override string ToString() { return Nombre + " · " + Empresa; }
    }

    public class ConvenioReservaVista
    {
        public string Reserva { get; set; }
        public string Huesped { get; set; }
        public DateTime Entrada { get; set; }
        public DateTime Salida { get; set; }
        public string Estado { get; set; }
        public string EntradaTexto { get { return Entrada.ToString("dd/MM/yyyy"); } }
        public string SalidaTexto { get { return Salida.ToString("dd/MM/yyyy"); } }
    }

    public class ConvenioSaldoVista
    {
        public string Convenio { get; set; }
        public decimal Saldo { get; set; }
        public string Descripcion { get; set; }
        public DateTime Fecha { get; set; }
        public string SaldoTexto { get { return Formato.Moneda(Saldo); } }
        public string FechaTexto { get { return Fecha.ToString("dd/MM/yyyy HH:mm"); } }
    }

    public class RolVista
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public override string ToString() { return Nombre; }
    }

    public class NotificacionVista
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Mensaje { get; set; }
        public DateTime Fecha { get; set; }
        public override string ToString() { return Fecha.ToString("dd/MM HH:mm") + " · " + Titulo; }
    }

    public class DestinatarioVista
    {
        public string Usuario { get; set; }
        public DateTime Enviada { get; set; }
        public DateTime? Recibida { get; set; }
        public string Estado { get; set; }
        public string EnviadaTexto { get { return Enviada.ToString("dd/MM HH:mm"); } }
        public string RecibidaTexto { get { return Recibida.HasValue ? Recibida.Value.ToString("dd/MM HH:mm") : "—"; } }
    }

    public class ReporteVista
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public string Formato { get; set; }
        public string Frecuencia { get; set; }
        public string Correo { get; set; }
        public bool Activo { get; set; }
        public string ActivoTexto { get { return Activo ? "Sí" : "No"; } }
        public override string ToString() { return Nombre; }
    }

    public class EjecucionVista
    {
        public string Reporte { get; set; }
        public DateTime Ejecutado { get; set; }
        public string Estado { get; set; }
        public string Archivo { get; set; }
        public string EjecutadoTexto { get { return Ejecutado.ToString("dd/MM/yyyy HH:mm"); } }
    }
}

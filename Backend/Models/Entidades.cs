using System;

namespace Crown__.Backend.Models
{
    // Espejo de los enums/columnas de la base de datos suministrada.
    public enum EstadoHabitacion
    {
        Disponible,
        Ocupada,
        Limpieza,
        Mantenimiento,
        FueraDeServicio
    }

    public enum EstadoReserva
    {
        Confirmada,
        EnCurso,
        Finalizada,
        Cancelada,
        NoShow
    }

    public enum CanalReserva
    {
        Mostrador,
        Telefono,
        Web,
        Agencia,
        Convenio,
        OTA
    }

    public enum PeriodoReporte
    {
        Diario,
        Semanal,
        Trimestral
    }

    public class Habitacion
    {
        public int Id { get; set; }
        public int CapacidadOverride { get; set; }
        public string NumeroHabitacion { get; set; }
        public int Piso { get; set; }
        public string CapacidadTipo { get; set; }   // capacidad_tipo (enum en la BD)
        public string Calidad { get; set; }          // calidad (enum en la BD)
        public decimal TarifaBase { get; set; }
        public EstadoHabitacion Estado { get; set; }
        public bool TieneVista { get; set; }
        public bool TieneBalcon { get; set; }
        public bool TieneJacuzzi { get; set; }
        public bool EsHipoalergenica { get; set; }
        public bool LibreDeHumo { get; set; }

        public int CapacidadPersonas
        {
            get
            {
                if (CapacidadOverride > 0) return CapacidadOverride;
                switch (CapacidadTipo)
                {
                    case "Sencilla": return 2;
                    case "Doble": return 4;
                    case "Familiar": return 6;
                    default: return 3; // Suite
                }
            }
        }

        public string DescripcionExtras
        {
            get
            {
                var partes = new System.Collections.Generic.List<string>();
                if (TieneVista) partes.Add("Vista");
                if (TieneBalcon) partes.Add("Balcón");
                if (TieneJacuzzi) partes.Add("Jacuzzi");
                if (EsHipoalergenica) partes.Add("Hipoalergénica");
                if (!LibreDeHumo) partes.Add("Fumadores");
                return partes.Count == 0 ? "—" : string.Join(" · ", partes);
            }
        }
    }

    public class Huesped
    {
        public Guid Id { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string TipoDocumento { get; set; }
        public string NumeroDocumento { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string Nacionalidad { get; set; }
        public string Segmentacion { get; set; }

        public string NombreCompleto
        {
            get { return Nombres + " " + Apellidos; }
        }
    }

    public class ConsentimientoHuesped
    {
        public Guid Id { get; set; }
        public Guid HuespedId { get; set; }
        public string Tipo { get; set; }
        public bool Autorizado { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class TareaOperativa
    {
        public int Id { get; set; }
        public string Habitacion { get; set; }
        public string Tipo { get; set; }
        public string Descripcion { get; set; }
        public string Estado { get; set; }
        public DateTime Fecha { get; set; }
        public string Etiqueta { get { return "Hab. " + Habitacion + " · " + Tipo + " · " + Descripcion; } }
    }

    public class FacturaDemo
    {
        public string Numero { get; set; }
        public Guid FolioId { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get { return Subtotal + Impuesto; } }
        public string Estado { get; set; }
    }

    public class NotaCreditoDemo
    {
        public string Numero { get; set; }
        public string Factura { get; set; }
        public string Motivo { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class LlaveDemo
    {
        public int Id { get; set; }
        public Guid ReservaId { get; set; }
        public string Codigo { get; set; }
        public DateTime Emitida { get; set; }
        public DateTime? Devuelta { get; set; }
        public string Estado { get; set; }
    }

    public class BloqueoHabitacionDemo { public int Id { get; set; } public int HabitacionId { get; set; } public DateTime Inicio { get; set; } public DateTime Fin { get; set; } public string Motivo { get; set; } }
    public class TipoHabitacionDemo { public string Nombre { get; set; } public int Capacidad { get; set; } }
    public class HistorialHabitacionDemo { public int HabitacionId { get; set; } public string Estado { get; set; } public DateTime Inicio { get; set; } public DateTime? Fin { get; set; } public string Motivo { get; set; } }

    public class ProductoDemo
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public bool Activo { get; set; }
    }

    public class ConsumoDemo
    {
        public int Id { get; set; }
        public Guid ReservaId { get; set; }
        public string Producto { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Total { get { return Cantidad * Precio; } }
        public DateTime Fecha { get; set; }
    }

    public class ConvenioDemo
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Empresa { get; set; }
        public decimal TarifaPreferencial { get; set; }
        public decimal LimiteCredito { get; set; }
        public DateTime Vencimiento { get; set; }
        public bool Activo { get; set; }
    }

    public class TemporadaDemo
    {
        public string Nombre { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
    }

    public class PaqueteDemo
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
    }

    public class PromocionDemo
    {
        public string Nombre { get; set; }
        public decimal Descuento { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
        public bool Activa { get; set; }
    }

    public class ConfiguracionDemo { public string Clave { get; set; } public string Valor { get; set; } public DateTime Fecha { get; set; } }
    public class PoliticaDemo { public string Nombre { get; set; } public string Valor { get; set; } public string Descripcion { get; set; } }
    public class ImpuestoDemo { public string Nombre { get; set; } public decimal Porcentaje { get; set; } public bool Activo { get; set; } }
    public class AuditoriaDemo { public string Usuario { get; set; } public string Cambio { get; set; } public string Anterior { get; set; } public string Nuevo { get; set; } public DateTime Fecha { get; set; } }
    public class DevolucionDemo { public int Id { get; set; } public int TransaccionId { get; set; } public decimal Monto { get; set; } public string Motivo { get; set; } public string Estado { get; set; } public DateTime Fecha { get; set; } }

    public class NotificacionDemo { public string Titulo { get; set; } public string Mensaje { get; set; } public string Rol { get; set; } public DateTime Fecha { get; set; } public string Estado { get; set; } }
    public class ReporteProgramadoDemo { public string Nombre { get; set; } public string Tipo { get; set; } public string Formato { get; set; } public string Frecuencia { get; set; } public string Correo { get; set; } public bool Activo { get; set; } }
    public class EjecucionReporteDemo { public string Reporte { get; set; } public DateTime Fecha { get; set; } public string Estado { get; set; } public string Archivo { get; set; } }
    public class EmpleadoDemo { public int Id { get; set; } public string Nombre { get; set; } public string Apellido { get; set; } public string Correo { get; set; } public string Telefono { get; set; } public string NombreCompleto { get { return Nombre + " " + Apellido; } } }
    public class RolDemo { public string Nombre { get; set; } public string Descripcion { get; set; } }
    public class AsignacionRolDemo { public int EmpleadoId { get; set; } public string Empleado { get; set; } public string Rol { get; set; } }
    public class ConceptoCargoDemo { public string Nombre { get; set; } public string Categoria { get; set; } }
    public class ServicioCatalogoDemo { public string Nombre { get; set; } public string Tipo { get; set; } public decimal Precio { get; set; } public bool Activo { get; set; } }
    public class CitaServicioDemo { public string Reserva { get; set; } public string Servicio { get; set; } public DateTime Fecha { get; set; } public int Duracion { get; set; } public string Estado { get; set; } }
    public class TrasladoDemo { public string Reserva { get; set; } public string Origen { get; set; } public string Destino { get; set; } public DateTime Fecha { get; set; } }
    public class ChecklistDemo { public int Id { get; set; } public string Nombre { get; set; } public string Descripcion { get; set; } }
    public class ChecklistItemDemo { public int ChecklistId { get; set; } public string Checklist { get; set; } public string Descripcion { get; set; } public bool Completado { get; set; } }

    public class Reserva
    {
        public Guid Id { get; set; }
        public string CodigoReserva { get; set; }
        public Guid HuespedId { get; set; }
        public int HabitacionId { get; set; }
        public CanalReserva Canal { get; set; }
        public EstadoReserva Estado { get; set; }
        public DateTime FechaCheckinEsperado { get; set; }
        public DateTime FechaCheckoutEsperado { get; set; }
        public decimal TarifaAplicada { get; set; }
        public string Observaciones { get; set; }
        public int CantidadPersonas { get; set; }
        public int? ConvenioId { get; set; }
        public DateTime CreadaEl { get; set; }

        public int Noches
        {
            get
            {
                var noches = (int)Math.Ceiling((FechaCheckoutEsperado.Date - FechaCheckinEsperado.Date).TotalDays);
                return noches <= 0 ? 1 : noches;
            }
        }

        public decimal Total
        {
            get { return TarifaAplicada * Noches; }
        }
    }

    public class Folio
    {
        public Guid Id { get; set; }
        public Guid ReservaId { get; set; }
        public Guid HuespedId { get; set; }
        public string TipoFolio { get; set; }
        public string Estado { get; set; }  // Abierto / Cerrado
        public decimal SaldoPendiente { get; set; }
    }

    public class Transaccion
    {
        public int Id { get; set; }
        public Guid FolioId { get; set; }
        public DateTime Fecha { get; set; }
        public string Concepto { get; set; }
        public decimal Cargo { get; set; }
        public decimal Abono { get; set; }
        public int Cantidad { get; set; }
        public string MetodoPago { get; set; }  // Efectivo / Tarjeta / Transferencia
        public string Usuario { get; set; }
    }

    public class CierreCaja
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public decimal FondoInicial { get; set; }
        public decimal TotalEfectivo { get; set; }
        public decimal TotalTarjeta { get; set; }
        public decimal TotalTransferencia { get; set; }

        public decimal TotalCobrado
        {
            get { return TotalEfectivo + TotalTarjeta + TotalTransferencia; }
        }
    }
}

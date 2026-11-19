using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Crown__.Backend.Models
{
    public class LineaIngreso
    {
        public string Cuenta { get; set; }
        public decimal Cargos { get; set; }
        public decimal Abonos { get; set; }
        public double Participacion { get; set; }
        public double AnchoBarra { get; set; }

        public decimal Saldo
        {
            get { return Cargos - Abonos; }
        }

        public string AbonosTexto
        {
            get { return Formato.Moneda(Abonos); }
        }

        public string CargosTexto
        {
            get { return Formato.Moneda(Cargos); }
        }

        public string ParticipacionTexto
        {
            get { return Formato.Porcentaje(Participacion) + " del cobrado"; }
        }
    }

    public class DetallePeriodo
    {
        public string Etiqueta { get; set; }
        public decimal Cargos { get; set; }
        public decimal Abonos { get; set; }

        public decimal Saldo
        {
            get { return Cargos - Abonos; }
        }
    }

    public class ResumenIngresos
    {
        public PeriodoReporte Periodo { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
        public string EtiquetaPeriodo { get; set; }
        public string TituloDetalle { get; set; }
        public string ColumnaDetalle { get; set; }

        public decimal TotalCargos { get; set; }
        public decimal TotalAbonos { get; set; }
        public int Transacciones { get; set; }
        public int Unidades { get; set; }

        public decimal Efectivo { get; set; }
        public decimal Tarjeta { get; set; }
        public decimal Transferencia { get; set; }

        public List<LineaIngreso> Cuentas { get; set; }
        public List<DetallePeriodo> Detalle { get; set; }

        public decimal TotalMedios
        {
            get { return Efectivo + Tarjeta + Transferencia; }
        }

        public decimal Saldo
        {
            get { return TotalCargos - TotalAbonos; }
        }

        public decimal PromedioUnidad
        {
            get
            {
                return TotalAbonos / Math.Max(1, Unidades);
            }
        }

        public LineaIngreso MejorCuenta
        {
            get
            {
                if (Cuentas == null || Cuentas.Count == 0)
                    return null;
                return Cuentas[0];
            }
        }
    }

    public class ResumenOperativo
    {
        public int TotalHabitaciones { get; set; }
        public int Ocupadas { get; set; }
        public int Disponibles { get; set; }
        public int Limpieza { get; set; }
        public int Mantenimiento { get; set; }
        public int FueraDeServicio { get; set; }
        public double Ocupacion { get; set; }
        public int Llegadas { get; set; }
        public int Salidas { get; set; }
        public int EnCasa { get; set; }
        public int ReservasActivas { get; set; }
        public decimal IngresosHoy { get; set; }
        public decimal SaldoPendiente { get; set; }

        public List<EstadoChip> Estados { get; set; }
        public List<LlegadaVista> LlegadasHoy { get; set; }
        public List<LlegadaVista> SalidasHoy { get; set; }
        public List<MovimientoVista> Movimientos { get; set; }
    }

    public class EstadoChip
    {
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public Brush Color { get; set; }
    }

    public class LlegadaVista
    {
        public string Huesped { get; set; }
        public string Habitacion { get; set; }
        public string Detalle { get; set; }
        public string Hora { get; set; }
    }

    public class MovimientoVista
    {
        public string Hora { get; set; }
        public string Concepto { get; set; }
        public string Huesped { get; set; }
        public string MetodoPago { get; set; }
        public string Monto { get; set; }
    }

    public class ReservaVista
    {
        public Reserva Reserva { get; internal set; }
        public string Codigo { get; set; }
        public string Huesped { get; set; }
        public string Habitacion { get; set; }
        public string Canal { get; set; }
        public string Checkin { get; set; }
        public string Checkout { get; set; }
        public int Noches { get; set; }
        public string Tarifa { get; set; }
        public string Total { get; set; }
        public string Estado { get; set; }
        public string EstadoCrudo { get; internal set; }
        public string Observaciones { get; set; }
    }

    public class HabitacionVista
    {
        public Habitacion Habitacion { get; internal set; }
        public string Numero { get; set; }
        public string Piso { get; set; }
        public string Tipo { get; set; }
        public string Calidad { get; set; }
        public string Capacidad { get; set; }
        public string Tarifa { get; set; }
        public string Extras { get; set; }
        public string Estado { get; set; }
        public string Disponibilidad { get; set; }
        public bool EstaDisponible { get; set; }
    }

    public class MedioPagoVista
    {
        public string Nombre { get; set; }
        public string Monto { get; set; }
        public double AnchoBarra { get; set; }
        public Brush Color { get; set; }
    }

    public class HabitacionOpcion
    {
        public int Id { get; set; }
        public string Texto { get; set; }
    }

    public class DetalleFilaVista
    {
        public string Etiqueta { get; set; }
        public string Cargos { get; set; }
        public string Abonos { get; set; }
        public string Saldo { get; set; }
    }
}

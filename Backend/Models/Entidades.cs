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
        public int Id { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string TipoDocumento { get; set; }
        public string NumeroDocumento { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Nacionalidad { get; set; }
        public string Segmentacion { get; set; }

        public string NombreCompleto
        {
            get { return Nombres + " " + Apellidos; }
        }

        public override string ToString()
        {
            return NombreCompleto + " · " + TipoDocumento + " " + NumeroDocumento;
        }
    }

    public class Reserva
    {
        public int Id { get; set; }
        public string CodigoReserva { get; set; }
        public int HuespedId { get; set; }
        public int HabitacionId { get; set; }
        public CanalReserva Canal { get; set; }
        public EstadoReserva Estado { get; set; }
        public DateTime FechaCheckinEsperado { get; set; }
        public DateTime FechaCheckoutEsperado { get; set; }
        public decimal TarifaAplicada { get; set; }
        public string Observaciones { get; set; }
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
        public int Id { get; set; }
        public int ReservaId { get; set; }
        public int HuespedId { get; set; }
        public string TipoFolio { get; set; }
        public string Estado { get; set; }  // Abierto / Cerrado
        public decimal SaldoPendiente { get; set; }
    }

    public class Transaccion
    {
        public int Id { get; set; }
        public int FolioId { get; set; }
        public DateTime Fecha { get; set; }
        public string Concepto { get; set; }
        public decimal Cargo { get; set; }
        public decimal Abono { get; set; }
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

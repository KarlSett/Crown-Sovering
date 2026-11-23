using System;
using System.Collections.Generic;
using System.Linq;
using Crown__.Backend.Models;

namespace Crown__.Backend.Data
{
    // Datos del hotel leídos de la base. Cada lista se guarda unos segundos para no repetir la consulta
    // en cada pantalla; Refrescar() la descarta después de una operación que cambia datos.
    public static class DatosHotel
    {
        private const int SegundosDeCache = 4;
        private static readonly object Candado = new object();
        private static DateTime cargadoEl = DateTime.MinValue;
        private static List<Habitacion> habitaciones;
        private static List<Huesped> huespedes;
        private static List<Reserva> reservas;
        private static List<Folio> folios;
        private static List<Transaccion> transacciones;

        public static readonly string[] CuentasIngreso =
            { "Alojamiento", "Restaurante", "Minibar", "Spa", "Lavanderia", "Transporte", "Otros", "Pagos" };

        public static void Inicializar()
        {
            Cargar();
        }

        public static void Refrescar()
        {
            lock (Candado) { cargadoEl = DateTime.MinValue; }
        }

        public static List<Habitacion> Habitaciones { get { Cargar(); return habitaciones; } }
        public static List<Huesped> Huespedes { get { Cargar(); return huespedes; } }
        public static List<Reserva> Reservas { get { Cargar(); return reservas; } }
        public static List<Folio> Folios { get { Cargar(); return folios; } }
        public static List<Transaccion> Transacciones { get { Cargar(); return transacciones; } }

        private static void Cargar()
        {
            lock (Candado)
            {
                if ((DateTime.Now - cargadoEl).TotalSeconds < SegundosDeCache && habitaciones != null) return;

                var repositorio = new Repositories.HotelRepository();
                habitaciones = repositorio.Habitaciones();
                huespedes = repositorio.Huespedes();
                reservas = repositorio.Reservas();
                folios = repositorio.Folios();
                transacciones = repositorio.Transacciones();
                cargadoEl = DateTime.Now;
            }
        }
    }
}

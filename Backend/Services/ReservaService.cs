using System;
using System.Collections.Generic;
using System.Globalization;
using Crown__.Backend.Models;
using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    public class ReservaService
    {
        // etiqueta del formulario -> nombre del canal en la base
        private static readonly Dictionary<string, string> Canales = new Dictionary<string, string>
        {
            { "Teléfono", "Telefono" },
            { "Mostrador", "Mostrador" },
            { "Web", "Web" },
            { "Agencia", "Agencia" },
            { "WhatsApp", "Telefono" },
            { "Correo electrónico", "Web" },
            { "Booking.com", "Web" },
            { "Corporativo", "Agencia" }
        };

        private readonly ReservaRepository reservas = new ReservaRepository();
        private readonly AuditoriaService auditoria = new AuditoriaService();

        public List<ReservaRegistro> Listar()
        {
            return reservas.ObtenerTodas();
        }

        // Devuelve la reserva creada, o null con el motivo en error.
        public ReservaRegistro Crear(string actorId, NuevaReserva datos, out string error)
        {
            int actor;
            error = Autorizacion.Verificar(actorId, out actor, "Recepcionista", "Administrador");
            if (error != null) return null;

            error = Validar(datos);
            if (error != null) return null;
            datos.Canal = Canales[datos.Canal];

            var reserva = reservas.Crear(datos, out error);
            if (reserva != null)
            {
                int noches = (datos.Salida.Date - datos.Llegada.Date).Days;
                string detalle = string.Format(CultureInfo.InvariantCulture,
                    "{{\"habitacion\": \"{0}\", \"noches\": {1}}}", reserva.Habitacion, noches);
                auditoria.Registrar(actor, "Reservas", "Crear reserva", reserva.Codigo, null, detalle);
            }
            return reserva;
        }

        // Reserva una habitación concreta para un huésped ya registrado. Devuelve null y el motivo en error si no se puede.
        public ReservaRegistro CrearPorIds(string actorId, int huespedId, int habitacionId, DateTime llegada, DateTime salida,
                                           string canal, int personas, out string error)
        {
            int actor;
            error = Autorizacion.Verificar(actorId, out actor, "Recepcionista", "Administrador");
            if (error != null) return null;
            if (salida.Date <= llegada.Date) { error = "La salida debe ser posterior a la llegada."; return null; }
            if (llegada.Date < DateTime.Today) { error = "La llegada no puede ser una fecha pasada."; return null; }
            string nombreCanal;
            if (!Canales.TryGetValue(canal, out nombreCanal)) nombreCanal = canal;

            var reserva = reservas.CrearPorIds(huespedId, habitacionId, llegada, salida, nombreCanal, personas, out error);
            if (reserva != null)
                auditoria.Registrar(actor, "Reservas", "Crear reserva", reserva.Codigo, null,
                    string.Format(System.Globalization.CultureInfo.InvariantCulture, "{{\"habitacion\": \"{0}\", \"noches\": {1}}}", reserva.Habitacion, (salida.Date - llegada.Date).Days));
            return reserva;
        }

        private static string Validar(NuevaReserva d)
        {
            if (string.IsNullOrWhiteSpace(d.Nombre)) return "Falta el nombre del huésped.";
            if (string.IsNullOrWhiteSpace(d.NumeroDocumento)) return "Falta el número de documento.";
            if (string.IsNullOrWhiteSpace(d.TipoHabitacion)) return "Falta el tipo de habitación.";
            if (d.Canal == null || !Canales.ContainsKey(d.Canal)) return "El canal de origen no es válido.";
            if (d.Llegada.Date < DateTime.Today) return "La llegada no puede ser una fecha pasada.";
            if (d.Salida.Date <= d.Llegada.Date) return "La salida debe ser posterior a la llegada.";
            if (d.Huespedes < 1 || d.Huespedes > 6) return "El número de huéspedes debe estar entre 1 y 6.";
            return null;
        }
    }
}

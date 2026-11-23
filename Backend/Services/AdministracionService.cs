using System;
using System.Collections.Generic;
using Crown__.Backend.Models;
using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    // Inventario de habitaciones, tarifas, convenios, notificaciones y reportes (Administrador y Gerente).
    // Cada operación devuelve null si salió bien, o el motivo del problema.
    public class AdministracionService
    {
        private static readonly string[] Roles = { "Administrador", "Gerente" };

        private readonly AdministracionRepository repositorio = new AdministracionRepository();
        private readonly HabitacionesService habitaciones = new HabitacionesService();
        private readonly AuditoriaService auditoria = new AuditoriaService();

        private string Autorizar(string actorId, out int actor)
        {
            return Autorizacion.Verificar(actorId, out actor, Roles);
        }

        // ---------------------------------------------------------------- Lecturas
        public List<PisoVista> Pisos() { return repositorio.Pisos(); }
        public List<TipoHabitacionVista> Tipos() { return repositorio.Tipos(); }
        public List<HabitacionGestion> Habitaciones() { return repositorio.Habitaciones(); }
        public List<HistorialEstadoVista> Historial(HabitacionGestion h, DateTime? desde, DateTime? hasta) { return h == null ? new List<HistorialEstadoVista>() : repositorio.Historial(h.Id, desde, hasta); }
        public List<BloqueoVista> Bloqueos() { return repositorio.Bloqueos(); }
        public List<TarifaVista> Tarifas() { return repositorio.Tarifas(); }
        public List<TemporadaVista> Temporadas() { return repositorio.Temporadas(); }
        public List<PaqueteVista> Paquetes() { return repositorio.Paquetes(); }
        public List<PromocionVista> Promociones() { return repositorio.Promociones(); }
        public List<PrecioCanalVista> PreciosPorCanal() { return repositorio.PreciosPorCanal(); }
        public List<CanalVista> Canales() { return new ReservasAvanzadasService().Canales(); }
        public List<ConvenioVista> Convenios() { return repositorio.Convenios(); }
        public List<ConvenioReservaVista> ReservasDelConvenio(ConvenioVista c) { return c == null ? new List<ConvenioReservaVista>() : repositorio.ReservasDelConvenio(c.Id); }
        public List<ConvenioSaldoVista> SaldosDelConvenio(ConvenioVista c) { return c == null ? new List<ConvenioSaldoVista>() : repositorio.SaldosDelConvenio(c.Id); }
        public List<RolVista> Roles_() { return repositorio.Roles(); }
        public List<NotificacionVista> Notificaciones() { return repositorio.Notificaciones(); }
        public List<DestinatarioVista> Destinatarios(NotificacionVista n) { return n == null ? new List<DestinatarioVista>() : repositorio.Destinatarios(n.Id); }
        public List<ReporteVista> Reportes() { return repositorio.Reportes(); }
        public List<EjecucionVista> Ejecuciones(ReporteVista r, string estado, DateTime? desde, DateTime? hasta) { return repositorio.Ejecuciones(r == null ? (int?)null : r.Id, estado, desde, hasta); }

        // ---------------------------------------------------------------- Inventario
        public string NuevoPiso(string actorId, string nombre)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre del piso.";
            if (repositorio.ExistePiso(nombre.Trim())) return "Ya existe el piso " + nombre.Trim() + ".";

            repositorio.NuevoPiso(nombre.Trim());
            auditoria.Registrar(actor, "Inventario", "Crear piso", nombre.Trim());
            return null;
        }

        public string NuevoTipo(string actorId, string nombre, int capacidad)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre del tipo.";
            if (capacidad < 1 || capacidad > 10) return "La capacidad debe estar entre 1 y 10 personas.";
            if (repositorio.ExisteTipo(nombre.Trim())) return "Ya existe el tipo " + nombre.Trim() + ".";

            repositorio.NuevoTipo(nombre.Trim(), capacidad);
            auditoria.Registrar(actor, "Inventario", "Crear tipo de habitación", nombre.Trim());
            return null;
        }

        public string NuevaHabitacion(string actorId, PisoVista piso, TipoHabitacionVista tipo, string numero)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (piso == null) return "Selecciona el piso.";
            if (tipo == null) return "Selecciona el tipo de habitación.";
            if (string.IsNullOrWhiteSpace(numero)) return "Escribe el número de la habitación.";
            if (repositorio.ExisteHabitacion(piso.Id, numero.Trim())) return "Ya existe la habitación " + numero.Trim() + " en " + piso.Nombre + ".";

            repositorio.NuevaHabitacion(piso.Id, tipo.Id, numero.Trim());
            auditoria.Registrar(actor, "Inventario", "Crear habitación", numero.Trim());
            return null;
        }

        public string CambiarTipo(string actorId, HabitacionGestion habitacion, TipoHabitacionVista tipo)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (habitacion == null) return "Selecciona una habitación.";
            if (tipo == null) return "Selecciona el tipo.";
            if (habitacion.Estado == "Ocupada") return "La habitación está ocupada: no se puede cambiar su tipo.";

            repositorio.CambiarTipo(habitacion.Id, tipo.Id);
            auditoria.Registrar(actor, "Inventario", "Cambiar tipo", habitacion.Numero + " → " + tipo.Nombre);
            return null;
        }

        public string CambiarEstado(string actorId, HabitacionGestion habitacion, string nuevoEstado)
        {
            if (habitacion == null) return "Selecciona una habitación.";
            return habitaciones.CambiarEstado(actorId, habitacion.Numero, nuevoEstado, "Cambio desde inventario");
        }

        public string NuevoBloqueo(string actorId, HabitacionGestion habitacion, DateTime? desde, DateTime? hasta, string motivo)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (habitacion == null) return "Selecciona la habitación.";
            if (!desde.HasValue || !hasta.HasValue) return "Selecciona las fechas del bloqueo.";
            if (hasta.Value.Date < desde.Value.Date) return "La fecha final no puede ser anterior a la inicial.";

            error = repositorio.NuevoBloqueo(habitacion, desde.Value, hasta.Value, motivo, actor);
            if (error == null) auditoria.Registrar(actor, "Inventario", "Bloquear habitación", habitacion.Numero + (string.IsNullOrWhiteSpace(motivo) ? "" : " · " + motivo));
            return error;
        }

        // ---------------------------------------------------------------- Tarifas
        public string NuevaTarifa(string actorId, TipoHabitacionVista tipo, string nombre, decimal precio)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (tipo == null) return "Selecciona el tipo de habitación.";
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre de la tarifa.";
            if (precio <= 0) return "El precio debe ser mayor que cero.";

            repositorio.NuevaTarifa(tipo.Id, nombre.Trim(), precio);
            auditoria.Registrar(actor, "Tarifas", "Crear tarifa", tipo.Nombre + " · " + nombre.Trim() + " · " + precio);
            return null;
        }

        public string CambiarPrecio(string actorId, TarifaVista tarifa, decimal precio)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (tarifa == null) return "Selecciona una tarifa.";
            if (precio <= 0) return "El precio debe ser mayor que cero.";

            repositorio.CambiarPrecio(tarifa.Id, precio);
            auditoria.Registrar(actor, "Tarifas", "Cambiar precio", tarifa + " · " + tarifa.PrecioBase + " → " + precio);
            return null;
        }

        public string NuevaTemporada(string actorId, string nombre, DateTime? inicio, DateTime? fin)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre de la temporada.";
            if (!inicio.HasValue || !fin.HasValue) return "Selecciona las fechas.";
            if (fin.Value.Date < inicio.Value.Date) return "La fecha final no puede ser anterior a la inicial.";

            repositorio.NuevaTemporada(nombre.Trim(), inicio.Value, fin.Value);
            auditoria.Registrar(actor, "Tarifas", "Crear temporada", nombre.Trim());
            return null;
        }

        public string NuevoPaquete(string actorId, string nombre, string descripcion, decimal precio)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre del paquete.";
            if (precio <= 0) return "El precio debe ser mayor que cero.";

            repositorio.NuevoPaquete(nombre.Trim(), descripcion, precio);
            auditoria.Registrar(actor, "Tarifas", "Crear paquete", nombre.Trim());
            return null;
        }

        public string NuevaPromocion(string actorId, string nombre, decimal descuento, DateTime? inicio, DateTime? fin, bool activa)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre de la promoción.";
            if (descuento <= 0 || descuento > 100) return "El descuento debe estar entre 0 y 100.";
            if (!inicio.HasValue || !fin.HasValue) return "Selecciona las fechas.";
            if (fin.Value.Date < inicio.Value.Date) return "La fecha final no puede ser anterior a la inicial.";

            repositorio.NuevaPromocion(nombre.Trim(), descuento, inicio.Value, fin.Value, activa);
            auditoria.Registrar(actor, "Tarifas", "Crear promoción", nombre.Trim());
            return null;
        }

        public string NuevoPrecioCanal(string actorId, TarifaVista tarifa, TemporadaVista temporada, CanalVista canal, decimal precio)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (tarifa == null || temporada == null || canal == null) return "Selecciona la tarifa, la temporada y el canal.";
            if (precio <= 0) return "El precio debe ser mayor que cero.";

            repositorio.NuevoPrecioCanal(tarifa.Id, temporada.Id, canal.Id, precio);
            auditoria.Registrar(actor, "Tarifas", "Precio por canal", tarifa + " · " + canal.Nombre);
            return null;
        }

        // ---------------------------------------------------------------- Convenios
        public string GuardarConvenio(string actorId, ConvenioVista c)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(c.Nombre)) return "Escribe el nombre del convenio.";
            if (string.IsNullOrWhiteSpace(c.Empresa)) return "Escribe la empresa.";
            if (c.Tarifa < 0 || c.Limite < 0) return "La tarifa y el límite no pueden ser negativos.";

            c.Nombre = c.Nombre.Trim();
            c.Empresa = c.Empresa.Trim();
            bool nuevo = c.Id == 0;
            repositorio.GuardarConvenio(c);
            auditoria.Registrar(actor, "Convenios", nuevo ? "Crear convenio" : "Editar convenio", c.Nombre);
            return null;
        }

        public string RegistrarSaldo(string actorId, ConvenioVista c, decimal saldo, string descripcion)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (c == null) return "Selecciona un convenio.";

            repositorio.RegistrarSaldo(c.Id, saldo, descripcion);
            auditoria.Registrar(actor, "Convenios", "Registrar saldo", c.Nombre + " · " + saldo);
            return null;
        }

        // ---------------------------------------------------------------- Notificaciones y reportes
        public string EnviarNotificacion(string actorId, string titulo, string mensaje, RolVista rol)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(titulo)) return "Escribe el título.";
            if (string.IsNullOrWhiteSpace(mensaje)) return "Escribe el mensaje.";
            if (rol == null) return "Selecciona el rol destinatario.";

            error = repositorio.EnviarNotificacion(titulo.Trim(), mensaje.Trim(), rol.Id);
            if (error == null) auditoria.Registrar(actor, "Notificaciones", "Enviar", titulo.Trim() + " → " + rol.Nombre);
            return error;
        }

        public string NuevoReporte(string actorId, string nombre, string tipo, string formato, string frecuencia, string correo, bool activo)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre del reporte.";
            if (string.IsNullOrWhiteSpace(tipo) || string.IsNullOrWhiteSpace(formato) || string.IsNullOrWhiteSpace(frecuencia)) return "Elige el tipo, el formato y la frecuencia.";
            if (!string.IsNullOrWhiteSpace(correo) && !correo.Contains("@")) return "El correo no es válido.";

            repositorio.NuevoReporte(nombre.Trim(), tipo, formato, frecuencia, correo, activo);
            auditoria.Registrar(actor, "Notificaciones", "Programar reporte", nombre.Trim());
            return null;
        }
    }
}

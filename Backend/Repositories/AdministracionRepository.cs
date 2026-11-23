using System;
using System.Collections.Generic;
using System.Linq;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;
using NpgsqlTypes;

namespace Crown__.Backend.Repositories
{
    // Inventario de habitaciones, tarifas, convenios, notificaciones y reportes.
    public class AdministracionRepository
    {
        static NpgsqlParameter Fecha(string nombre, DateTime valor) { return new NpgsqlParameter(nombre, NpgsqlDbType.Date) { Value = valor.Date }; }
        static NpgsqlParameter Nulo(string nombre, string valor) { return new NpgsqlParameter(nombre, NpgsqlDbType.Varchar) { Value = string.IsNullOrWhiteSpace(valor) ? DBNull.Value : (object)valor.Trim() }; }
        bool Existe(string sql, params NpgsqlParameter[] p) { return ConexionDb.Consultar(sql, r => r.GetInt32(0), p).Any(); }

        // ---------------------------------------------------------------- Inventario

        public List<PisoVista> Pisos()
        {
            return ConexionDb.Consultar("SELECT piso_id, nombre FROM public.piso ORDER BY nombre", r => new PisoVista { Id = r.GetInt32(0), Nombre = r.GetString(1) });
        }

        public bool ExistePiso(string nombre) { return Existe("SELECT 1 FROM public.piso WHERE lower(nombre) = lower(@n)", new NpgsqlParameter("n", nombre)); }

        public void NuevoPiso(string nombre)
        {
            ConexionDb.Ejecutar("INSERT INTO public.piso (hotel_id, nombre) VALUES ((SELECT MIN(hotel_id) FROM public.hotel), @n)", new NpgsqlParameter("n", nombre));
        }

        public List<TipoHabitacionVista> Tipos()
        {
            return ConexionDb.Consultar("SELECT tipo_habitacion_id, nombre, capacidad FROM public.tipohabitacion ORDER BY capacidad, nombre",
                r => new TipoHabitacionVista { Id = r.GetInt32(0), Nombre = r.GetString(1), Capacidad = r.GetInt32(2) });
        }

        public bool ExisteTipo(string nombre) { return Existe("SELECT 1 FROM public.tipohabitacion WHERE lower(nombre) = lower(@n)", new NpgsqlParameter("n", nombre)); }

        public void NuevoTipo(string nombre, int capacidad)
        {
            ConexionDb.Ejecutar("INSERT INTO public.tipohabitacion (nombre, capacidad) VALUES (@n, @c)", new NpgsqlParameter("n", nombre), new NpgsqlParameter("c", capacidad));
        }

        public List<HabitacionGestion> Habitaciones()
        {
            return ConexionDb.Consultar(
                @"SELECT h.habitacion_id, h.piso_id, h.numero, h.tipo_habitacion_id, t.nombre, h.estado
                  FROM public.habitacion h JOIN public.tipohabitacion t ON t.tipo_habitacion_id = h.tipo_habitacion_id ORDER BY h.numero",
                r => new HabitacionGestion { Id = r.GetInt32(0), PisoId = r.GetInt32(1), Numero = r.GetString(2), TipoId = r.GetInt32(3), Tipo = r.GetString(4), Estado = EstadosHabitacion.ParaPantalla(r.GetString(5)) });
        }

        public bool ExisteHabitacion(int pisoId, string numero) { return Existe("SELECT 1 FROM public.habitacion WHERE piso_id = @p AND numero = @n", new NpgsqlParameter("p", pisoId), new NpgsqlParameter("n", numero)); }

        public void NuevaHabitacion(int pisoId, int tipoId, string numero)
        {
            ConexionDb.Ejecutar("INSERT INTO public.habitacion (piso_id, tipo_habitacion_id, numero, estado) VALUES (@p, @t, @n, 'DISPONIBLE')",
                new NpgsqlParameter("p", pisoId), new NpgsqlParameter("t", tipoId), new NpgsqlParameter("n", numero));
        }

        public void CambiarTipo(int habitacionId, int tipoId)
        {
            ConexionDb.Ejecutar("UPDATE public.habitacion SET tipo_habitacion_id = @t WHERE habitacion_id = @id", new NpgsqlParameter("t", tipoId), new NpgsqlParameter("id", habitacionId));
        }

        public List<HistorialEstadoVista> Historial(int habitacionId, DateTime? desde, DateTime? hasta)
        {
            return ConexionDb.Consultar(
                @"SELECT estado, fecha_inicio, fecha_fin, COALESCE(motivo, '') FROM public.habitacionestadohistorial
                  WHERE habitacion_id = @h AND (@desde::date IS NULL OR fecha_inicio::date >= @desde::date) AND (@hasta::date IS NULL OR fecha_inicio::date <= @hasta::date)
                  ORDER BY fecha_inicio DESC, historial_id DESC",
                r => new HistorialEstadoVista { Estado = EstadosHabitacion.ParaPantalla(r.GetString(0)), Inicio = r.GetDateTime(1), Fin = r.IsDBNull(2) ? (DateTime?)null : r.GetDateTime(2), Motivo = r.GetString(3) },
                new NpgsqlParameter("h", habitacionId),
                new NpgsqlParameter("desde", NpgsqlDbType.Date) { Value = desde.HasValue ? (object)desde.Value.Date : DBNull.Value },
                new NpgsqlParameter("hasta", NpgsqlDbType.Date) { Value = hasta.HasValue ? (object)hasta.Value.Date : DBNull.Value });
        }

        public List<BloqueoVista> Bloqueos()
        {
            return ConexionDb.Consultar(
                @"SELECT h.numero, b.fecha_inicio, b.fecha_fin, COALESCE(b.motivo, '') FROM public.bloqueohabitacion b
                  JOIN public.habitacion h ON h.habitacion_id = b.habitacion_id ORDER BY b.fecha_inicio DESC",
                r => new BloqueoVista { Habitacion = r.GetString(0), Inicio = r.GetDateTime(1), Fin = r.GetDateTime(2), Motivo = r.GetString(3) });
        }

        // Guarda el bloqueo y, si cubre hoy y la habitación está libre, la pasa a Bloqueada.
        public string NuevoBloqueo(HabitacionGestion habitacion, DateTime desde, DateTime hasta, string motivo, int usuarioId)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    ReservaRepository.Escalar(conn, tx,
                        "INSERT INTO public.bloqueohabitacion (habitacion_id, fecha_inicio, fecha_fin, motivo) VALUES (@h, @d, @f, @m)",
                        new NpgsqlParameter("h", habitacion.Id), new NpgsqlParameter("d", NpgsqlDbType.Timestamp) { Value = desde.Date },
                        new NpgsqlParameter("f", NpgsqlDbType.Timestamp) { Value = hasta.Date.AddDays(1).AddSeconds(-1) }, Nulo("m", motivo));
                    string error = null;
                    if (desde.Date <= DateTime.Today && hasta.Date >= DateTime.Today && habitacion.Estado == "Disponible")
                        error = HabitacionesRepository.CambiarEstadoEn(conn, tx, habitacion.Numero, "Bloqueada", usuarioId, string.IsNullOrWhiteSpace(motivo) ? "Bloqueo" : motivo, true);
                    if (error == null) tx.Commit();
                    return error;
                }
            }
        }

        // ---------------------------------------------------------------- Tarifas

        public List<TarifaVista> Tarifas()
        {
            return ConexionDb.Consultar(
                "SELECT f.tarifa_id, t.nombre, f.nombre, f.precio_base FROM public.tarifa f JOIN public.tipohabitacion t ON t.tipo_habitacion_id = f.tipo_habitacion_id ORDER BY t.capacidad, t.nombre, f.nombre",
                r => new TarifaVista { Id = r.GetInt32(0), Tipo = r.GetString(1), Nombre = r.GetString(2), PrecioBase = r.GetDecimal(3) });
        }

        public void NuevaTarifa(int tipoId, string nombre, decimal precio)
        {
            ConexionDb.Ejecutar("INSERT INTO public.tarifa (tipo_habitacion_id, nombre, precio_base) VALUES (@t, @n, @p)",
                new NpgsqlParameter("t", tipoId), new NpgsqlParameter("n", nombre), new NpgsqlParameter("p", precio));
        }

        public void CambiarPrecio(int tarifaId, decimal precio)
        {
            ConexionDb.Ejecutar("UPDATE public.tarifa SET precio_base = @p WHERE tarifa_id = @id", new NpgsqlParameter("p", precio), new NpgsqlParameter("id", tarifaId));
        }

        public List<TemporadaVista> Temporadas()
        {
            return ConexionDb.Consultar("SELECT temporada_id, nombre, fecha_inicio, fecha_fin FROM public.temporada ORDER BY fecha_inicio",
                r => new TemporadaVista { Id = r.GetInt32(0), Nombre = r.GetString(1), Inicio = r.GetDateTime(2), Fin = r.GetDateTime(3) });
        }

        public void NuevaTemporada(string nombre, DateTime inicio, DateTime fin)
        {
            ConexionDb.Ejecutar("INSERT INTO public.temporada (nombre, fecha_inicio, fecha_fin) VALUES (@n, @i, @f)", new NpgsqlParameter("n", nombre), Fecha("i", inicio), Fecha("f", fin));
        }

        public List<PaqueteVista> Paquetes()
        {
            return ConexionDb.Consultar("SELECT nombre, COALESCE(descripcion, ''), precio FROM public.paquete ORDER BY nombre",
                r => new PaqueteVista { Nombre = r.GetString(0), Descripcion = r.GetString(1), Precio = r.GetDecimal(2) });
        }

        public void NuevoPaquete(string nombre, string descripcion, decimal precio)
        {
            ConexionDb.Ejecutar("INSERT INTO public.paquete (nombre, descripcion, precio) VALUES (@n, @d, @p)", new NpgsqlParameter("n", nombre), Nulo("d", descripcion), new NpgsqlParameter("p", precio));
        }

        public List<PromocionVista> Promociones()
        {
            return ConexionDb.Consultar("SELECT nombre, descuento, fecha_inicio, fecha_fin, COALESCE(estado, '') FROM public.promocion ORDER BY fecha_inicio DESC",
                r => new PromocionVista { Nombre = r.GetString(0), Descuento = r.GetDecimal(1), Inicio = r.GetDateTime(2), Fin = r.GetDateTime(3), Estado = r.GetString(4) == "ACTIVA" ? "Activa" : "Inactiva" });
        }

        public void NuevaPromocion(string nombre, decimal descuento, DateTime inicio, DateTime fin, bool activa)
        {
            ConexionDb.Ejecutar("INSERT INTO public.promocion (nombre, descuento, fecha_inicio, fecha_fin, estado) VALUES (@n, @d, @i, @f, @e)",
                new NpgsqlParameter("n", nombre), new NpgsqlParameter("d", descuento), Fecha("i", inicio), Fecha("f", fin), new NpgsqlParameter("e", activa ? "ACTIVA" : "INACTIVA"));
        }

        public List<PrecioCanalVista> PreciosPorCanal()
        {
            return ConexionDb.Consultar(
                @"SELECT t.nombre || ' · ' || f.nombre, s.nombre, c.nombre, tc.precio FROM public.tarifaconfiguracion tc
                  JOIN public.tarifa f ON f.tarifa_id = tc.tarifa_id JOIN public.tipohabitacion t ON t.tipo_habitacion_id = f.tipo_habitacion_id
                  JOIN public.temporada s ON s.temporada_id = tc.temporada_id JOIN public.canalreserva c ON c.canal_id = tc.canal_id ORDER BY 1, 2, 3",
                r => new PrecioCanalVista { Tarifa = r.GetString(0), Temporada = r.GetString(1), Canal = r.GetString(2), Precio = r.GetDecimal(3) });
        }

        public void NuevoPrecioCanal(int tarifaId, int temporadaId, int canalId, decimal precio)
        {
            ConexionDb.Ejecutar("INSERT INTO public.tarifaconfiguracion (tarifa_id, temporada_id, canal_id, precio) VALUES (@t, @s, @c, @p)",
                new NpgsqlParameter("t", tarifaId), new NpgsqlParameter("s", temporadaId), new NpgsqlParameter("c", canalId), new NpgsqlParameter("p", precio));
        }

        // ---------------------------------------------------------------- Convenios

        public List<ConvenioVista> Convenios()
        {
            return ConexionDb.Consultar(
                "SELECT convenio_id, nombre, empresa, COALESCE(tarifa_preferencial, 0), COALESCE(limite_credito, 0), fecha_vencimiento, COALESCE(estado, '') FROM public.convenio ORDER BY nombre",
                r => new ConvenioVista { Id = r.GetInt32(0), Nombre = r.GetString(1), Empresa = r.GetString(2), Tarifa = r.GetDecimal(3), Limite = r.GetDecimal(4), Vence = r.IsDBNull(5) ? (DateTime?)null : r.GetDateTime(5), Estado = r.GetString(6) == "ACTIVO" ? "Activo" : "Inactivo" });
        }

        public int GuardarConvenio(ConvenioVista c)
        {
            var parametros = new[]
            {
                new NpgsqlParameter("n", c.Nombre), new NpgsqlParameter("e", c.Empresa), new NpgsqlParameter("t", c.Tarifa), new NpgsqlParameter("l", c.Limite),
                new NpgsqlParameter("v", NpgsqlDbType.Date) { Value = c.Vence.HasValue ? (object)c.Vence.Value.Date : DBNull.Value },
                new NpgsqlParameter("s", c.Estado == "Activo" ? "ACTIVO" : "INACTIVO"), new NpgsqlParameter("id", c.Id)
            };
            var ids = ConexionDb.Consultar(c.Id == 0
                ? "INSERT INTO public.convenio (nombre, empresa, tarifa_preferencial, limite_credito, fecha_vencimiento, estado) VALUES (@n, @e, @t, @l, @v, @s) RETURNING convenio_id"
                : "UPDATE public.convenio SET nombre = @n, empresa = @e, tarifa_preferencial = @t, limite_credito = @l, fecha_vencimiento = @v, estado = @s WHERE convenio_id = @id RETURNING convenio_id",
                r => r.GetInt32(0), parametros);
            return ids.Count == 0 ? c.Id : ids[0];
        }

        public List<ConvenioReservaVista> ReservasDelConvenio(int convenioId)
        {
            return ConexionDb.Consultar(
                @"SELECT cr.reserva_id, hu.nombre || ' ' || hu.apellido, r.fecha_entrada, r.fecha_salida, r.estado FROM public.convenioreserva cr
                  JOIN public.reserva r ON r.reserva_id = cr.reserva_id JOIN public.huesped hu ON hu.huesped_id = r.huesped_principal_id WHERE cr.convenio_id = @c",
                r => new ConvenioReservaVista { Reserva = ReservaRegistro.CodigoDe(r.GetInt32(0)), Huesped = r.GetString(1), Entrada = r.GetDateTime(2), Salida = r.GetDateTime(3), Estado = ReservaRepository.ParaPantalla(r.GetString(4)) },
                new NpgsqlParameter("c", convenioId));
        }

        public List<ConvenioSaldoVista> SaldosDelConvenio(int convenioId)
        {
            return ConexionDb.Consultar(
                @"SELECT c.nombre, h.saldo, COALESCE(h.descripcion, ''), COALESCE(h.fecha, LOCALTIMESTAMP) FROM public.conveniohistorial h
                  JOIN public.convenio c ON c.convenio_id = h.convenio_id WHERE h.convenio_id = @c ORDER BY h.fecha DESC",
                r => new ConvenioSaldoVista { Convenio = r.GetString(0), Saldo = r.GetDecimal(1), Descripcion = r.GetString(2), Fecha = r.GetDateTime(3) },
                new NpgsqlParameter("c", convenioId));
        }

        public void RegistrarSaldo(int convenioId, decimal saldo, string descripcion)
        {
            ConexionDb.Ejecutar("INSERT INTO public.conveniohistorial (convenio_id, saldo, fecha, descripcion) VALUES (@c, @s, LOCALTIMESTAMP, @d)",
                new NpgsqlParameter("c", convenioId), new NpgsqlParameter("s", saldo), Nulo("d", descripcion));
        }

        // ---------------------------------------------------------------- Notificaciones y reportes

        public List<RolVista> Roles()
        {
            return ConexionDb.Consultar("SELECT rol_id, nombre FROM public.rol ORDER BY nombre", r => new RolVista { Id = r.GetInt32(0), Nombre = r.GetString(1) });
        }

        public List<NotificacionVista> Notificaciones()
        {
            return ConexionDb.Consultar("SELECT notificacion_id, titulo, mensaje, COALESCE(fecha, LOCALTIMESTAMP) FROM public.notificacion ORDER BY fecha DESC, notificacion_id DESC",
                r => new NotificacionVista { Id = r.GetInt32(0), Titulo = r.GetString(1), Mensaje = r.GetString(2), Fecha = r.GetDateTime(3) });
        }

        public List<DestinatarioVista> Destinatarios(int notificacionId)
        {
            return ConexionDb.Consultar(
                @"SELECT u.username, COALESCE(n.fecha_envio, LOCALTIMESTAMP), n.fecha_recepcion, COALESCE(n.estado, '') FROM public.notificacionrecepcion n
                  JOIN public.usuario u ON u.usuario_id = n.usuario_id WHERE n.notificacion_id = @n ORDER BY u.username",
                r => new DestinatarioVista { Usuario = r.GetString(0), Enviada = r.GetDateTime(1), Recibida = r.IsDBNull(2) ? (DateTime?)null : r.GetDateTime(2), Estado = r.GetString(3) == "LEIDA" ? "Leída" : r.GetString(3) == "ENVIADA" ? "Enviada" : r.GetString(3) },
                new NpgsqlParameter("n", notificacionId));
        }

        // Crea la notificación y la envía a todos los usuarios activos del rol.
        public string EnviarNotificacion(string titulo, string mensaje, int rolId)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    int id = (int)ReservaRepository.Escalar(conn, tx,
                        "INSERT INTO public.notificacion (titulo, mensaje, fecha) VALUES (@t, @m, LOCALTIMESTAMP) RETURNING notificacion_id",
                        new NpgsqlParameter("t", titulo), new NpgsqlParameter("m", mensaje));
                    ReservaRepository.Escalar(conn, tx, "INSERT INTO public.notificacionrol (notificacion_id, rol_id) VALUES (@n, @r)", new NpgsqlParameter("n", id), new NpgsqlParameter("r", rolId));
                    ReservaRepository.Escalar(conn, tx,
                        @"INSERT INTO public.notificacionrecepcion (notificacion_id, usuario_id, fecha_envio, estado)
                          SELECT @n, ur.usuario_id, LOCALTIMESTAMP, 'ENVIADA' FROM public.usuariorol ur JOIN public.usuario u ON u.usuario_id = ur.usuario_id
                          WHERE ur.rol_id = @r AND u.estado = 'ACTIVO'",
                        new NpgsqlParameter("n", id), new NpgsqlParameter("r", rolId));
                    tx.Commit();
                    return null;
                }
            }
        }

        public List<ReporteVista> Reportes()
        {
            return ConexionDb.Consultar("SELECT reporte_id, nombre, tipo_reporte, formato, frecuencia, COALESCE(correo_destino, ''), COALESCE(activo, true) FROM public.reporteprogramado ORDER BY nombre",
                r => new ReporteVista { Id = r.GetInt32(0), Nombre = r.GetString(1), Tipo = r.GetString(2), Formato = r.GetString(3), Frecuencia = r.GetString(4), Correo = r.GetString(5), Activo = r.GetBoolean(6) });
        }

        public void NuevoReporte(string nombre, string tipo, string formato, string frecuencia, string correo, bool activo)
        {
            ConexionDb.Ejecutar("INSERT INTO public.reporteprogramado (nombre, tipo_reporte, formato, frecuencia, correo_destino, activo) VALUES (@n, @t, @f, @q, @c, @a)",
                new NpgsqlParameter("n", nombre), new NpgsqlParameter("t", tipo), new NpgsqlParameter("f", formato), new NpgsqlParameter("q", frecuencia), Nulo("c", correo), new NpgsqlParameter("a", activo));
        }

        public List<EjecucionVista> Ejecuciones(int? reporteId, string estado, DateTime? desde, DateTime? hasta)
        {
            return ConexionDb.Consultar(
                @"SELECT p.nombre, COALESCE(e.fecha_ejecucion, LOCALTIMESTAMP), e.estado, COALESCE(e.archivo_url, '') FROM public.reporteejecucion e
                  JOIN public.reporteprogramado p ON p.reporte_id = e.reporte_id
                  WHERE (@r::int IS NULL OR e.reporte_id = @r) AND (@e::text IS NULL OR e.estado = @e)
                    AND (@d::date IS NULL OR e.fecha_ejecucion::date >= @d) AND (@h::date IS NULL OR e.fecha_ejecucion::date <= @h) ORDER BY e.fecha_ejecucion DESC",
                r => new EjecucionVista { Reporte = r.GetString(0), Ejecutado = r.GetDateTime(1), Estado = r.GetString(2), Archivo = r.GetString(3) },
                new NpgsqlParameter("r", NpgsqlDbType.Integer) { Value = reporteId.HasValue ? (object)reporteId.Value : DBNull.Value },
                new NpgsqlParameter("e", NpgsqlDbType.Text) { Value = string.IsNullOrEmpty(estado) ? DBNull.Value : (object)estado },
                new NpgsqlParameter("d", NpgsqlDbType.Date) { Value = desde.HasValue ? (object)desde.Value.Date : DBNull.Value },
                new NpgsqlParameter("h", NpgsqlDbType.Date) { Value = hasta.HasValue ? (object)hasta.Value.Date : DBNull.Value });
        }
    }
}

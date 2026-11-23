using System;
using System.Collections.Generic;
using System.Linq;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;
using NpgsqlTypes;

namespace Crown__.Backend.Repositories
{
    // Usuarios, empleados, roles y la configuración del hotel.
    public class SeguridadRepository
    {
        static NpgsqlParameter Nulo(string nombre, string valor) { return new NpgsqlParameter(nombre, NpgsqlDbType.Varchar) { Value = string.IsNullOrWhiteSpace(valor) ? DBNull.Value : (object)valor.Trim() }; }
        bool Existe(string sql, params NpgsqlParameter[] p) { return ConexionDb.Consultar(sql, r => r.GetInt32(0), p).Any(); }
        const string Hotel = "(SELECT MIN(hotel_id) FROM public.hotel)";

        // ---------------------------------------------------------------- Seguridad

        public List<UsuarioGestion> Usuarios()
        {
            return ConexionDb.Consultar(
                @"SELECT u.usuario_id, u.username, COALESCE(e.nombre || ' ' || e.apellido, '—'),
                         COALESCE((SELECT string_agg(r.nombre, ', ' ORDER BY r.nombre) FROM public.usuariorol ur JOIN public.rol r ON r.rol_id = ur.rol_id WHERE ur.usuario_id = u.usuario_id), ''),
                         COALESCE(u.estado, 'ACTIVO')
                  FROM public.usuario u LEFT JOIN public.empleado e ON e.empleado_id = u.empleado_id ORDER BY u.username",
                r => new UsuarioGestion { Id = r.GetInt32(0), Username = r.GetString(1), Empleado = r.GetString(2), Roles = r.GetString(3), Estado = r.GetString(4) == "ACTIVO" ? "Activo" : "Inactivo" });
        }

        public bool ExisteUsuario(string username) { return Existe("SELECT 1 FROM public.usuario WHERE lower(username) = lower(@u)", new NpgsqlParameter("u", username)); }

        public void NuevoUsuario(string username, int? empleadoId, string hash, bool activo)
        {
            ConexionDb.Ejecutar("INSERT INTO public.usuario (empleado_id, username, password_hash, estado) VALUES (@e, @u, @h, @s)",
                new NpgsqlParameter("e", NpgsqlDbType.Integer) { Value = empleadoId.HasValue ? (object)empleadoId.Value : DBNull.Value },
                new NpgsqlParameter("u", username), new NpgsqlParameter("h", hash), new NpgsqlParameter("s", activo ? "ACTIVO" : "INACTIVO"));
        }

        public void CambiarPassword(int usuarioId, string hash)
        {
            ConexionDb.Ejecutar("UPDATE public.usuario SET password_hash = @h WHERE usuario_id = @id", new NpgsqlParameter("h", hash), new NpgsqlParameter("id", usuarioId));
        }

        public int AdministradoresActivos(int exceptoUsuarioId)
        {
            return ConexionDb.Consultar(
                @"SELECT COUNT(*) FROM public.usuario u JOIN public.usuariorol ur ON ur.usuario_id = u.usuario_id JOIN public.rol r ON r.rol_id = ur.rol_id
                  WHERE r.nombre = 'Administrador' AND u.estado = 'ACTIVO' AND u.usuario_id <> @id",
                r => Convert.ToInt32(r.GetInt64(0)), new NpgsqlParameter("id", exceptoUsuarioId)).First();
        }

        public List<EmpleadoFicha> Empleados()
        {
            return ConexionDb.Consultar("SELECT empleado_id, nombre, apellido, COALESCE(email, ''), COALESCE(telefono, '') FROM public.empleado ORDER BY nombre, apellido",
                r => new EmpleadoFicha { Id = r.GetInt32(0), Nombre = r.GetString(1), Apellido = r.GetString(2), Correo = r.GetString(3), Telefono = r.GetString(4) });
        }

        public void NuevoEmpleado(string nombre, string apellido, string correo, string telefono)
        {
            ConexionDb.Ejecutar("INSERT INTO public.empleado (nombre, apellido, email, telefono) VALUES (@n, @a, @e, @t)",
                new NpgsqlParameter("n", nombre), new NpgsqlParameter("a", apellido), Nulo("e", correo), Nulo("t", telefono));
        }

        public List<RolGestion> Roles()
        {
            return ConexionDb.Consultar(
                @"SELECT r.rol_id, r.nombre, COALESCE(r.descripcion, ''), (SELECT COUNT(*) FROM public.usuariorol ur WHERE ur.rol_id = r.rol_id) FROM public.rol r ORDER BY r.nombre",
                r => new RolGestion { Id = r.GetInt32(0), Nombre = r.GetString(1), Descripcion = r.GetString(2), Usuarios = Convert.ToInt32(r.GetInt64(3)) });
        }

        public bool ExisteRol(string nombre) { return Existe("SELECT 1 FROM public.rol WHERE lower(nombre) = lower(@n)", new NpgsqlParameter("n", nombre)); }

        public void NuevoRol(string nombre, string descripcion)
        {
            ConexionDb.Ejecutar("INSERT INTO public.rol (nombre, descripcion) VALUES (@n, @d)", new NpgsqlParameter("n", nombre), Nulo("d", descripcion));
        }

        public List<AsignacionVista> Asignaciones()
        {
            return ConexionDb.Consultar(
                @"SELECT u.usuario_id, r.rol_id, u.username, r.nombre FROM public.usuariorol ur JOIN public.usuario u ON u.usuario_id = ur.usuario_id
                  JOIN public.rol r ON r.rol_id = ur.rol_id ORDER BY u.username, r.nombre",
                r => new AsignacionVista { UsuarioId = r.GetInt32(0), RolId = r.GetInt32(1), Usuario = r.GetString(2), Rol = r.GetString(3) });
        }

        public bool TieneRol(int usuarioId, int rolId) { return Existe("SELECT 1 FROM public.usuariorol WHERE usuario_id = @u AND rol_id = @r", new NpgsqlParameter("u", usuarioId), new NpgsqlParameter("r", rolId)); }

        public void AsignarRol(int usuarioId, int rolId)
        {
            ConexionDb.Ejecutar("INSERT INTO public.usuariorol (usuario_id, rol_id) VALUES (@u, @r)", new NpgsqlParameter("u", usuarioId), new NpgsqlParameter("r", rolId));
        }

        public void QuitarRol(int usuarioId, int rolId)
        {
            ConexionDb.Ejecutar("DELETE FROM public.usuariorol WHERE usuario_id = @u AND rol_id = @r", new NpgsqlParameter("u", usuarioId), new NpgsqlParameter("r", rolId));
        }

        // ---------------------------------------------------------------- Configuración

        public HotelVista DatosHotel()
        {
            return ConexionDb.Consultar("SELECT hotel_id, nombre, COALESCE(direccion, ''), categoria, COALESCE(telefono, ''), COALESCE(email, '') FROM public.hotel ORDER BY hotel_id LIMIT 1",
                r => new HotelVista { Id = r.GetInt32(0), Nombre = r.GetString(1), Direccion = r.GetString(2), Categoria = r.GetInt16(3), Telefono = r.GetString(4), Correo = r.GetString(5) }).FirstOrDefault();
        }

        public void GuardarHotel(HotelVista h)
        {
            ConexionDb.Ejecutar("UPDATE public.hotel SET nombre = @n, direccion = @d, categoria = @c, telefono = @t, email = @e WHERE hotel_id = @id",
                new NpgsqlParameter("n", h.Nombre), Nulo("d", h.Direccion), new NpgsqlParameter("c", (short)h.Categoria), Nulo("t", h.Telefono), Nulo("e", h.Correo), new NpgsqlParameter("id", h.Id));
        }

        public string ValorDe(string clave)
        {
            return ConexionDb.Consultar("SELECT valor FROM public.configuracion WHERE clave = @c ORDER BY configuracion_id LIMIT 1", r => r.IsDBNull(0) ? "" : r.GetString(0), new NpgsqlParameter("c", clave)).FirstOrDefault();
        }

        public List<ParametroVista> Parametros()
        {
            return ConexionDb.Consultar("SELECT clave, COALESCE(valor, ''), COALESCE(fecha_actualizacion, LOCALTIMESTAMP) FROM public.configuracion ORDER BY clave",
                r => new ParametroVista { Clave = r.GetString(0), Valor = r.GetString(1), Actualizado = r.GetDateTime(2) });
        }

        public void GuardarParametro(string clave, string valor)
        {
            ConexionDb.Ejecutar(
                @"WITH u AS (UPDATE public.configuracion SET valor = @v, fecha_actualizacion = LOCALTIMESTAMP WHERE clave = @c RETURNING 1)
                  INSERT INTO public.configuracion (hotel_id, clave, valor, fecha_actualizacion) SELECT " + Hotel + @", @c, @v, LOCALTIMESTAMP WHERE NOT EXISTS (SELECT 1 FROM u)",
                new NpgsqlParameter("c", clave), new NpgsqlParameter("v", valor));
        }

        public List<PoliticaVista> Politicas()
        {
            return ConexionDb.Consultar("SELECT nombre, COALESCE(valor, ''), COALESCE(descripcion, '') FROM public.politica ORDER BY nombre",
                r => new PoliticaVista { Nombre = r.GetString(0), Valor = r.GetString(1), Descripcion = r.GetString(2) });
        }

        public void GuardarPolitica(string nombre, string valor, string descripcion)
        {
            ConexionDb.Ejecutar(
                @"WITH u AS (UPDATE public.politica SET valor = @v, descripcion = @d WHERE nombre = @n RETURNING 1)
                  INSERT INTO public.politica (hotel_id, nombre, descripcion, valor) SELECT " + Hotel + @", @n, @d, @v WHERE NOT EXISTS (SELECT 1 FROM u)",
                new NpgsqlParameter("n", nombre), new NpgsqlParameter("v", valor), Nulo("d", descripcion));
        }

        public List<ImpuestoVista> Impuestos()
        {
            return ConexionDb.Consultar("SELECT nombre, porcentaje, COALESCE(activo, true) FROM public.impuesto ORDER BY nombre",
                r => new ImpuestoVista { Nombre = r.GetString(0), Porcentaje = r.GetDecimal(1), Activo = r.GetBoolean(2) });
        }

        public void GuardarImpuesto(string nombre, decimal porcentaje, bool activo)
        {
            ConexionDb.Ejecutar(
                @"WITH u AS (UPDATE public.impuesto SET porcentaje = @p, activo = @a WHERE nombre = @n RETURNING 1)
                  INSERT INTO public.impuesto (hotel_id, nombre, porcentaje, activo) SELECT " + Hotel + @", @n, @p, @a WHERE NOT EXISTS (SELECT 1 FROM u)",
                new NpgsqlParameter("n", nombre), new NpgsqlParameter("p", porcentaje), new NpgsqlParameter("a", activo));
        }

        public List<ModuloVista> Modulos()
        {
            return ConexionDb.Consultar(
                @"SELECT m.modulo_id, m.nombre, COALESCE(m.descripcion, ''), COALESCE(hm.activo, false) FROM public.modulo m
                  LEFT JOIN public.hotelmodulo hm ON hm.modulo_id = m.modulo_id AND hm.hotel_id = " + Hotel + " ORDER BY m.nombre",
                r => new ModuloVista { Id = r.GetInt32(0), Nombre = r.GetString(1), Descripcion = r.GetString(2), Activo = r.GetBoolean(3) });
        }

        public void CambiarModulo(int moduloId, bool activo)
        {
            ConexionDb.Ejecutar(
                @"WITH u AS (UPDATE public.hotelmodulo SET activo = @a WHERE modulo_id = @m AND hotel_id = " + Hotel + @" RETURNING 1)
                  INSERT INTO public.hotelmodulo (hotel_id, modulo_id, activo) SELECT " + Hotel + @", @m, @a WHERE NOT EXISTS (SELECT 1 FROM u)",
                new NpgsqlParameter("m", moduloId), new NpgsqlParameter("a", activo));
        }

        public List<AuditoriaFila> Auditoria(string usuario, DateTime? desde, DateTime? hasta)
        {
            return ConexionDb.Consultar(
                @"SELECT COALESCE(a.fecha, LOCALTIMESTAMP), COALESCE(u.username, '—'), a.cambio, COALESCE(a.valor_anterior, ''), COALESCE(a.valor_nuevo, '')
                  FROM public.configuracionaudit a LEFT JOIN public.usuario u ON u.usuario_id = a.usuario_id
                  WHERE (@u::text IS NULL OR lower(u.username) LIKE '%' || lower(@u) || '%')
                    AND (@d::date IS NULL OR a.fecha::date >= @d) AND (@h::date IS NULL OR a.fecha::date <= @h)
                  ORDER BY a.fecha DESC, a.audit_id DESC LIMIT 300",
                r => new AuditoriaFila { Fecha = r.GetDateTime(0), Usuario = r.GetString(1), Cambio = r.GetString(2), Anterior = r.GetString(3), Nuevo = r.GetString(4) },
                new NpgsqlParameter("u", NpgsqlDbType.Text) { Value = string.IsNullOrWhiteSpace(usuario) ? DBNull.Value : (object)usuario.Trim() },
                new NpgsqlParameter("d", NpgsqlDbType.Date) { Value = desde.HasValue ? (object)desde.Value.Date : DBNull.Value },
                new NpgsqlParameter("h", NpgsqlDbType.Date) { Value = hasta.HasValue ? (object)hasta.Value.Date : DBNull.Value });
        }
    }
}

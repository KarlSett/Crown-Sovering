using System;
using System.Collections.Generic;
using System.Linq;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;

namespace Crown__.Backend.Repositories
{
    public class UsuarioRepository
    {
        // El último acceso sale de la auditoría, porque la tabla usuario no lo guarda.
        private const string Seleccion = @"SELECT u.usuario_id, COALESCE(e.nombre || ' ' || e.apellido, u.username), u.username, u.password_hash,
                                                  COALESCE((SELECT string_agg(r.nombre, ',') FROM public.usuariorol ur
                                                            JOIN public.rol r ON r.rol_id = ur.rol_id WHERE ur.usuario_id = u.usuario_id), ''),
                                                  COALESCE(u.estado, 'ACTIVO') = 'ACTIVO',
                                                  (SELECT MAX(a.fecha) FROM public.configuracionaudit a
                                                   WHERE a.usuario_id = u.usuario_id AND a.cambio = @inicio)
                                           FROM public.usuario u
                                           LEFT JOIN public.empleado e ON e.empleado_id = u.empleado_id";

        private static NpgsqlParameter ParametroInicio()
        {
            return new NpgsqlParameter("inicio", AuditoriaRepository.Cambio("Autenticación", "Inicio de sesión"));
        }

        public Usuario ObtenerPorUsername(string username)
        {
            return ConexionDb.Consultar(Seleccion + " WHERE lower(u.username) = lower(@username)", Mapear,
                ParametroInicio(), new NpgsqlParameter("username", username)).FirstOrDefault();
        }

        public Usuario ObtenerPorId(int id)
        {
            return ConexionDb.Consultar(Seleccion + " WHERE u.usuario_id = @id", Mapear,
                ParametroInicio(), new NpgsqlParameter("id", id)).FirstOrDefault();
        }

        public List<Usuario> ListarTodos()
        {
            return ConexionDb.Consultar(Seleccion + " ORDER BY u.username", Mapear, ParametroInicio());
        }

        public void CambiarActivo(int id, bool activo)
        {
            ConexionDb.Ejecutar("UPDATE public.usuario SET estado = @estado WHERE usuario_id = @id",
                new NpgsqlParameter("id", id), new NpgsqlParameter("estado", activo ? "ACTIVO" : "INACTIVO"));
        }

        private static Usuario Mapear(NpgsqlDataReader r)
        {
            return new Usuario
            {
                Id = r.GetInt32(0),
                NombreCompleto = r.GetString(1),
                Username = r.GetString(2),
                PasswordHash = r.GetString(3),
                Roles = r.GetString(4).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                Activo = r.GetBoolean(5),
                UltimoAcceso = r.IsDBNull(6) ? (DateTime?)null : DateTime.SpecifyKind(r.GetDateTime(6), DateTimeKind.Utc)
            };
        }
    }
}

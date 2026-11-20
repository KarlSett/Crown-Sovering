using System;
using System.Collections.Generic;
using System.Linq;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;
using NpgsqlTypes;

namespace Crown__.Backend.Repositories
{
    // La base nueva no tiene una tabla de auditoría general: se usa configuracionaudit,
    // guardando en "cambio" el módulo y la acción ("Autenticación · Inicio de sesión").
    public class AuditoriaRepository
    {
        private const string Separador = " · ";

        public static string Cambio(string modulo, string accion)
        {
            return modulo + Separador + accion;
        }

        public void Registrar(int? usuarioId, string modulo, string accion, string motivo, string valorAnterior, string valorNuevo)
        {
            string detalle = string.Join(" ", new[] { motivo, valorNuevo }.Where(t => !string.IsNullOrEmpty(t)));
            ConexionDb.Ejecutar(@"INSERT INTO public.configuracionaudit (hotel_id, usuario_id, cambio, valor_anterior, valor_nuevo)
                                  VALUES ((SELECT MIN(hotel_id) FROM public.hotel), @usuario, @cambio, @anterior, @nuevo)",
                new NpgsqlParameter("usuario", NpgsqlDbType.Integer) { Value = usuarioId.HasValue ? (object)usuarioId.Value : DBNull.Value },
                new NpgsqlParameter("cambio", Cambio(modulo, accion)),
                new NpgsqlParameter("anterior", NpgsqlDbType.Text) { Value = (object)valorAnterior ?? DBNull.Value },
                new NpgsqlParameter("nuevo", NpgsqlDbType.Text) { Value = detalle.Length == 0 ? DBNull.Value : (object)detalle });
        }

        // Intentos fallidos de los últimos minutos, contados desde el último inicio de sesión correcto.
        public int FallosRecientes(int usuarioId, string cambioFallido, string cambioCorrecto, int minutos)
        {
            const string sql = @"SELECT COUNT(*) FROM public.configuracionaudit a
                                 WHERE a.usuario_id = @usuario AND a.cambio = @fallido
                                   AND a.fecha > LOCALTIMESTAMP - make_interval(mins => @minutos)
                                   AND a.fecha > COALESCE((SELECT MAX(b.fecha) FROM public.configuracionaudit b
                                                           WHERE b.usuario_id = @usuario AND b.cambio = @correcto), TIMESTAMP '-infinity')";
            return (int)ConexionDb.Consultar(sql, r => r.GetInt64(0),
                new NpgsqlParameter("usuario", usuarioId), new NpgsqlParameter("fallido", cambioFallido),
                new NpgsqlParameter("correcto", cambioCorrecto), new NpgsqlParameter("minutos", minutos)).First();
        }

        public List<AuditoriaRegistro> Ultimos(int limite)
        {
            const string sql = @"SELECT a.audit_id, COALESCE(e.nombre || ' ' || e.apellido, u.username, 'Sistema'), a.cambio,
                                        a.valor_nuevo, COALESCE(a.fecha, LOCALTIMESTAMP)
                                 FROM public.configuracionaudit a
                                 LEFT JOIN public.usuario u ON u.usuario_id = a.usuario_id
                                 LEFT JOIN public.empleado e ON e.empleado_id = u.empleado_id
                                 ORDER BY a.fecha DESC, a.audit_id DESC
                                 LIMIT @limite";
            return ConexionDb.Consultar(sql, r =>
            {
                string cambio = r.GetString(2);
                int corte = cambio.IndexOf(Separador, StringComparison.Ordinal);
                return new AuditoriaRegistro
                {
                    Id = r.GetInt32(0),
                    Usuario = r.GetString(1),
                    Modulo = corte < 0 ? "General" : cambio.Substring(0, corte),
                    Accion = corte < 0 ? cambio : cambio.Substring(corte + Separador.Length),
                    Motivo = r.IsDBNull(3) ? null : r.GetString(3),
                    Fecha = DateTime.SpecifyKind(r.GetDateTime(4), DateTimeKind.Utc)
                };
            }, new NpgsqlParameter("limite", limite));
        }
    }
}

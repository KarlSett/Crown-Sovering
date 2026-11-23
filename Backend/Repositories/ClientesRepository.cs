using System;
using System.Collections.Generic;
using System.Linq;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;
using NpgsqlTypes;

namespace Crown__.Backend.Repositories
{
    // Clientes del hotel = tabla huesped. La base no guarda segmento, alergias ni notas.
    public class ClientesRepository
    {
        public List<ClienteVista> Listar()
        {
            const string sql = @"SELECT huesped_id, nombre, apellido, documento, email, telefono, fecha_nacimiento
                                 FROM public.huesped
                                 ORDER BY apellido, nombre";
            return ConexionDb.Consultar(sql, r => new ClienteVista
            {
                Id = r.GetInt32(0),
                Nombre = r.GetString(1),
                Apellidos = r.GetString(2),
                Documento = r.IsDBNull(3) ? null : r.GetString(3),
                Email = r.IsDBNull(4) ? null : r.GetString(4),
                Telefono = r.IsDBNull(5) ? null : r.GetString(5),
                FechaNacimiento = r.IsDBNull(6) ? (DateTime?)null : r.GetDateTime(6),
                Estancias = new string[0]
            });
        }

        public bool ExisteDocumento(string documento, int exceptoId)
        {
            return ConexionDb.Consultar(
                "SELECT 1 FROM public.huesped WHERE lower(documento) = lower(@documento) AND huesped_id <> @id", r => r.GetInt32(0),
                new NpgsqlParameter("documento", documento), new NpgsqlParameter("id", exceptoId)).Any();
        }

        internal const string SqlCrear = @"INSERT INTO public.huesped (nombre, apellido, documento, email, telefono, fecha_nacimiento)
                                           VALUES (@nombre, @apellido, @documento, @email, @telefono, @nacimiento) RETURNING huesped_id";
        internal const string SqlActualizar = @"UPDATE public.huesped
                                                SET nombre = @nombre, apellido = @apellido, documento = @documento, email = @email,
                                                    telefono = @telefono, fecha_nacimiento = @nacimiento
                                                WHERE huesped_id = @id RETURNING huesped_id";

        // Crea el cliente (id 0) o actualiza uno existente. Devuelve el identificador.
        public int Guardar(ClienteVista cliente)
        {
            var ids = ConexionDb.Consultar(cliente.Id == 0 ? SqlCrear : SqlActualizar, r => r.GetInt32(0), Parametros(cliente));
            return ids.Count == 0 ? cliente.Id : ids[0];
        }

        internal static NpgsqlParameter[] Parametros(ClienteVista cliente)
        {
            string nombres, apellidos;
            if (string.IsNullOrWhiteSpace(cliente.Apellidos))
                NombrePersona.Separar(cliente.Nombre, out nombres, out apellidos);
            else
            {
                nombres = cliente.Nombre.Trim();
                apellidos = cliente.Apellidos.Trim();
            }

            return new[]
            {
                new NpgsqlParameter("nombre", nombres),
                new NpgsqlParameter("apellido", apellidos),
                new NpgsqlParameter("documento", (object)cliente.Documento ?? DBNull.Value),
                new NpgsqlParameter("email", NpgsqlDbType.Varchar) { Value = string.IsNullOrWhiteSpace(cliente.Email) ? DBNull.Value : (object)cliente.Email },
                new NpgsqlParameter("telefono", NpgsqlDbType.Varchar) { Value = string.IsNullOrWhiteSpace(cliente.Telefono) ? DBNull.Value : (object)cliente.Telefono },
                new NpgsqlParameter("nacimiento", NpgsqlDbType.Date) { Value = cliente.FechaNacimiento.HasValue ? (object)cliente.FechaNacimiento.Value.Date : DBNull.Value },
                new NpgsqlParameter("id", cliente.Id)
            };
        }

        // Solo se puede eliminar un huésped que nunca tuvo una reserva.
        public bool TieneReservas(int huespedId)
        {
            return ConexionDb.Consultar(
                "SELECT 1 FROM public.reserva WHERE huesped_principal_id = @id UNION SELECT 1 FROM public.reservahuesped WHERE huesped_id = @id",
                r => r.GetInt32(0), new NpgsqlParameter("id", huespedId)).Any();
        }

        public void Eliminar(int huespedId)
        {
            ConexionDb.Ejecutar("DELETE FROM public.consentimiento WHERE huesped_id = @id", new NpgsqlParameter("id", huespedId));
            ConexionDb.Ejecutar("DELETE FROM public.huesped WHERE huesped_id = @id", new NpgsqlParameter("id", huespedId));
        }

        public List<ConsentimientoVista> Consentimientos(int huespedId)
        {
            return ConexionDb.Consultar(
                "SELECT tipo, autorizado, COALESCE(fecha, LOCALTIMESTAMP) FROM public.consentimiento WHERE huesped_id = @id ORDER BY fecha DESC, consentimiento_id DESC",
                r => new ConsentimientoVista { Tipo = r.GetString(0), Autorizado = r.GetBoolean(1), Fecha = r.GetDateTime(2) },
                new NpgsqlParameter("id", huespedId));
        }

        public void GuardarConsentimiento(int huespedId, string tipo, bool autorizado, DateTime fecha)
        {
            ConexionDb.Ejecutar(
                "INSERT INTO public.consentimiento (huesped_id, tipo, autorizado, fecha) VALUES (@id, @tipo, @autorizado, @fecha)",
                new NpgsqlParameter("id", huespedId), new NpgsqlParameter("tipo", tipo),
                new NpgsqlParameter("autorizado", autorizado), new NpgsqlParameter("fecha", NpgsqlDbType.Timestamp) { Value = fecha });
        }
    }
}

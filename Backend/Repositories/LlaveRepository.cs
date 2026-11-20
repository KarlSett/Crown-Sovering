using System;
using System.Collections.Generic;
using System.Linq;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;

namespace Crown__.Backend.Repositories
{
    // Detalle de la estancia en curso de una reserva y sus llaves.
    public class LlaveRepository
    {
        public EstanciaDetalle Detalle(int reservaId)
        {
            return ConexionDb.Consultar(
                @"SELECT estancia_id, COALESCE(tipo_checkin, 'NORMAL'), COALESCE(tipo_checkout, ''), COALESCE(estado, '')
                  FROM public.estancia WHERE reserva_id = @reserva ORDER BY estancia_id DESC LIMIT 1",
                r => new EstanciaDetalle { EstanciaId = r.GetInt32(0), TipoCheckin = r.GetString(1), TipoCheckout = r.GetString(2), Estado = r.GetString(3) },
                new NpgsqlParameter("reserva", reservaId)).FirstOrDefault();
        }

        public void ActualizarTipos(int estanciaId, string tipoCheckin, string tipoCheckout)
        {
            ConexionDb.Ejecutar("UPDATE public.estancia SET tipo_checkin = @entrada, tipo_checkout = @salida WHERE estancia_id = @id",
                new NpgsqlParameter("entrada", (object)tipoCheckin ?? DBNull.Value),
                new NpgsqlParameter("salida", string.IsNullOrEmpty(tipoCheckout) ? (object)DBNull.Value : tipoCheckout),
                new NpgsqlParameter("id", estanciaId));
        }

        public List<LlaveVista> Llaves(int estanciaId)
        {
            return ConexionDb.Consultar(
                "SELECT llave_id, codigo, COALESCE(fecha_emision, LOCALTIMESTAMP), fecha_devolucion, COALESCE(estado, '') FROM public.llave WHERE estancia_id = @id ORDER BY llave_id",
                r => new LlaveVista
                {
                    Id = r.GetInt32(0),
                    Codigo = r.GetString(1),
                    Emitida = r.GetDateTime(2),
                    Devuelta = r.IsDBNull(3) ? (DateTime?)null : r.GetDateTime(3),
                    Estado = r.GetString(4) == "EMITIDA" ? "Emitida" : r.GetString(4) == "DEVUELTA" ? "Devuelta" : r.GetString(4)
                },
                new NpgsqlParameter("id", estanciaId));
        }

        public bool ExisteLlaveActiva(int estanciaId, string codigo)
        {
            return ConexionDb.Consultar("SELECT 1 FROM public.llave WHERE estancia_id = @id AND codigo = @codigo AND estado = 'EMITIDA'",
                r => r.GetInt32(0), new NpgsqlParameter("id", estanciaId), new NpgsqlParameter("codigo", codigo)).Any();
        }

        public void Emitir(int estanciaId, string codigo)
        {
            ConexionDb.Ejecutar("INSERT INTO public.llave (estancia_id, codigo, fecha_emision, estado) VALUES (@id, @codigo, LOCALTIMESTAMP, 'EMITIDA')",
                new NpgsqlParameter("id", estanciaId), new NpgsqlParameter("codigo", codigo));
        }

        public void Devolver(int llaveId)
        {
            ConexionDb.Ejecutar("UPDATE public.llave SET fecha_devolucion = LOCALTIMESTAMP, estado = 'DEVUELTA' WHERE llave_id = @id AND estado = 'EMITIDA'",
                new NpgsqlParameter("id", llaveId));
        }
    }
}

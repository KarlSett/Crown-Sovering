using System;
using System.Collections.Generic;
using System.Linq;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Npgsql;
using NpgsqlTypes;

namespace Crown__.Backend.Repositories
{
    // Tareas de limpieza e inspección, checklists, incidencias, órdenes de mantenimiento y objetos olvidados.
    // Estados de tarea: PENDIENTE, EN_CURSO, COMPLETADA. Tipos: LIMPIEZA, INSPECCION.
    public class LimpiezaRepository
    {
        public List<TareaLimpieza> Tareas()
        {
            return ConexionDb.Consultar(
                @"SELECT t.tarea_id, t.habitacion_id, h.numero, t.tipo, t.fecha_programada, t.estado, COALESCE(e.nombre || ' ' || e.apellido, '')
                  FROM public.tarealimpieza t JOIN public.habitacion h ON h.habitacion_id = t.habitacion_id
                  LEFT JOIN public.empleado e ON e.empleado_id = t.empleado_id ORDER BY t.fecha_programada, t.tarea_id",
                r => new TareaLimpieza
                {
                    Id = r.GetInt32(0), HabitacionId = r.GetInt32(1), Habitacion = r.GetString(2),
                    Tipo = r.GetString(3) == "INSPECCION" ? "Inspección" : "Limpieza",
                    Programada = r.GetDateTime(4), Estado = r.GetString(5), Empleado = r.GetString(6)
                });
        }

        public List<EmpleadoVista> Empleados()
        {
            return ConexionDb.Consultar("SELECT empleado_id, nombre || ' ' || apellido FROM public.empleado ORDER BY nombre, apellido",
                r => new EmpleadoVista { Id = r.GetInt32(0), Nombre = r.GetString(1) });
        }

        public void NuevaTarea(int habitacionId, string tipo, DateTime programada, int? empleadoId)
        {
            ConexionDb.Ejecutar(
                "INSERT INTO public.tarealimpieza (habitacion_id, empleado_id, tipo, fecha_programada, estado) VALUES (@h, @e, @t, @f, 'PENDIENTE')",
                new NpgsqlParameter("h", habitacionId), new NpgsqlParameter("e", empleadoId.HasValue ? (object)empleadoId.Value : DBNull.Value),
                new NpgsqlParameter("t", tipo), new NpgsqlParameter("f", NpgsqlDbType.Timestamp) { Value = programada });
        }

        // Avanza la tarea y mueve la habitación por su ciclo (Sucia → En limpieza → Limpia → Inspeccionada).
        public string CambiarEstado(int tareaId, string nuevoEstado, int usuarioId, bool aprobada = true)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    string error = CambiarEstadoEn(conn, tx, tareaId, nuevoEstado, usuarioId, aprobada);
                    if (error == null) tx.Commit();
                    return error;
                }
            }
        }

        internal static string CambiarEstadoEn(NpgsqlConnection conn, NpgsqlTransaction tx, int tareaId, string nuevoEstado, int usuarioId, bool aprobada)
        {
            string tipo = null, estado = null, numero = null;
            using (var cmd = new NpgsqlCommand(
                @"SELECT t.tipo, t.estado, h.numero FROM public.tarealimpieza t JOIN public.habitacion h ON h.habitacion_id = t.habitacion_id
                  WHERE t.tarea_id = @id FOR UPDATE OF t", conn, tx))
            {
                cmd.Parameters.AddWithValue("id", tareaId);
                using (var r = cmd.ExecuteReader())
                    if (r.Read()) { tipo = r.GetString(0); estado = r.GetString(1); numero = r.GetString(2); }
            }
            if (tipo == null) return "La tarea ya no existe.";
            if (estado == "COMPLETADA") return "La tarea ya está completada.";
            if (nuevoEstado == "EN_CURSO" && estado != "PENDIENTE") return "La tarea ya está en curso.";

            ReservaRepository.Escalar(conn, tx, "UPDATE public.tarealimpieza SET estado = @e WHERE tarea_id = @id",
                new NpgsqlParameter("e", nuevoEstado), new NpgsqlParameter("id", tareaId));

            // la habitación sigue a la tarea; si no puede pasar a ese estado, la tarea se registra igual
            string destino = null;
            if (tipo == "LIMPIEZA") destino = nuevoEstado == "EN_CURSO" ? "En limpieza" : nuevoEstado == "COMPLETADA" ? "Limpia" : null;
            else if (nuevoEstado == "COMPLETADA" && aprobada) destino = "Inspeccionada";
            if (destino != null)
                HabitacionesRepository.CambiarEstadoEn(conn, tx, numero, destino, usuarioId, "Tarea de " + tipo.ToLowerInvariant(), true);
            return null;
        }

        public List<ChecklistVista> Checklists()
        {
            return ConexionDb.Consultar(
                @"SELECT c.checklist_id, c.nombre, COALESCE(c.descripcion, ''), (SELECT COUNT(*) FROM public.checklistitem i WHERE i.checklist_id = c.checklist_id)
                  FROM public.checklist c ORDER BY c.nombre",
                r => new ChecklistVista { Id = r.GetInt32(0), Nombre = r.GetString(1), Descripcion = r.GetString(2), Items = Convert.ToInt32(r.GetInt64(3)) });
        }

        public bool ExisteChecklist(string nombre)
        {
            return ConexionDb.Consultar("SELECT 1 FROM public.checklist WHERE lower(nombre) = lower(@n)", r => r.GetInt32(0), new NpgsqlParameter("n", nombre)).Any();
        }

        public void GuardarChecklist(string nombre, string descripcion)
        {
            ConexionDb.Ejecutar("INSERT INTO public.checklist (nombre, descripcion) VALUES (@n, @d)",
                new NpgsqlParameter("n", nombre), new NpgsqlParameter("d", string.IsNullOrWhiteSpace(descripcion) ? (object)DBNull.Value : descripcion));
        }

        public List<ChecklistItemVista> Items()
        {
            return ConexionDb.Consultar(
                @"SELECT c.nombre, i.descripcion,
                         COALESCE((SELECT COUNT(*) FILTER (WHERE tc.completado) || ' de ' || COUNT(*) FROM public.tareachecklist tc WHERE tc.item_id = i.item_id), '0 de 0')
                  FROM public.checklistitem i JOIN public.checklist c ON c.checklist_id = i.checklist_id ORDER BY c.nombre, i.item_id",
                r => new ChecklistItemVista { Checklist = r.GetString(0), Descripcion = r.GetString(1), Completado = r.GetString(2) });
        }

        public void GuardarItem(int checklistId, string descripcion)
        {
            ConexionDb.Ejecutar("INSERT INTO public.checklistitem (checklist_id, descripcion) VALUES (@c, @d)",
                new NpgsqlParameter("c", checklistId), new NpgsqlParameter("d", descripcion));
        }

        public List<InspeccionVista> Inspecciones()
        {
            return ConexionDb.Consultar(
                @"SELECT 'Hab. ' || h.numero || ' · tarea ' || i.tarea_id, COALESCE(e.nombre || ' ' || e.apellido, '—'), COALESCE(i.resultado, ''), COALESCE(i.observaciones, ''), COALESCE(i.fecha, LOCALTIMESTAMP)
                  FROM public.inspeccion i JOIN public.tarealimpieza t ON t.tarea_id = i.tarea_id JOIN public.habitacion h ON h.habitacion_id = t.habitacion_id
                  LEFT JOIN public.empleado e ON e.empleado_id = i.empleado_id ORDER BY i.fecha DESC, i.inspeccion_id DESC",
                r => new InspeccionVista { Tarea = r.GetString(0), Empleado = r.GetString(1), Resultado = r.GetString(2) == "APROBADA" ? "Aprobada" : r.GetString(2) == "RECHAZADA" ? "Rechazada" : r.GetString(2), Observaciones = r.GetString(3), Fecha = r.GetDateTime(4) });
        }

        // Registra el resultado de una inspección y completa su tarea. Si se aprueba, la habitación queda Inspeccionada.
        public string RegistrarInspeccion(int tareaId, int? empleadoId, bool aprobada, string observaciones, int usuarioId)
        {
            using (var conn = ConexionDb.Crear())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    string error = RegistrarInspeccionEn(conn, tx, tareaId, empleadoId, aprobada, observaciones, usuarioId);
                    if (error == null) tx.Commit();
                    return error;
                }
            }
        }

        internal static string RegistrarInspeccionEn(NpgsqlConnection conn, NpgsqlTransaction tx, int tareaId, int? empleadoId, bool aprobada, string observaciones, int usuarioId)
        {
            object tipo = ReservaRepository.Escalar(conn, tx, "SELECT tipo FROM public.tarealimpieza WHERE tarea_id = @id", new NpgsqlParameter("id", tareaId));
            if (tipo == null) return "La tarea ya no existe.";
            if ((string)tipo != "INSPECCION") return "Solo se registra el resultado en tareas de inspección.";

            string error = CambiarEstadoEn(conn, tx, tareaId, "COMPLETADA", usuarioId, aprobada);
            if (error != null) return error;
            ReservaRepository.Escalar(conn, tx,
                "INSERT INTO public.inspeccion (tarea_id, empleado_id, resultado, observaciones, fecha) VALUES (@t, @e, @r, @o, LOCALTIMESTAMP)",
                new NpgsqlParameter("t", tareaId), new NpgsqlParameter("e", empleadoId.HasValue ? (object)empleadoId.Value : DBNull.Value),
                new NpgsqlParameter("r", aprobada ? "APROBADA" : "RECHAZADA"),
                new NpgsqlParameter("o", string.IsNullOrWhiteSpace(observaciones) ? (object)DBNull.Value : observaciones.Trim()));
            return null;
        }

        public List<OrdenVista> Ordenes()
        {
            return ConexionDb.Consultar(
                @"SELECT o.orden_id, o.incidencia_id, 'Hab. ' || COALESCE(h.numero, '-') || ' · ' || i.descripcion, COALESCE(e.nombre || ' ' || e.apellido, ''), COALESCE(o.estado, '')
                  FROM public.ordenmantenimiento o JOIN public.incidencia i ON i.incidencia_id = o.incidencia_id
                  LEFT JOIN public.habitacion h ON h.habitacion_id = i.habitacion_id
                  LEFT JOIN public.empleado e ON e.empleado_id = o.empleado_id ORDER BY o.orden_id DESC",
                r => new OrdenVista { Id = r.GetInt32(0), IncidenciaId = r.GetInt32(1), Incidencia = r.GetString(2), Empleado = r.GetString(3), Estado = Estado(r.GetString(4)) });
        }

        static string Estado(string e) { return e == "PENDIENTE" ? "Pendiente" : e == "EN_CURSO" ? "En curso" : e == "COMPLETADA" ? "Completada" : e; }

        public string AsignarOrden(int ordenId, int empleadoId)
        {
            object estado = ConexionDb.Consultar("SELECT estado FROM public.ordenmantenimiento WHERE orden_id = @id", r => r.GetString(0), new NpgsqlParameter("id", ordenId)).FirstOrDefault();
            if (estado == null) return "La orden ya no existe.";
            if ((string)estado == "COMPLETADA") return "La orden ya está completada.";
            ConexionDb.Ejecutar("UPDATE public.ordenmantenimiento SET empleado_id = @e, estado = 'EN_CURSO', fecha_inicio = COALESCE(fecha_inicio, LOCALTIMESTAMP) WHERE orden_id = @id",
                new NpgsqlParameter("e", empleadoId), new NpgsqlParameter("id", ordenId));
            return null;
        }

        public List<ObjetoOlvidadoVista> Objetos()
        {
            return ConexionDb.Consultar(
                @"SELECT o.objeto_id, COALESCE(h.numero, '-'), o.descripcion, COALESCE(o.fecha_encontrado, LOCALTIMESTAMP), COALESCE(o.estado, '')
                  FROM public.objetoolvidado o LEFT JOIN public.habitacion h ON h.habitacion_id = o.habitacion_id ORDER BY o.fecha_encontrado DESC, o.objeto_id DESC",
                r => new ObjetoOlvidadoVista { Id = r.GetInt32(0), Habitacion = r.GetString(1), Descripcion = r.GetString(2), Encontrado = r.GetDateTime(3), Estado = r.GetString(4) == "GUARDADO" ? "Guardado" : r.GetString(4) == "ENTREGADO" ? "Entregado" : r.GetString(4) });
        }

        public void RegistrarObjeto(int habitacionId, string descripcion)
        {
            ConexionDb.Ejecutar("INSERT INTO public.objetoolvidado (habitacion_id, descripcion, fecha_encontrado, estado) VALUES (@h, @d, LOCALTIMESTAMP, 'GUARDADO')",
                new NpgsqlParameter("h", habitacionId), new NpgsqlParameter("d", descripcion));
        }

        public void EntregarObjeto(int objetoId)
        {
            ConexionDb.Ejecutar("UPDATE public.objetoolvidado SET estado = 'ENTREGADO' WHERE objeto_id = @id AND estado <> 'ENTREGADO'", new NpgsqlParameter("id", objetoId));
        }
    }
}

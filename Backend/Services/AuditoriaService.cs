using System;
using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    public class AuditoriaService
    {
        private readonly AuditoriaRepository repositorio = new AuditoriaRepository();

        // Si la base falla, la auditoría no debe impedir que el login o la operación sigan.
        public void Registrar(int? usuarioId, string modulo, string accion, string motivo, string anterior = null, string nuevo = null)
        {
            try
            {
                repositorio.Registrar(usuarioId, modulo, accion, motivo, anterior, nuevo);
            }
            catch (Exception)
            {
            }
        }

        // Versión de los servicios que todavía no pasaron al esquema nuevo (identificadores Guid): no registra nada.
        public void Registrar(Guid? usuarioId, string modulo, string accion, string motivo, string anterior = null, string nuevo = null)
        {
        }
    }
}

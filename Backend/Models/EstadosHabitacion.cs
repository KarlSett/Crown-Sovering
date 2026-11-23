using System.Collections.Generic;
using System.Linq;

namespace Crown__.Backend.Models
{
    // En la base los estados se guardan en mayúsculas con guion bajo (EN_LIMPIEZA); la pantalla los muestra como "En limpieza".
    public static class EstadosHabitacion
    {
        // Ciclo controlado de estados. "Ocupada" solo se alcanza con un check-in.
        private static readonly Dictionary<string, string[]> Siguiente = new Dictionary<string, string[]>
        {
            { "Disponible", new[] { "Mantenimiento", "Bloqueada", "Fuera de servicio" } },
            { "Ocupada", new[] { "Sucia" } },
            { "Sucia", new[] { "En limpieza" } },
            { "En limpieza", new[] { "Limpia" } },
            { "Limpia", new[] { "Inspeccionada" } },
            { "Inspeccionada", new[] { "Disponible" } },
            { "Mantenimiento", new[] { "Disponible" } },
            { "Bloqueada", new[] { "Disponible" } },
            { "Fuera de servicio", new[] { "Disponible" } }
        };

        public static string ParaBase(string pantalla)
        {
            return pantalla.Trim().ToUpperInvariant().Replace(' ', '_');
        }

        public static string ParaPantalla(string baseDeDatos)
        {
            if (string.IsNullOrWhiteSpace(baseDeDatos)) return "Disponible";
            string texto = baseDeDatos.Trim().Replace('_', ' ').ToLowerInvariant();
            return char.ToUpperInvariant(texto[0]) + texto.Substring(1);
        }

        public static string[] Siguientes(string estadoPantalla)
        {
            string[] siguientes;
            return Siguiente.TryGetValue(estadoPantalla, out siguientes) ? siguientes : new string[0];
        }

        public static bool Permitida(string actual, string nuevo)
        {
            return Siguientes(actual).Contains(nuevo);
        }
    }
}

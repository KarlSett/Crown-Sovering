using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Crown__.Backend.Services
{
    /// <summary>Read-only matching: approximate names, exact identifiers, lower scores first.</summary>
    public sealed class BusquedaTolerante
    {
        private readonly string query;
        private readonly string[] words;
        public BusquedaTolerante(string text)
        {
            query = Normalizar(text);
            words = Palabras(query);
        }

        public int Puntuar(string name, params string[] identifiers)
        {
            if (query.Length == 0) return 0;
            foreach (var identifier in identifiers)
            {
                string value = Normalizar(identifier);
                if (value == query) return 0;
                if (value.Contains(query)) return 1;
            }
            string normalized = Normalizar(name);
            if (normalized == query) return 0;
            if (normalized.Contains(query)) return 1;
            // Numeric identifiers and long/free-form input never receive fuzzy corrections.
            if (query.Length > 128 || words.Length == 0 || words.Length > 8 || query.Any(c => char.IsDigit(c) || c == '@')) return -1;
            string[] candidates = Palabras(normalized);
            var used = new bool[candidates.Length];
            var matched = new bool[words.Length];
            for (int i = 0; i < words.Length; i++)
                for (int j = 0; j < candidates.Length; j++)
                    if (!used[j] && candidates[j].StartsWith(words[i], StringComparison.Ordinal))
                    { matched[i] = true; used[j] = true; break; }
            int edits = 0;
            for (int i = 0; i < words.Length; i++)
            {
                if (matched[i]) continue;
                int limit = words[i].Length < 4 ? 0 : words[i].Length < 9 ? 1 : 2;
                if (limit == 0) return -1;
                int best = limit + 1, bestIndex = -1;
                for (int j = 0; j < candidates.Length; j++)
                {
                    if (used[j]) continue;
                    int distance = Distancia(words[i], candidates[j], limit);
                    if (distance < best) { best = distance; bestIndex = j; }
                }
                if (bestIndex < 0 || (edits += best) > 2) return -1;
                used[bestIndex] = true;
            }
            return edits == 0 ? 2 : 10 + edits;
        }

        private static string Normalizar(string text)
        {
            var result = new StringBuilder();
            bool space = false;
            foreach (char c in (text ?? string.Empty).Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsWhiteSpace(c)) { space = result.Length > 0; continue; }
                if (space) result.Append(' ');
                space = false;
                result.Append(char.ToLowerInvariant(c));
            }
            return result.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string[] Palabras(string text)
        {
            return new string(text.Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray())
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        // Bounded optimal-string-alignment distance: insertion, deletion, substitution or adjacent swap.
        private static int Distancia(string a, string b, int limit)
        {
            if (Math.Abs(a.Length - b.Length) > limit) return limit + 1;
            var previous = new int[b.Length + 1];
            var previousPrevious = new int[b.Length + 1];
            var current = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) previous[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                int minimum = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
                    if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                        current[j] = Math.Min(current[j], previousPrevious[j - 2] + 1);
                    minimum = Math.Min(minimum, current[j]);
                }
                if (minimum > limit) return limit + 1;
                var recycled = previousPrevious; previousPrevious = previous; previous = current; current = recycled;
            }
            return previous[b.Length];
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Crown__.Frontend.Views
{
    internal sealed class ReservationImportPreview
    {
        internal int ValidRows;
        internal readonly List<string> Errors = new List<string>();
        internal readonly List<string> Examples = new List<string>();
        private static readonly string[] Headers = { "huesped", "llegada", "salida", "tipo", "tarifa", "canal" };

        internal static ReservationImportPreview Read(string path)
        {
            if (new FileInfo(path).Length > 2 * 1024 * 1024) throw new FormatException("El límite es 2 MB.");
            List<string[]> rows;
            if (string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase)) rows = ParseCsv(File.ReadAllText(path, Encoding.UTF8));
            else if (string.Equals(Path.GetExtension(path), ".xlsx", StringComparison.OrdinalIgnoreCase)) rows = ReadExcel(path);
            else throw new FormatException("Selecciona un archivo .csv o .xlsx.");
            return ValidateRows(rows);
        }

        internal static ReservationImportPreview ValidateRows(List<string[]> rows)
        {
            if (rows.Count == 0) throw new FormatException("El archivo está vacío.");
            if (rows.Count > 2001) throw new FormatException("El límite es 2.000 filas de datos.");
            var header = rows[0].Select(h => h.Trim().TrimStart('\uFEFF').ToLowerInvariant()).ToArray();
            if (Headers.Any(h => header.Count(c => c == h) != 1)) throw new FormatException("Incluye una sola columna de cada tipo: " + string.Join(", ", Headers) + ".");
            int[] indexes = Headers.Select(h => Array.IndexOf(header, h)).ToArray();
            var report = new ReservationImportPreview();
            for (int i = 1; i < rows.Count; i++) {
                var row = rows[i];
                string[] values = indexes.Select(index => index < row.Length ? row[index].Trim() : "").ToArray();
                var errors = new List<string>(); DateTime arrival, departure; decimal price;
                if (values.Any(string.IsNullOrWhiteSpace)) errors.Add("hay campos obligatorios vacíos");
                bool dates = DateTime.TryParseExact(values[1], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out arrival)
                    & DateTime.TryParseExact(values[2], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out departure);
                if (!dates) errors.Add("las fechas deben usar dd/MM/aaaa");
                else if (departure <= arrival) errors.Add("la salida debe ser posterior a la llegada");
                if (!System.Text.RegularExpressions.Regex.IsMatch(values[4], @"^\d{1,7}([.,]\d{1,2})?$") || !decimal.TryParse(values[4].Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out price) || price <= 0) errors.Add("la tarifa debe ser positiva y tener hasta dos decimales");
                if (!new[]{"Individual", "Doble", "Matrimonial", "Triple", "Suite", "Familiar"}.Contains(values[3], StringComparer.OrdinalIgnoreCase)) errors.Add("tipo de habitación no reconocido");
                if (!new[]{"Teléfono", "Telefono", "WhatsApp", "Correo", "Mostrador", "Agencia", "Empresa"}.Contains(values[5], StringComparer.OrdinalIgnoreCase)) errors.Add("canal de origen no reconocido");
                if (errors.Count > 0) report.Errors.Add("Fila " + (i + 1) + ": " + string.Join("; ", errors) + ".");
                else { report.ValidRows++; if (report.Examples.Count < 8) report.Examples.Add("Fila " + (i + 1) + ": " + values[0] + " · " + values[1] + " – " + values[2] + " · " + values[3]); }
            }
            if (rows.Count == 1) report.Errors.Add("El archivo contiene encabezados, pero ninguna reserva.");
            return report;
        }

        internal static List<string[]> ParseCsv(string content)
        {
            var records = new List<string[]>(); var row = new List<string>(); var cell = new StringBuilder();
            bool quoted = false, closedQuote = false;
            string firstLine = content.Split('\n')[0];
            char delimiter = firstLine.Count(c => c == ';') > firstLine.Count(c => c == ',') ? ';' : ',';
            for (int i = 0; i < content.Length; i++) {
                char value = content[i];
                if (quoted) {
                    if (value == '"') { if (i + 1 < content.Length && content[i + 1] == '"') { cell.Append('"'); i++; } else { quoted = false; closedQuote = true; } }
                    else cell.Append(value);
                } else if (value == '"') {
                    if (cell.Length != 0 || closedQuote) throw new FormatException("Comillas inválidas en el CSV.");
                    quoted = true;
                } else if (value == delimiter || value == '\n' || value == '\r') {
                    row.Add(cell.ToString()); cell.Clear(); closedQuote = false;
                    if (value != delimiter) {
                        if (value == '\r' && i + 1 < content.Length && content[i + 1] == '\n') i++;
                        // Preserve empty records so reported row numbers match the source file.
                        records.Add(row.ToArray()); row.Clear();
                        if (records.Count > 2001) throw new FormatException("El límite es 2.000 filas de datos.");
                    }
                } else {
                    if (closedQuote && !char.IsWhiteSpace(value)) throw new FormatException("Texto después del cierre de comillas.");
                    if (!closedQuote) cell.Append(value);
                }
            }
            if (quoted) throw new FormatException("Hay un campo con comillas sin cerrar.");
            if (cell.Length > 0 || row.Count > 0 || closedQuote) { row.Add(cell.ToString()); records.Add(row.ToArray()); }
            return records;
        }

        private static List<string[]> ReadExcel(string path)
        {
            using (var zip = ZipFile.OpenRead(path)) {
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                var shared = zip.GetEntry("xl/sharedStrings.xml");
                var strings = shared == null ? new string[0] : ReadXml(shared).Descendants(ns + "si").Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value))).ToArray();
                // Resolve the first worksheet through its relationship, not a guessed sheet1 path.
                XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
                var book = ReadXml(zip.GetEntry("xl/workbook.xml"));
                var sheet = book.Descendants(ns + "sheet").FirstOrDefault();
                if (sheet == null) throw new FormatException("El libro no contiene hojas.");
                var relationships = ReadXml(zip.GetEntry("xl/_rels/workbook.xml.rels"));
                var relationship = relationships.Root.Elements().FirstOrDefault(e => (string)e.Attribute("Id") == (string)sheet.Attribute(rel + "id"));
                if (relationship == null || (string)relationship.Attribute("TargetMode") == "External") throw new FormatException("La hoja no es local al archivo.");
                string target = (string)relationship.Attribute("Target") ?? "";
                string part = target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target;
                if (part.Contains("..")) throw new FormatException("Ruta de hoja no admitida.");
                var xml = ReadXml(zip.GetEntry(part)); var result = new List<string[]>();
                foreach (var excelRow in xml.Descendants(ns + "row")) {
                    int number; if (!int.TryParse((string)excelRow.Attribute("r"), out number) || number < 1 || number > 2001) throw new FormatException("El límite es 2.000 filas de datos.");
                    while (result.Count < number - 1) result.Add(new string[0]);
                    var cells = new SortedDictionary<int, string>();
                    foreach (var element in excelRow.Elements(ns + "c")) {
                        string address = (string)element.Attribute("r") ?? ""; int index = 0;
                        foreach (char letter in address.TakeWhile(char.IsLetter)) { index = index * 26 + char.ToUpperInvariant(letter) - 'A' + 1; if (index > 200) throw new FormatException("La hoja contiene demasiadas columnas."); }
                        if (index < 1) throw new FormatException("La hoja contiene una celda sin referencia.");
                        if (element.Element(ns + "f") != null) throw new FormatException("Usa valores, sin fórmulas, en las celdas de importación.");
                        string value = (string)element.Element(ns + "v") ?? "", type = (string)element.Attribute("t"); int sharedIndex;
                        if (type == "s") { if (!int.TryParse(value, out sharedIndex) || sharedIndex < 0 || sharedIndex >= strings.Length) throw new FormatException("Referencia de texto inválida."); value = strings[sharedIndex]; }
                        if (type == "inlineStr") value = string.Concat(element.Descendants(ns + "t").Select(t => t.Value));
                        cells[index - 1] = value;
                    }
                    var values = new string[cells.Count == 0 ? 0 : cells.Keys.Max() + 1];
                    foreach (var pair in cells) values[pair.Key] = pair.Value;
                    result.Add(values.Select(v => v ?? "").ToArray());
                }
                return result;
            }
        }
        private static XDocument ReadXml(ZipArchiveEntry entry)
        {
            if (entry == null) throw new FormatException("El archivo Excel está incompleto.");
            if (entry.Length > 8 * 1024 * 1024) throw new FormatException("La hoja descomprimida supera el límite de revisión.");
            var settings = new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 8 * 1024 * 1024 };
            using (var stream = entry.Open()) using (var reader = System.Xml.XmlReader.Create(stream, settings)) return XDocument.Load(reader);
        }
    }
}

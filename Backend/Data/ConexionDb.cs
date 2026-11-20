using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Npgsql;

namespace Crown__.Backend.Data
{
    // Arma la conexión a PostgreSQL con los datos de Conexion.config. No ejecuta consultas.
    public static class ConexionDb
    {
        public static NpgsqlConnection Crear()
        {
            var conexion = new NpgsqlConnection(CadenaConexion());
            // La base trabaja en UTC y la PC no: sin esto "hoy" cambia de día a las 20:00 hora de Bolivia.
            conexion.StateChange += (origen, e) =>
            {
                if (e.CurrentState != System.Data.ConnectionState.Open) return;
                using (var cmd = new NpgsqlCommand("SET TIME ZONE '" + ZonaDeLaPc() + "'", (NpgsqlConnection)origen))
                    cmd.ExecuteNonQuery();
            };
            return conexion;
        }

        // Las lecturas se reintentan una vez si la conexión se cae a mitad de camino (pasa a veces con el pooler).
        public static List<T> Consultar<T>(string sql, Func<NpgsqlDataReader, T> mapear, params NpgsqlParameter[] parametros)
        {
            try
            {
                return LeerFilas(sql, mapear, parametros);
            }
            catch (NpgsqlException ex) when (ex.IsTransient || ex.InnerException is System.IO.IOException)
            {
                return LeerFilas(sql, mapear, parametros);
            }
        }

        private static List<T> LeerFilas<T>(string sql, Func<NpgsqlDataReader, T> mapear, NpgsqlParameter[] parametros)
        {
            var filas = new List<T>();
            using (var conn = Crear())
            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                // se copian porque un parámetro no puede estar en dos comandos (el reintento crea otro)
                cmd.Parameters.AddRange(parametros.Select(p => p.Clone()).ToArray());
                conn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) filas.Add(mapear(r));
            }
            return filas;
        }

        public static void Ejecutar(string sql, params NpgsqlParameter[] parametros)
        {
            using (var conn = Crear())
            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddRange(parametros);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static string CadenaConexion()
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = Leer("Db.Host"),
                Port = int.Parse(Leer("Db.Port")),
                Database = Leer("Db.Database"),
                Username = Leer("Db.Username"),
                Password = Leer("Db.Password"),
                SslMode = SslMode.Require
            };
            return builder.ConnectionString;
        }

        // "Etc/GMT+4" es UTC-4: PostgreSQL invierte el signo en esas zonas fijas.
        private static string ZonaDeLaPc()
        {
            double horas = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalHours;
            if (horas != Math.Floor(horas) || Math.Abs(horas) > 12) return "UTC";
            int entero = (int)horas;
            return entero == 0 ? "UTC" : "Etc/GMT" + (entero < 0 ? "+" : "-") + Math.Abs(entero);
        }

        private static string Leer(string clave)
        {
            string valor = ConfigurationManager.AppSettings[clave];
            if (string.IsNullOrWhiteSpace(valor)) ArchivoCercano.Value.TryGetValue(clave, out valor);
            if (string.IsNullOrWhiteSpace(valor) || valor.StartsWith("PON_AQUI"))
                throw new InvalidOperationException("Falta configurar " + clave + " en Conexion.config (no se encontró junto al programa ni en las carpetas de arriba).");
            return valor;
        }

        // Si App.config no alcanzó a cargar Conexion.config, se busca el archivo junto al programa y subiendo carpetas.
        private static readonly Lazy<Dictionary<string, string>> ArchivoCercano = new Lazy<Dictionary<string, string>>(BuscarArchivo);

        private static Dictionary<string, string> BuscarArchivo()
        {
            var carpeta = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int nivel = 0; nivel < 5 && carpeta != null; nivel++, carpeta = carpeta.Parent)
            {
                string ruta = System.IO.Path.Combine(carpeta.FullName, "Conexion.config");
                if (!System.IO.File.Exists(ruta)) continue;
                try
                {
                    return System.Xml.Linq.XDocument.Load(ruta).Root.Elements("add")
                        .ToDictionary(e => (string)e.Attribute("key"), e => (string)e.Attribute("value"));
                }
                catch (Exception)
                {
                    // archivo mal formado: se sigue con el mensaje de "falta configurar"
                }
            }
            return new Dictionary<string, string>();
        }
    }
}

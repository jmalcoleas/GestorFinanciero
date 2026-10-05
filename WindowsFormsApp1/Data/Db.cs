using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace GestorFinancieroApp.Data
{
    /// <summary>
    /// Localiza el servidor SQL, crea la base de datos y el esquema si hace falta.
    /// </summary>
    internal static class Db
    {
        private const string DbName = "GestionFinanciera";

        public static string ConnectionString { get; private set; }

        public static void Initialize()
        {
            string chosen = null, firstReachable = null;
            Exception last = null;

            foreach (string server in CandidateServers())
            {
                try
                {
                    using (var cn = new SqlConnection(ServerConnection(server, "master")))
                    {
                        cn.Open();
                        if (firstReachable == null) firstReachable = server;
                        using (var cmd = new SqlCommand("SELECT DB_ID(@n)", cn))
                        {
                            cmd.Parameters.AddWithValue("@n", DbName);
                            if (!(cmd.ExecuteScalar() is DBNull)) { chosen = server; break; }
                        }
                    }
                }
                catch (SqlException ex) { last = ex; }
            }

            // Si la base de datos no existe en ningún servidor, se crea en el primero que responda.
            if (chosen == null) chosen = firstReachable;
            if (chosen == null)
                throw new InvalidOperationException(
                    "No se pudo conectar con SQL Server (LocalDB ni SQL Express).\n" +
                    "Puedes indicar el servidor en App.config, clave \"SqlServer\".", last);

            using (var cn = new SqlConnection(ServerConnection(chosen, "master")))
            {
                cn.Open();
                using (var cmd = new SqlCommand(
                    "IF DB_ID('" + DbName + "') IS NULL CREATE DATABASE [" + DbName + "]", cn))
                    cmd.ExecuteNonQuery();
            }

            ConnectionString = ServerConnection(chosen, DbName);
            ApplySchema();
        }

        private static string[] CandidateServers()
        {
            string configured = ConfigurationManager.AppSettings["SqlServer"];
            if (!string.IsNullOrWhiteSpace(configured)) return new[] { configured.Trim() };
            return new[] { @"(localdb)\MSSQLLocalDB", @".\SQLEXPRESS" };
        }

        private static string ServerConnection(string server, string database)
        {
            return "Server=" + server + ";Database=" + database +
                   ";Integrated Security=true;Connect Timeout=20;MultipleActiveResultSets=false";
        }

        private static void ApplySchema()
        {
            string script;
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("schema.sql"))
            {
                if (s == null) throw new InvalidOperationException("Falta el recurso schema.sql.");
                using (var reader = new StreamReader(s)) script = reader.ReadToEnd();
            }

            using (var cn = new SqlConnection(ConnectionString))
            {
                cn.Open();
                foreach (string batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(batch)) continue;
                    using (var cmd = new SqlCommand(batch, cn)) cmd.ExecuteNonQuery();
                }
            }
        }
    }
}

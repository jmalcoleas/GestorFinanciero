using System;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace GestorFinancieroApp.Data
{
    /// <summary>
    /// Base de datos SQLite portable: un único archivo (datos\gestor.db) junto al ejecutable.
    /// Para llevarte la app y tus datos a otro equipo basta con copiar la carpeta entera.
    /// </summary>
    internal static class Db
    {
        public static string ConnectionString { get; private set; }
        public static string DatabasePath { get; private set; }

        public static void Initialize()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "datos");
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "No se puede escribir en la carpeta de la aplicación.\n" +
                    "Si abriste el programa desde dentro del .zip, extráelo primero a una carpeta normal.", ex);
            }

            DatabasePath = Path.Combine(dir, "gestor.db");
            ConnectionString = new SQLiteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                ForeignKeys = true,
                Pooling = false,           // evita que el archivo quede bloqueado al copiarlo
                JournalMode = SQLiteJournalModeEnum.Delete
            }.ToString();

            ApplySchema();
        }

        private static void ApplySchema()
        {
            string script;
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("schema.sql"))
            {
                if (s == null) throw new InvalidOperationException("Falta el recurso schema.sql.");
                using (var reader = new StreamReader(s)) script = reader.ReadToEnd();
            }

            using (var cn = new SQLiteConnection(ConnectionString))
            {
                cn.Open();
                using (var cmd = new SQLiteCommand(script, cn)) cmd.ExecuteNonQuery();
            }
        }
    }
}

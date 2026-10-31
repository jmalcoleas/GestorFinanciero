using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;

namespace GestorFinancieroApp.Data
{
    /// <summary>Un cambio de la base de datos, identificado por el número de versión al que lleva.</summary>
    internal sealed class Migration
    {
        public Migration(int version, Action<SQLiteConnection, SQLiteTransaction> apply)
        {
            Version = version;
            Apply = apply;
        }

        public int Version { get; private set; }
        public Action<SQLiteConnection, SQLiteTransaction> Apply { get; private set; }
    }

    /// <summary>
    /// Actualiza la base de datos por versiones (PRAGMA user_version). Antes de cambiar una base existente
    /// guarda una copia en datos\copias, y cada migración va en su propia transacción: si falla, esa
    /// migración se deshace entera y los datos quedan como estaban.
    /// schema.sql define la versión 1 y no se modifica: los cambios nuevos se añaden a la lista All.
    /// </summary>
    internal static class Migrations
    {
        private static readonly Migration[] All =
        {
            new Migration(1, (cn, tx) => { }) // Versión base: la que crea schema.sql.
        };

        public static int LatestVersion { get { return All.Max(m => m.Version); } }

        /// <returns>La ruta de la copia creada, o null si no hizo falta ninguna.</returns>
        public static string Run(string connectionString, string databasePath, bool isNewDatabase)
        {
            return Run(connectionString, databasePath, isNewDatabase, All);
        }

        internal static string Run(string connectionString, string databasePath, bool isNewDatabase,
            IEnumerable<Migration> migrations)
        {
            List<Migration> ordered = migrations.OrderBy(m => m.Version).ToList();
            int latest = ordered.Count == 0 ? 0 : ordered.Last().Version;

            int current = ReadVersion(connectionString);
            if (current > latest)
                throw new InvalidOperationException(
                    "Estos datos se crearon con una versión más nueva de la aplicación.\n" +
                    "Actualiza el programa antes de abrirlos.");
            if (current == latest) return null;

            // La copia se hace con la conexión cerrada, para que el archivo esté completo.
            string backup = isNewDatabase ? null : Backup(databasePath, current);

            using (var cn = new SQLiteConnection(connectionString))
            {
                cn.Open();
                foreach (Migration migration in ordered.Where(m => m.Version > current))
                {
                    try
                    {
                        using (var tx = cn.BeginTransaction())
                        {
                            migration.Apply(cn, tx);
                            using (var cmd = new SQLiteCommand(
                                "PRAGMA user_version = " + migration.Version.ToString(CultureInfo.InvariantCulture), cn, tx))
                                cmd.ExecuteNonQuery();
                            tx.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            "No se pudieron actualizar los datos a la versión " + migration.Version + ". " +
                            "No se ha perdido nada." +
                            (backup == null ? "" : "\nHay una copia de seguridad en:\n" + backup), ex);
                    }
                }
            }
            return backup;
        }

        internal static int ReadVersion(string connectionString)
        {
            using (var cn = new SQLiteConnection(connectionString))
            using (var cmd = new SQLiteCommand("PRAGMA user_version", cn))
            {
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static string Backup(string databasePath, int version)
        {
            string dir = Path.Combine(Path.GetDirectoryName(databasePath), "copias");
            Directory.CreateDirectory(dir);

            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string target = Path.Combine(dir, "gestor-v" + version + "-" + stamp + ".db");
            File.Copy(databasePath, target, false);
            return target;
        }
    }
}

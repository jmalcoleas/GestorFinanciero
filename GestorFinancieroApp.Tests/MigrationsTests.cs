using System;
using System.Data.SQLite;
using System.IO;
using GestorFinancieroApp.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GestorFinancieroApp.Tests
{
    [TestClass]
    public class MigrationsTests
    {
        private string _dir;

        [TestInitialize]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "gf-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TestCleanup]
        public void TearDown()
        {
            Directory.Delete(_dir, true);
        }

        private string DbPath { get { return Path.Combine(_dir, "gestor.db"); } }
        private string Connection { get { return "Data Source=" + DbPath + ";Pooling=False"; } }

        private void Execute(string sql)
        {
            using (var cn = new SQLiteConnection(Connection))
            using (var cmd = new SQLiteCommand(sql, cn))
            {
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private long Count(string table)
        {
            using (var cn = new SQLiteConnection(Connection))
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM " + table, cn))
            {
                cn.Open();
                return (long)cmd.ExecuteScalar();
            }
        }

        private static void Sql(SQLiteConnection cn, SQLiteTransaction tx, string sql)
        {
            using (var cmd = new SQLiteCommand(sql, cn, tx)) cmd.ExecuteNonQuery();
        }

        [TestMethod]
        public void BaseNueva_QuedaEnLaUltimaVersionSinCopia()
        {
            Db.Initialize(_dir);

            Assert.AreEqual(Migrations.LatestVersion, Migrations.ReadVersion(Db.ConnectionString));
            Assert.IsFalse(Directory.Exists(Path.Combine(_dir, "copias")));
        }

        [TestMethod]
        public void BaseExistenteSinVersion_SeActualizaConservandoDatosYCopia()
        {
            Execute("CREATE TABLE notas (texto TEXT); INSERT INTO notas VALUES ('dato');");

            string backup = Migrations.Run(Connection, DbPath, false, new[]
            {
                new Migration(1, (cn, tx) => Sql(cn, tx, "ALTER TABLE notas ADD COLUMN extra TEXT"))
            });

            Assert.AreEqual(1, Migrations.ReadVersion(Connection));
            Assert.AreEqual(1, Count("notas"));
            Assert.IsTrue(File.Exists(backup));
        }

        [TestMethod]
        public void MigracionQueFalla_DejaLosDatosIntactos()
        {
            Execute("CREATE TABLE notas (texto TEXT); INSERT INTO notas VALUES ('dato');");

            Assert.ThrowsException<InvalidOperationException>(() =>
                Migrations.Run(Connection, DbPath, false, new[]
                {
                    new Migration(1, (cn, tx) =>
                    {
                        Sql(cn, tx, "DELETE FROM notas");
                        throw new InvalidDataException("fallo simulado");
                    })
                }));

            Assert.AreEqual(0, Migrations.ReadVersion(Connection));
            Assert.AreEqual(1, Count("notas"));
        }

        [TestMethod]
        public void DatosDeUnaVersionMasNueva_SeRechazan()
        {
            Execute("PRAGMA user_version = 99");

            Assert.ThrowsException<InvalidOperationException>(() =>
                Migrations.Run(Connection, DbPath, false, new Migration[0]));
        }

        [TestMethod]
        public void SegundoArranque_NoCreaOtraCopia()
        {
            Execute("CREATE TABLE notas (texto TEXT)");
            var migrations = new[] { new Migration(1, (cn, tx) => { }) };

            Assert.IsNotNull(Migrations.Run(Connection, DbPath, false, migrations));
            Assert.IsNull(Migrations.Run(Connection, DbPath, false, migrations));
        }
    }
}

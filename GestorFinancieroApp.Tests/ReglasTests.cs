using System;
using System.Collections.Generic;
using System.IO;
using GestorFinancieroApp.Data;
using GestorFinancieroApp.Logic;
using GestorFinancieroApp.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GestorFinancieroApp.Tests
{
    [TestClass]
    public class IndicatorTests
    {
        private static readonly DateTime Octubre = new DateTime(2026, 10, 1);

        [TestMethod]
        public void Categoria_CambiaDeColorAl80PorCientoYAlPasarse()
        {
            Assert.AreEqual(Level.Good, Indicator.ForCategory(100m, 79.99m));
            Assert.AreEqual(Level.Warning, Indicator.ForCategory(100m, 80m));
            Assert.AreEqual(Level.Warning, Indicator.ForCategory(100m, 100m));
            Assert.AreEqual(Level.Over, Indicator.ForCategory(100m, 100.01m));
        }

        [TestMethod]
        public void Mes_ComparaElGastoConElRitmoDelMes()
        {
            var mitad = new DateTime(2026, 10, 15); // 15/31 del mes: unos 48 puntos.

            Assert.AreEqual(Level.None, Indicator.Overall(0m, 0m, Octubre, mitad));
            Assert.AreEqual(Level.Good, Indicator.Overall(100m, 50m, Octubre, mitad));
            Assert.AreEqual(Level.Warning, Indicator.Overall(100m, 65m, Octubre, mitad));
            Assert.AreEqual(Level.Over, Indicator.Overall(100m, 80m, Octubre, mitad));
            Assert.AreEqual(Level.Over, Indicator.Overall(100m, 101m, Octubre, new DateTime(2026, 11, 2)));
        }

        [TestMethod]
        public void MesCumplido_ExigeQueNingunPresupuestoSeExceda()
        {
            var bien = new BudgetRow { Amount = 55m, Spent = 55m };
            var mal = new BudgetRow { Amount = 55m, Spent = 55.01m };

            Assert.IsFalse(Indicator.MonthSuccess(new List<BudgetRow>()));
            Assert.IsTrue(Indicator.MonthSuccess(new[] { bien }));
            Assert.IsFalse(Indicator.MonthSuccess(new[] { bien, mal }));
        }
    }

    [TestClass]
    public class MoneyTests
    {
        [DataTestMethod]
        [DataRow("55", 55.0)]
        [DataRow("55,5", 55.5)]
        [DataRow("55.5", 55.5)]
        [DataRow("1.200,50", 1200.5)]
        [DataRow("12,345", 12.35)]
        [DataRow("20 €", 20.0)]
        public void TryParse_AceptaLosFormatosHabituales(string texto, double esperado)
        {
            decimal valor;
            Assert.IsTrue(Money.TryParse(texto, out valor));
            Assert.AreEqual((decimal)esperado, valor);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("abc")]
        [DataRow("-5")]
        public void TryParse_RechazaLoQueNoEsUnImporte(string texto)
        {
            decimal valor;
            Assert.IsFalse(Money.TryParse(texto, out valor));
        }
    }

    [TestClass]
    public class PasswordHasherTests
    {
        [TestMethod]
        public void Verify_SoloAceptaLaContrasenaCorrecta()
        {
            string hash = PasswordHasher.Hash("claveSegura1");

            Assert.IsTrue(PasswordHasher.Verify("claveSegura1", hash));
            Assert.IsFalse(PasswordHasher.Verify("otraClave", hash));
            Assert.IsFalse(PasswordHasher.Verify("claveSegura1", "hash_provisional"));
        }

        [TestMethod]
        public void Hash_UsaUnaSalDistintaCadaVez()
        {
            Assert.AreNotEqual(PasswordHasher.Hash("igual"), PasswordHasher.Hash("igual"));
        }
    }

    [TestClass]
    public class ReglasDelDineroTests
    {
        private string _dir;

        [TestInitialize]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "gf-tests-" + Guid.NewGuid().ToString("N"));
            Db.Initialize(_dir);
        }

        [TestCleanup]
        public void TearDown()
        {
            Directory.Delete(_dir, true);
        }

        [TestMethod]
        public void EjemploDelReadme_CuadraAlCentimo()
        {
            User user = Repository.CreateUser("Prueba", "prueba@test.local", "hash", 1000m, 700m);
            int salario = Repository.GetOrCreateCategory(user.Id, "Salario", "income");
            int cenas = Repository.GetOrCreateCategory(user.Id, "Cenas", "expense");
            var dia = new DateTime(2026, 10, 2);

            Repository.AddTransaction(user.Id, salario, dia, 170m, 60m, "Ingreso");
            Repository.AddTransaction(user.Id, cenas, dia, 20m, 0m, "Cena");

            decimal cuenta, ahorros, ingresos, aAhorros, gastos;
            Repository.GetTotals(user.Id, out cuenta, out ahorros);
            Repository.GetSummary(user.Id, 2026, 10, out ingresos, out aAhorros, out gastos);

            Assert.AreEqual(1090m, cuenta);
            Assert.AreEqual(760m, ahorros);
            Assert.AreEqual(90m, ingresos - aAhorros - gastos);
        }
    }
}

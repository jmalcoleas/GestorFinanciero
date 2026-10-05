using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GestorFinancieroApp.Data;
using GestorFinancieroApp.Logic;

namespace GestorFinancieroApp.UI
{
    /// <summary>Mensaje de enhorabuena al cerrar un mes cumpliendo todos los presupuestos.</summary>
    internal sealed class CelebrationForm : Form
    {
        private static readonly Random Rng = new Random();

        public CelebrationForm(string userName, DateTime month, IList<BudgetRow> budgets)
        {
            Text = "¡Mes cumplido!";
            Font = Ui.Base;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = ShowInTaskbar = false;
            ClientSize = new Size(440, 300);

            var header = new Label
            {
                Text = "🎉  ¡Enhorabuena!",
                Dock = DockStyle.Top,
                Height = 70,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Indicator.Good,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold)
            };
            var body = new Label
            {
                Text = BuildMessage(userName, month, budgets),
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 20, 24, 8),
                Font = new Font("Segoe UI", 11f)
            };
            var ok = Ui.PrimaryButton("¡Gracias!");
            ok.DialogResult = DialogResult.OK;

            Controls.Add(body);
            Controls.Add(Ui.ButtonBar(ok));
            Controls.Add(header);
            AcceptButton = ok;
        }

        private static string BuildMessage(string name, DateTime month, IList<BudgetRow> budgets)
        {
            string mes = Money.MonthName(month).ToLower(Money.Es);
            decimal saved = budgets.Sum(b => b.Amount) - budgets.Sum(b => b.Spent);
            BudgetRow best = budgets.OrderByDescending(b => b.Remaining).First();
            string savedText = Money.Format(saved);

            string[] templates =
            {
                "¡Muy bien, {0}! Has cerrado {1} cumpliendo todos tus presupuestos y te han sobrado {2}. ¡Así se hace!",
                "{0}, ¡lo has conseguido! {1} ha terminado con todos tus gastos bajo control y {2} de margen. Donde mejor lo has hecho: «{3}».",
                "¡Mes redondo, {0}! Ninguna categoría se pasó de presupuesto en {1}. Te sobran {2}: ¡date un capricho (con cabeza)!"
            };
            return string.Format(templates[Rng.Next(templates.Length)], name, mes, savedText, best.CategoryName);
        }
    }
}

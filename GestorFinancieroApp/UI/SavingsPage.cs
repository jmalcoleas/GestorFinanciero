using System;
using System.Drawing;
using System.Windows.Forms;
using GestorFinancieroApp.Data;
using GestorFinancieroApp.Logic;

namespace GestorFinancieroApp.UI
{
    /// <summary>Página de ahorros, con las pestañas Cuenta de ahorro e Inversiones.</summary>
    internal sealed class SavingsPage : UserControl
    {
        private readonly User _user;
        private readonly Label _lblTotal = new Label();
        private readonly Label _lblMonth = new Label();

        public SavingsPage(User user)
        {
            _user = user;
            Dock = DockStyle.Fill;

            var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            cards.Controls.Add(Ui.Card("Ahorros (total)", _lblTotal, Ui.Savings), 0, 0);
            cards.Controls.Add(Ui.Card("A ahorros este mes", _lblMonth, Ui.Savings), 1, 0);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(Placeholder("Cuenta de ahorro", "Aquí estarán el saldo, los intereses y el historial."));
            tabs.TabPages.Add(Placeholder("Inversiones", "Aquí estarán tus aportaciones y el valor de tu inversión."));

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(cards, 0, 0);
            root.Controls.Add(tabs, 0, 1);
            Controls.Add(root);
        }

        public void ShowMonth(DateTime month)
        {
            Ui.Guard(this, () =>
            {
                decimal account, total, income, savings, expenses;
                Repository.GetTotals(_user.Id, out account, out total);
                Repository.GetSummary(_user.Id, month.Year, month.Month, out income, out savings, out expenses);
                _lblTotal.Text = Money.Format(total);
                _lblMonth.Text = Money.Format(savings);
            });
        }

        private static TabPage Placeholder(string title, string text)
        {
            var page = new TabPage(title);
            page.Controls.Add(new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Indicator.Muted
            });
            return page;
        }
    }
}

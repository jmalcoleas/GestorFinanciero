using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GestorFinancieroApp.Data;
using GestorFinancieroApp.Logic;

namespace GestorFinancieroApp.UI
{
    /// <summary>Marco de la aplicación: barra lateral, selector de mes y las dos páginas.</summary>
    internal sealed class MainForm : Form
    {
        private static readonly Color Sidebar = Color.FromArgb(24, 32, 46);
        private static readonly Color SidebarText = Color.FromArgb(205, 213, 224);

        private readonly User _user;
        private DateTime _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        private readonly Label _lblPage = new Label();
        private readonly Label _lblMonth = new Label();
        private readonly IncomeExpensesPage _incomePage;
        private readonly SavingsPage _savingsPage;
        private Button _navIncome, _navSavings;

        public bool LoggedOut { get; private set; }

        public MainForm(User user)
        {
            _user = user;
            Text = "Gestor Financiero";
            Icon = Ui.AppIcon;
            Font = Ui.Base;
            ClientSize = new Size(1120, 720);
            MinimumSize = new Size(1020, 640);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Ui.Surface;

            _incomePage = new IncomeExpensesPage(user);
            _savingsPage = new SavingsPage(user);
            _incomePage.MonthRequested += month => { _month = month; RefreshAll(); };

            var host = new Panel { Dock = DockStyle.Fill };
            host.Controls.Add(_incomePage);
            host.Controls.Add(_savingsPage);

            var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12) };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            content.Controls.Add(BuildHeader(), 0, 0);
            content.Controls.Add(host, 0, 1);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(BuildSidebar(), 0, 0);
            root.Controls.Add(content, 1, 0);
            Controls.Add(root);

            ShowPage(_incomePage, "Ingresos y gastos", _navIncome);

            Shown += (s, e) =>
            {
                RefreshAll();
                PromptMonthlyIncome();
                CheckCelebrations();
            };
        }

        // ------------------------------------------------------------------ Construcción de la interfaz

        private Control BuildSidebar()
        {
            var bar = new Panel { Dock = DockStyle.Fill, BackColor = Sidebar, Margin = Padding.Empty };

            var hello = new Label
            {
                Text = "Hola, " + _user.Name,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 64,
                Padding = new Padding(16, 0, 8, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _navIncome = SidebarButton("Ingresos y gastos");
            _navSavings = SidebarButton("Ahorros");
            _navIncome.Click += (s, e) => ShowPage(_incomePage, "Ingresos y gastos", _navIncome);
            _navSavings.Click += (s, e) => ShowPage(_savingsPage, "Ahorros", _navSavings);

            var balances = SidebarButton("Saldos de partida");
            var logout = SidebarButton("Cerrar sesión");
            balances.Dock = logout.Dock = DockStyle.Bottom;
            balances.Click += (s, e) => EditInitialBalances();
            logout.Click += (s, e) => { LoggedOut = true; Close(); };

            // Lo último añadido se acopla primero: el saludo queda arriba y la sesión abajo.
            bar.Controls.Add(_navSavings);
            bar.Controls.Add(_navIncome);
            bar.Controls.Add(hello);
            bar.Controls.Add(balances);
            bar.Controls.Add(logout);
            return bar;
        }

        private static Button SidebarButton(string text)
        {
            var b = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 44,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                ForeColor = SidebarText,
                BackColor = Sidebar,
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 51, 70);
            return b;
        }

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _lblPage.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
            _lblPage.AutoSize = true;
            _lblPage.Anchor = AnchorStyles.Left;

            var prev = Ui.SecondaryButton("◀");
            var next = Ui.SecondaryButton("▶");
            var today = Ui.SecondaryButton("Hoy");
            prev.Click += (s, e) => { _month = _month.AddMonths(-1); RefreshAll(); };
            next.Click += (s, e) => { _month = _month.AddMonths(1); RefreshAll(); };
            today.Click += (s, e) =>
            {
                _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                RefreshAll();
            };
            _lblMonth.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            _lblMonth.AutoSize = false;
            _lblMonth.Width = 180;
            _lblMonth.Height = 32;
            _lblMonth.TextAlign = ContentAlignment.MiddleCenter;

            var nav = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.Right, WrapContents = false };
            nav.Controls.AddRange(new Control[] { prev, _lblMonth, next, today });

            header.Controls.Add(_lblPage, 0, 0);
            header.Controls.Add(nav, 1, 0);
            return header;
        }

        // ------------------------------------------------------------------ Navegación y datos

        private void ShowPage(Control page, string title, Button nav)
        {
            _incomePage.Visible = page == _incomePage;
            _savingsPage.Visible = page == _savingsPage;
            _lblPage.Text = title;

            foreach (Button b in new[] { _navIncome, _navSavings })
            {
                bool active = b == nav;
                b.BackColor = active ? Ui.Accent : Sidebar;
                b.ForeColor = active ? Color.White : SidebarText;
                b.FlatAppearance.MouseOverBackColor = active ? Ui.Accent : Color.FromArgb(40, 51, 70);
            }

            if (IsHandleCreated) RefreshAll();
        }

        private void RefreshAll()
        {
            _lblMonth.Text = Money.MonthName(_month);
            _incomePage.ShowMonth(_month);
            _savingsPage.ShowMonth(_month);
        }

        /// <summary>
        /// Al abrir la app, si este mes aún no hay ningún ingreso, lo primero que se pide es el ingreso principal.
        /// Si se omite, no se vuelve a preguntar ese mes.
        /// </summary>
        private void PromptMonthlyIncome()
        {
            DateTime now = DateTime.Today;
            var thisMonth = new DateTime(now.Year, now.Month, 1);
            decimal income = 0, savings, expenses;
            bool skipped = false;
            if (!Ui.Guard(this, () =>
            {
                Repository.GetSummary(_user.Id, thisMonth.Year, thisMonth.Month, out income, out savings, out expenses);
                skipped = Repository.IsIncomePromptSkipped(_user.Id, thisMonth.Year, thisMonth.Month);
            })) return;
            if (income > 0 || skipped) return;

            _month = thisMonth;
            using (var dlg = new TransactionDialog(_user.Id, null, true, null, now, thisMonth))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    Ui.Guard(this, () => Repository.AddTransaction(
                        _user.Id, dlg.CategoryId, dlg.Date, dlg.Amount, dlg.Savings, dlg.Description));
                else
                    Ui.Guard(this, () => Repository.SkipIncomePrompt(_user.Id, thisMonth.Year, thisMonth.Month));
            }
            RefreshAll();
        }

        private void EditInitialBalances()
        {
            using (var dlg = new BalancesDialog(_user))
                if (dlg.ShowDialog(this) == DialogResult.OK &&
                    Ui.Guard(this, () => Repository.UpdateInitialBalances(_user.Id, dlg.InitialBalance, dlg.InitialSavings)))
                {
                    _user.InitialBalance = dlg.InitialBalance;
                    _user.InitialSavings = dlg.InitialSavings;
                    RefreshAll();
                }
        }

        private void CheckCelebrations()
        {
            var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            for (int back = 3; back >= 1; back--)
            {
                DateTime m = thisMonth.AddMonths(-back);
                List<BudgetRow> budgets = null;
                bool done = false;
                if (!Ui.Guard(this, () =>
                {
                    done = Repository.IsCelebrated(_user.Id, m.Year, m.Month);
                    if (!done) budgets = Repository.GetBudgets(_user.Id, m.Year, m.Month);
                })) return;
                if (done || !Indicator.MonthSuccess(budgets)) continue;

                if (!Ui.Guard(this, () => Repository.MarkCelebrated(_user.Id, m.Year, m.Month))) return;
                using (var f = new CelebrationForm(_user.Name, m, budgets)) f.ShowDialog(this);
            }
        }
    }
}

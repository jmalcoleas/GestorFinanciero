using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GestorFinancieroApp.Data;
using GestorFinancieroApp.Logic;

namespace GestorFinancieroApp.UI
{
    internal sealed class MainForm : Form
    {
        private readonly User _user;
        private DateTime _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        private readonly Label _lblMonth = new Label();
        private readonly Label _lblAccount = new Label();
        private readonly Label _lblTotalSavings = new Label();
        private readonly Label _lblIncome = new Label();
        private readonly Label _lblMonthSavings = new Label();
        private readonly Label _lblExpenses = new Label();
        private readonly Label _lblAvailable = new Label();
        private readonly Label _lblStatusTitle = new Label();
        private readonly Label _lblStatusDetail = new Label();
        private readonly ColorBar _bar = new ColorBar();
        private readonly DataGridView _gridBudgets = NewGrid();
        private readonly DataGridView _gridTx = NewGrid();

        public bool LoggedOut { get; private set; }

        public MainForm(User user)
        {
            _user = user;
            Text = "Gestor Financiero";
            Icon = Ui.AppIcon;
            Font = Ui.Base;
            ClientSize = new Size(1000, 700);
            MinimumSize = new Size(900, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Ui.Surface;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(BuildTopBar(), 0, 0);
            root.Controls.Add(BuildCards(), 0, 1);
            root.Controls.Add(BuildStatusPanel(), 0, 2);
            root.Controls.Add(BuildTabs(), 0, 3);
            Controls.Add(root);

            Shown += (s, e) =>
            {
                RefreshAll();
                PromptMonthlyIncome();
                CheckCelebrations();
            };
        }

        // ------------------------------------------------------------------ Construcción de la interfaz

        private Control BuildTopBar()
        {
            var bar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var hello = new Label
            {
                Text = "Hola, " + _user.Name,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            var prev = Ui.SecondaryButton("◀");
            var next = Ui.SecondaryButton("▶");
            var today = Ui.SecondaryButton("Hoy");
            prev.Click += (s, e) => ChangeMonth(-1);
            next.Click += (s, e) => ChangeMonth(1);
            today.Click += (s, e) =>
            {
                _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                RefreshAll();
            };
            _lblMonth.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            _lblMonth.AutoSize = false;
            _lblMonth.Width = 190;
            _lblMonth.TextAlign = ContentAlignment.MiddleCenter;
            _lblMonth.Anchor = AnchorStyles.None;

            var nav = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.None, WrapContents = false };
            nav.Controls.AddRange(new Control[] { prev, _lblMonth, next, today });

            var balances = Ui.SecondaryButton("Saldos de partida");
            balances.Click += (s, e) => EditInitialBalances();
            var logout = Ui.SecondaryButton("Cerrar sesión");
            logout.Click += (s, e) => { LoggedOut = true; Close(); };
            var right = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.Right, WrapContents = false };
            right.Controls.AddRange(new Control[] { balances, logout });

            bar.Controls.Add(hello, 0, 0);
            bar.Controls.Add(nav, 1, 0);
            bar.Controls.Add(right, 2, 0);
            return bar;
        }

        private Control BuildCards()
        {
            // Izquierda: totales acumulados de todos los meses. Derecha: el mes que estás viendo.
            var savingsColor = Color.FromArgb(130, 80, 223);
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1 };
            for (int i = 0; i < 6; i++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6));
            t.Controls.Add(Card("Cuenta (total)", _lblAccount, Ui.Accent), 0, 0);
            t.Controls.Add(Card("Ahorros (total)", _lblTotalSavings, savingsColor), 1, 0);
            t.Controls.Add(Card("Ingresos del mes", _lblIncome, Indicator.Good), 2, 0);
            t.Controls.Add(Card("A ahorros este mes", _lblMonthSavings, savingsColor), 3, 0);
            t.Controls.Add(Card("Gastos del mes", _lblExpenses, Indicator.Bad), 4, 0);
            t.Controls.Add(Card("Disponible del mes", _lblAvailable, Ui.Accent), 5, 0);
            return t;
        }

        private static Control Card(string caption, Label value, Color accent)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 8, 8, 8),
                Padding = new Padding(16, 10, 10, 6)
            };
            var strip = new Panel { Dock = DockStyle.Left, Width = 5, BackColor = accent };
            value.Dock = DockStyle.Fill;
            value.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            value.TextAlign = ContentAlignment.MiddleLeft;
            var cap = new Label
            {
                Text = caption,
                Dock = DockStyle.Top,
                Height = 24,
                ForeColor = Indicator.Muted
            };
            card.Controls.Add(value);
            card.Controls.Add(cap);
            card.Controls.Add(strip);
            return card;
        }

        private Control BuildStatusPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0, 0, 8, 8),
                Padding = new Padding(16, 8, 16, 8)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _lblStatusTitle.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            _lblStatusTitle.Dock = DockStyle.Fill;
            _bar.Dock = DockStyle.Fill;
            _bar.Margin = new Padding(0, 4, 0, 4);
            _lblStatusDetail.Dock = DockStyle.Fill;
            _lblStatusDetail.ForeColor = Indicator.Muted;

            panel.Controls.Add(_lblStatusTitle, 0, 0);
            panel.Controls.Add(_bar, 0, 1);
            panel.Controls.Add(_lblStatusDetail, 0, 2);
            return panel;
        }

        private Control BuildTabs()
        {
            var tabs = new TabControl { Dock = DockStyle.Fill };

            var budgetsTab = new TabPage("Presupuestos del mes");
            var addBudget = Ui.PrimaryButton("Nuevo presupuesto");
            var editBudget = Ui.SecondaryButton("Editar");
            var deleteBudget = Ui.SecondaryButton("Eliminar");
            var spend = Ui.SecondaryButton("Apuntar gasto");
            var copy = Ui.SecondaryButton("Copiar del mes anterior");
            addBudget.Click += (s, e) => AddBudget();
            editBudget.Click += (s, e) => EditBudget();
            deleteBudget.Click += (s, e) => DeleteBudget();
            spend.Click += (s, e) => AddTransaction(false, SelectedBudget());
            copy.Click += (s, e) => CopyBudgets();
            _gridBudgets.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditBudget(); };
            AddGridColumns(_gridBudgets, "Destino", "Presupuesto", "Gastado", "Restante", "Uso", "Estado");
            budgetsTab.Controls.Add(_gridBudgets);
            budgetsTab.Controls.Add(Toolbar(addBudget, editBudget, deleteBudget, spend, copy));

            var txTab = new TabPage("Movimientos");
            var addIncome = Ui.PrimaryButton("Nuevo ingreso");
            var addExpense = Ui.PrimaryButton("Nuevo gasto");
            var editTx = Ui.SecondaryButton("Editar");
            var deleteTx = Ui.SecondaryButton("Eliminar");
            addIncome.Click += (s, e) => AddTransaction(true, null);
            addExpense.Click += (s, e) => AddTransaction(false, null);
            editTx.Click += (s, e) => EditTransaction();
            deleteTx.Click += (s, e) => DeleteTransaction();
            _gridTx.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditTransaction(); };
            AddGridColumns(_gridTx, "Fecha", "Descripción", "Categoría", "Tipo", "A ahorros", "Importe");
            txTab.Controls.Add(_gridTx);
            txTab.Controls.Add(Toolbar(addIncome, addExpense, editTx, deleteTx));

            tabs.TabPages.Add(budgetsTab);
            tabs.TabPages.Add(txTab);
            return tabs;
        }

        private static FlowLayoutPanel Toolbar(params Button[] buttons)
        {
            var p = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(4, 6, 4, 6)
            };
            p.Controls.AddRange(buttons);
            return p;
        }

        private static DataGridView NewGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 30 }
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = Ui.Surface;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Ui.Surface;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(60, 64, 70);
            g.ColumnHeadersHeight = 32;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            return g;
        }

        private static void AddGridColumns(DataGridView g, params string[] headers)
        {
            foreach (string h in headers)
            {
                var col = new DataGridViewTextBoxColumn { HeaderText = h, SortMode = DataGridViewColumnSortMode.NotSortable };
                bool numeric = h == "Presupuesto" || h == "Gastado" || h == "Restante" || h == "Uso" || h == "Importe" || h == "A ahorros";
                if (numeric) col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                if (h == "Descripción" || h == "Destino") col.FillWeight = 160;
                g.Columns.Add(col);
            }
        }

        // ------------------------------------------------------------------ Datos

        private void ChangeMonth(int delta)
        {
            _month = _month.AddMonths(delta);
            RefreshAll();
        }

        private void RefreshAll()
        {
            Ui.Guard(this, () =>
            {
                int y = _month.Year, m = _month.Month;
                _lblMonth.Text = Money.MonthName(_month);

                decimal account, totalSavings;
                Repository.GetTotals(_user.Id, out account, out totalSavings);
                _lblAccount.Text = Money.Format(account);
                _lblAccount.ForeColor = account < 0 ? Indicator.Bad : Color.Black;
                _lblTotalSavings.Text = Money.Format(totalSavings);

                // Disponible del mes = ingresos - lo destinado a ahorros - gastos.
                decimal income, savings, expenses;
                Repository.GetSummary(_user.Id, y, m, out income, out savings, out expenses);
                _lblIncome.Text = Money.Format(income);
                _lblMonthSavings.Text = Money.Format(savings);
                _lblExpenses.Text = Money.Format(expenses);
                decimal available = income - savings - expenses;
                _lblAvailable.Text = Money.Format(available);
                _lblAvailable.ForeColor = available < 0 ? Indicator.Bad : Color.Black;

                var budgets = Repository.GetBudgets(_user.Id, y, m);
                FillBudgets(budgets);
                UpdateStatus(budgets);
                FillTransactions(Repository.GetTransactions(_user.Id, y, m));
            });
        }

        private void FillBudgets(List<BudgetRow> budgets)
        {
            _gridBudgets.Rows.Clear();
            foreach (BudgetRow b in budgets)
            {
                Level level = Indicator.ForCategory(b.Amount, b.Spent);
                string status = level == Level.Over ? "Excedido" : level == Level.Warning ? "Casi agotado" : "Bien";
                int i = _gridBudgets.Rows.Add(b.CategoryName, Money.Format(b.Amount), Money.Format(b.Spent),
                    Money.Format(b.Remaining), b.Ratio.ToString("P0", Money.Es), status);

                DataGridViewRow row = _gridBudgets.Rows[i];
                row.Tag = b;
                if (b.Remaining < 0) row.Cells[3].Style.ForeColor = Indicator.Bad;
                DataGridViewCell cell = row.Cells[5];
                Color c = Indicator.ColorOf(level);
                cell.Style.BackColor = cell.Style.SelectionBackColor = c;
                cell.Style.ForeColor = cell.Style.SelectionForeColor = Color.White;
                cell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            _gridBudgets.ClearSelection();
        }

        private void FillTransactions(List<TransactionRow> items)
        {
            _gridTx.Rows.Clear();
            foreach (TransactionRow t in items)
            {
                int i = _gridTx.Rows.Add(t.Date.ToString("dd/MM/yyyy"), t.Description, t.CategoryName,
                    t.IsIncome ? "Ingreso" : "Gasto",
                    t.IsIncome && t.Savings > 0 ? Money.Format(t.Savings) : "",
                    (t.IsIncome ? "+" : "−") + Money.Format(t.Amount));
                DataGridViewRow row = _gridTx.Rows[i];
                row.Tag = t;
                row.Cells[5].Style.ForeColor = t.IsIncome ? Indicator.Good : Indicator.Bad;
            }
            _gridTx.ClearSelection();
        }

        private void UpdateStatus(List<BudgetRow> budgets)
        {
            DateTime today = DateTime.Today;
            decimal totalBudget = budgets.Sum(b => b.Amount);
            decimal spent = budgets.Sum(b => b.Spent);
            Level level = Indicator.Overall(totalBudget, spent, _month, today);
            double expected = Indicator.ExpectedRatio(_month, today);
            bool closed = Indicator.IsClosed(_month, today);
            Color color = Indicator.ColorOf(level);

            _lblStatusTitle.ForeColor = color;
            if (level == Level.None)
            {
                _lblStatusTitle.Text = "Sin presupuestos este mes";
                _lblStatusDetail.Text = "Crea un presupuesto (por ejemplo «Cenas con mi pareja: 55 €») " +
                                        "y aquí verás si vas bien con tus gastos.";
                _bar.Set(0, -1, color);
                return;
            }

            if (level == Level.Good)
                _lblStatusTitle.Text = closed && Indicator.MonthSuccess(budgets) ? "✔ Mes cumplido" : "● Vas bien con tus gastos";
            else if (level == Level.Warning)
                _lblStatusTitle.Text = "● Ojo: vas algo por encima del ritmo del mes";
            else
                _lblStatusTitle.Text = spent > totalBudget ? "● Te has pasado del presupuesto" : "● Estás gastando demasiado rápido";

            double ratio = (double)(spent / totalBudget);
            _bar.Set(ratio, closed ? -1 : expected, color);

            string detail = string.Format("Has gastado {0} de {1} presupuestados ({2}). ",
                Money.Format(spent), Money.Format(totalBudget), ratio.ToString("P0", Money.Es));
            if (!closed)
                detail += string.Format("Llevas {0} del mes (marca oscura en la barra).", expected.ToString("P0", Money.Es));
            _lblStatusDetail.Text = detail;
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

        // ------------------------------------------------------------------ Acciones

        private BudgetRow SelectedBudget()
        {
            return _gridBudgets.CurrentRow == null ? null : _gridBudgets.CurrentRow.Tag as BudgetRow;
        }

        private TransactionRow SelectedTransaction()
        {
            return _gridTx.CurrentRow == null ? null : _gridTx.CurrentRow.Tag as TransactionRow;
        }

        private DateTime DefaultDate()
        {
            DateTime today = DateTime.Today;
            return today.Year == _month.Year && today.Month == _month.Month ? today : _month;
        }

        private void AddBudget()
        {
            using (var dlg = new BudgetDialog(_user.Id, null, _month))
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    if (Ui.Guard(this, () => Repository.SaveBudget(_user.Id, dlg.CategoryId, _month.Year, _month.Month, dlg.Amount)))
                        RefreshAll();
        }

        private void EditBudget()
        {
            BudgetRow b = SelectedBudget();
            if (b == null) { Ui.Warn(this, "Selecciona un presupuesto de la lista."); return; }
            using (var dlg = new BudgetDialog(_user.Id, b, _month))
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    if (Ui.Guard(this, () => Repository.SaveBudget(_user.Id, b.CategoryId, _month.Year, _month.Month, dlg.Amount)))
                        RefreshAll();
        }

        private void DeleteBudget()
        {
            BudgetRow b = SelectedBudget();
            if (b == null) { Ui.Warn(this, "Selecciona un presupuesto de la lista."); return; }
            if (MessageBox.Show(this, "¿Eliminar el presupuesto «" + b.CategoryName + "»?\nLos gastos apuntados no se borran.",
                    "Eliminar presupuesto", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (Ui.Guard(this, () => Repository.DeleteBudget(_user.Id, b.Id))) RefreshAll();
        }

        private void CopyBudgets()
        {
            DateTime prev = _month.AddMonths(-1);
            int copied = 0;
            if (!Ui.Guard(this, () => copied = Repository.CopyBudgets(_user.Id, prev.Year, prev.Month, _month.Year, _month.Month)))
                return;
            MessageBox.Show(this,
                copied == 0
                    ? "No había presupuestos nuevos que copiar de " + Money.MonthName(prev) + "."
                    : "Se copiaron " + copied + " presupuestos de " + Money.MonthName(prev) + ".",
                "Copiar presupuestos", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshAll();
        }

        private void AddTransaction(bool income, BudgetRow forBudget)
        {
            using (var dlg = new TransactionDialog(_user.Id, null, income,
                forBudget == null ? null : forBudget.CategoryName, DefaultDate()))
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    if (Ui.Guard(this, () => Repository.AddTransaction(_user.Id, dlg.CategoryId, dlg.Date, dlg.Amount, dlg.Savings, dlg.Description)))
                    {
                        // Si se apuntó en otro mes, saltamos a él para que el usuario vea el movimiento.
                        if (dlg.Date.Year != _month.Year || dlg.Date.Month != _month.Month)
                            _month = new DateTime(dlg.Date.Year, dlg.Date.Month, 1);
                        RefreshAll();
                    }
        }

        private void EditTransaction()
        {
            TransactionRow t = SelectedTransaction();
            if (t == null) { Ui.Warn(this, "Selecciona un movimiento de la lista."); return; }
            using (var dlg = new TransactionDialog(_user.Id, t, t.IsIncome, null, t.Date))
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    if (Ui.Guard(this, () => Repository.UpdateTransaction(_user.Id, t.Id, dlg.CategoryId, dlg.Date, dlg.Amount, dlg.Savings, dlg.Description)))
                    {
                        if (dlg.Date.Year != _month.Year || dlg.Date.Month != _month.Month)
                            _month = new DateTime(dlg.Date.Year, dlg.Date.Month, 1);
                        RefreshAll();
                    }
        }

        private void DeleteTransaction()
        {
            TransactionRow t = SelectedTransaction();
            if (t == null) { Ui.Warn(this, "Selecciona un movimiento de la lista."); return; }
            if (MessageBox.Show(this, "¿Eliminar «" + (string.IsNullOrEmpty(t.Description) ? t.CategoryName : t.Description) +
                                      "» por " + Money.Format(t.Amount) + "?",
                    "Eliminar movimiento", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (Ui.Guard(this, () => Repository.DeleteTransaction(_user.Id, t.Id))) RefreshAll();
        }
    }
}

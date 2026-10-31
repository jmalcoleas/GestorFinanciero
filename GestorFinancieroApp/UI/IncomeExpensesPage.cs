using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GestorFinancieroApp.Data;
using GestorFinancieroApp.Logic;

namespace GestorFinancieroApp.UI
{
    /// <summary>Página de ingresos y gastos: cifras del mes, movimientos y presupuestos.</summary>
    internal sealed class IncomeExpensesPage : UserControl
    {
        private readonly User _user;
        private DateTime _month;

        private readonly Label _lblAccount = new Label();
        private readonly Label _lblIncome = new Label();
        private readonly Label _lblMonthSavings = new Label();
        private readonly Label _lblExpenses = new Label();
        private readonly Label _lblAvailable = new Label();
        private readonly Label _lblStatusTitle = new Label();
        private readonly Label _lblStatusDetail = new Label();
        private readonly ColorBar _bar = new ColorBar();
        private readonly DataGridView _gridBudgets = Ui.NewGrid();
        private readonly DataGridView _gridTx = Ui.NewGrid();

        /// <summary>Se lanza al guardar un movimiento de otro mes, para que el marco salte a ese mes.</summary>
        public event Action<DateTime> MonthRequested;

        public IncomeExpensesPage(User user)
        {
            _user = user;
            Dock = DockStyle.Fill;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(BuildCards(), 0, 0);
            root.Controls.Add(BuildTabs(), 0, 1);
            Controls.Add(root);
        }

        public void ShowMonth(DateTime month)
        {
            _month = month;
            Reload();
        }

        // ------------------------------------------------------------------ Construcción de la interfaz

        private Control BuildCards()
        {
            // La primera tarjeta acumula todos los meses; las demás son del mes que estás viendo.
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1 };
            for (int i = 0; i < 5; i++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 5));
            t.Controls.Add(Ui.Card("Cuenta (total)", _lblAccount, Ui.Accent), 0, 0);
            t.Controls.Add(Ui.Card("Ingresos del mes", _lblIncome, Indicator.Good), 1, 0);
            t.Controls.Add(Ui.Card("A ahorros este mes", _lblMonthSavings, Ui.Savings), 2, 0);
            t.Controls.Add(Ui.Card("Gastos del mes", _lblExpenses, Indicator.Bad), 3, 0);
            t.Controls.Add(Ui.Card("Disponible del mes", _lblAvailable, Ui.Accent), 4, 0);
            return t;
        }

        private Control BuildStatusPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 96,
                BackColor = Color.White,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12, 6, 12, 6)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _lblStatusTitle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            _lblStatusTitle.Dock = DockStyle.Fill;
            _bar.Dock = DockStyle.Fill;
            _bar.Margin = new Padding(0, 3, 0, 3);
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
            Ui.AddGridColumns(_gridTx, "Fecha", "Descripción", "Categoría", "Tipo", "A ahorros", "Importe");
            txTab.Controls.Add(_gridTx);
            txTab.Controls.Add(Ui.Toolbar(addIncome, addExpense, editTx, deleteTx));

            var budgetsTab = new TabPage("Presupuestos");
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
            Ui.AddGridColumns(_gridBudgets, "Destino", "Presupuesto", "Gastado", "Restante", "Uso", "Estado");
            budgetsTab.Controls.Add(_gridBudgets);
            budgetsTab.Controls.Add(Ui.Toolbar(addBudget, editBudget, deleteBudget, spend, copy));
            budgetsTab.Controls.Add(BuildStatusPanel());

            tabs.TabPages.Add(txTab);
            tabs.TabPages.Add(budgetsTab);
            return tabs;
        }

        // ------------------------------------------------------------------ Datos

        private void Reload()
        {
            Ui.Guard(this, () =>
            {
                int y = _month.Year, m = _month.Month;

                decimal account, totalSavings;
                Repository.GetTotals(_user.Id, out account, out totalSavings);
                _lblAccount.Text = Money.Format(account);
                _lblAccount.ForeColor = account < 0 ? Indicator.Bad : Color.Black;

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

        // Si se guardó en otro mes, se salta a él para ver el movimiento; si no, basta con recargar.
        private void ShowDate(DateTime date)
        {
            if (date.Year == _month.Year && date.Month == _month.Month) Reload();
            else if (MonthRequested != null) MonthRequested(new DateTime(date.Year, date.Month, 1));
        }

        private void AddBudget()
        {
            using (var dlg = new BudgetDialog(_user.Id, null, _month))
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    if (Ui.Guard(this, () => Repository.SaveBudget(_user.Id, dlg.CategoryId, _month.Year, _month.Month, dlg.Amount)))
                        Reload();
        }

        private void EditBudget()
        {
            BudgetRow b = SelectedBudget();
            if (b == null) { Ui.Warn(this, "Selecciona un presupuesto de la lista."); return; }
            using (var dlg = new BudgetDialog(_user.Id, b, _month))
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    if (Ui.Guard(this, () => Repository.SaveBudget(_user.Id, b.CategoryId, _month.Year, _month.Month, dlg.Amount)))
                        Reload();
        }

        private void DeleteBudget()
        {
            BudgetRow b = SelectedBudget();
            if (b == null) { Ui.Warn(this, "Selecciona un presupuesto de la lista."); return; }
            if (MessageBox.Show(this, "¿Eliminar el presupuesto «" + b.CategoryName + "»?\nLos gastos apuntados no se borran.",
                    "Eliminar presupuesto", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (Ui.Guard(this, () => Repository.DeleteBudget(_user.Id, b.Id))) Reload();
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
            Reload();
        }

        private void AddTransaction(bool income, BudgetRow forBudget)
        {
            using (var dlg = new TransactionDialog(_user.Id, null, income,
                forBudget == null ? null : forBudget.CategoryName, DefaultDate()))
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    if (Ui.Guard(this, () => Repository.AddTransaction(_user.Id, dlg.CategoryId, dlg.Date, dlg.Amount, dlg.Savings, dlg.Description)))
                        ShowDate(dlg.Date);
        }

        private void EditTransaction()
        {
            TransactionRow t = SelectedTransaction();
            if (t == null) { Ui.Warn(this, "Selecciona un movimiento de la lista."); return; }
            using (var dlg = new TransactionDialog(_user.Id, t, t.IsIncome, null, t.Date))
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    if (Ui.Guard(this, () => Repository.UpdateTransaction(_user.Id, t.Id, dlg.CategoryId, dlg.Date, dlg.Amount, dlg.Savings, dlg.Description)))
                        ShowDate(dlg.Date);
        }

        private void DeleteTransaction()
        {
            TransactionRow t = SelectedTransaction();
            if (t == null) { Ui.Warn(this, "Selecciona un movimiento de la lista."); return; }
            if (MessageBox.Show(this, "¿Eliminar «" + (string.IsNullOrEmpty(t.Description) ? t.CategoryName : t.Description) +
                                      "» por " + Money.Format(t.Amount) + "?",
                    "Eliminar movimiento", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (Ui.Guard(this, () => Repository.DeleteTransaction(_user.Id, t.Id))) Reload();
        }
    }
}

using System;
using System.Windows.Forms;
using GestorFinancieroApp.Data;
using GestorFinancieroApp.Logic;

namespace GestorFinancieroApp.UI
{
    /// <summary>Corrige los saldos con los que empezaste (dinero en cuenta y ahorros).</summary>
    internal sealed class BalancesDialog : Form
    {
        private readonly TextBox _balance = new TextBox();
        private readonly TextBox _savings = new TextBox();

        public decimal InitialBalance { get; private set; }
        public decimal InitialSavings { get; private set; }

        public BalancesDialog(User user)
        {
            Text = "Saldos de partida";
            Font = Ui.Base;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            _balance.Text = user.InitialBalance.ToString("0.00", Money.Es);
            _savings.Text = user.InitialSavings.ToString("0.00", Money.Es);

            var grid = Ui.NewFormGrid(220);
            var hint = new Label
            {
                Text = "Es el dinero con el que empezaste. A estos saldos se suman los ingresos " +
                       "y se restan los gastos que apuntes.",
                AutoSize = false,
                Height = 50,
                Width = 360,
                ForeColor = Indicator.Muted
            };
            grid.SetColumnSpan(hint, 2);
            grid.Controls.Add(hint, 0, grid.RowCount++);
            Ui.AddRow(grid, "Dinero en la cuenta (€)", _balance);
            Ui.AddRow(grid, "Dinero ahorrado (€)", _savings);

            var ok = Ui.PrimaryButton("Guardar");
            var cancel = Ui.SecondaryButton("Cancelar");
            ok.Click += (s, e) => Save();
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(grid);
            Controls.Add(Ui.ButtonBar(ok, cancel));
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void Save()
        {
            decimal balance, savings;
            if (!Money.TryParse(_balance.Text, out balance) || balance < 0 || balance > Money.Max ||
                !Money.TryParse(_savings.Text, out savings) || savings < 0 || savings > Money.Max)
            {
                Ui.Warn(this, "Los dos importes deben ser números de 0 o más.");
                return;
            }
            InitialBalance = balance;
            InitialSavings = savings;
            DialogResult = DialogResult.OK;
        }
    }
}

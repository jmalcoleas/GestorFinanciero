using System;
using System.Linq;
using System.Windows.Forms;
using GestorFinancieroApp.Data;
using GestorFinancieroApp.Logic;

namespace GestorFinancieroApp.UI
{
    /// <summary>Define cuánto quieres gastar en una categoría durante un mes.</summary>
    internal sealed class BudgetDialog : Form
    {
        private readonly int _userId;
        private readonly ComboBox _category = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, MaxLength = 100 };
        private readonly TextBox _amount = new TextBox();

        public int CategoryId { get; private set; }
        public decimal Amount { get; private set; }

        /// <param name="existing">Presupuesto a editar, o null para uno nuevo.</param>
        public BudgetDialog(int userId, BudgetRow existing, DateTime month)
        {
            _userId = userId;
            Text = (existing == null ? "Nuevo presupuesto" : "Editar presupuesto") + " · " + Money.MonthName(month);
            Font = Ui.Base;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            Ui.Guard(this, () => _category.Items.AddRange(
                Repository.GetCategories(userId, "expense").Select(c => (object)c.Name).ToArray()));

            var grid = Ui.NewFormGrid(260);
            Ui.AddRow(grid, "Destino del gasto", _category);
            Ui.AddRow(grid, "Cantidad (€)", _amount);
            var hint = new Label
            {
                Text = "Ej.: «Cenas con mi pareja» y 55. Si el nombre no existe, se crea la categoría.",
                AutoSize = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = 44,
                ForeColor = Indicator.Muted
            };
            grid.Controls.Add(hint, 1, grid.RowCount++);

            var ok = Ui.PrimaryButton("Guardar");
            var cancel = Ui.SecondaryButton("Cancelar");
            ok.Click += (s, e) => Save();
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(grid);
            Controls.Add(Ui.ButtonBar(ok, cancel));
            AcceptButton = ok;
            CancelButton = cancel;

            if (existing != null)
            {
                _category.Text = existing.CategoryName;
                _category.Enabled = false;
                _amount.Text = existing.Amount.ToString("0.00", Money.Es);
            }
        }

        private void Save()
        {
            string category = _category.Text.Trim();
            decimal amount;
            if (category.Length == 0) { Ui.Warn(this, "Elige o escribe a qué quieres destinar el dinero."); return; }
            if (!Money.TryParse(_amount.Text, out amount) || amount <= 0 || amount > Money.Max)
            {
                Ui.Warn(this, "La cantidad debe ser un número mayor que 0 (por ejemplo 55).");
                return;
            }

            int categoryId = 0;
            if (!Ui.Guard(this, () => categoryId = Repository.GetOrCreateCategory(_userId, category, "expense"))) return;

            CategoryId = categoryId;
            Amount = amount;
            DialogResult = DialogResult.OK;
        }
    }
}

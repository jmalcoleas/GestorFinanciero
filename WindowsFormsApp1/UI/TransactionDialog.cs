using System;
using System.Linq;
using System.Windows.Forms;
using GestorFinancieroApp.Data;
using GestorFinancieroApp.Logic;

namespace GestorFinancieroApp.UI
{
    /// <summary>Alta y edición de ingresos y gastos. Si escribes una categoría nueva, se crea al guardar.</summary>
    internal sealed class TransactionDialog : Form
    {
        private readonly int _userId;
        private readonly ComboBox _type = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _category = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, MaxLength = 100 };
        private readonly DateTimePicker _date = new DateTimePicker { Format = DateTimePickerFormat.Short };
        private readonly TextBox _amount = new TextBox();
        private readonly TextBox _description = new TextBox { MaxLength = 255 };

        public int CategoryId { get; private set; }
        public DateTime Date { get; private set; }
        public decimal Amount { get; private set; }
        public string Description { get; private set; }
        public bool IsIncome { get { return _type.SelectedIndex == 1; } }

        /// <param name="existing">Transacción a editar, o null para una nueva.</param>
        public TransactionDialog(int userId, TransactionRow existing, bool income, string categoryName, DateTime date)
        {
            _userId = userId;
            Text = existing == null ? "Nuevo movimiento" : "Editar movimiento";
            Font = Ui.Base;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            _type.Items.AddRange(new object[] { "Gasto", "Ingreso" });
            _type.SelectedIndex = (existing != null ? existing.IsIncome : income) ? 1 : 0;
            _type.SelectedIndexChanged += (s, e) => { _category.Text = ""; LoadCategories(); };

            var grid = Ui.NewFormGrid(260);
            Ui.AddRow(grid, "Tipo", _type);
            Ui.AddRow(grid, "Categoría", _category);
            Ui.AddRow(grid, "Fecha", _date);
            Ui.AddRow(grid, "Importe (€)", _amount);
            Ui.AddRow(grid, "Descripción", _description);

            var ok = Ui.PrimaryButton("Guardar");
            var cancel = Ui.SecondaryButton("Cancelar");
            ok.Click += (s, e) => Save(existing);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(grid);
            Controls.Add(Ui.ButtonBar(ok, cancel));
            AcceptButton = ok;
            CancelButton = cancel;

            LoadCategories();
            if (existing != null)
            {
                _category.Text = existing.CategoryName;
                _date.Value = existing.Date;
                _amount.Text = existing.Amount.ToString("0.00", Money.Es);
                _description.Text = existing.Description;
            }
            else
            {
                _category.Text = categoryName ?? "";
                _date.Value = date;
            }
        }

        private void LoadCategories()
        {
            _category.Items.Clear();
            string type = IsIncome ? "income" : "expense";
            Ui.Guard(this, () =>
                _category.Items.AddRange(Repository.GetCategories(_userId, type).Select(c => (object)c.Name).ToArray()));
        }

        private void Save(TransactionRow existing)
        {
            string category = _category.Text.Trim();
            decimal amount;
            if (category.Length == 0) { Ui.Warn(this, "Elige o escribe una categoría."); return; }
            if (!Money.TryParse(_amount.Text, out amount) || amount <= 0 || amount > Money.Max)
            {
                Ui.Warn(this, "El importe debe ser un número mayor que 0 (por ejemplo 12,50).");
                return;
            }

            string type = IsIncome ? "income" : "expense";
            int categoryId = 0;
            if (!Ui.Guard(this, () => categoryId = Repository.GetOrCreateCategory(_userId, category, type))) return;

            CategoryId = categoryId;
            Date = _date.Value.Date;
            Amount = amount;
            Description = _description.Text.Trim();
            DialogResult = DialogResult.OK;
        }
    }
}

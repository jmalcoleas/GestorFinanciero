using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GestorFinancieroApp.Data;
using GestorFinancieroApp.Logic;

namespace GestorFinancieroApp.UI
{
    /// <summary>
    /// Alta y edición de ingresos y gastos. Si escribes una categoría nueva, se crea al guardar.
    /// En un ingreso puedes indicar cuánto va directo a ahorros; el resto entra en la cuenta.
    /// </summary>
    internal sealed class TransactionDialog : Form
    {
        private readonly int _userId;
        private readonly ComboBox _type = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _category = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, MaxLength = 100 };
        private readonly DateTimePicker _date = new DateTimePicker { Format = DateTimePickerFormat.Short };
        private readonly TextBox _amount = new TextBox();
        private readonly TextBox _savings = new TextBox { Text = "0" };
        private readonly Label _split = new Label { AutoSize = false, Height = 22, ForeColor = Indicator.Muted };
        private readonly TextBox _description = new TextBox { MaxLength = 255 };
        private Label _savingsLabel;

        public int CategoryId { get; private set; }
        public DateTime Date { get; private set; }
        public decimal Amount { get; private set; }
        public decimal Savings { get; private set; }
        public string Description { get; private set; }
        public bool IsIncome { get { return _type.SelectedIndex == 1; } }

        /// <param name="existing">Transacción a editar, o null para una nueva.</param>
        /// <param name="monthPrompt">Si no es null, el diálogo se usa como "ingreso principal" de ese mes (solo ingreso, con opción de omitir).</param>
        public TransactionDialog(int userId, TransactionRow existing, bool income, string categoryName, DateTime date,
            DateTime? monthPrompt = null)
        {
            _userId = userId;
            Text = monthPrompt.HasValue ? "Ingreso principal de " + Money.MonthName(monthPrompt.Value).ToLower(Money.Es)
                 : existing == null ? "Nuevo movimiento" : "Editar movimiento";
            Font = Ui.Base;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            _type.Items.AddRange(new object[] { "Gasto", "Ingreso" });
            _type.SelectedIndex = (monthPrompt.HasValue || (existing != null ? existing.IsIncome : income)) ? 1 : 0;
            _type.Enabled = !monthPrompt.HasValue;
            _type.SelectedIndexChanged += (s, e) => { _category.Text = ""; LoadCategories(); UpdateSavingsVisibility(); };

            var grid = Ui.NewFormGrid(280);
            if (monthPrompt.HasValue)
            {
                var intro = new Label
                {
                    Text = "Empieza el mes apuntando tu ingreso y cuánto de él va a ahorros. " +
                           "El resto quedará disponible para gastar.",
                    AutoSize = false,
                    Height = 50,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right,
                    ForeColor = Indicator.Muted
                };
                grid.SetColumnSpan(intro, 2);
                grid.Controls.Add(intro, 0, grid.RowCount++);
            }
            Ui.AddRow(grid, "Tipo", _type);
            Ui.AddRow(grid, "Categoría", _category);
            Ui.AddRow(grid, "Fecha", _date);
            Ui.AddRow(grid, "Importe (€)", _amount);
            _savingsLabel = Ui.AddRow(grid, "De ello a ahorros (€)", _savings);
            _split.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            grid.Controls.Add(_split, 1, grid.RowCount++);
            Ui.AddRow(grid, "Descripción", _description);

            _amount.TextChanged += (s, e) => UpdateSplit();
            _savings.TextChanged += (s, e) => UpdateSplit();

            var ok = Ui.PrimaryButton("Guardar");
            var cancel = Ui.SecondaryButton(monthPrompt.HasValue ? "Omitir este mes" : "Cancelar");
            ok.Click += (s, e) => Save();
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
                _savings.Text = existing.Savings.ToString("0.00", Money.Es);
                _description.Text = existing.Description;
            }
            else
            {
                _category.Text = categoryName ?? (monthPrompt.HasValue ? "Salario" : "");
                _date.Value = date;
            }
            UpdateSavingsVisibility();
        }

        private void UpdateSavingsVisibility()
        {
            _savings.Visible = _savingsLabel.Visible = _split.Visible = IsIncome;
            UpdateSplit();
        }

        private void UpdateSplit()
        {
            decimal amount, savings;
            if (!IsIncome) return;
            if (Money.TryParse(_amount.Text, out amount) && Money.TryParse(_savings.Text, out savings) &&
                savings >= 0 && savings <= amount)
            {
                _split.ForeColor = Indicator.Muted;
                _split.Text = "A la cuenta: " + Money.Format(amount - savings);
            }
            else
            {
                _split.ForeColor = Indicator.Bad;
                _split.Text = "Los ahorros no pueden superar el importe";
            }
        }

        private void LoadCategories()
        {
            _category.Items.Clear();
            string type = IsIncome ? "income" : "expense";
            Ui.Guard(this, () =>
                _category.Items.AddRange(Repository.GetCategories(_userId, type).Select(c => (object)c.Name).ToArray()));
        }

        private void Save()
        {
            string category = _category.Text.Trim();
            decimal amount, savings = 0;
            if (category.Length == 0) { Ui.Warn(this, "Elige o escribe una categoría."); return; }
            if (!Money.TryParse(_amount.Text, out amount) || amount <= 0 || amount > Money.Max)
            {
                Ui.Warn(this, "El importe debe ser un número mayor que 0 (por ejemplo 12,50).");
                return;
            }
            if (IsIncome && (!Money.TryParse(_savings.Text, out savings) || savings < 0 || savings > amount))
            {
                Ui.Warn(this, "Lo que va a ahorros debe ser un número entre 0 y el importe del ingreso.");
                return;
            }

            string type = IsIncome ? "income" : "expense";
            int categoryId = 0;
            if (!Ui.Guard(this, () => categoryId = Repository.GetOrCreateCategory(_userId, category, type))) return;

            CategoryId = categoryId;
            Date = _date.Value.Date;
            Amount = amount;
            Savings = IsIncome ? savings : 0;
            Description = _description.Text.Trim();
            DialogResult = DialogResult.OK;
        }
    }
}

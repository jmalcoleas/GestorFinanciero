using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GestorFinancieroApp.Data;
using GestorFinancieroApp.Logic;
using GestorFinancieroApp.Security;

namespace GestorFinancieroApp.UI
{
    internal sealed class LoginForm : Form
    {
        private readonly TextBox _name = new TextBox { MaxLength = 100 };
        private readonly TextBox _email = new TextBox { MaxLength = 150 };
        private readonly TextBox _pass = new TextBox { UseSystemPasswordChar = true, MaxLength = 100 };
        private readonly TextBox _pass2 = new TextBox { UseSystemPasswordChar = true, MaxLength = 100 };
        private readonly TextBox _balance = new TextBox { Text = "0" };
        private readonly TextBox _savings = new TextBox { Text = "0" };
        private Label _nameLabel, _pass2Label, _balanceLabel, _savingsLabel, _title;
        private Button _submit, _toggle;
        private bool _registerMode;

        public User User { get; private set; }

        public LoginForm()
        {
            Text = "Gestor Financiero";
            Icon = Ui.AppIcon;
            Font = Ui.Base;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            _title = new Label
            {
                Text = "Gestor Financiero",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Ui.Accent,
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(16, 16, 16, 0)
            };

            var grid = Ui.NewFormGrid(240);
            _nameLabel = Ui.AddRow(grid, "Nombre", _name);
            Ui.AddRow(grid, "Correo", _email);
            Ui.AddRow(grid, "Contraseña", _pass);
            _pass2Label = Ui.AddRow(grid, "Repite contraseña", _pass2);
            _balanceLabel = Ui.AddRow(grid, "Dinero en la cuenta (€)", _balance);
            _savingsLabel = Ui.AddRow(grid, "Dinero ahorrado (€)", _savings);

            _submit = Ui.PrimaryButton("Entrar");
            _toggle = new Button
            {
                Text = "¿No tienes cuenta? Crear una",
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Ui.Accent
            };
            _toggle.FlatAppearance.BorderSize = 0;
            _submit.Click += (s, e) => Submit();
            _toggle.Click += (s, e) => SetMode(!_registerMode);

            // El orden de alta importa: lo último añadido se acopla primero.
            Controls.Add(grid);
            Controls.Add(Ui.ButtonBar(_submit, _toggle));
            Controls.Add(_title);
            AcceptButton = _submit;
            SetMode(false);
        }

        private void SetMode(bool register)
        {
            _registerMode = register;
            _name.Visible = _nameLabel.Visible = register;
            _pass2.Visible = _pass2Label.Visible = register;
            _balance.Visible = _balanceLabel.Visible = register;
            _savings.Visible = _savingsLabel.Visible = register;
            _submit.Text = register ? "Crear cuenta" : "Entrar";
            _toggle.Text = register ? "Ya tengo cuenta" : "¿No tienes cuenta? Crear una";
            _pass.Clear();
            _pass2.Clear();
        }

        private void Submit()
        {
            string email = _email.Text.Trim().ToLowerInvariant();
            string pass = _pass.Text;

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                Ui.Warn(this, "Escribe un correo válido.");
                return;
            }

            if (_registerMode) Register(email, pass);
            else Login(email, pass);
        }

        private void Login(string email, string pass)
        {
            User found = null;
            if (!Ui.Guard(this, () => found = Repository.FindUserByEmail(email))) return;

            // Mismo mensaje si falla el correo o la contraseña, para no revelar qué cuentas existen.
            if (found == null || !PasswordHasher.Verify(pass, found.PasswordHash))
            {
                MessageBox.Show(this, "Correo o contraseña incorrectos.", "No se pudo entrar",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _pass.Clear();
                return;
            }

            found.PasswordHash = null;
            User = found;
            DialogResult = DialogResult.OK;
        }

        private void Register(string email, string pass)
        {
            string name = _name.Text.Trim();
            if (name.Length == 0) { Ui.Warn(this, "Escribe tu nombre."); return; }
            if (pass.Length < 8) { Ui.Warn(this, "La contraseña debe tener al menos 8 caracteres."); return; }
            if (pass != _pass2.Text) { Ui.Warn(this, "Las contraseñas no coinciden."); return; }

            decimal balance, savings;
            if (!Money.TryParse(_balance.Text, out balance) || balance < 0 || balance > Money.Max ||
                !Money.TryParse(_savings.Text, out savings) || savings < 0 || savings > Money.Max)
            {
                Ui.Warn(this, "El dinero en la cuenta y el ahorrado deben ser números de 0 o más (pon 0 si no tienes).");
                return;
            }

            User created = null;
            bool exists = false;
            bool ok = Ui.Guard(this, () =>
            {
                exists = Repository.EmailExists(email);
                if (!exists) created = Repository.CreateUser(name, email, PasswordHasher.Hash(pass), balance, savings);
            });
            if (!ok) return;
            if (exists) { Ui.Warn(this, "Ya existe una cuenta con ese correo."); return; }

            User = created;
            DialogResult = DialogResult.OK;
        }
    }
}

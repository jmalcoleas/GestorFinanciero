using System;
using System.Data.Common;
using System.Drawing;
using System.Windows.Forms;

namespace GestorFinancieroApp.UI
{
    internal static class Ui
    {
        public static readonly Font Base = new Font("Segoe UI", 10f);
        public static readonly Color Accent = Color.FromArgb(31, 111, 235);
        public static readonly Color Surface = Color.FromArgb(244, 246, 249);
        public static readonly Color Savings = Color.FromArgb(130, 80, 223);

        /// <summary>Icono del ejecutable, para la barra de título y la barra de tareas.</summary>
        public static readonly Icon AppIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        public static Form NewDialog(string title)
        {
            return new Form
            {
                Text = title,
                Font = Base,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
        }

        public static TableLayoutPanel NewFormGrid(int minInputWidth)
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                Padding = new Padding(16, 16, 16, 8)
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, minInputWidth));
            return t;
        }

        public static Label AddRow(TableLayoutPanel t, string label, Control input)
        {
            int row = t.RowCount++;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var l = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 14, 8) };
            input.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            input.Margin = new Padding(0, 6, 0, 6);
            t.Controls.Add(l, 0, row);
            t.Controls.Add(input, 1, row);
            return l;
        }

        public static Button PrimaryButton(string text)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                Padding = new Padding(10, 3, 10, 3),
                FlatStyle = FlatStyle.Flat,
                BackColor = Accent,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        public static Button SecondaryButton(string text)
        {
            return new Button { Text = text, AutoSize = true, Padding = new Padding(10, 3, 10, 3) };
        }

        /// <summary>Barra inferior de botones alineados a la derecha.</summary>
        public static FlowLayoutPanel ButtonBar(params Button[] buttons)
        {
            var p = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12, 8, 12, 12)
            };
            foreach (Button b in buttons) p.Controls.Add(b);
            return p;
        }

        /// <summary>Tarjeta con un título pequeño y una cifra grande.</summary>
        public static Control Card(string caption, Label value, Color accent)
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
                ForeColor = Color.FromArgb(110, 118, 129)
            };
            card.Controls.Add(value);
            card.Controls.Add(cap);
            card.Controls.Add(strip);
            return card;
        }

        public static FlowLayoutPanel Toolbar(params Button[] buttons)
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

        public static DataGridView NewGrid()
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
            g.ColumnHeadersDefaultCellStyle.BackColor = Surface;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Surface;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(60, 64, 70);
            g.ColumnHeadersHeight = 32;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            return g;
        }

        public static void AddGridColumns(DataGridView g, params string[] headers)
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

        public static void Warn(IWin32Window owner, string message)
        {
            MessageBox.Show(owner, message, "Revisa los datos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>Ejecuta una acción de base de datos mostrando el error en vez de cerrar la aplicación.</summary>
        public static bool Guard(IWin32Window owner, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (DbException ex)
            {
                MessageBox.Show(owner, "Error de base de datos:\n" + ex.Message, "Gestor Financiero",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }

    /// <summary>Barra de progreso con color propio y una marca con el ritmo esperado del mes.</summary>
    internal sealed class ColorBar : Control
    {
        private double _ratio, _expected = -1;
        private Color _fill = Color.Gray;

        public ColorBar()
        {
            DoubleBuffered = true;
            Height = 18;
        }

        public void Set(double ratio, double expected, Color fill)
        {
            _ratio = Math.Max(0, ratio);
            _expected = expected;
            _fill = fill;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.FromArgb(225, 229, 234));
            int w = (int)Math.Round(Math.Min(_ratio, 1.0) * Width);
            using (var b = new SolidBrush(_fill)) e.Graphics.FillRectangle(b, 0, 0, w, Height);
            if (_expected > 0 && _expected < 1)
            {
                int x = (int)Math.Round(_expected * Width);
                using (var p = new Pen(Color.FromArgb(60, 64, 70), 2)) e.Graphics.DrawLine(p, x, 0, x, Height);
            }
        }
    }
}

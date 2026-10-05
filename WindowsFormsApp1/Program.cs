using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using GestorFinancieroApp.Data;
using GestorFinancieroApp.Logic;
using GestorFinancieroApp.UI;

namespace GestorFinancieroApp
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            CultureInfo.DefaultThreadCurrentCulture = Money.Es;
            CultureInfo.DefaultThreadCurrentUICulture = Money.Es;
            Thread.CurrentThread.CurrentCulture = Money.Es;
            Thread.CurrentThread.CurrentUICulture = Money.Es;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Db.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo preparar la base de datos:\n\n" + ex.Message +
                                (ex.InnerException != null ? "\n\n" + ex.InnerException.Message : ""),
                    "Gestor Financiero", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Bucle sesión: login -> ventana principal -> (cerrar sesión) -> login.
            while (true)
            {
                User user;
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK) return;
                    user = login.User;
                }

                using (var main = new MainForm(user))
                {
                    Application.Run(main);
                    if (!main.LoggedOut) return;
                }
            }
        }
    }
}

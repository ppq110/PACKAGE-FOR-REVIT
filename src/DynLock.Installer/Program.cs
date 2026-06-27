using System;
using System.Windows.Forms;

namespace DynLock.Installer
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var login = new LoginForm())
            {
                try
                {
                    login.Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                        System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
                catch { }

                if (login.ShowDialog() != DialogResult.OK)
                    return;
            }

            var form = new MainForm();
            try
            {
                // Lay icon tu chinh file exe (da embed BimDynamo.ico qua ApplicationIcon)
                form.Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            catch { }
            Application.Run(form);
        }
    }
}

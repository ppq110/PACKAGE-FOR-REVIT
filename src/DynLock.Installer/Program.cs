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

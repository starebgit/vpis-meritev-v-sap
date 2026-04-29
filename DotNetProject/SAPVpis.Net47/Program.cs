using System;
using System.Windows.Forms;
using SAPVpis.Net47.Forms;

namespace SAPVpis.Net47
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}

using System;
using System.Windows.Forms;

namespace CSVParserTool
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            NetFxAssemblyLoadFix.Register();

            if (ToolUpdateService.TryRunInstallerMode(args))
                return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}

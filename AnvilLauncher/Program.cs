using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnvilLauncher.Core;

namespace AnvilLauncher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] p_Arguments)
        {
            if (p_Arguments.Length >= 2)
            {
                var l_flag = p_Arguments[0];
                var l_buildDirectory = p_Arguments[1];

                if (l_flag == "-b")
                {
                    var s_Builder = new ManifestBuilder(l_buildDirectory);
                    var s_UpdateTask = s_Builder.GenerateUpdate(l_buildDirectory, 0);

                    Task.WaitAll(s_UpdateTask);
                    return;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMain());
        }
    }
}

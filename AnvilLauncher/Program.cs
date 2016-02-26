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
            if (p_Arguments.Length >= 6)
            {
                var s_Flag = p_Arguments[0];

                var s_BuildDirectory = p_Arguments[1];
                var s_OutputDirectory = p_Arguments[2];
                var s_BuildNumber = uint.Parse(p_Arguments[3]);
                var s_Commit = p_Arguments[4];
                var s_BaseAddress = p_Arguments[5];

                if (s_Flag == "-b")
                {
                    var s_Builder = new ManifestBuilder(s_BuildDirectory);
                    var s_UpdateTask = s_Builder.GenerateUpdate(s_BuildDirectory, s_OutputDirectory, s_BuildNumber, s_Commit, s_BaseAddress);

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

using System;
using System.Diagnostics;
using System.Windows.Forms;
namespace Malaco
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Process myProcess = new Process();
            //myProcess.StartInfo.UseShellExecute = true;
            //myProcess.StartInfo.FileName = $"{Environment.CurrentDirectory}\\java\\jdk-13.0.2\\bin\\java.exe";
            //myProcess.StartInfo.Arguments = $"-jar {Environment.CurrentDirectory + "\\Lavalink.jar"}";
            //myProcess.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new User_Interface());
        }
    }
}

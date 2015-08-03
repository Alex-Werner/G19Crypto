using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace G19Crypto
{
    class Program
    {
        private static Mutex Mutex = new Mutex(true, "G19Info");

        [STAThread]
        static void Main()
        {
            if (Mutex.WaitOne(TimeSpan.Zero, true))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new HomeScreen());
                Mutex.ReleaseMutex();
            }
            else
            {
                MessageBox.Show("Only one instance at a time is allowed.", "Error: trying to start multiple instances", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }
    }
}

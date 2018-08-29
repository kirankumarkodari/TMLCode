using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace PDFInvoker
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new frmPDFInvoker());
            }
            catch (Exception ex)
            {
                //Write ex.Message to a file
                using (StreamWriter outfile = new StreamWriter(@".\error.txt"))
                {
                    outfile.Write(ex.Message.ToString());
                }
            }           
        }
    }
}

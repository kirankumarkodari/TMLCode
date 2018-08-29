using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PDFInvoker
{
    public partial class ExitDialog : Form
    {
        public ExitDialog()
        {
            InitializeComponent();            
        }

        public string GetPassword()
        {
            return tbPassword.Text;
        }

        public void  clearPassword()
        {
            tbPassword.Text = "";
        }
    }
}

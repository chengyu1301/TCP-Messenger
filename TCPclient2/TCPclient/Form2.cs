using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TCPclient
{
    public partial class Form2 : Form
    {
        Form1 f;

        public Form2(Form1 fo)
        {
            InitializeComponent();
            f = (Form1) fo;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            f.setIP(textBox1.Text, textBox2.Text);
            f.showup();
            this.Close();
        }
    }
}

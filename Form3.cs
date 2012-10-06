using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LengthSlope
{
    public partial class Form3 : Form
    {
        private bool open = true;
        public Form3()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (open)
            {
                if (this.Opacity <= 0.95)
                {
                    this.Opacity += 0.05;
                }
            }
            else
            {
                if (this.Opacity > 0.2)
                {
                    this.Opacity -= 0.2;
                }
                else
                {
                    this.timer1.Enabled = false;
                    this.Close();
                }
            }
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            this.timer1.Enabled = true;
        }

        private void Form3_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            open = false;
        }

        private void label1_Click(object sender, EventArgs e)
        {
            open = false;
        }
    }
}
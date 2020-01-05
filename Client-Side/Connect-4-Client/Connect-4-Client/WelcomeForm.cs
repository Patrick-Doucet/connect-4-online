using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Connect_4_Client
{
    public partial class WelcomeForm : Form
    {
        public string nameText = ""; // Contains the name given by the user

        /// <summary>
        /// Constructor to the WelcomeForm
        /// </summary>
        public WelcomeForm()
        {
            // Initial configs
            InitializeComponent();
            CenterToScreen();
        }

        /// <summary>
        /// Every time the text is changed in the text box, save the contents in the nameText variable
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            TextBox objTextBox = (TextBox)sender; // Create temporary textbox with the content of the textbox on screen
            nameText = objTextBox.Text; // save its content
        }

        /// <summary>
        /// Close the application if X is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WelcomeForm_FormClosing(object sender, EventArgs e)
        {
            Application.Exit();
        }
        
        /// <summary>
        /// When the enter button is clicked, close the current window (not the whole application)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}

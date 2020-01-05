using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Connect_4_Client
{
    public partial class AcceptForm : Form
    {
        Socket clientServerSocket; // Reference to server socket
        string[] uInfo; // user information
        public bool wantsToPlay = false; // boolean representing if you clicked accept:true or reject:false

        /// <summary>
        /// Constructor of accept form, shows who wants to play vs you and the options accept/reject
        /// </summary>
        /// <param name="s">Reference to server socket</param>
        /// <param name="userInfo">Information of the user that wants to play vs you</param>
        public AcceptForm(Socket s, string userInfo)
        {
            // Initial config
            InitializeComponent();
            CenterToScreen();

            // Save class reference to server socket
            clientServerSocket = s;

            // split user info into uInfo[0] = name and uInfo[1] = IP
            uInfo = userInfo.Split(':');

            // Put the name of the user to display
            label1.Text = label1.Text.Insert(0, uInfo[0]);
        }

        /// <summary>
        /// Accept button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            // Send an accept message to server socket
            string sendString = "##accept##";
            byte[] bytesToSend = Encoding.ASCII.GetBytes(sendString);
            clientServerSocket.Send(bytesToSend);

            // Affect wantsToPlay = true so the parent window can know what button was pressed
            wantsToPlay = true;

            // Close this window
            this.Close();
        }

        /// <summary>
        /// Reject button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            // Send an reject message to server socket
            string sendString = "##reject##";
            byte[] bytesToSend = Encoding.ASCII.GetBytes(sendString);
            clientServerSocket.Send(bytesToSend);

            // Close this window
            this.Close();
        }
    }
}

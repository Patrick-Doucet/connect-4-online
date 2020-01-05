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
    public partial class WinForm : Form
    {
        private GameForm game; // Reference to the gameform parent window
        private bool win; // Contains if we won or not
        private int playerWinCount; // Contains how many times you have won
        private int opponentWinCount; // Contains how many times the opponent has won

        /// <summary>
        /// Constructor for the WinForm
        /// </summary>
        /// <param name="gameForm">Reference to the parent GameForm</param>
        public WinForm(GameForm gameForm)
        {
            // Initial configs
            InitializeComponent();
            CenterToScreen();

            // Keep class references to the values from the parent form (gameForm)
            game = gameForm;
            win = game.turn; // Who won
            playerWinCount = game.playerWins; // how many times you won
            opponentWinCount = game.opponentWins; // how many times opponent has won
            updateLabels(); // update all the text labels
        }

        /// <summary>
        /// Update all the labels shown on the window
        /// </summary>
        private void updateLabels()
        {
            // Show "You Won!" if you won, and "You Lost!" if you did not win
            if (win)
            {
                label1.Text = "You Won!";
            }
            else
            {
                label1.Text = "You Lost!";
            }

            // Update playerWin - opponentWin labels with current scores
            label5.Text = opponentWinCount.ToString();
            label3.Text = playerWinCount.ToString();
        }

        /// <summary>
        /// Play Again button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            // Send message to other user that he wishes to play again
            string playAgain = "##play##";

            // call isReset method from parent GameForm
            game.isReset(playAgain);
            
            // close the current form
            this.Close();
        }

        /// <summary>
        /// Leave button event click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            // Send message to other user that he doesn't wish to play again
            string stopPlaying = "##stop##";

            // call isReset method from parent GameForm
            game.isReset(stopPlaying);

            // close the current form
            this.Close();
        }
    }
}

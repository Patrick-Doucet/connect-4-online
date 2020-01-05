using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Connect_4_Client
{
    public partial class GameForm : Form
    {

        private Rectangle[] gameBoardColumns; // Columns of the gameboard
        private int rows = 6; // Number of rows for the gameboard
        private int columns = 7; // Number of columns for the gameboard
        private int[,] boardState; // Initialized at 0 (State.Blank)
        private enum State { Blank, Red, Black }; // The possible states a square can be (Blank = 0, Red = 1, Black = 2)
        public bool turn; // Is it your turn?
        private bool isStarting; // If you have first turn
        private State color; // What color you are playing (Red|Black)
        public string pName; // Your username
        public string oName; // The opponents username
        public int playerWins = 0; // How many times you have won
        public int opponentWins = 0; // How many times the opponent has won
        TcpClient client; // The Tcp socket from client to client
        int port = 11123; // The port to connet to
        string ipToConnect; // The ip to connect to if you were the one who joined the game
        public bool isClosed = false; // Was the game closed?
        NetworkStream nwStream; // The network stream to read/write the data to


        /// <summary>
        /// Constructor for the GameForm
        /// </summary>
        /// <param name="starting">If you have the first move</param>
        /// <param name="playerName">Your name</param>
        /// <param name="opponentName">Opponent name</param>
        /// <param name="ip">If you are joining the game, you will have the ip of the socket to connect to</param>
        public GameForm(bool starting, string playerName, string opponentName, string ip = null)
        {
            // Initial configs
            InitializeComponent();
            CenterToScreen();

            // Class references to passed in parameters
            isStarting = starting;
            ipToConnect = ip;
            pName = playerName;
            oName = opponentName;
            updateLabelText(); // update all text labels

            // Initialize gameboard state logic
            gameBoardColumns = new Rectangle[7];
            boardState = new int[rows, columns]; // initialized at 0

            // if you are starting, you have to create the listener to let your opponent connect to you
            if (isStarting)
            {
                // listener for the opponent
                TcpListener listener;
                listener = new TcpListener(IPAddress.Any, port); // listen for any type of ip on the given port (11123)
                listener.Start(); // start listener

                try
                {
                    // accept client and keep socket reference
                    client = listener.AcceptTcpClient();
                }
                catch
                {
                    // if an issue occur, put socket to null and output message
                    client = null;
                    MessageBox.Show("Something went wrong");
                }
                
                turn = true; // You are starting, so it is your turn
                color = State.Red; // You play as red as default
            }
            else if (ipToConnect != null)
            {
                // Establish the remote endpoint for the socket.  
                // connects to ipToConnect
                // uses port 11123
                client = new TcpClient();
                try
                {
                    client.Connect(ipToConnect, port); // connect to listener
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Something went wrong");
                }

                turn = false; // You start second
                color = State.Black; // You play as black
            }

            nwStream = client.GetStream(); // Create stream binded to the client socket
            nwStream.ReadTimeout = 100; // Read for data every 100 milliseconds

            // If it isn't your turn, listen at the beginning
            if(!turn)
            {
                // If the game was closed, don't try to read
                if (!isClosed)
                {
                    //---get the incoming data through a network stream---
                    byte[] buffer = new byte[client.ReceiveBufferSize];
                    int bytesRead = 0;

                    // try to read data every 100 milliseconds, leave once you receive something
                    while (bytesRead == 0)
                    {
                        try
                        {
                            //---read incoming stream---
                            bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);
                        }
                        catch
                        {
                            //MessageBox.Show("Waiting for player move");
                        }
                    }

                    //---convert the data received into a int---
                    string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead); // Convert to string
                    int column = -1;
                    int.TryParse(dataReceived, out column); // parse string and convert to int

                    // update board
                    drawMove(column);
                }
            }
        }

        /// <summary>
        /// Update all window labels
        /// </summary>
        private void updateLabelText()
        {
            // Labels change based on if you are red or black
            if(isStarting)
            {
                // Show correct player names
                label1.Text = "Red: " + pName + " - " + playerWins;
                label2.Text = "Black: " + oName + " - " + opponentWins;
            }
            else
            {
                // Show correct player names
                label1.Text = "Red: " + oName + " - " + opponentWins;
                label2.Text = "Black: " + pName + " - " + playerWins;
            }
        }

        /// <summary>
        /// Draw the graphics on the window (gets called after form constructor)
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); // override base function

            // Create blue background
            e.Graphics.FillRectangle(Brushes.CornflowerBlue, 50, 20, 700, 500);

            // For each column/row, draw circles to represent each possible playable square of the grid
            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    // 0 means we need to draw White -> State.Blank = 0
                    if(boardState[j, i] == 0)
                    {
                        // Draw the white circles on the gameBoard
                        e.Graphics.FillEllipse(Brushes.White, 70 + 100 * i, 40 + 80 * j, 60, 60);
                    }
                    // 1 means we need to draw Red -> State.Red = 1
                    else if(boardState[j, i] == 1)
                    {
                        // Draw the red circles on the gameBoard
                        e.Graphics.FillEllipse(Brushes.Red, 70 + 100 * i, 40 + 80 * j, 60, 60);
                    }
                    // 2 means we need to draw Black -> State.Black = 2
                    else if (boardState[j, i] == 2)
                    {
                        // Draw the black circles on the gameBoard
                        e.Graphics.FillEllipse(Brushes.Black, 70 + 100 * i, 40 + 80 * j, 60, 60);
                    }
                    // Set the bounds of each column
                    gameBoardColumns[i] = new Rectangle(70 + 100 * i, 20, 70, 500); // This sets the clickable bound of the columns (so we know which column was clicked)
                }
            }
        }

        /// <summary>
        /// Detected where in the column the move should be drawn, and draw it
        /// </summary>
        /// <param name="clickedColumn">The column to draw the move</param>
        public void drawMove(int clickedColumn)
        {
            // Draw chip on click column if it is not full
            for (int i = rows - 1; i >= 0; i--)
            {
                // First detected blank state in board
                if (boardState[i, clickedColumn] == 0)
                {
                    // Get the State to draw
                    State drawingColor = getTurnState();
                    // Change state of board
                    boardState[i, clickedColumn] = (int)drawingColor;
                    // Draw colored chip inside of it
                    Graphics g = CreateGraphics();
                    Brush currentColor = getTurnColor(drawingColor);

                    // If an invalid turn
                    if (currentColor == null) throw new Exception("Expected a State.Red or State.Black turn");

                    // If the turn is valid, draw the associated colored chip
                    g.FillEllipse(currentColor, 70 + 100 * clickedColumn, 40 + 80 * i, 60, 60);

                    // Check if it was a winning move
                    if (isWin(i, clickedColumn))
                    {
                        // Increment the score of whoever won the game
                        if (turn) playerWins++;
                        else opponentWins++;
                        // Open the win form
                        openWinForm();
                        return;
                    }
                    turn = !turn; // Flip who's turn it is
                    break;
                }
            }
        }

        /// <summary>
        /// GameForm mouse click event (if you clicked the board)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GameForm_MouseClick(object sender, MouseEventArgs e)
        {
            // Check if it's your turn, if so, send your move
            if (turn)
            {
                // Check which column we have click (if we did click one)
                int clickedColumn = findClickedColumnIndex(e.Location);

                // Invalid move
                if (clickedColumn == -1) return;

                // Send your move to the other player
                //---get the incoming data through a network stream---
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(clickedColumn.ToString());

                // write to networkstream
                nwStream.Write(bytesToSend, 0, bytesToSend.Length);

                // draw the move on your end
                drawMove(clickedColumn);
            }

            // If the game was closed, don't try to read
            if (!isClosed)
            {
                //---get the incoming data through a network stream---
                byte[] buffer = new byte[client.ReceiveBufferSize];
                int bytesRead = 0;

                // Wait for the other player's move
                while (bytesRead == 0)
                {
                    try
                    {
                        //---read incoming stream---
                        bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);
                    }
                    catch
                    {
                        //MessageBox.Show("Waiting for player move");
                    }
                }

                //---convert the data received into a int---
                string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead); // Convert to string

                int column = -1;
                int.TryParse(dataReceived, out column); // parse string and convert to int

                // Update move
                drawMove(column);
            }
        }

        /// <summary>
        /// Check if we need to reset the board or close the game
        /// </summary>
        /// <param name="answer">The answer to if the player wants to play again</param>
        public void isReset(string answer)
        {
            // send response to the other player
            byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(answer);

            nwStream.Write(bytesToSend, 0, bytesToSend.Length);

            //---get the incoming data through a network stream---
            byte[] buffer = new byte[client.ReceiveBufferSize];
            int bytesRead = 0;

            // Wait for the other player's answer
            while (bytesRead == 0)
            {
                try
                {
                    //---read incoming stream---
                    bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);
                }
                catch
                {
                    //MessageBox.Show("Waiting for player move");
                }
            }

            //---convert the data received into a int---
            string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            // 4 possible cases
            // - play/play -> reset board and play again
            // - play/leave -> leave game
            // - leave/play -> leave game
            // - leave/leave -> leave game
            if (answer == "##play##")
            {
                if(dataReceived == "##play##")
                {
                    // reset board
                    resetGameBoard();
                }
                else
                {
                    isClosed = true; // set that we closed the game
                    // received leave, close the game
                    closeGame();
                }
            }
            else if (answer == "##stop##")
            {
                isClosed = true; // set that we closed the game

                // close the game
                closeGame();
            }
        }

        /// <summary>
        /// Reset the drawn game board and the game board state, also flip the turn/color of the player
        /// </summary>
        public void resetGameBoard()
        {
            // reinitialize to 0
            Array.Clear(boardState, 0, boardState.Length);
            isStarting = !isStarting; // flip turn
            turn = isStarting;

            //Change sides
            if (color == State.Red) color = State.Black;
            else if (color == State.Black) color = State.Red;

            // Update all text label
            updateLabelText();
            this.Refresh(); // This forces a call to OnPaint which means we repaint the board
        }

        /// <summary>
        /// Close the stream, the socket and the GameForm
        /// </summary>
        private void closeGame()
        {
            // Close the network stream
            nwStream.Close();
            // Close the tcpclient
            client.Close();
            // Close the window
            this.Close();
        }
        
        /// <summary>
        /// Check what your color is to set the brush color
        /// </summary>
        /// <returns>Brush color</returns>
        private Brush getTurnColor(State drawingColor)
        {
            if (drawingColor == State.Red) return Brushes.Red;
            else if (drawingColor == State.Black) return Brushes.Black;
            return null;
        }

        /// <summary>
        /// Check if it is your turn and what color you have to return the correct state logic
        /// </summary>
        /// <returns></returns>
        private State getTurnState()
        {
            if (turn && color == State.Red) return State.Red;
            else if (turn && color == State.Black) return State.Black;
            else if (!turn && color == State.Red) return State.Black;
            else if (!turn && color == State.Black) return State.Red;
            else return State.Blank;
        }
        
        /// <summary>
        /// Find which column was clicked on the game board
        /// </summary>
        /// <param name="location"></param>
        /// <returns>Clicked Column index</returns>
        private int findClickedColumnIndex(Point location)
        {
            // Go through all the gameBoardColumn rectangles that were set, and check if you clicked inside their pixel ranges
            for(int i = 0; i < columns; i++)
            {
                if (location.X >= gameBoardColumns[i].X && Location.Y >= gameBoardColumns[i].Y)
                {
                    if (location.X <= (gameBoardColumns[i].X + gameBoardColumns[i].Width) && Location.Y <= gameBoardColumns[i].Y + gameBoardColumns[i].Height)
                    {
                        // if so, return the column that was clicked
                        return i;
                    }
                }
            }
            // If you clicked out of the bounds of the game board return -1
            return -1;
        }

        /// <summary>
        /// Checks vertically downwards from the current chip to see if the game is won
        /// </summary>
        /// <param name="row">Row of the current chip</param>
        /// <param name="column">Column of the current chip</param>
        /// <param name="currentColor">Current Color of the chip</param>
        /// <returns>If the game is won</returns>
        private bool checkVerticalWin(int row, int column, int currentColor)
        {
            int counter = 0;
            // Check the chips below the newly placed one to see if there is a match of 4
            for (int i = row; i < rows; i++)
            {
                // Same State found
                if (boardState[i, column] == currentColor)
                {
                    counter++;
                }
                // Different State found
                else
                {
                    break;
                }

                // If at any point the counter becomes 4, there is a victory
                if (counter == 4)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks the 2 directions [left, right] of the current chip to see if the game is won
        /// </summary>
        /// <param name="row">Row of the current chip</param>
        /// <param name="column">Column of the current chip</param>
        /// <param name="currentColor">Current Color of the chip</param>
        /// <returns>If the game is won</returns>
        private bool checkHorizontalWin(int row, int column, int currentColor)
        {
            // Horizontal case
            // Check if there is a connect 4 using the left and right of the placed chip

            int counter = 1; // includes the current dropped chip

            int leftOffset = 1;
            int rightOffset = 1;

            // Check for same state chips on the left side
            while (true)
            {
                // Off of the board
                if (column - leftOffset <= 0)
                {
                    break;
                }
                // Same State found to the left
                if (boardState[row, column - leftOffset] == currentColor)
                {
                    leftOffset++;
                    counter++;
                }
                // Different State found to the left
                else
                {
                    break;
                }
            }

            // Check for same state chips on the right side
            while (true)
            {
                // Off of the board
                if (column + rightOffset >= columns)
                {
                    break;
                }
                // Same State found to the right
                if (boardState[row, column + rightOffset] == currentColor)
                {
                    rightOffset++;
                    counter++;
                }
                // Different State found to the right
                else
                {
                    break;
                }
            }

            // If at any point the counter becomes 4, there is a victory
            if (counter == 4)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks the 2 diagonals [topleft-bottomright, topright-bottomleft] of the current chip to see if the game is won
        /// </summary>
        /// <param name="row">Row of the current chip</param>
        /// <param name="column">Column of the current chip</param>
        /// <param name="currentColor">Current Color of the chip</param>
        /// <returns>If the game is won</returns>
        private bool checkDiagonalWin(int row, int column, int currentColor)
        {

            int topLeftOffset = 1;
            int bottomRightOffset = 1;
            int topRightOffset = 1;
            int bottomLeftOffset = 1;

            #region topLeftBottomRight
            // Check top-left/bottom-right for a connect 4
            int counter = 1; // includes the current dropped chip

            // Check for same state chips on the top-left side
            while (true)
            {
                // Off of the board
                if (column - topLeftOffset <= 0 || row - topLeftOffset <= 0)
                {
                    break;
                }
                // Same State found to the top-left
                if (boardState[row - topLeftOffset, column - topLeftOffset] == currentColor)
                {
                    topLeftOffset++;
                    counter++;
                }
                // Different State found to the top-left
                else
                {
                    break;
                }
            }

            // Check for same state chips on the bottom-right side
            while (true)
            {
                // Off of the board
                if (column + bottomRightOffset >= columns || row + bottomRightOffset >= rows)
                {
                    break;
                }
                // Same State found to the bottom-right
                if (boardState[row + bottomRightOffset, column + bottomRightOffset] == currentColor)
                {
                    bottomRightOffset++;
                    counter++;
                }
                // Different State found to the bottom-right
                else
                {
                    break;
                }
            }

            // If at any point the counter becomes 4, there is a victory
            if (counter == 4)
            {
                return true;
            }
            #endregion

            #region topRightBottomLeft
            // Reset counter and check top-right/bottom-left
            counter = 1; // includes the current dropped chip

            // Check for same state chips on the top-right side
            while (true)
            {
                // Off of the board
                if (column + topRightOffset >= columns || row - topRightOffset <= 0)
                {
                    break;
                }
                // Same State found to the top-right
                if (boardState[row - topRightOffset, column + topRightOffset] == currentColor)
                {
                    topRightOffset++;
                    counter++;
                }
                // Different State found to the top-right
                else
                {
                    break;
                }
            }

            // Check for same state chips on the bottom-left side
            while (true)
            {
                // Off of the board
                if (column - bottomLeftOffset <= 0 || row + bottomLeftOffset >= rows)
                {
                    break;
                }
                // Same State found to the bottom-left
                if (boardState[row + bottomLeftOffset, column - bottomLeftOffset] == currentColor)
                {
                    bottomLeftOffset++;
                    counter++;
                }
                // Different State found to the bottom-left
                else
                {
                    break;
                }
            }

            // If at any point the counter becomes 4, there is a victory
            if (counter == 4)
            {
                return true;
            }

            #endregion

            return false;
        }


        /// <summary>
        /// Check if a win was caused on the boardState with the added chip
        /// </summary>
        /// <returns>Boolean depicting if there is a winner</returns>
        private bool isWin(int row, int column)
        {
            // Initialize which color to check for
            int currentColor = boardState[row, column];

            // if there is one of the following cases, the match is won
            // - 4 chips of the same color directly touching each other in a vertical line
            // - 4 chips of the same color directly touching each other in a horizontal line
            // - 4 chips of the same color directly touching each other in a diagonal line 

            // Vertical case
            if(checkVerticalWin(row, column, currentColor) || checkHorizontalWin(row, column, currentColor) || checkDiagonalWin(row, column, currentColor))
            {
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Create and show a win form with the correct values when the game enters the "win" state
        /// </summary>
        private void openWinForm()
        {
            WinForm wForm = new WinForm(this);
            wForm.ShowDialog(this); // give reference to current Game Form
        }
    }
}

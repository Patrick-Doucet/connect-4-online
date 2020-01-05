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
using System.Threading;
using System.Net;

namespace Connect_4_Client
{
    public partial class MainForm : Form
    {

        // Try connecting to server before even opening a form
        // First create a socket (so we can keep a reference throughout the program)         
        // Create a TCP/IP socket.
        // Since the server uses AF_INET (IPV4 address), AddressFamily.InterNetwork is used to force IPV4 on client side
        Socket socketClient = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);

        // Thread for client to listen to server socket on
        Thread myThread;

        // Possible opponent information 
        public static string opponentIp; // ip
        public static string oName; // name
        // Your name
        public static string pName; // player name
        public string usernameText // getter setter for the username text found inside label3 (the label beside the words "welcome:")
        {
            get
            {
                return label3.Text;
            }
            set
            {
                label3.Text = value;
            }
        }

        /// <summary>
        /// Constructor for the main window of the application
        /// </summary>
        public MainForm()
        {
            // Initial configurations for the window
            InitializeComponent();
            // Center the window to the middle of your screen
            CenterToScreen();
            // event notification when closing the window
            this.FormClosing += MainForm_FormClosing;
            try
            {
                // Start Socket connection towards server
                StartClientSocket(socketClient);

                // Create the form asking the user his name
                WelcomeForm welcomeForm = new WelcomeForm();
                welcomeForm.ShowDialog(this); // giving "this" here means we pass a reference of the parent window to the new child window

                // Fetch the player name from the welcomeForm text box and insert it in the label of the main window
                // Also save it in pName for later use
                this.label3.Text = welcomeForm.nameText;
                pName = welcomeForm.nameText;

                try
                {
                    // Send your name to the server
                    sendNameToServer();

                    // Fetch all the other users that are connected
                    fetchClientListFromServer();

                }
                catch
                {
                    // Throw if it fails
                    throw new Exception();
                }

                // Since the socket needs to be always listening for information from the server
                // Launch listen function on thread
                // Here the thread calls AlwaysListening with the socket to listen on
                myThread = new Thread(new ThreadStart(() => ThreadMethods.AlwaysListening(socketClient, listBox1) /*Call method*/));
                myThread.IsBackground = true; // With this flag Application.Exit() will kill the thread
                myThread.Start(); // Start the thread
            }
            catch
            {
                // The connection was not able to be made.
                MessageBox.Show("Can't connect to server.");
                throw new Exception();
            }
        }


        /// <summary>
        /// Check the status of the connection of a given socket to see if you are still connected to the server
        /// </summary>
        /// <param name="s">Socket to check if connected</param>
        /// <returns>If is connected</returns>
        public static bool IsSocketConnected(Socket s)
        {
            // Check the status of the connection to see if you are still connected
            // Directly check if it is connected (TCP only)
            if (!s.Connected)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Creating the client socket and connecting to server
        /// </summary>
        /// <param name="s">Socket to start connection</param>
        public static void StartClientSocket(Socket s)
        {
            // Data buffer for incoming data.  
            byte[] bytes = new byte[1024];

            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                // connects to ip given from DNS "nicepepito.crabdance.com"
                // uses port 10123
                IPHostEntry ipHostInfo = Dns.GetHostEntry("nicepepito.crabdance.com");
                IPAddress ipAddress = ipHostInfo.AddressList[0]; // Get first ip from list

                int port = 10123; // port number
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port); // create endpoint

                // Connect the socket to the remote endpoint. Catch any errors.  
                try
                {
                    s.Connect(remoteEP); // connect socket s to remoteEP

                    MessageBox.Show("Socket connected");
                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Closes the socket if it is still connected
        /// </summary>
        /// <param name="s">Socket to close</param>
        public static void CloseClientSocket(Socket s)
        {
            // Check if the socket is still connected to the server
            if (IsSocketConnected(s))
            {
                // Send message to server saying that the socket is closing
                // Encode the data string into a byte array.  
                byte[] msg = Encoding.ASCII.GetBytes("##bye##");

                // Send the data through the socket.  
                try { s.Send(msg); } catch { }

            }

            // Closing socket towards the server
            // Release the socket.  
            s.Shutdown(SocketShutdown.Both); // disable read and write
            s.Close(); // close socket
        }

        /// <summary>
        /// When double clicking a user, ask that user to play a game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Check if table index exists in table
            int index = this.listBox1.IndexFromPoint(e.Location);
            if (index != ListBox.NoMatches)
            {
                // Get the person info
                string personInfo = listBox1.GetItemText(listBox1.SelectedItem);

                // Split the personInfo into Username (words[0]) and IP (words[1])
                string[] words = personInfo.Split(':');
                oName = words[0];
                opponentIp = words[1];

                // Send to server socket the username of the potential opponent
                byte[] nameInBytes = System.Text.Encoding.ASCII.GetBytes(words[0]);
                socketClient.Send(nameInBytes);
            }
        }

        /// <summary>
        /// Close the application if X is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_FormClosing(object sender, EventArgs e)
        {
            Application.Exit(); // Close application and threads
            CloseClientSocket(socketClient); // Close socket towards the server
        }

        /// <summary>
        /// Repopulates the list of users in the table
        /// </summary>
        /// <param name="entries">All users to be added to list</param>
        private void updateList(string[] entries)
        {
            // First clear the listBox before adding all the clients
            if (listBox1.Items.Count != 0)
            {
                listBox1.Items.Clear();
            }

            // Add every entry found inside the listBox
            for (int i = 0; i < entries.Length; i++)
            {
                listBox1.Items.Add(entries[i]);
            }
        }

        /// <summary>
        /// Send your username to the server through the clientServerSocket
        /// </summary>
        private void sendNameToServer()
        {
            // Send to server socket the client username
            byte[] nameInBytes = System.Text.Encoding.ASCII.GetBytes(usernameText);
            socketClient.Send(nameInBytes);
        }
        
        /// <summary>
        /// Receive the client list from the server through the clientServerSocket
        /// </summary>
        private void fetchClientListFromServer()
        {
            // Initializing byte array for retrieval of data from server
            byte[] bytes = new byte[1024];
            int bytesRec;

            // Receive data telling the client how many users to expect
            bytesRec = socketClient.Receive(bytes);

            string lobbyInfo = Encoding.ASCII.GetString(bytes, 0, bytesRec); // convert to string

            // Clear byte array for next message
            Array.Clear(bytes, 0, bytes.Length);

            // Add every client received inside a list of strings
            string[] entries = lobbyInfo.Split('!'); // split string into array of users

            entries = entries.Skip(1).ToArray(); // Remove first element since it only shows how many clients are connected
            
            // Update the listBox
            updateList(entries);
        }

        /// <summary>
        /// Create the game instance
        /// </summary>
        public static void createGame()
        {
            // start the gameForm and start first
            GameForm gameForm = new GameForm(true, pName, oName); // no need for the ip, this user will wait for the other one
            gameForm.ShowDialog();
        }

        /// <summary>
        /// Join the game instance
        /// </summary>
        public static void joinGame()
        {
            // start the gameForm and start second
            GameForm gameForm = new GameForm(false, pName, oName, opponentIp); // need the opponentIp to connect to his listener
            gameForm.ShowDialog();
        }

        // Class containing all the methods for the socket thread
        public class ThreadMethods
        {
            /// <summary>
            /// Always listen for messages coming from the server
            /// </summary>
            /// <param name="clientServerSocket">socket to listen on</param>
            /// <param name="lobbyBox">reference to the box where the user entries are</param>
            public static void AlwaysListening(Socket clientServerSocket, ListBox lobbyBox)
            {
                // Initializing byte array for retrieval of data from server
                byte[] bytes = new byte[1024];
                int bytesRec = 0;
                while (true)
                {
                    
                    try
                    {
                        // Receive bytes from server
                        bytesRec = clientServerSocket.Receive(bytes);
                    }
                    catch
                    { }

                    // Convert bytes to string
                    string recvString = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    
                    // Possible pre-defined responses
                    string accept = "##accept##\0";
                    string reject = "##reject##\0";
                    string disconnect = "##disconnect##\0";

                    // If client list is received
                    // Add every client received inside a list of strings
                    string[] entries = recvString.Split('!');

                    // Clear byte array for next message
                    Array.Clear(bytes, 0, bytes.Length);

                    // Either receive an accept or reject from the opponent
                    // Or, the socket receive a number -> this means the client update the table because a new user connected
                    // Or, a username is sent -> this means someone wants to play vs the client
                    // Or, the server has disconnected and sends disconnect to client
                    // Case accept
                    if (recvString == accept)
                    {
                        // Stop listening to the server
                        CloseClientSocket(clientServerSocket);
                        joinGame();
                        Application.Exit();
                    }
                    // Case reject
                    else if (recvString == reject)
                    {
                        // Do nothing
                    }
                    // Case disconnect
                    else if (recvString == disconnect)
                    {
                        // Close client because the server is no longer running
                        // Show prompt first to why you are closing
                        MessageBox.Show("The server was closed. You have been disconnected...");
                        CloseClientSocket(clientServerSocket);
                        Application.Exit();
                    }
                    // Case client list sent
                    else if (entries.Length > 1)
                    {
                        entries = entries.Skip(1).ToArray(); // Remove first element since it only shows how many clients are connected

                        // Update the listBox
                        updateLobby(entries, lobbyBox);
                    }
                    // Special case, sending user list, but there are no users connected except yourself
                    else if(recvString == "0\0")
                    {
                        // Clear the listBox
                        if (lobbyBox.Items.Count != 0)
                        {
                            lobbyBox.Items.Clear();
                        }
                    }
                    // Case where user wants to play vs you
                    else
                    {
                        // Add all users from the lobby to list
                        List<string> allEntries = new List<string>();
                        foreach (string s in lobbyBox.Items)
                        {
                            allEntries.Add(s);
                        }

                        // Check if the username you received is a valid user (Look through list of users)
                        if(allEntries.Any(recvString.Contains))
                        {
                            // User was found, save his name
                            string[] opInfo = recvString.Split(':');
                            oName = opInfo[0];

                            // Open new window asking you if you'd like to play vs this user
                            AcceptForm acceptForm = new AcceptForm(clientServerSocket, recvString); // pass reference to socket and username
                            acceptForm.ShowDialog();

                            // If the user had clicked "accept" close the socket to the server and start the game
                            if(acceptForm.wantsToPlay)
                            {
                                Thread.Sleep(100); // To avoid sending too many strings in the buffer before the server can read them
                                // Stop listening to the server
                                CloseClientSocket(clientServerSocket);
                                // Start the game
                                createGame();
                                // When the game is completed, close the application
                                Application.Exit();
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Repopulates the list of users in the table (Thread version of function)
            /// This exists twice because the thread class isn't in the scope to use the other updateList function
            /// </summary>
            public static void updateLobby(string[] entries, ListBox lobbyBox)
            {
                // First clear the listBox before adding all the clients
                if (lobbyBox.Items.Count != 0)
                {
                    lobbyBox.Items.Clear();
                }

                // Add every entry found inside the listBox
                for (int i = 0; i < entries.Length; i++)
                {
                    lobbyBox.Items.Add(entries[i]);
                }
            }
        }
    }
}


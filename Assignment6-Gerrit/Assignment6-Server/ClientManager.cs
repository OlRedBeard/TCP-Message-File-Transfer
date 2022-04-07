using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Added
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.ComponentModel;
using FileShare;

namespace Assignment6_Server
{
    public class ClientManager
    {
        public string name = "";
        public string oldName = "";

        // Keep a string around for the latest message
        private string latest = "";

        // List of commands
        private string[] commands = { 
            "!help - Get a list of available commands.",
            "!list - Displays a list of shared files.",
            "!get [filename] - Download a shared file.",
            "!user [newname] - Change your display name to [newname].", 
            "!shh [username] - Send a private message to [username]"
        };

        public static TcpListener listener;
        public static int clientCounter = 0;

        private BackgroundWorker bgw = new BackgroundWorker();
        private Socket connection;
        private NetworkStream socketStream;
        private BinaryReader reader;
        private BinaryWriter writer;

        public event NewClientConnectedEventHandler NewClientConnected;
        public delegate void NewClientConnectedEventHandler(ClientManager client);

        public event ClientDisconnectedEventHandler ClientDisconnected;
        public delegate void ClientDisconnectedEventHandler(ClientManager client);

        public event ClientRenameEventHandler ClientRenamed;
        public delegate void ClientRenameEventHandler(ClientManager client, string oldName);

        public event ReceivedMessageEventHandler ReceivedMessage;
        public delegate void ReceivedMessageEventHandler(ClientManager client, string message);

        public event ReceivedFileEventHandler ReceivedFile;
        public delegate void ReceivedFileEventHandler(ClientManager client, SharedFile file);

        public event PrivateMessageEventHandler PrivateMessaged;
        public delegate void PrivateMessageEventHandler(string? sender, ClientManager receiver, string message);

        public event ListEventHandler ListRequested;
        public delegate void ListEventHandler(ClientManager client);

        public event GetEventHandler FileRequested;
        public delegate void GetEventHandler(ClientManager client, string fileName);

        bool done = false;

        public ClientManager(TcpListener listener)
        {
            ClientManager.listener = listener;
            bgw.WorkerSupportsCancellation = true;
            bgw.WorkerReportsProgress = true;
            bgw.DoWork += Bgw_DoWork;
            bgw.ProgressChanged += Bgw_ProgressChanged;
            bgw.RunWorkerAsync();
        }

        public void SendMessage(string message)
        {
            try
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(writer.BaseStream, message);
            }
            catch
            {
                throw;
            }
        }

        public void SendFile(SharedFile file)
        {
            try
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(writer.BaseStream, file);
            }
            catch
            {
                throw;
            }
        }

        private void Bgw_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            try
            {
                // Client connected
                if(e.ProgressPercentage == 0)
                {
                    NewClientConnected(this);
                }
                // String message received
                else if (e.ProgressPercentage == 1)
                {
                    ReceivedMessage(this, (string)e.UserState);
                }
                // Client disconnected
                else if (e.ProgressPercentage == 2)
                {
                    ClientDisconnected(this);
                }
                // File was sent
                else if (e.ProgressPercentage == 3)
                {
                    ReceivedFile(this, (SharedFile)e.UserState);
                }
                // Client changed username
                else if (e.ProgressPercentage == 4)
                {
                    ClientRenamed(this, this.oldName);
                }
                // Help request
                else if (e.ProgressPercentage == 5)
                {
                    foreach (string msg in commands)
                    {
                        PrivateMessaged(null, this, msg);
                    }
                }
                // File list request
                else if (e.ProgressPercentage == 6)
                {
                    ListRequested(this);
                }
                // Get file request
                else if (e.ProgressPercentage == 7)
                {
                    FileRequested(this, (string)e.UserState);
                }
                // Private message
                else if (e.ProgressPercentage == 8)
                {
                    string unsplitted = (string)e.UserState;
                    string msg = unsplitted.Split(',')[1];
                    string receiver = unsplitted.Split(',')[0];

                    PrivateMessaged(receiver, this, msg);
                }
            }
            catch
            {
                throw;
            }
        }

        private void Bgw_DoWork(object? sender, DoWorkEventArgs e)
        {
            try
            {
                // This will wait for a new connection
                connection = listener.AcceptSocket();
                clientCounter++;

                // Give them a name with their index (temporarily)
                this.name = "Client" + clientCounter;

                // Use the BGW to report progress (usually percent progress, instead will indicate the type of event)
                // 0 - new client connected
                bgw.ReportProgress(0);

                // Set up our network connection
                socketStream = new NetworkStream(connection);
                reader = new BinaryReader(socketStream);
                writer = new BinaryWriter(socketStream);

                // When connection ceases this will become true
                //done = false;
                while (!done)
                {
                    try
                    {
                        IFormatter formatter = new BinaryFormatter();
                        object o = (object)formatter.Deserialize(reader.BaseStream);

                        // Here we check for the type of object
                        if(o is string)
                        {
                            string chk = o.ToString()[0].ToString();

                            if (chk != "!")
                            {
                                latest = (string)o;
                                // 1 - new message
                                bgw.ReportProgress(1, latest);
                            }
                            else
                            {
                                // Logic for commands
                                if (o.ToString().Split(" ")[0] == "!user")
                                {
                                    this.oldName = this.name;
                                    this.name = o.ToString().Split(" ")[1];
                                    // 4 - username change
                                    bgw.ReportProgress(4);
                                }
                                else if (o.ToString().Split(" ")[0] == "!help")
                                {
                                    // 5 - help request
                                    bgw.ReportProgress(5);
                                }
                                else if (o.ToString().Split(" ")[0] == "!list")
                                {
                                    // 6 - file list request
                                    bgw.ReportProgress(6);
                                }
                                else if (o.ToString().Split(" ")[0] == "!get")
                                {
                                    latest = o.ToString().Split(" ")[1];
                                    // 7 - download request
                                    bgw.ReportProgress(7, latest);
                                }
                                else if (o.ToString().Split(" ")[0] == "!shh")
                                {
                                    latest = o.ToString().Split(" ")[1] + ",";
                                    for (int i = 2; i < o.ToString().Split(" ").Length; i++)
                                    {
                                        latest += o.ToString().Split(" ")[i] + " ";
                                    }
                                    // 8 - private message
                                    bgw.ReportProgress(8, latest);
                                }
                            }                            
                        }
                        if(o is SharedFile)
                        {
                            // 3 - new file
                            bgw.ReportProgress(3, o);
                        }
                    }
                    catch
                    {
                        done = true;
                        throw;
                    }
                }
            }
            catch
            {
                // 2 - client disconnected
                bgw.ReportProgress(2, latest);
                done = true;
            }
        }
    }
}

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

namespace Assignment6_Server
{
    public class ClientManager
    {
        public string name = "";
        // Keep a string around for the latest message
        private string latest = "";

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

        public event ReceivedMessageEventHandler ReceivedMessage;
        public delegate void ReceivedMessageEventHandler(ClientManager client, string message);

        public event ReceivedFileEventHandler ReceivedFile;
        public delegate void ReceivedFileEventHandler(ClientManager client, byte[] file);

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

        public void SendFile(byte[] file)
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
                    ReceivedFile(this, (byte[])e.UserState);
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
                // Give them a name with their index
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
                            latest = (string)o;
                            // 1 - new message
                            bgw.ReportProgress(1, latest);
                        }
                        if(o is byte[])
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Added
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.Sockets;
using System.IO;
using System.Windows.Forms; // What?! This is now bound to Windows forms
using FileShare;

namespace Assignment6_Client
{
    public class ClientCommunication
    {
        public string clientName = SystemInformation.ComputerName;
        public string serverName;
        public int port = 5000;

        private TcpClient client;
        private NetworkStream nStream;
        private BinaryReader reader;
        private BinaryWriter writer;
        private BackgroundWorker bgw = new BackgroundWorker();

        public event ConnectedEventHandler Connected;
        public delegate void ConnectedEventHandler(string servername, int port);

        public event ConnectionFailedEventHandler ConnectionFailed;
        public delegate void ConnectionFailedEventHandler(string servername, int port);

        public event ReceivedMessageEventHandler ReceivedMessage;
        public delegate void ReceivedMessageEventHandler(string message);

        public event ReceivedFileEventHandler ReceivedFile;
        public delegate void ReceivedFileEventHandler(SharedFile file);

        public ClientCommunication(string servername)
        {
            this.serverName = servername;
            bgw.WorkerReportsProgress = true;
            bgw.WorkerSupportsCancellation = true;
            bgw.ProgressChanged += Bgw_ProgressChanged;
            bgw.DoWork += Bgw_DoWork;
            bgw.RunWorkerAsync();            
        }

        public void SendMessage(string msg)
        {
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(writer.BaseStream, msg);
        }

        public void SendMessage(SharedFile msg)
        {
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(writer.BaseStream, msg);
        }

        private void Bgw_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Bgw_DoWork(object? sender, DoWorkEventArgs e)
        {
            try
            {
                client = new TcpClient();
                client.Connect(this.serverName, this.port);
                nStream = client.GetStream();
                reader = new BinaryReader(nStream);
                writer = new BinaryWriter(nStream);

                Connected(this.serverName, this.port);

                try
                {
                    while (true)
                    {
                        IFormatter formatter = new BinaryFormatter();
                        object o = (object)formatter.Deserialize(nStream);
                        if (o is string)
                        {
                            ReceivedMessage((string)o);
                        } // Change below to accept SharedFile
                        if (o is SharedFile)
                        {
                            ReceivedFile((SharedFile)o);
                        }
                        // TO DO: Receive a byte[] for a file
                    }
                }
                catch
                {
                    throw;
                }
            }
            catch
            {
                ConnectionFailed(this.serverName, this.port);
            }
        }
    }
}

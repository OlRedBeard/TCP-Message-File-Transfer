using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
// Added
using System.Net.Sockets;
using System.Net;
using System.IO;
using FileShare;

namespace Assignment6_Server
{
    public partial class Form1 : Form
    {
        public static int port = 5000;
        TcpListener listener;
        ClientManager mngr;
        public static List<ClientManager> lstClients = new List<ClientManager>();
        public static List<SharedFile> sharedFiles = new List<SharedFile>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cmbIPaddress.DataSource = Dns.GetHostEntry(SystemInformation.ComputerName).AddressList
                .Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToList();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                IPAddress serverName = (IPAddress)cmbIPaddress.SelectedValue;
                listener = new TcpListener(serverName, port);
                listener.Start();
                // TO DO: Come back and create a log for starting the server

                mngr = new ClientManager(listener);
                mngr.NewClientConnected += Mngr_NewClientConnected;
                mngr.ClientDisconnected += Mngr_ClientDisconnected;
                mngr.ReceivedMessage += Mngr_ReceivedMessage;
                mngr.ReceivedFile += Mngr_ReceivedFile;

                lstMessages.Items.Add(">>>> Server has started");
            }
            catch
            {
                throw;
            }
        }

        private void RelayAllMessages(ClientManager sendClient, string message)
        {
            foreach (ClientManager cli in lstClients)
            {
                cli.SendMessage(sendClient.name + ": " + message);
            }
        }
        private void RelayAllFiles(byte[] message)
        {
            //foreach (ClientManager cli in lstClients)
            //{
            //    cli.SendFile(message);
            //}
        }

        private void Mngr_ReceivedMessage(ClientManager client, string message)
        {
            RelayAllMessages(client, message);
            lstMessages.Items.Add(client.name + ": " + message);
        }

        private void Mngr_ClientDisconnected(ClientManager client)
        {
            // Remove the client that disconnected from our list
            lstClients.Remove(client);
            string msg = ">>>> " + client.name + " has disconnected.";

            // Inform every client in the list that a client disconnected
            RelayAllMessages(client, msg);
            lstMessages.Items.Add(msg);
        }

        private void Mngr_NewClientConnected(ClientManager client)
        {
            // Add the client that connected to our list
            lstClients.Add(client);
            string msg = ">>>> " + client.name + " has connected.";

            // Inform every client in the list that a new client connected
            foreach(ClientManager cli in lstClients)
            {
                cli.SendMessage(msg);
            }
            lstMessages.Items.Add(msg);

            mngr = new ClientManager(listener);
            mngr.NewClientConnected += Mngr_NewClientConnected;
            mngr.ClientDisconnected += Mngr_ClientDisconnected;
            mngr.ReceivedMessage += Mngr_ReceivedMessage;
            mngr.ReceivedFile += Mngr_ReceivedFile;
        }

        private void Mngr_ReceivedFile(ClientManager client, SharedFile file)
        {
            sharedFiles.Add(file);
            string filePath = Environment.CurrentDirectory + "/files/" + file.FileName;
            file.SetPath(filePath);
            File.WriteAllBytes(file.FilePath, file.FileBytes);
        }
    }
}

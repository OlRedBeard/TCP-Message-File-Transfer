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

            if(!Directory.Exists(Environment.CurrentDirectory + "/files/"))
            {
                Directory.CreateDirectory(Environment.CurrentDirectory + "/files/");
            }
            else
            {
                string[] allFiles = Directory.GetFiles(Environment.CurrentDirectory + "/files/");

                foreach(string file in allFiles)
                {
                    byte[] fileContents = File.ReadAllBytes(file);
                    string fname = file.Split("/").LastOrDefault();
                    SharedFile tmp = new SharedFile("Serv", fname, fileContents);
                    sharedFiles.Add(tmp);
                }
            }
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
                mngr.ClientRenamed += Mngr_ClientRenamed;
                mngr.ReceivedMessage += Mngr_ReceivedMessage;
                mngr.ReceivedFile += Mngr_ReceivedFile;
                mngr.PrivateMessaged += Mngr_PrivateMessaged;
                mngr.ListRequested += Mngr_ListRequested;
                mngr.FileRequested += Mngr_FileRequested;

                lstMessages.Items.Add(">>>> Server has started");
            }
            catch
            {
                throw;
            }
        }

        private void DownloadFile(ClientManager client, SharedFile file)
        {
            for (int i = 0; i < lstClients.Count; i++)
            {
                if (lstClients[i].name == client.name)
                    lstClients[i].SendFile(file);
            }
        }

        private void SendPrivateMessage(string? sender, ClientManager receiver, string msg)
        {
            if (sender == null)
            {
                for (int i = 0; i < lstClients.Count; i++)
                {
                    if (lstClients[i].name == receiver.name)
                        lstClients[i].SendMessage("||| " + msg);
                }
            }
            else
            {
                for (int i = 0; i < lstClients.Count; i++)
                {
                    if (lstClients[i].name == sender) // Is this right?
                        lstClients[i].SendMessage("||| " + receiver.name + ": " + msg);
                }
            }
        }

        private void RelayAllMessages(ClientManager sendClient, string message)
        {
            foreach (ClientManager cli in lstClients)
            {
                cli.SendMessage(sendClient.name + ": " + message);
            }
        }

        private void ServerMessage(string message)
        {
            foreach (ClientManager cli in lstClients)
            {
                cli.SendMessage(message);
            }
        }
        private void Mngr_FileRequested(ClientManager client, string fileName)
        {
            SharedFile requested = null;
            for (int i = 0; i < sharedFiles.Count; i++)
            {
                if (sharedFiles[i].FileName == fileName)
                {
                    requested = sharedFiles[i];
                    DownloadFile(client, requested);
                }
            }

            if (requested == null)
            {
                SendPrivateMessage(null, client, "File not found, check spelling.");
            }
        }

        private void Mngr_ListRequested(ClientManager client)
        {
            if (sharedFiles.Count == 0)
                SendPrivateMessage(null, client, "There are no shared files yet");
            else
            {
                foreach (SharedFile tmp in sharedFiles)
                {
                    string msg = tmp.ToString();
                    SendPrivateMessage(null, client, msg);
                }
            }            
        }

        private void Mngr_PrivateMessaged(string? sender, ClientManager receiver, string message)
        {
            if (sender != null)
            {
                SendPrivateMessage(sender, receiver, message);
                lstMessages.Items.Add(sender + " private messaged " + receiver.name);
            }
            else
            {
                SendPrivateMessage(sender, receiver, message);
                lstMessages.Items.Add("Server responded to a request from " + receiver.name);
            }
        }

        private void Mngr_ReceivedMessage(ClientManager client, string message)
        {
            RelayAllMessages(client, message);
            lstMessages.Items.Add(client.name + ": " + message);
        }

        private void Mngr_ClientRenamed(ClientManager client, string oldName)
        {
            string msg = ">>>> " + oldName + " was renamed " + client.name + ".";

            // Inform every client in the list that a client disconnected
            ServerMessage(msg);
            lstMessages.Items.Add(msg);
        }

        private void Mngr_ClientDisconnected(ClientManager client)
        {
            // Remove the client that disconnected from our list
            lstClients.Remove(client);
            string msg = ">>>> " + client.name + " has disconnected.";

            // Inform every client in the list that a client disconnected
            ServerMessage(msg);
            lstMessages.Items.Add(msg);
        }

        private void Mngr_NewClientConnected(ClientManager client)
        {
            // Add the client that connected to our list
            lstClients.Add(client);
            string msg = ">>>> " + client.name + " has connected.";

            // Inform every client in the list that a new client connected
            ServerMessage(msg);

            mngr = new ClientManager(listener);
            mngr.NewClientConnected += Mngr_NewClientConnected;
            mngr.ClientDisconnected += Mngr_ClientDisconnected;
            mngr.ClientRenamed += Mngr_ClientRenamed;
            mngr.ReceivedMessage += Mngr_ReceivedMessage;
            mngr.ReceivedFile += Mngr_ReceivedFile;
            mngr.PrivateMessaged += Mngr_PrivateMessaged;
            mngr.ListRequested += Mngr_ListRequested;
            mngr.FileRequested += Mngr_FileRequested;
        }

        private void Mngr_ReceivedFile(ClientManager client, SharedFile file)
        {
            sharedFiles.Add(file);
            string filePath = Environment.CurrentDirectory + "/files/" + file.FileName;
            int counter = 0;

            while (File.Exists(filePath))
            {
                if (file.FileName.Contains($"({counter})"))
                {
                    counter++;
                    file.FileName = file.FileName.Split('(')[0] + $@"({counter})" + file.FileName.Split(')')[1];
                }
                else
                {
                    file.FileName = file.FileName.Split('.')[0] + $@"({counter})." + file.FileName.Split('.')[1];
                }
                
                filePath = Environment.CurrentDirectory + "/files/" + file.FileName;
                //counter++;
            }

            file.SetPath(filePath);
            File.WriteAllBytes(file.FilePath, file.FileBytes);
            lstMessages.Items.Add(">>>> " + client.name + " uploaded " + file.FileName);
        }
    }
}

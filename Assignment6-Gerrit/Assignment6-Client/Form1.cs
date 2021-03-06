using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
// ADDED
using System.Collections.Concurrent;
using System.IO;
using FileShare;

namespace Assignment6_Client
{
    public partial class Form1 : Form
    {
        ClientCommunication serv;
        ConcurrentQueue<string> msgQ = new ConcurrentQueue<string>();

        public string Username = "";

        private void DisplayMessages()
        {
            while (msgQ.Count > 0)
            {
                string tmp;
                msgQ.TryDequeue(out tmp);
                lstMessages.Items.Add(tmp);
                lstMessages.SelectedIndex = lstMessages.Items.Count - 1;
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            serv = new ClientCommunication(cmbIPaddress.Text);
            serv.ReceivedMessage += Serv_ReceivedMessage;
            serv.ReceivedFile += Serv_ReceivedFile;
            serv.Connected += Serv_Connected;
            serv.ConnectionFailed += Serv_ConnectionFailed;

            // Send username if provided
            if (txtUsername.Text != "")
                this.Username = txtUsername.Text;

            // disable username field
            txtUsername.Enabled = false;
            txtUsername.Visible = false;
        }

        private void Serv_ReceivedFile(SharedFile file)
        {
            // Change to accept a SharedFile and decode it using it's original name
            File.WriteAllBytes(file.FileName, file.FileBytes);
            msgQ.Enqueue(">>>> Downloaded " + file.FileName);
            this.BeginInvoke(new MethodInvoker(DisplayMessages));
        }

        private void Serv_ConnectionFailed(string servername, int port)
        {
            //throw new NotImplementedException();
            msgQ.Enqueue(">>>> Connection lost: " + servername + "@" + port);
            this.BeginInvoke(new MethodInvoker(DisplayMessages));
        }

        private void Serv_Connected(string servername, int port)
        {
            string incomingConnectionMessage = ">>>> " + servername + "@" + port + " connected";
            msgQ.Enqueue(incomingConnectionMessage);
            this.BeginInvoke(new MethodInvoker(DisplayMessages));

            // Send username if provided
            if (this.Username != "")
                serv.SendMessage("!user " + this.Username);

            string helpMessage = ">>>> Send !help for a list of commands";
            msgQ.Enqueue(incomingConnectionMessage);
            this.BeginInvoke(new MethodInvoker(DisplayMessages));
        }

        private void Serv_ReceivedMessage(string message)
        {
            msgQ.Enqueue(message);
            this.BeginInvoke(new MethodInvoker(DisplayMessages));
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            serv.SendMessage(txtMessage.Text);
            if (txtMessage.Text.Split(" ")[0] == "!shh")
            {
                string message = "";
                for (int i = 2; i < txtMessage.Text.Split(" ").Length; i++)
                {
                    message += txtMessage.Text.Split(" ")[i] + " ";
                }
                msgQ.Enqueue("|||>To " + txtMessage.Text.Split(" ")[1] + ": " + message);
                this.BeginInvoke(new MethodInvoker(DisplayMessages));
            }
            txtMessage.Text = "";
        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            byte[] fileContents;

            OpenFileDialog diag = new OpenFileDialog();
            if (diag.ShowDialog() == DialogResult.OK)
            {
                // turns file into a SharedFile and then sends it to the server
                fileContents = File.ReadAllBytes(diag.FileName);
                SharedFile tmp = new SharedFile(this.Username, diag.FileName.Split('\\').LastOrDefault(), fileContents);
                serv.SendMessage(tmp);
            }
        }
    }
}

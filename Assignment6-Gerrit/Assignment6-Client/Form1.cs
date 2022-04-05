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

namespace Assignment6_Client
{
    public partial class Form1 : Form
    {
        ClientCommunication serv;
        ConcurrentQueue<string> msgQ = new ConcurrentQueue<string>();

        private void DisplayMessages()
        {
            while (msgQ.Count > 0)
            {
                string tmp;
                msgQ.TryDequeue(out tmp);
                lstMessages.Items.Add(tmp);
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
        }

        private void Serv_ReceivedFile(byte[] message)
        {
            File.WriteAllBytes("hardcodedfilename.txt", message);
        }

        private void Serv_ConnectionFailed(string servername, int port)
        {
            //throw new NotImplementedException();
            msgQ.Enqueue("Connection lost: " + servername + "@" + port);
            this.BeginInvoke(new MethodInvoker(DisplayMessages));
        }

        private void Serv_Connected(string servername, int port)
        {
            string incomingConnectionMessage = ">>>> " + servername + "@" + port + " connected";
            msgQ.Enqueue(incomingConnectionMessage);
        }

        private void Serv_ReceivedMessage(string message)
        {
            msgQ.Enqueue(message);
            this.BeginInvoke(new MethodInvoker(DisplayMessages));
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            serv.SendMessage(txtMessage.Text);
            txtMessage.Text = "";
        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            byte[] fileContents;

            OpenFileDialog diag = new OpenFileDialog();
            if (diag.ShowDialog() == DialogResult.OK)
            {
                fileContents = File.ReadAllBytes(diag.FileName);
                serv.SendMessage(fileContents);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TCPclient
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        private static extern bool FlashWindowEx(ref FLASHWINFO fi);
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }
        private void flashWindow()
        {
            FLASHWINFO FlashWINInfo = new FLASHWINFO();
            FlashWINInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(FlashWINInfo));
            FlashWINInfo.hwnd = this.Handle;
            FlashWINInfo.dwFlags = 3 | 2 | 12;
            FlashWINInfo.uCount = uint.MaxValue;
            FlashWINInfo.dwTimeout = 0;
            FlashWindowEx(ref FlashWINInfo);
        }


        private object receivelocker = new Object();

        string serverIP = "36.234.159.37";
        string serverPort="9090";
        string pmuser = "";

        Socket T;
        Thread th;
        string username;
        int loginState = 0;

        public Form1()
        {
            InitializeComponent();
            
        }

        private void sendMessage(string str)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(str);
            T.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                th.Abort();
                T.Close();
            }
            catch (Exception)
            {
                ;
            }
            /*
            if (buttonLogin.Enabled == false)
            {
                sendMessage("9|" + username + "|quit");
                T.Close();
            }*/
        }

        private void listenRoutine()
        {
            EndPoint serverEP = (EndPoint)T.RemoteEndPoint;
            byte[] buffer = new byte[4096];
            int inLength = 0;
            string msg;
            string cmd;
            string who;
            string str;
            string time;
            while (true)
            {
                try
                {
                    inLength = T.ReceiveFrom(buffer, ref serverEP);
                }

                catch (Exception)
                {
                    T.Close();
                    listBoxUserList.Items.Clear(); 
                    time = DateTime.Now.ToString("(HH:mm)");
                    richTextBoxBoard.SelectionColor = Color.Red;
                    richTextBoxBoard.AppendText( time+" Connection has lost");
                    buttonLogin.Enabled = true;
                    textBoxUsername.Enabled = true;
                    this.ActiveControl = textBoxUsername;
                    textBoxUsername.Focus();
                    loginState = 0;
                    this.Text = "TCP Messenger";
                    labelStatus.Text = "未連線";
                    buttonLogout.Enabled = false;
                    th.Abort();
                }

                    msg = Encoding.UTF8.GetString(buffer, 0, inLength);

                string[] c = msg.Split('|');
                cmd = c[0];
                who = c[1];
                str = c[2];
                time = DateTime.Now.ToString("(HH:mm)");
                //MessageBox.Show("CLIENT   Cmd:" + cmd + " Who:" + who+ " Str:" + str);
                switch (cmd)
                {
                    case "0":
                        listBoxUserList.Items.Clear();
                        string[] M = str.Split(',');
                        for (int i = 0; i < M.Length; ++i)
                        {
                            if(M[i] != username)
                               listBoxUserList.Items.Add(M[i]);
                        }

                        int index = listBoxUserList.Items.IndexOf(pmuser);
                        if (index > -1)
                        {
                            listBoxUserList.SetSelected(index, true);
                        }
                        else
                        {
                            labelStatus.Text = "廣播模式";
                        }

  
                        break;
                    case "1":
                        if (who != username)
                        {
                            richTextBoxBoard.SelectionColor = Color.Black;
                            richTextBoxBoard.AppendText( time +"[廣播]" + who + ": " + str + "\r\n");
                            richTextBoxBoard.SelectionStart = richTextBoxBoard.Text.Length;
                            richTextBoxBoard.ScrollToCaret();
                            flashWindow();
                        }
                        break;
                    case "2":
                        richTextBoxBoard.SelectionColor = Color.Blue;
                        richTextBoxBoard.AppendText(time+"[私訊]" + who + ": " + str + "\r\n");
                        richTextBoxBoard.SelectionStart = richTextBoxBoard.Text.Length;
                        richTextBoxBoard.ScrollToCaret();
                        flashWindow();
                        break;
                    case "99":
                        richTextBoxBoard.SelectionColor = Color.Red;
                        string[] m = str.Split(' ');
                        if (m[0] != username)
                        {
                            richTextBoxBoard.AppendText(time + " " + str + "\r\n");
                            richTextBoxBoard.SelectionStart = richTextBoxBoard.Text.Length;
                            richTextBoxBoard.ScrollToCaret();
                        }
                        break;
                    case "64":
                        richTextBoxBoard.Text = "";
                        richTextBoxBoard.SelectionColor = Color.Red;
                        richTextBoxBoard.AppendText(time + " Unable to connect to server!");
                        buttonLogin.Enabled = true;
                        buttonLogout.Enabled = false;
                        textBoxUsername.Enabled = true;
                        loginState = 0;
                        MessageBox.Show("無效的使用者名稱，請重新登入!");
                        th.Abort();
                        break;


                }
            }
        
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            sendRoutine();
        }
       

        private void sendRoutine()
        {
            if (textBoxMsgToSend.Text == "")
                return;
            String time = DateTime.Now.ToString("(HH:mm)");
            if (listBoxUserList.SelectedIndex < 0)
            {
                sendMessage("1|" + username + "|" + textBoxMsgToSend.Text);                         //broadcast
                richTextBoxBoard.SelectionColor = Color.Black;
                richTextBoxBoard.AppendText( time + "[廣播]" + username + ": " + textBoxMsgToSend.Text + "\r\n");
            }
            else
            {
                sendMessage("2|" + username + "|" + textBoxMsgToSend.Text + "|" + listBoxUserList.SelectedItem);
                richTextBoxBoard.SelectionColor = Color.Blue;
                richTextBoxBoard.AppendText( time + "[私訊給" + listBoxUserList.SelectedItem + "]: " + textBoxMsgToSend.Text + "\r\n");
            }

            textBoxMsgToSend.Text = "";
        }

        private void buttonBroadcast_Click(object sender, EventArgs e)
        {
            listBoxUserList.ClearSelected();
            labelStatus.Text = "廣播模式";
        }

        private void listBoxUserList_SelectedValueChanged(object sender, EventArgs e)
        {

            if (listBoxUserList.SelectedItems.Count != 0)
            {
                pmuser = listBoxUserList.GetItemText(listBoxUserList.SelectedItem);
                labelStatus.Text = "私訊給 " + listBoxUserList.GetItemText(listBoxUserList.SelectedItem);
            }

        }

        private void buttonLogin_Click_1(object sender, EventArgs e)
        {
            login();
        }

        private void textBoxMsgToSend_KeyPress(object sender, KeyPressEventArgs e)
        {

            if (e.KeyChar == '\r')
            {

                
                if (loginState == 1)
                {
                    sendRoutine();
                }
                textBoxMsgToSend.Text = "";
                e.Handled = true;
            }
        }


        private void textBoxUsername_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                e.Handled = true;
                login();
            }
        }

        private void login()
        {
            CheckForIllegalCrossThreadCalls = false;
            username = textBoxUsername.Text.TrimEnd(' ');
            textBoxUsername.Text = username;
            try
            {
                IPEndPoint EP = new IPEndPoint(IPAddress.Parse(serverIP), int.Parse(serverPort));
                T = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                T.Connect(EP);
                th = new Thread(listenRoutine);
                th.IsBackground = true;
                th.Start();
                textBoxMsgToSend.Focus();
                sendMessage("0|" + username + "|login");
                this.Text += " - Welcome " + username + "!";
            }
            catch (Exception)
            {
                MessageBox.Show("Cannot connect to server!");
                textBoxUsername.Focus();
                return;
            }

            buttonLogin.Enabled = false;
            loginState = 1;
            buttonBroadcast.Enabled = true;
            buttonSend.Enabled = true;
            richTextBoxBoard.Text = "";
            String time = DateTime.Now.ToString("(HH:mm)");
            richTextBoxBoard.SelectionColor = Color.Red;
            richTextBoxBoard.AppendText(time + " Login successfully!!!"+Environment.NewLine);
            richTextBoxBoard.SelectionColor = Color.Black;
            textBoxUsername.Enabled = false;
            buttonLogout.Enabled = true;
            labelStatus.Text = "廣播模式";
        }

        private void richTextBoxBoard_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }




        private string MyIP()
        {
            string hostname = Dns.GetHostName();
            IPAddress[] ip = Dns.GetHostEntry(hostname).AddressList;
            foreach (IPAddress iptem in ip)
            {
                if (iptem.AddressFamily == AddressFamily.InterNetwork)
                    return iptem.ToString();
            }

            return "";
        }

        private void buttonLogout_Click(object sender, EventArgs e)
        {
            sendMessage("9|" + username + "|quit");
            T.Close();
            th.Abort();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Form2 f = new Form2(this);
            f.Show();
            this.Opacity = 0;
            f.Focus();
        }

        public void setIP(string IP, string port)
        {
            serverIP = IP;
            serverPort = port;
        }

        public void showup()
        {
            this.Opacity = 1;
        }



    }
}

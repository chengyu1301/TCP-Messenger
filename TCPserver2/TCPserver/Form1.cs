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
using System.Collections;

namespace TCPserver
{
    public partial class Form1 : Form
    {
        private object listlocker = new Object();
        private object sendlocker = new Object();
        private object receivelocker = new Object();
        TcpListener server;
        Socket client;
        Thread serverTh;
        Thread clientTh;
        Hashtable HT = new Hashtable();
        string serverIP = IPAddress.Any.ToString();
        string serverPort = "9090";

        public Form1()
        {
            InitializeComponent();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            serverPort = textBox1.Text;
            CheckForIllegalCrossThreadCalls = false;
            serverTh = new Thread(serverRoutine);
            serverTh.IsBackground = true;
            serverTh.Start();
            buttonStart.Enabled = false;
        }

        private void serverRoutine()
        {
            IPEndPoint EP = new IPEndPoint(IPAddress.Parse(serverIP), int.Parse(serverPort));
            server = new TcpListener(EP);
            server.Start(100);
            while (true)
            {
                client = server.AcceptSocket();
                clientTh = new Thread(clientRoutine);
                clientTh.IsBackground = true;
                clientTh.Start();
            }
        }



        private void clientRoutine()
        {
            Socket sck = client;
            Thread th = clientTh;
            string socketname = "";
            while (true)
            {

                try
                {
                    byte[] buffer = new byte[4096];
                    string repeatedName = "64|server|repeated username";
                    int inLength = sck.Receive(buffer);
                    lock (receivelocker)
                    {
                        string msg = Encoding.UTF8.GetString(buffer, 0, inLength);
                        string[] c = msg.Split('|');
                        string cmd = c[0];
                        string who = c[1];
                        string str = c[2];
                        //MessageBox.Show("SERVER  Cmd:" + cmd + " Who:" + who + " Str:" + str);
                        switch (cmd)
                        {
                            case "0":                                 // for user login
                                if (HT.ContainsKey(who) || who == "" || who.Length > 20 || who == "You" || who == "you" || who == "server" || who == "Server")
                                {
                                    buffer = Encoding.UTF8.GetBytes(repeatedName);
                                    Thread.Sleep(10);
                                    lock (sendlocker)
                                    {
                                        sck.Send(buffer, 0, buffer.Length, SocketFlags.None);
                                    }
                                    break;

                                }

                                socketname = who;
                                lock (listlocker)
                                {
                                    HT.Add(who, sck);
                                    listBoxUser.Items.Add(who);
                                }

                                sendToAll("99" + "|server|" + who + " has logged in");

                                sendToAll(cmd + "|server|" + getOnlineList());
                                break;
                            case "1":                                 // for broadcast

                                sendToAll(cmd + "|" + who + "|" + str);
                                break;
                            /* case "9":                                 // for user logoout
                                 HT.Remove(who);
                                 listBoxUser.Items.Remove(who);
                                 sendToAll("99" + "|server|" + who + " has quit");
                                 sendToAll(0 + "|server|" + getOnlineList());
                                 break;*/
                            case "2":                                 // for private message
                                string to = c[3];
                                sendToClient(cmd + "|" + who + "|" + c[2], to);
                                break;
                        }
                   } 
                   } 
                
                catch (Exception)
                {
                    lock (receivelocker)
                    {
                        lock (listlocker)
                        {
                            HT.Remove(socketname);
                            listBoxUser.Items.Remove(socketname);
                        }
                        Console.WriteLine(socketname + "has lost");
                        Thread.Sleep(500);
                        sendToAll("99" + "|server|" + socketname + " has lost connection");
                        sendToAll(0 + "|server|" + getOnlineList());

                        th.Abort();
                    }
                }
    


            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.ExitThread();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
            //timerUpdateList.Enabled = true;
        }

        private string getOnlineList()
        {

                if (listBoxUser.Items.Count > 0)
                {
                    string list = "";
                    for (int i = 0; i < listBoxUser.Items.Count - 1; ++i)
                    {
                        list += listBoxUser.Items[i] + ",";
                    }
                    list += listBoxUser.Items[listBoxUser.Items.Count - 1];
                    return list;
                }

            return "";
        }



        private void sendToClient(string str, string user)
        {

                byte[] buffer = Encoding.UTF8.GetBytes(str);
                lock (listlocker)
                {
                    Socket sck = (Socket)HT[user];

                try
                {
                    lock (sendlocker)
                    {
                        sck.Send(buffer, 0, buffer.Length, SocketFlags.None);
                    }
                }
                catch (Exception)
                {
                    ;
                }
                }
        }

        private void sendToAll(string str)
        {

                byte[] buffer = Encoding.UTF8.GetBytes(str);
                /*List<Socket> sks = HT.Values.Cast<Socket>().ToList();*/

                lock (listlocker)
                {

                    foreach (Socket s in HT.Values)
                    {
                        try
                        {
                            lock (sendlocker)
                            {
                                s.Send(buffer, 0, buffer.Length, SocketFlags.None);
                            }
                        }
                        catch (Exception)
                        {
                            ;
                        }

                    }
                }

            
        }

        private void listBoxUser_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string usr="";

            lock (listlocker)
            {
                try
                {

                    usr = listBoxUser.GetItemText(listBoxUser.SelectedItem);

                }
                finally
                {
                    sendToClient("99|server|You have been kicked out", usr);
                    ((Socket)HT[usr]).Close();
                    HT.Remove(usr);
                    listBoxUser.Items.Remove(usr);
                    sendToAll("99" + "|server|" + usr + " has been kicked out");

                }
            }

        }

        private void listBoxUser_SizeChanged(object sender, EventArgs e)
        {

                sendToAll("0" + "|server|" + getOnlineList());

        }

        private void timerUpdateList_Tick(object sender, EventArgs e)
        {

                sendToAll("0" + "|server|" + getOnlineList());

        }
    }
}

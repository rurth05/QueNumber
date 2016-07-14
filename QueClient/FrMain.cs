using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using QueClient.Common;

namespace QueClient
{

    //The commands for interaction between the server and the client
    enum Command
    {
        Login,      //Log into the server
        Logout,     //Logout of the server
        Message,    //Send a text message to all the chat clients
        List,       //Get a list of users in the chat room from the server
        Null        //No command
    }

    public partial class FrMain : Form
    {
        #region Declaration
        public Socket clientSocket; //The main client socket
        public string strName;      //Name by which the user logs into the room
        public EndPoint epServer;   //The EndPoint of the server

        byte[] byteData = new byte[1024];
        #endregion

        private int i = 1;
        private IniFile inif;
        public string conString = ConfigurationManager.ConnectionStrings["conString"].ToString();

        public FrMain()
        {
            InitializeComponent();
            lblQueNo.Text = i.ToString();
            inif = new IniFile(Application.StartupPath + "\\settings.ini");
            lblQueStation.Text = ToTitleCase(inif.Read("Connection", "ClientID"));
            //_tcpClient = new TcpClient();

            try
            {
                strName = ToTitleCase(inif.Read("Connection", "ClientID"));
                //Using UDP sockets
                clientSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Dgram, ProtocolType.Udp);

                //IP address of the server machine
                IPAddress ipAddress = IPAddress.Parse(inif.Read("Connection", "IPAddress"));
                //Server is listening on port 1000
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, Convert.ToInt32(inif.Read("Connection", "Port")));

                epServer = (EndPoint)ipEndPoint;

                Data msgToSend = new Data();
                msgToSend.cmdCommand = Command.Login;
                msgToSend.strMessage = null;
                msgToSend.strName = strName;

                byte[] byteData = msgToSend.ToByte();

                //Login to the server
                clientSocket.BeginSendTo(byteData, 0, byteData.Length,
                    SocketFlags.None, epServer, new AsyncCallback(OnSend), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SGSclient",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            } 
        }

        private void FrMain_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;

            this.Text = "Que Client : " + strName;

            //The user has logged into the system so we now request the server to send
            //the names of all users who are in the chat room
            Data msgToSend = new Data();
            msgToSend.cmdCommand = Command.List;
            msgToSend.strName = strName;
            msgToSend.strMessage = null;

            byteData = msgToSend.ToByte();

            clientSocket.BeginSendTo(byteData, 0, byteData.Length, SocketFlags.None, epServer,
                new AsyncCallback(OnSend), null);

            byteData = new byte[1024];
            //Start listening to the data asynchronously
            clientSocket.BeginReceiveFrom(byteData,
                                       0, byteData.Length,
                                       SocketFlags.None,
                                       ref epServer,
                                       new AsyncCallback(OnReceive),
                                       null);
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            try
            {
                string message = string.Empty;
                double no = 0;
                string query = "SELECT * FROM ANTRIAN";
                var con = new SqlConnection(conString);
                var cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    no = Convert.ToDouble(rd["NO_ANTRIAN"]) + 1;
                    lblQueNo.Text = no.ToString();
                    string saveQue = "UPDATE ANTRIAN SET NO_ANTRIAN = @No";
                    var con1 = new SqlConnection(conString);
                    var com = new SqlCommand(saveQue, con1);
                    com.Parameters.Add("@No", SqlDbType.Decimal).Value = lblQueNo.Text;
                    con1.Open();
                    com.ExecuteNonQuery();
                    con1.Close();

                     message = lblQueNo.Text;
                }
                con.Close();

                //Fill the info for the message to be send
                Data msgToSend = new Data();

                msgToSend.strName = strName;
                msgToSend.strMessage = message;
                msgToSend.cmdCommand = Command.Message;

                byte[] byteData = msgToSend.ToByte();

                //Send it to the server
                clientSocket.BeginSendTo(byteData, 0, byteData.Length, SocketFlags.None, epServer, new AsyncCallback(OnSend), null);
                
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to send message to the server.", "SGSclientUDP: " + strName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            } 
        }

        private void OnSend(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SGSclient: " + strName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndReceive(ar);

                //Convert the bytes received into an object of type Data
                Data msgReceived = new Data(byteData);

                //Accordingly process the message received
                switch (msgReceived.cmdCommand)
                {
                    case Command.Login:
                        lstChatters.Items.Add(msgReceived.strName);
                        break;

                    case Command.Logout:
                        lstChatters.Items.Remove(msgReceived.strName);
                        break;

                    case Command.Message:
                        var str = msgReceived.strMessage.Split('$');
                        txtLastQue.Text = str[0];
                        txtTotal.Text = str[1];
                        break;

                    case Command.List:
                        lstChatters.Items.AddRange(msgReceived.strMessage.Split('*'));
                        lstChatters.Items.RemoveAt(lstChatters.Items.Count - 1);
                        txtChatBox.Text += ">>" + strName + " connected.\r\n";
                        break;
                }

                if (msgReceived.strMessage != null && msgReceived.cmdCommand != Command.List)
                    txtChatBox.Text += msgReceived.strMessage + "\r\n";

                byteData = new byte[1024];

                //Start listening to receive more data from the user
                clientSocket.BeginReceiveFrom(byteData, 0, byteData.Length, SocketFlags.None, ref epServer,
                                           new AsyncCallback(OnReceive), null);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SGSclient: " + strName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            inif.Write("Connection", "IPAddress", txtIPAddress.Text);
            inif.Write("Connection", "Port", txtPort.Text);
            inif.Write("Connection", "ClientID", txtClientID.Text);
            Application.Restart();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab.Text == "Settings")
            {
                txtIPAddress.Text = inif.Read("Connection", "IPAddress");
                txtPort.Text = inif.Read("Connection", "Port");
                txtClientID.Text = ToTitleCase(inif.Read("Connection", "ClientID"));
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        public string ToTitleCase(string str)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
        }
    }

    //The data structure by which the server and the client interact with 
    //each other
    class Data
    {
        //Default constructor
        public Data()
        {
            this.cmdCommand = Command.Null;
            this.strMessage = null;
            this.strName = null;
        }

        //Converts the bytes into an object of type Data
        public Data(byte[] data)
        {
            //The first four bytes are for the Command
            this.cmdCommand = (Command)BitConverter.ToInt32(data, 0);

            //The next four store the length of the name
            int nameLen = BitConverter.ToInt32(data, 4);

            //The next four store the length of the message
            int msgLen = BitConverter.ToInt32(data, 8);

            //This check makes sure that strName has been passed in the array of bytes
            if (nameLen > 0)
                this.strName = Encoding.UTF8.GetString(data, 12, nameLen);
            else
                this.strName = null;

            //This checks for a null message field
            if (msgLen > 0)
                this.strMessage = Encoding.UTF8.GetString(data, 12 + nameLen, msgLen);
            else
                this.strMessage = null;
        }

        //Converts the Data structure into an array of bytes
        public byte[] ToByte()
        {
            List<byte> result = new List<byte>();

            //First four are for the Command
            result.AddRange(BitConverter.GetBytes((int)cmdCommand));

            //Add the length of the name
            if (strName != null)
                result.AddRange(BitConverter.GetBytes(strName.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Length of the message
            if (strMessage != null)
                result.AddRange(BitConverter.GetBytes(strMessage.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Add the name
            if (strName != null)
                result.AddRange(Encoding.UTF8.GetBytes(strName));

            //And, lastly we add the message text to our array of bytes
            if (strMessage != null)
                result.AddRange(Encoding.UTF8.GetBytes(strMessage));

            return result.ToArray();
        }

        public string strName;      //Name by which the client logs into the room
        public string strMessage;   //Message text
        public Command cmdCommand;  //Command type (login, logout, send message, etcetera)
    }
}

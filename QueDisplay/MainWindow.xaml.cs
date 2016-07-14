using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace QueDisplay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    //The commands for interaction between the server and the client
    //enum Command
    //{
    //    Login,      //Log into the server
    //    Logout,     //Logout of the server
    //    Message,    //Send a text message to all the chat clients
    //    List,       //Get a list of users in the chat room from the server
    //    Null        //No command
    //}
    
    public partial class MainWindow : Window
    {
        //#region Declaration

        //private int _portNo = 8000;
        //private TcpListener _tcpListener;
        //private TcpClient _tcpClient;
        //private NetworkStream _networkStream;
        //private Thread _thread;

        //#endregion

        //The ClientInfo structure holds the required information about every
        //client connected to the server
        struct ClientInfo
        {
            public EndPoint endpoint;   //Socket of the client
            public string strName;      //Name by which the user logged into the chat room
        }

        //The collection of all clients logged into the room (an array of type ClientInfo)
        ArrayList clientList;

        //The main socket on which the server listens to the clients
        Socket serverSocket;

        byte[] byteData = new byte[1024];
        
        const double gap = 100.0; // pixel gap between each TextBlock
        const int timer_interval = 16; // number of ms between timer ticks. 16 is near 1/60th second, for smoother updates on LCD displays
        const double move_amount = 2.5; // number of pixels to move each timer tick. 1 - 1.5 is ideal, any higher will introduce judders

        private LinkedList<TextBlock> textBlocks = new LinkedList<TextBlock>();
        private Timer timer = new Timer();

        //public static Hashtable clientsList = new Hashtable();

        public string conString = ConfigurationManager.ConnectionStrings["conString"].ToString();

        public MainWindow()
        {
            InitializeComponent();

            clientList = new ArrayList();

            AddTextBlock("WINDOWS PRESENTATION FOUNDATION");

            canvas1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate(Object state)
            {
                var node = textBlocks.First;
                while (node != null)
                {
                    double left = 0;
                    if (node.Previous != null)
                    {
                        Canvas.SetLeft(node.Value, Canvas.GetLeft(node.Previous.Value) + node.Previous.Value.ActualWidth + gap);
                    }
                    else
                    {
                        Canvas.SetLeft(node.Value, canvas1.Width + gap);
                    }
                    node = node.Next;
                }
                return null;

            }), null);

            timer.Interval = timer_interval;
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Start();
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            canvas1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate(Object state)
            {
                var node = textBlocks.First;
                var lastNode = textBlocks.Last;

                while (node != null)
                {
                    double newLeft = Canvas.GetLeft(node.Value) - move_amount;
                    if (newLeft < (0 - (node.Value.ActualWidth + gap)))
                    {
                        textBlocks.Remove(node);
                        var lastNodeLeftPos = Canvas.GetLeft(lastNode.Value);
                        textBlocks.AddLast(node);

                        if ((lastNodeLeftPos + lastNode.Value.ActualWidth + gap) > canvas1.Width) // Last element is offscreen
                        {
                            newLeft = lastNodeLeftPos + lastNode.Value.ActualWidth + gap;
                        }
                        else
                        {
                            newLeft = canvas1.Width + gap;
                        }
                    }
                    Canvas.SetLeft(node.Value, newLeft);
                    node = node == lastNode ? null : node.Next;
                }
                return null;
            }), null);

            //Fill the info for the message to be send
            Data msgToSend = new Data();

            msgToSend.strName = "";
            msgToSend.strMessage = UpdateQue();
            msgToSend.cmdCommand = Command.Message;

            byte[] byteData = msgToSend.ToByte();

            IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint epSender = (EndPoint)ipeSender;

            foreach (ClientInfo clientInfo in clientList)
            {
                if (clientInfo.endpoint != epSender ||
                    msgToSend.cmdCommand != Command.Login)
                {
                    //Send the message to all users
                    serverSocket.BeginSendTo(byteData, 0, byteData.Length,
                                             SocketFlags.None,
                                             clientInfo.endpoint,
                                             new AsyncCallback(OnSend),
                                             clientInfo.endpoint);
                }
            }
        }

        public void DispQue(string queNum, string countName)
        {
            var switchOffAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.Zero
            };

            var switchOnAnimation = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.Zero,
                BeginTime = TimeSpan.FromSeconds(0.5)
            };

            var blinkStoryboard = new Storyboard
            {
                Duration = TimeSpan.FromSeconds(1),
                RepeatBehavior = new RepeatBehavior(5)
            };

            
            TextBlock tb = new TextBlock();
            tb.FontSize = 72;
            tb.FontWeight = FontWeights.Bold;
            tb.Background = Brushes.RoyalBlue;
            tb.Text = string.Empty;
            tb.Inlines.Add(queNum);
            tb.Inlines.Add(Environment.NewLine);
            var run = new Run(countName);
            run.Foreground = Brushes.Red;
            run.FontSize = 48;
            tb.Inlines.Add(run);
            tb.TextAlignment = TextAlignment.Center;

            Storyboard.SetTarget(switchOffAnimation, tb);
            Storyboard.SetTargetProperty(switchOffAnimation, new PropertyPath(Canvas.OpacityProperty));
            blinkStoryboard.Children.Add(switchOffAnimation);

            Storyboard.SetTarget(switchOnAnimation, tb);
            Storyboard.SetTargetProperty(switchOnAnimation, new PropertyPath(Canvas.OpacityProperty));
            blinkStoryboard.Children.Add(switchOnAnimation);

            tb.BeginStoryboard(blinkStoryboard);

            stackPanel1.Children.Add(tb);
        }

        void AddTextBlock(string Text)
        {
            TextBlock tb = new TextBlock();
            tb.Text = Text;
            tb.FontSize = 64;
            tb.FontFamily = new FontFamily("Times New Roman");
            tb.FontWeight = FontWeights.Normal;
            tb.Foreground = Brushes.WhiteSmoke;

            canvas1.Children.Add(tb);

            Canvas.SetTop(tb, 20);
            Canvas.SetLeft(tb, -999);

            textBlocks.AddLast(tb);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //We are using UDP sockets
                serverSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Dgram, ProtocolType.Udp);

                //Assign the any IP of the machine and listen on port number 1000
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 1000);

                //Bind this address to the server
                serverSocket.Bind(ipEndPoint);

                IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
                //The epSender identifies the incoming clients
                EndPoint epSender = (EndPoint)ipeSender;

                //Start receiving data
                serverSocket.BeginReceiveFrom(byteData, 0, byteData.Length,
                    SocketFlags.None, ref epSender, new AsyncCallback(OnReceive), epSender);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            //Start();
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
                    EndPoint epSender = (EndPoint)ipeSender;

                    serverSocket.EndReceiveFrom(ar, ref epSender);

                    //Transform the array of bytes received from the user into an
                    //intelligent form of object Data
                    Data msgReceived = new Data(byteData);

                    //We will send this object in response the users request
                    Data msgToSend = new Data();

                    byte[] message;

                    //If the message is to login, logout, or simple text message
                    //then when send to others the type of the message remains the same
                    msgToSend.cmdCommand = msgReceived.cmdCommand;
                    msgToSend.strName = msgReceived.strName;

                    switch (msgReceived.cmdCommand)
                    {
                        case Command.Login:

                            //When a user logs in to the server then we add her to our
                            //list of clients

                            ClientInfo clientInfo = new ClientInfo();
                            clientInfo.endpoint = epSender;
                            clientInfo.strName = msgReceived.strName;

                            clientList.Add(clientInfo);

                            //Set the text of the message that we will broadcast to all users
                            msgToSend.strMessage = ">> " + msgReceived.strName +
                                                   " connected.";
                            break;

                        case Command.Logout:

                            //When a user wants to log out of the server then we search for her 
                            //in the list of clients and close the corresponding connection

                            int nIndex = 0;
                            foreach (ClientInfo client in clientList)
                            {
                                if (client.endpoint == epSender)
                                {
                                    clientList.RemoveAt(nIndex);
                                    break;
                                }
                                ++nIndex;
                            }

                            msgToSend.strMessage = ">>" + msgReceived.strName +
                                                   " disconnect";
                            break;

                        case Command.Message:

                            //Set the text of the message that we will broadcast to all users
                            msgToSend.strMessage = msgReceived.strMessage + "$" +
                                                   msgReceived.strName;
                            break;

                        case Command.List:

                            //Send the names of all users in the chat room to the new user
                            msgToSend.cmdCommand = Command.List;
                            msgToSend.strName = null;
                            msgToSend.strMessage = null;

                            //Collect the names of the user in the chat room
                            foreach (ClientInfo client in clientList)
                            {
                                //To keep things simple we use asterisk as the marker to separate the user names
                                msgToSend.strMessage += client.strName + "*";
                            }

                            message = msgToSend.ToByte();

                            //Send the name of the users in the chat room
                            serverSocket.BeginSendTo(message, 0, message.Length,
                                                     SocketFlags.None, epSender,
                                                     new AsyncCallback(OnSend),
                                                     epSender);
                            break;
                    }

                    if (msgToSend.cmdCommand != Command.List)
                    //List messages are not broadcasted
                    {
                        message = msgToSend.ToByte();

                        foreach (ClientInfo clientInfo in clientList)
                        {
                            if (clientInfo.endpoint != epSender ||
                                msgToSend.cmdCommand != Command.Login)
                            {
                                //Send the message to all users
                                serverSocket.BeginSendTo(message, 0, message.Length,
                                                         SocketFlags.None,
                                                         clientInfo.endpoint,
                                                         new AsyncCallback(OnSend),
                                                         clientInfo.endpoint);
                            }
                        }

                        if (msgToSend.cmdCommand == Command.Message)
                        {
                            var str = msgToSend.strMessage.Split('$');
                            DispQue(str[0],str[1]);
                        }
                    }

                    //If the user is logging out then we need not listen from her
                    if (msgReceived.cmdCommand != Command.Logout)
                    {
                        //Start listening to the message send by the user
                        serverSocket.BeginReceiveFrom(byteData, 0, byteData.Length,
                                                      SocketFlags.None,
                                                      ref epSender,
                                                      new AsyncCallback(OnReceive),
                                                      epSender);
                    }
                }));

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void OnSend(IAsyncResult ar)
        {
            try
            {
                serverSocket.EndSend(ar);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public string UpdateQue()
        {
            string str = string.Empty;
            string query = "SELECT * FROM ANTRIAN";
            var con = new SqlConnection(conString);
            var cmd = new SqlCommand(query, con);
            con.Open();
            SqlDataReader rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                str = rd["NO_ANTRIAN"] + "$" + rd["TOTAL_ANTRIAN"];
            }
            con.Close();

            return str;
        }
    }

    //The data structure by which the server and the client interact with 
    //each other
    //class Data
    //{
    //    //Default constructor
    //    public Data()
    //    {
    //        this.cmdCommand = Command.Null;
    //        this.strMessage = null;
    //        this.strName = null;
    //    }

    //    //Converts the bytes into an object of type Data
    //    public Data(byte[] data)
    //    {
    //        //The first four bytes are for the Command
    //        this.cmdCommand = (Command)BitConverter.ToInt32(data, 0);

    //        //The next four store the length of the name
    //        int nameLen = BitConverter.ToInt32(data, 4);

    //        //The next four store the length of the message
    //        int msgLen = BitConverter.ToInt32(data, 8);

    //        //This check makes sure that strName has been passed in the array of bytes
    //        if (nameLen > 0)
    //            this.strName = Encoding.UTF8.GetString(data, 12, nameLen);
    //        else
    //            this.strName = null;

    //        //This checks for a null message field
    //        if (msgLen > 0)
    //            this.strMessage = Encoding.UTF8.GetString(data, 12 + nameLen, msgLen);
    //        else
    //            this.strMessage = null;
    //    }

    //    //Converts the Data structure into an array of bytes
    //    public byte[] ToByte()
    //    {
    //        List<byte> result = new List<byte>();

    //        //First four are for the Command
    //        result.AddRange(BitConverter.GetBytes((int)cmdCommand));

    //        //Add the length of the name
    //        if (strName != null)
    //            result.AddRange(BitConverter.GetBytes(strName.Length));
    //        else
    //            result.AddRange(BitConverter.GetBytes(0));

    //        //Length of the message
    //        if (strMessage != null)
    //            result.AddRange(BitConverter.GetBytes(strMessage.Length));
    //        else
    //            result.AddRange(BitConverter.GetBytes(0));

    //        //Add the name
    //        if (strName != null)
    //            result.AddRange(Encoding.UTF8.GetBytes(strName));

    //        //And, lastly we add the message text to our array of bytes
    //        if (strMessage != null)
    //            result.AddRange(Encoding.UTF8.GetBytes(strMessage));

    //        return result.ToArray();
    //    }

    //    public string strName;      //Name by which the client logs into the room
    //    public string strMessage;   //Message text
    //    public Command cmdCommand;  //Command type (login, logout, send message, etcetera)
    //}
}

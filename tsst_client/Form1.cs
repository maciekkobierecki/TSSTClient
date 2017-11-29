using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace tsst_client
{
    public partial class Form1 : Form
    {
        public const int PORT_NUMBER_SIZE = 4;
        static Socket output_socket = null;
        static Socket inputSocket = null;
        Packet messageOut;
        Packet messageIn;
        private int inPort;
        private int outPort;
        private int outCounter;
        private int inCounter;

        public Form1()
        {
            InitializeComponent();
            outCounter = 0;
            inCounter = 0;
        }

        private void Connect()                  //Połączenie
        {
            output_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAdd = IPAddress.Parse("127.0.0.1");
            outPort = Int32.Parse(textBox1.Text);
            IPEndPoint remoteEP = new IPEndPoint(ipAdd, outPort); //Int32.Parse(ConfigurationManager.AppSettings["output_port"]));
            output_socket.Connect(remoteEP);

        }

        private void button1_Click(object sender, EventArgs e)
        {

            Connect();
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void send_Click(object sender, EventArgs e)
        {
            SendMessage(messageOut);
        }

        private void SendMessage(Packet packet)              //Wysyłanie. Wysyła określoną liczbę pakietów co określony odstęp czasu. 
        {
            string random_meassage;
            Thread thread;
            thread = new Thread(async () =>
            {
                for (int i = 1; i <= Int32.Parse(nb_of_m_tb.Text); i++)
                {
                    random_meassage = message_tb.Text + RandomString();
                    packet = new Packet(random_meassage,"", "", outPort, 0, GetTimeStamp(), "I1");
                    if (output_socket.Connected)
                    {
                        int serializedObjectSize = GetSerializedMessage(packet).Length;
                        int dataSize = serializedObjectSize + PORT_NUMBER_SIZE;
                        byte[] mSize = BitConverter.GetBytes(dataSize);
                        byte[] portNumber = BitConverter.GetBytes(outPort);
                        output_socket.Send(mSize);
                        output_socket.Send(portNumber);
                        byte[] serializedData = GetSerializedMessage(packet);
                        output_socket.Send(serializedData);
                        logs_list.Invoke(new Action(delegate ()
                        {
                        logs_list.Items.Add(++inCounter + ": " + dataSize + "|" + packet.s + " | " + packet.sourcePort + " | " + packet.timestamp);
                            logs_list.SelectedIndex = logs_list.Items.Count - 1;
                        }));

                    }
                    await Task.Delay(Int32.Parse(delay_tb.Text));
                }
            }
            );
            thread.Start();
        }

        private void SendSingleMessage(Packet sm)
        {
            Thread tr;
            tr = new Thread(() =>
            {
                Connect();
                if (sm._interface == "I1")             //
                    sm._interface = "I2";              //do usunięcia (testy)
                else if (sm._interface == "I2")        //
                    sm._interface = "I3";
                else
                    sm._interface = "I1";
                output_socket.Send(GetSerializedMessage(sm));
                logs_list.Invoke(new Action(delegate ()
                {
                    logs_list.Items.Add(": " + sm.s + " | " + sm.sourcePort + " | " + sm.timestamp);
                    logs_list.SelectedIndex = logs_list.Items.Count - 1;
                }));
            }
            );
            tr.Start();
        }

        private void Listen()      
        {
            inputSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAdd = IPAddress.Parse("127.0.0.1");
            inPort = Int32.Parse(textBox2.Text);
            IPEndPoint remoteEP = new IPEndPoint(ipAdd, inPort); //Int32.Parse(ConfigurationManager.AppSettings["input_port"]));
            inputSocket.Connect(remoteEP);
            while (true)
            {
                ProcessIncomingData();
            }
        }

        private int ReceiveDataSize()
        {
            byte[] objectSize = new byte[4];
            inputSocket.Receive(objectSize, 0, 4, SocketFlags.None);
            int messageSize = BitConverter.ToInt32(objectSize, 0);
            return messageSize;
        }

        private void RemoveSourcePortNumberFromData()
        {
            byte[] bytes = new byte[4];
            inputSocket.Receive(bytes, 0, 4, SocketFlags.None);
        }

        private int DecreaseDataSizeByPortNumber(int numberToDecrease)
        {
            int decreased = numberToDecrease - 4;
            return decreased;
        }

        private void ProcessIncomingData()
        {
            int messageSize;
            messageSize = ReceiveDataSize();
            RemoveSourcePortNumberFromData();
            int decreased = DecreaseDataSizeByPortNumber(messageSize);
            byte[] bytes = new byte[messageSize];
            int readByte = inputSocket.Receive(bytes, 0, decreased, SocketFlags.None);
            Thread t;
            t = new Thread(() =>
            {
                messageIn = GetDeserializedMessage(bytes);
                receive_logs_list.Invoke(new Action(delegate ()
                {
                    receive_logs_list.Items.Add(outCounter + ": " + messageSize + "|" + messageIn.s + " | " + messageIn.sourcePort + " | " + messageIn.timestamp);
                    receive_logs_list.SelectedIndex = receive_logs_list.Items.Count - 1;
                }));
            }
            );
            t.Start();
            outCounter++;

        }
        private byte[] GetSerializedMessage(Packet mes)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, mes);
            return ms.ToArray();
        }

        private Packet GetDeserializedMessage(byte[] b)
        {
            Packet m = null; 
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(b, 0, b.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            m = (Packet)binForm.Deserialize(memStream);
            return m;
        }




        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Listen();
            }
            catch (IOException)
            {
                logs_list.Invoke(new Action(delegate ()
                {
                    logs_list.Items.Add("Problem with communication");
                }));
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("Processing cancelled", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(e.Result.ToString(), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private string RandomString()       //Dokleja randomowego stringa z tej puli o randomowej długości z danego przedziału (tak ma być)
        {
            Random random = new Random();
            int string_length = random.Next(1, 5);
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, string_length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GetTimeStamp()
        {
            DateTime dateTime = DateTime.Now;
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss.ff");
        }





    
    }
}
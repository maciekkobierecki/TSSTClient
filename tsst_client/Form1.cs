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
        public const int INTEGER_SIZE = 4;
        public const String SEND_FUNCTION = "send";
        public const String RECEIVE_FUNCTION = "receive";
        static Socket output_socket = null;
        static Socket inputSocket = null;
        Packet messageIn;
        private int inPort;
        private int outPort;
        private int outCounter;
        private int inCounter;
        private int sendingDelay;


        public Form1()
        {
            InitializeComponent();
            outCounter = 0;
            inCounter = 0;
        }

        private void Connect()
        {
            output_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAdd = IPAddress.Parse("127.0.0.1");
            outPort = Int32.Parse(textBox1.Text);
            IPEndPoint remoteEP = new IPEndPoint(ipAdd, outPort);
            output_socket.Connect(remoteEP);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            Connect();
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void send_Click(object sender, EventArgs e)
        {
            ProcessUIOrder();
            sendingDelay = Int32.Parse(delay_tb.Text);
        }

        private void ProcessUIOrder() 
        {
            Thread thread;
            thread = new Thread(() => sendPackets());
            thread.Start();
        }

        private void sendPackets()
        {
            Packet packet = null;
            for (int i = 1; i <= Int32.Parse(nb_of_m_tb.Text); i++)
            {
                packet = NextPacket();
                if (output_socket.Connected)
                {
                    byte[] serializedMessage = SerializePacket(packet);
                    int serializedObjectSize = serializedMessage.Length;
                    int dataSize = serializedObjectSize + 2 * PORT_NUMBER_SIZE;
                    byte[] sizeOfData = ConvertToByteArray(dataSize);
                    byte[] portNumber = ConvertToByteArray(outPort);
                    Send(sizeOfData, portNumber, portNumber, serializedMessage);
                    inCounter++;
                    updateUI(SEND_FUNCTION, inCounter, packet);

                }
                Thread.Sleep(sendingDelay);
            }
        }

        private Packet NextPacket()
        {
            string random_meassage;
            random_meassage = message_tb.Text + RandomString();
            Packet packet=new Packet(random_meassage, destinationTextBox.Text, "", outPort, 0, GetTimeStamp(), "I1");
            return packet;
        }
        
        private byte[] ConvertToByteArray(int number)
        {
            byte[] converted = BitConverter.GetBytes(number);
            return converted;
        }
        private void Send(byte[] dataSize, byte[] destinationPort, byte[] sourcePort, byte[] serializedPacket)
        {
            output_socket.Send(dataSize);
            output_socket.Send(destinationPort);
            output_socket.Send(sourcePort);
            output_socket.Send(serializedPacket);
        }

        private void updateUI(String function,int counter, Packet packet)
        {
            ListBox updatingListBox = GetProperListBox(function);
            updatingListBox.Invoke(new Action(delegate ()
            {
                updatingListBox.Items.Add(counter + "|" + packet.s + " | " + packet.timestamp);
                updatingListBox.SelectedIndex = updatingListBox.Items.Count - 1;
            }));
        }

        private ListBox GetProperListBox(String function)
        {
            if (function.Equals(SEND_FUNCTION))
                return logs_list;
            else if (function.Equals(RECEIVE_FUNCTION))
                return receive_logs_list;
            else
                return null;
        }

        private void Listen()      
        {
            inputSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAdd = IPAddress.Parse("127.0.0.1");
            inPort = Int32.Parse(textBox2.Text);
            IPEndPoint remoteEP = new IPEndPoint(ipAdd, inPort);
            inputSocket.Connect(remoteEP);
            while (true)
            {
                ProcessIncomingData();
            }
        }

        private int ReceiveDataSize()
        {
            byte[] objectSize = new byte[INTEGER_SIZE];
            inputSocket.Receive(objectSize, 0, INTEGER_SIZE, SocketFlags.None);
            int messageSize = BitConverter.ToInt32(objectSize, 0);
            return messageSize;
        }

        private void RemoveSourcePortNumberFromData()
        {
            byte[] bytes = new byte[PORT_NUMBER_SIZE];
            inputSocket.Receive(bytes, 0, 4, SocketFlags.None);
        }

        private int DecreaseDataSizeByPortNumber(int numberToDecrease)
        {
            int decreased = numberToDecrease - 2*PORT_NUMBER_SIZE;
            return decreased;
        }

        private void ProcessIncomingData()
        {
            int messageSize;
            messageSize = ReceiveDataSize();
            RemoveSourcePortNumberFromData();
            RemoveSourcePortNumberFromData();
            int decreased = DecreaseDataSizeByPortNumber(messageSize);
            byte[] bytes;
            bytes = ReceiveData(decreased);
            messageIn = DeserializePacket(bytes);
            outCounter++;
            updateUI(RECEIVE_FUNCTION, outCounter, messageIn);
        }

        private byte[] ReceiveData(int inputSize)
        {
            byte[] bytes = new byte[inputSize];
            int totalReceived = 0;
            do
            {
                int received = inputSocket.Receive(bytes, totalReceived, inputSize - totalReceived, SocketFlags.Partial);
                totalReceived += received;
            } while (totalReceived != inputSize);

            return bytes;
        }

        private byte[] SerializePacket(Packet mes)
        {
            BinaryFormatter bf;
            MemoryStream ms;
            ms = new MemoryStream();
            bf = new BinaryFormatter();
            bf.Serialize(ms, mes);
            byte[] bytes = ms.ToArray();
            return bytes;
        }

        private Packet DeserializePacket(byte[] b)
        {
            Packet m = null;
            b[0] = 1;
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

        private string RandomString()     
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PT_Linx_DEMO
{
    public class TcpClientManager
    {
        private TcpClient _client;
        private NetworkStream _stream;
        public event EventHandler<DataReceivedEventArgs_TCP> DataReceived;
        private static TcpClientManager _instance;
        //private List<byte> frameBuffer = new List<byte>(); // Buffer แยก Frame
        //private readonly object bufferLock = new object(); // ป้องกันการอ่านซ้ำซ้อน

        public static TcpClientManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TcpClientManager();
                }
                return _instance;
            }
        }

        private TcpClientManager() { }

        public async Task ConnectAsync(string ipAddress, int port)
        {
            try
            {
                if (_client != null && _client.Connected)
                {
                    Console.WriteLine("Already connected.");
                    return;
                }

                _client = new TcpClient();
                await _client.ConnectAsync(ipAddress, port);
                _stream = _client.GetStream();
                Console.WriteLine("Connected to server.");
                MessageBox.Show("Connected to server.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                StartListening();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection error: " + ex.Message);
            }
        }

        public void Disconnect()
        {
            try
            {
                _stream?.Close();
                _client?.Close();
                Console.WriteLine("Disconnected from server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Disconnection error: " + ex.Message);
            }
        }

        //public async Task SendCommandAsync(byte[] commandBytes)
        //{
        //    try
        //    {
        //        if (_client != null && _client.Connected)
        //        {
        //            await _stream.WriteAsync(commandBytes, 0, commandBytes.Length);
        //            await _stream.FlushAsync();
        //        }
        //        else
        //        {
        //            Console.WriteLine("Not connected to server.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Send error: " + ex.Message);
        //    }
        //}
        private Queue<byte[]> sendQueue = new Queue<byte[]>();
        private bool isSending = false;

        public async Task SendCommandAsync(byte[] commandBytes)
        {
            lock (sendQueue)
            {
                sendQueue.Enqueue(commandBytes);
            }

            await ProcessSendQueue();
        }

        private async Task ProcessSendQueue()
        {
            if (isSending) return;
            isSending = true;

            while (sendQueue.Count > 0)
            {
                byte[] commandToSend;
                lock (sendQueue)
                {
                    commandToSend = sendQueue.Dequeue();
                }

                try
                {
                    if (_client != null && _client.Connected)
                    {
                        await _stream.WriteAsync(commandToSend, 0, commandToSend.Length);
                        await _stream.FlushAsync();
                        await Task.Delay(50); // ป้องกันการส่งติดกันเร็วเกินไป
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Send error: " + ex.Message);
                }
            }

            isSending = false;
        }

        private async void StartListening()
        {
            try
            {
                while (_client.Connected)
                {
                    byte[] buffer = new byte[8192]; // เพิ่มขนาด Buffer
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        ProcessReceivedData(buffer, bytesRead);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Read error: " + ex.Message);
            }
        }

        //private void ProcessReceivedData(byte[] buffer, int bytesRead)
        //{
        //    lock (bufferLock)
        //    {
        //        // เพิ่มข้อมูลเข้า buffer
        //        frameBuffer.AddRange(buffer.Take(bytesRead));

        //        // วนลูปเช็ค Frame (เช็คทุกครั้งเผื่อข้อมูลมาติดกัน)
        //        while (frameBuffer.Count > 1)
        //        {
        //            if (frameBuffer[frameBuffer.Count - 2] == 0x1B &&
        //                frameBuffer[frameBuffer.Count - 1] == 0x03)
        //            {
        //                string hexString = BitConverter.ToString(frameBuffer.ToArray()).Replace("-", " ");
        //                Console.WriteLine("Received Frame: " + hexString);
        //                DataReceived?.Invoke(this, new DataReceivedEventArgs_TCP(hexString));
        //                frameBuffer.Clear();
        //            }
        //            else if (frameBuffer.Count >= 2 &&
        //                     frameBuffer[frameBuffer.Count - 2] == 0x1B &&
        //                     frameBuffer[frameBuffer.Count - 1] == 0x0F)
        //            {
        //                string hexString = BitConverter.ToString(frameBuffer.ToArray()).Replace("-", " ");
        //                Console.WriteLine("Received Count Frame: " + hexString);
        //                DataReceived?.Invoke(this, new DataReceivedEventArgs_TCP(hexString));
        //                frameBuffer.Clear();
        //            }
        //            else
        //            {
        //                break; // หยุด loop ถ้ายังไม่ได้ frame ครบ
        //            }
        //        }
        //    }
        //}
        private Queue<byte> frameBuffer = new Queue<byte>();
        private readonly object bufferLock = new object();

        private void ProcessReceivedData(byte[] buffer, int bytesRead)
        {
            lock (bufferLock)
            {
                // เพิ่มข้อมูลเข้า Queue buffer
                for (int i = 0; i < bytesRead; i++)
                {
                    frameBuffer.Enqueue(buffer[i]);
                }

                // วนลูปตรวจจับ Frame (สามารถแยก Frame ได้เร็วขึ้น)
                while (frameBuffer.Count >= 2)
                {
                    byte[] frameArray = frameBuffer.ToArray(); // คัดลอกเป็น Array
                    int frameLength = frameArray.Length;

                    for (int i = 0; i < frameLength - 1; i++)
                    {
                        if (frameArray[i] == 0x1B && frameArray[i + 1] == 0x03)
                        {
                            string hexString = BitConverter.ToString(frameArray, 0, i + 2).Replace("-", " ");
                            Console.WriteLine("Received Frame: " + hexString);
                            DataReceived?.Invoke(this, new DataReceivedEventArgs_TCP(hexString));

                            // ลบเฉพาะ Frame ที่อ่านแล้วออกจาก Queue
                            for (int j = 0; j <= i + 1; j++)
                            {
                                frameBuffer.Dequeue();
                            }

                            break; // ตรวจสอบ Frame ถัดไป
                        }
                        else if (frameArray[i] == 0x1B && frameArray[i + 1] == 0x0F)
                        {
                            string hexString = BitConverter.ToString(frameArray, 0, i + 2).Replace("-", " ");
                            Console.WriteLine("Received Count Frame: " + hexString);
                            DataReceived?.Invoke(this, new DataReceivedEventArgs_TCP(hexString));

                            // ลบเฉพาะ Frame ที่อ่านแล้วออกจาก Queue
                            for (int j = 0; j <= i + 1; j++)
                            {
                                frameBuffer.Dequeue();
                            }

                            break; // ตรวจสอบ Frame ถัดไป
                        }
                    }
                }
            }
        }


        public bool IsConnected()
        {
            return _client != null && _client.Connected;
        }
    }

    public class DataReceivedEventArgs_TCP : EventArgs
    {
        public string Data { get; private set; }

        public DataReceivedEventArgs_TCP(string data)
        {
            Data = data;
        }
    }
}

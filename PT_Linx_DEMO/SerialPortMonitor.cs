using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace PT_Linx_DEMO.Class
{
    public class SerialPortManager
    {
        public SerialPort serialPort;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public static SerialPortManager _instance;
        private List<byte> receivedBuffer = new List<byte>(); // เก็บข้อมูลที่รับมา

        public static SerialPortManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SerialPortManager();
                }
                return _instance;
            }
        }

        public SerialPortManager()
        {
            serialPort = new SerialPort();
            serialPort.Encoding = Encoding.GetEncoding(28591);
            serialPort.DataReceived += new SerialDataReceivedEventHandler(OnDataReceived);
        }

        public void ConfigureSerialPort(string portName, string baudRate, string dataBits, string stopBits, string parity)
        {
            try
            {
                serialPort.PortName = portName;
                serialPort.BaudRate = Convert.ToInt32(baudRate);
                serialPort.DataBits = Convert.ToInt32(dataBits);
                serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBits);
                serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), parity);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Configuration error: " + ex.Message);
                Console.WriteLine("Configuration error: " + ex.Message);
            }
        }

        public void OpenSerialPort()
        {
            try
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                    Console.WriteLine("Serial Port Opened Successfully!");
                    //MessageBox.Show("Serial Port Opened Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Serial Port is already open.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid port number", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine("Open error: " + ex.Message);
            }
        }

        public void CloseSerialPort()
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                    //MessageBox.Show("Serial Port Closed Successfully!");
                }
                else
                {
                    //MessageBox.Show("Serial Port is not open.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Close error: " + ex.Message);
            }
        }

        public void SetProgressBar(ProgressBar progressBar)
        {
            if (serialPort.IsOpen)
            {
                progressBar.Value = 100;
            }
            else
            {
                progressBar.Value = 0;
            }
        }

        public async Task SendCommandAsync(byte[] commandBytes)
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    serialPort.Write(commandBytes, 0, commandBytes.Length);
                    // Adding a small delay might help with timing issues
                    await Task.Delay(5); // Delay for 100 milliseconds

                    // Log command sent successfully
                    //Console.WriteLine("Command sent successfully.");

                    stopwatch.Stop();
                    var elapsed_time = stopwatch.Elapsed.TotalMilliseconds;
                    Console.WriteLine("Time elapsed send (ms): {0}", elapsed_time);
                }
                else
                {
                    // Log serial port not open
                    Console.WriteLine("Serial Port is not open.");
                }
            }
            catch (Exception ex)
            {
                // Log send error
                Console.WriteLine("Send error: " + ex.Message);
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            int bytesToRead = serialPort.BytesToRead; // จำนวนไบต์ที่รออยู่ในบัฟเฟอร์
            if (bytesToRead > 0)
            {
                byte[] buffer = new byte[bytesToRead];

                serialPort.Read(buffer, 0, bytesToRead); // อ่านข้อมูลทั้งหมดที่มี

                // เพิ่มข้อมูลเข้าไปใน Buffer
                receivedBuffer.AddRange(buffer);
                //Console.WriteLine("Received Buffer (Hex): " + BitConverter.ToString(buffer));
                string hexString = ByteArrayToString(receivedBuffer.ToArray());
                Console.WriteLine("hex--->" + hexString);

                // ตรวจสอบว่าได้รับ End Frame หรือยัง (เช่น 1B 03)
                if (receivedBuffer.Count > 1 && receivedBuffer[receivedBuffer.Count - 2] == 0x1B && receivedBuffer[receivedBuffer.Count - 1] == 0x03)
                {
                    // แสดงผลที่ Console
                    Console.WriteLine("Received Text: " + hexString);

                    // แจ้ง event พร้อมส่งค่า
                    DataReceived?.Invoke(this, new DataReceivedEventArgs(hexString));
                    receivedBuffer.Clear();
                }
                if (receivedBuffer.Count >= 2 &&
                    receivedBuffer[receivedBuffer.Count - 2] == 0x1B &&
                    receivedBuffer[receivedBuffer.Count - 1] == 0x0F)
                {
                    Console.WriteLine("Received Count: " + hexString);
                    DataReceived?.Invoke(this, new DataReceivedEventArgs(hexString));
                    receivedBuffer.Clear();
                }
                stopwatch.Stop();
                var elapsed_time = stopwatch.Elapsed.TotalMilliseconds;
                Console.WriteLine("Time elapsed (ms): {0}", elapsed_time);
            }
        }

        public static string ByteArrayToString(byte[] byteArray)
        {
            return string.Join(" ", byteArray.Select(b => b.ToString("X2")));
        }

        public bool IsOpen()
        {
            return serialPort.IsOpen;
        }

        public void DiscardInBuffer()
        {
            if (serialPort.IsOpen)
            {
                serialPort.DiscardInBuffer();
            }
        }
    }
    public class DataReceivedEventArgs : EventArgs
    {
        public string Data { get; private set; }

        public DataReceivedEventArgs(string data)
        {
            Data = data;
        }
    }
}
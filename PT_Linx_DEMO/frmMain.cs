using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using PT_Linx_DEMO.Class;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace PT_Linx_DEMO
{
    public partial class frmMain : Form
    {
        private SerialPortManager _serialPortMonitor;
        private TcpClientManager _tcpClientManager;
        List<string> csvList = new List<string>();
        public int number_csv = 0;

        public frmMain()
        {
            InitializeComponent();
            _serialPortMonitor = SerialPortManager.Instance;
            _serialPortMonitor.DataReceived += SerialPortManager_DataReceived;

            _tcpClientManager = TcpClientManager.Instance;
            _tcpClientManager.DataReceived += Server_DataReceived;

            listBox1.MouseWheel += ListBox1_MouseWheel;
        }
        //private void Server_DataReceived(object sender, DataReceivedEventArgs_TCP e)
        //{
        //    // แสดงข้อมูลที่ได้รับใน TextBox
        //    Invoke(new Action(() =>
        //    {
        //        //txtReceived.Text += e.Data + Environment.NewLine;
        //        string dataIN = e.Data;
        //        // แยกค่า Hex ออกเป็นแต่ละตัว
        //        string[] hexValues = dataIN.Split(' ');

        //        // แปลงแต่ละค่าเป็นฐาน 10
        //        List<int> decimalValues = new List<int>();
        //        foreach (var hex in hexValues)
        //        {
        //            int decimalValue = Convert.ToInt32(hex, 16);
        //            decimalValues.Add(decimalValue);
        //        }

        //        if (string.Join(", ", decimalValues).Contains("27, 15"))
        //        {
        //            number_csv++;
        //            send_text(number_csv);
        //        }

        //        //List<List<int>> validValues = new List<List<int>>
        //        //{
        //        //     new List<int> { 27, 15 },
        //        //     new List<int> { 27, 6, 0, 0, 158, 27, 3 }
        //        //};
        //        //// ตรวจสอบว่าตรงกับค่าใดค่าหนึ่งหรือไม่
        //        //bool isValid = validValues.Any(validList => decimalValues.SequenceEqual(validList));

        //        //// ถ้าตรงกับค่าที่กำหนด ให้แสดงค่าจริง, ถ้าไม่ตรงให้แสดง "unknown"
        //        //string displayText = isValid ? string.Join(", ", decimalValues) : "unknown";

        //        //listBox1.Items.Add(displayText);

        //        //if (!isValid)
        //        //{
        //        //    MessageBox.Show("พัง");
        //        //}

        //        //listBox1.Items.Add(string.Join(", ", decimalValues) + " ------------------------>> " + DateTime.Now.ToString("HH:mm:ss.fff"));
        //        int maxItems = 50; // กำหนดจำนวนสูงสุด
        //        listBox1.Items.Add(string.Join(", ", decimalValues));
        //        if (listBox1.Items.Count > maxItems)
        //        {
        //            listBox1.Items.Clear();
        //        }
        //        listBox1.TopIndex = listBox1.Items.Count - 1;
        //    }));
        //}
        private async void Server_DataReceived(object sender, DataReceivedEventArgs_TCP e)
        {
            Invoke(new Action(async () =>
            {
                string dataIN = e.Data;
                string[] hexValues = dataIN.Split(' ');

                List<int> decimalValues = hexValues.Select(hex => Convert.ToInt32(hex, 16)).ToList();

                //if (decimalValues.Contains(27) && decimalValues.Contains(15))
                //{
                    number_csv++;

                    byte[] messageBytes = Encoding.UTF8.GetBytes(csvList[number_csv]);

                    List<byte> commandBytesList = new List<byte> { 0x1B, 0x02, 0x9E, 0x01 };
                    commandBytesList.AddRange(Encoding.UTF8.GetBytes("MMMMMM"));
                    commandBytesList.Add(0x00);
                    commandBytesList.AddRange(messageBytes);
                    commandBytesList.Add(0x00);
                    commandBytesList.Add(0x1B);
                    commandBytesList.Add(0x03);

                    byte[] commandBytes = commandBytesList.ToArray();

                    // แปลง commandBytes เป็น hex string ก่อนส่ง (ถ้าต้องการแสดง)
                    string hexToSend = ByteArrayToHexString(commandBytes);

                    if (_serialPortMonitor.IsOpen() && comboBox1.Text.Length <= 2)
                    {
                        await _serialPortMonitor.SendCommandAsync(commandBytes);
                    }
                    else if (_tcpClientManager.IsConnected())
                    {
                        await _tcpClientManager.SendCommandAsync(commandBytes);
                    }
                    else
                    {
                        MessageBox.Show("No connection available to send the command.");
                    }

                    txtPrintText.Text = csvList[number_csv];
                    txtRow.Text = number_csv.ToString();

                    SaveToDataLog(csvList[number_csv - 1]);

                Console.WriteLine("hexToSend--->>"+hexToSend);


                listBox1.Items.Add("Sent (Hex): " + hexToSend);
                //}

                int maxItems = 100; // กำหนดจำนวนสูงสุด
                listBox1.Items.Add(string.Join(", ", decimalValues));
                if (listBox1.Items.Count > maxItems)
                {
                    listBox1.Items.Clear();  // ลบรายการแรกสุด
                }
                listBox1.TopIndex = listBox1.Items.Count - 1;
            }));
        }

        // ฟังก์ชันแปลง byte[] เป็น hex string (ตัวใหญ่, คั่นด้วย space)
        private string ByteArrayToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", " ");
        }

        private void SerialPortManager_DataReceived(object sender, DataReceivedEventArgs e)
        {
            this.Invoke(new MethodInvoker(() =>
            {
                string dataIN = e.Data;

                // แยกค่า Hex ออกเป็นแต่ละตัว
                string[] hexValues = dataIN.Split(' ');

                // แปลงแต่ละค่าเป็นฐาน 10
                List<int> decimalValues = new List<int>();
                foreach (var hex in hexValues)
                {
                    if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int decimalValue))
                    {
                        decimalValues.Add(decimalValue);
                    }
                }

                if(string.Join(", ", decimalValues).Contains("27, 15"))
                {
                    number_csv++;
                    send_text(number_csv);

                    // ✨ บันทึกข้อมูลลง DataLog
                    SaveToDataLog(csvList[number_csv - 1]);

                }
                int maxItems = 100; // กำหนดจำนวนสูงสุด
                listBox1.Items.Add(string.Join(", ", decimalValues));
                if (listBox1.Items.Count > maxItems)
                {
                    listBox1.Items.Clear();  // ลบรายการแรกสุด
                }
                listBox1.TopIndex = listBox1.Items.Count - 1;
            }));
        }


        private void btnStart_Click(object sender, EventArgs e)
        {
            bool serialOpen = _serialPortMonitor.IsOpen();
            bool tcpConnected = _tcpClientManager.IsConnected();

            if (serialOpen && comboBox1.Text.Length <= 2|| tcpConnected) // อย่างน้อย 1 อย่างต้องเชื่อมต่ออยู่
            {
                if (csvList.Count >  0 ) 
                {
                    send_text(number_csv);
                    command_start_print();

                    btnStart.Enabled = false;
                    btnStop.Enabled = true;
                    txtRow.Enabled = false;
                    txtLenght.Enabled = false;
                }
                else
                {
                    MessageBox.Show("Please Upload File Data.");
                }
            }
            else
            {
                MessageBox.Show("Neither Serial Port nor TCP Client is connected.");
            }
        }

        private async Task send_text(int count)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(csvList[count]);

            List<byte> commandBytesList = new List<byte> { 0x1B, 0x02, 0x9E, 0x01 };
            commandBytesList.AddRange(Encoding.UTF8.GetBytes("MMMMMM"));
            commandBytesList.Add(0x00);
            commandBytesList.AddRange(messageBytes);
            commandBytesList.Add(0x00);
            commandBytesList.Add(0x1B);
            commandBytesList.Add(0x03);

            byte[] commandBytes = commandBytesList.ToArray();

            //listBox1.Items.Add("Send Command ------------------------>>" + DateTime.Now.ToString("HH:mm:ss.fff"));

            if (_serialPortMonitor.IsOpen() && comboBox1.Text.Length <= 2)
            {
                await _serialPortMonitor.SendCommandAsync(commandBytes);
            }
            else if (_tcpClientManager.IsConnected())
            {
                await _tcpClientManager.SendCommandAsync(commandBytes);
            }
            else
            {
                MessageBox.Show("No connection available to send the command.");
            }
            txtPrintText.Text = csvList[count];
            txtRow.Text = count.ToString();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            _serialPortMonitor.CloseSerialPort();
            string selectedText = comboBox1.SelectedItem?.ToString();

            _serialPortMonitor.ConfigureSerialPort("COM"+selectedText, "9600", "8", "One", "None");
            _serialPortMonitor.OpenSerialPort();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();

            if (ports.Length > 0)
            {
                string portNumber = Regex.Match(ports[0], @"\d+").Value;
                comboBox1.SelectedItem = portNumber;
            }

            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        private async void btnUpload_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt;*.csv)|*.txt;*.csv|All Files (*.*)|*.*",
                Title = "เลือกไฟล์"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;

                try
                {
                    // แสดงสถานะ Loading
                    btnUpload.Enabled = false;
                    btnStart.Enabled = false;
                    btnUpload.Text = "Loading...";

                    // อ่านไฟล์แบบทีละบรรทัด (Asynchronous)
                    csvList.Clear();
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string line;
                        bool isFirstLine = true;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            csvList.Add(line);

                            // ถ้าเป็นบรรทัดแรก ให้นำไปใส่ใน txtData
                            if (isFirstLine)
                            {
                                txtPrintText.Text = line;
                                isFirstLine = false;
                            }
                        }
                    }

                    // อัปเดต UI หลังจากโหลดเสร็จ
                    btnUpload.Text = "Open File";
                    btnUpload.Enabled = false;

                    //MessageBox.Show($"โหลดข้อมูลสำเร็จ: {csvList.Count} รายการ", "สำเร็จ");

                    // แสดงข้อมูลใน Console
                    foreach (string guid in csvList)
                    {
                        Console.WriteLine(guid);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btnUpload.Text = "Open File";
                    btnUpload.Enabled = true;
                    btnStart.Enabled = true;
                }
            }
        }

        private async void btnStop_Click(object sender, EventArgs e)
        {
            byte[] commandBytes = { 0x1B, 0x02, 0x12, 0x1B, 0x03 }; // command start jet

            if (_serialPortMonitor.IsOpen() && comboBox1.Text.Length <= 2)
            {
                await _serialPortMonitor.SendCommandAsync(commandBytes);
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                txtRow.Enabled = false;
                txtLenght.Enabled = false;
            }
            else if (_tcpClientManager.IsConnected())
            {
                await _tcpClientManager.SendCommandAsync(commandBytes);
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                txtRow.Enabled = false;
                txtLenght.Enabled = false;
            }
            else
            {
                MessageBox.Show("No connection available to send the command.");
            }

            // ✅ หน่วงเวลา 3 วินาที
            await Task.Delay(1000);

            // ✅ ล้างข้อมูล ListBox หลังจาก 3 วินาที
            listBox1.Items.Clear();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            string ipAddress = "192.168.167.6"; // ปรับเป็น IP ของอุปกรณ์ที่ต้องการเชื่อมต่อ
            int port = 29043;                  // ปรับเป็น Port ที่ใช้งานจริง
            _tcpClientManager.ConnectAsync(ipAddress, port); // เริ่ม TCP Server
            comboBox1.Text = "192.168.167.6";
        }

        private void ListBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            ((HandledMouseEventArgs)e).Handled = true;
        }

        public async void command_start_print()
        {
            byte[] commandBytes = { 0x1B, 0x02, 0x11, 0x1B, 0x03 }; // command start jet

            if (_serialPortMonitor.IsOpen() && comboBox1.Text.Length <= 2)
            {
                await _serialPortMonitor.SendCommandAsync(commandBytes);
                btnStart.Enabled = false;
                btnStop.Enabled = true;
            }
            else if (_tcpClientManager.IsConnected())
            {
                await _tcpClientManager.SendCommandAsync(commandBytes);
                btnStart.Enabled = false;
                btnStop.Enabled = true;
            }
            else
            {
                MessageBox.Show("No connection available to send the command.");
            }
        }

        // ฟังก์ชันบันทึกลงไฟล์ DataLog.txt
        private void SaveToDataLog(string csvData)
        {
            string logFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MyAppLogs");
            Directory.CreateDirectory(logFolderPath);

            string logFilePath = Path.Combine(logFolderPath, "DataLog.txt");
            string timestamp = DateTime.Now.ToString("dd/MM/yyyy h:mm:ss tt");
            string logEntry = $"{timestamp}, Start, {csvData}";

            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true, Encoding.UTF8))
                {
                    writer.WriteLine(logEntry);
                }
            }
            catch (IOException)
            {
                MessageBox.Show("ไม่สามารถบันทึก Log ได้: ไฟล์ถูกใช้งานโดยโปรแกรมอื่น");
            }
        }


        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _serialPortMonitor?.CloseSerialPort();
            _tcpClientManager?.Disconnect();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            string command = textBox1.Text.Trim();

            // ฟังก์ชันช่วยแปลง hex string เป็น byte array
            byte[] commandBytes;
            try
            {
                commandBytes = HexStringToByteArray(command);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid hex string format: " + ex.Message);
                return;
            }

            if (_serialPortMonitor.IsOpen())
            {
                await _serialPortMonitor.SendCommandAsync(commandBytes);
            }
            else if (_tcpClientManager.IsConnected())
            {
                await _tcpClientManager.SendCommandAsync(commandBytes);
            }
            else
            {
                MessageBox.Show("No connection available to send the command.");
            }
        }

        private byte[] HexStringToByteArray(string hex)
        {
            hex = hex.Replace(" ", ""); // ลบช่องว่างออก
            if (hex.Length % 2 != 0)
                throw new FormatException("Hex string must have an even length");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}

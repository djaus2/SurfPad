using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using System.Collections.ObjectModel;
using Windows.UI.Popups;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Diagnostics;
using Windows.UI.Core;

namespace SurfPadIoT.Pages
{
    /// <summary>
    /// The Bluetooth Serial page for the app
    /// </summary>
    public sealed partial class USBSerialTerminalPage : Page
    {
        const int PauseBtwSentCharsmS = 1000;
        public const string EOStringStr = "~";
        public const char EOStringChar = '~';
        public const byte EOStringByte = 126;
        private const int cFineStructure = 137; //ASCII per mile sign
        private const string ARDUINO_DBGMSG = "VMDPV_1|1_VMDPV\r\n";
        string Title = "USB Serial Terminal REMOTE - UWP";
        private SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;

        ObservableCollection<DeviceInformation> listofDevices;
        private CancellationTokenSource ReadCancellationTokenSource;

        enum Mode
        {
            Disconnected,
            JustConnected,
            Connected,
            ACK0,
            ACK2,
            ACK4,
            AwaitJson,
            JsonConfig,
            Config,
            Running,
            Ready,
            Json1,
            Json2
        }
        Mode _Mode = Mode.Disconnected;

        public USBSerialTerminalPage()
        {
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            this.InitializeComponent();
            MyTitle.Text = Title;
            uartTitle.Text = Title;
            listofDevices = new ObservableCollection<DeviceInformation>();
            ListAvailablePorts(); 
            MainPage.USBSerialTerminalPage = this;
            _Mode = Mode.Disconnected;
        }

        //// Remove later of reasign.
        //private TextBlock status = new TextBlock();
        private Button comPortInput = new Button();
        private Button refresh = new Button();
        private Button closeDevice = new Button();
        private Button sendTextButton = new Button();
        //private TextBlock sendText = new TextBlock();

        /// <summary>
        /// ListAvailablePorts
        /// - Use SerialDevice.GetDeviceSelector to enumerate all serial devices
        /// - Attaches the DeviceInformation to the ListBox source so that DeviceIds are displayed
        /// </summary>
        private async void ListAvailablePorts()
        {

            bool done = false;
            try
            {
                string aqs = SerialDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(aqs);

                var numDevices = dis.Count;
                status.Text = "Select a device and connect";


                listofDevices.Clear();

                if (numDevices != 0)
                {
                    for (int i = 0; i < numDevices; i++)
                    {
                        if ((dis[i].Name.ToUpper().Contains("USB")) || (dis[i].Id.ToUpper().Contains("FTDI")) || (dis[i].Name.ToLower().Contains("minwinpc")))
                            this.listofDevices.Add(dis[i]);
                    }

                    DeviceListSource.Source = this.listofDevices;
                    comPortInput.IsEnabled = true;
                    ConnectDevices.SelectedIndex = -1;
  
                }
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }

            if (done)
            {

            }

        }

        /// <summary>
        /// comPortInput_Click: Action to take when 'Connect' button is clicked
        /// - Get the selected device index and use Id to create the SerialDevice object
        /// - Configure default settings for the serial port
        /// - Create the ReadCancellationTokenSource token
        /// - Start listening on the serial port input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void comPortInput_Click(object sender, RoutedEventArgs e)
        {
            var selection = ConnectDevices.SelectedItems;

            if (selection.Count <= 0)
            {
                if (ConnectDevices.Items.Count == 1)
                    ConnectDevices.SelectedIndex = 0;
                else
                {
                    status.Text = "Select a device and connect";
                    return;
                }
            }

            DeviceInformation entry = (DeviceInformation)selection[0];
            bool ret = await ConnectSerial(entry);
            //if (ret) backButton_Click(null, null);
        }

        public async Task<bool> ConnectSerial(DeviceInformation entry)
        {
            bool ret = false;
            try
            {
                serialPort = await SerialDevice.FromIdAsync(entry.Id);

                if (serialPort == null)
                {
                    status.Text = entry.Name + " (USB) Serial Port failed to open.";
                    return false;
                }
                // Disable the 'Connect' button 
                comPortInput.IsEnabled = false;

                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.BaudRate = MainPage.cBAUD;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;

                // Display configured settings
                status.Text = "Serial port configured successfully: ";
                status.Text += serialPort.BaudRate + "-";
                status.Text += serialPort.DataBits + "-";
                status.Text += serialPort.Parity.ToString() + "-";
                status.Text += serialPort.StopBits;

                // Set the RcvdText field to invoke the TextChanged callback
                // The callback launches an async Read task to wait for data
                recvdText.Text = "Waiting for data...";

                refresh.IsEnabled = false;
                closeDevice.IsEnabled = true;

                // Create cancellation token object to close I/O operations when closing the device
                ReadCancellationTokenSource = new CancellationTokenSource();
                _Mode = Mode.JustConnected;
                // Enable 'WRITE' button to allow sending data
                sendTextButton.IsEnabled = true;
                //lcdMsg += "~" + ArduinoLCDDisplay.LCD.CMD_DISPLAY_LINE_2_CH + "PressBack/Select   ";
                //Send(lcdMsg);


                ///////////////////////
                ret = true;
                if (ret)
                {
                    _Mode = Mode.JustConnected;
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        status.Text = "USB Serial Connected: Press [Start Recv]";
                        this.buttonDisconnect.IsEnabled = true;
                        this.buttonSend.IsEnabled = true;
                        this.buttonStartRecv.IsEnabled = true;
                        this.buttonStopRecv.IsEnabled = false;
                        //SendCh('0');
                        //this.buttonStartRecv.IsEnabled = false;
                        //this.buttonStopRecv.IsEnabled = true;
                        DeviceInformation di = (DeviceInformation)ConnectDevices.SelectedItem;
                        this.TxtBlock_SelectedID.Text = di.Id;
                        this.textBlockBTName.Text = di.Name;
                        
                    });
                    ///////////////////////
                }
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
                comPortInput.IsEnabled = true;
                sendTextButton.IsEnabled = false;
            }
            return ret;
        }

        public async void Send(string msg) //2
        {
            try
            {
                if (serialPort != null)
                {
                    // Create the DataWriter object and attach to OutputStream
                    dataWriteObject = new DataWriter(serialPort.OutputStream);

                    //Launch the WriteAsync task to perform the write
                    await WriteAsync(msg);
                }
                else
                {
                    status.Text = "Select a device and connect";
                }
            }
            catch (Exception ex)
            {
                status.Text = "Send(): " + ex.Message;
            }
            finally
            {
                // Cleanup once complete
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }

        public async void SendCh(char  ch) 
        {
            string msg = "" + ch;
            try
            {
                if (serialPort != null)
                {
                    //Launch the WriteAsync task to perform the write
                    await WriteAsync(msg);
                }
                else
                {
                    status.Text = "Select a device and connect";
                }
            }
            catch (Exception ex)
            {
                status.Text = "Send(): " + ex.Message;
            }
            finally
            {
                // Cleanup once complete
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }

        /// <summary>
        /// sendTextButton_Click: Action to take when 'WRITE' button is clicked
        /// - Create a DataWriter object with the OutputStream of the SerialDevice
        /// - Create an async task that performs the write operation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void sendTextButton_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                if (serialPort != null)
                {
                    // Create the DataWriter object and attach to OutputStream
                    dataWriteObject = new DataWriter(serialPort.OutputStream);

                    //Launch the WriteAsync task to perform the write
                    await WriteAsync("");
                }
                else
                {
                    status.Text = "Select a device and connect";
                }
            }
            catch (Exception ex)
            {
                status.Text = "sendTextButton_Click: " + ex.Message;
            }
            finally
            {
                // Cleanup once complete
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }


        async private void ConnectDevice_Click(object sender, RoutedEventArgs e)
        {
            var selection = ConnectDevices.SelectedItems;

            if (selection.Count <= 0)
            {
                if (ConnectDevices.Items.Count == 1)
                    ConnectDevices.SelectedIndex = 0;
                else
                {
                    status.Text = "Select a device and connect";
                    return;
                }
            }

            DeviceInformation entry = (DeviceInformation)selection[0];
            bool res = await ConnectSerial(entry);

        }
        
        private void ConnectDevices_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {

            ConnectDevice_Click(sender, e);
        }
        
        Windows.Storage.Streams.Buffer OutBuff;
        
        private void button_Click(object sender, RoutedEventArgs e)
        {
            OutBuff = new Windows.Storage.Streams.Buffer(100);
            Button button = (Button)sender;
            if (button != null)
            {
                switch ((string)button.Content)
                {
                    case "Disconnect":
                        CloseDevice();
                        this.textBlockBTName.Text = "";
                        this.TxtBlock_SelectedID.Text = "";
                        this.buttonDisconnect.IsEnabled = false;
                        this.buttonSend.IsEnabled = false;
                        this.buttonStartRecv.IsEnabled = false;
                        this.buttonStopRecv.IsEnabled = false;
                        break;
                    case "Send":
 
                        if (sendChar.Text.Length > 0)
                        {
                            char ch = sendChar.Text[0];
                            SendCh(ch);
                            sendChar.Text = "";
                        }
                        else
                        {
                            Send(this.sendText.Text);
                            this.sendText.Text = "";
                        }
                        break;
                    case "Clear Send":
                        this.status.Text = "";
                        this.SendText.Text = "";
                        this.recvdText.Text = "";
                        break;
                    case "Start Recv":
                        this.buttonStartRecv.IsEnabled = false;
                        this.buttonStopRecv.IsEnabled = true;
                        Listen();
                        break;
                    case "Stop Recv":
                        this.buttonStartRecv.IsEnabled = false;
                        this.buttonStopRecv.IsEnabled = false;
                        CancelReadTask();
                        break;
                    case "Refresh":
                        ListAvailablePorts();
                        break;
                    case "Back":
                        //this.Frame.GoBack();
                        backButton_Click(null, null);
                        break;
                }
            }
        }

        private  void backButton_Click(object sender, RoutedEventArgs e)
        {
            if (_Mode == Mode.JustConnected)
            {
                _Mode = Mode.Running;
                //Send("ACK1#");
            }
            this.Frame.GoBack();;

        }





        /// <summary>
        /// WriteAsync: Task that asynchronously writes data from the input text box 'sendText' to the OutputStream 
        /// </summary>
        /// <returns></returns>
        private async Task WriteAsync(string msg)
        {
            Task<UInt32> storeAsyncTask;

            if (msg == "")
                msg = "none";// sendText.Text;
            if (msg.Length != 0)
            //if (msg.sendText.Text.Length != 0)
            {
                // Create the DataWriter object and attach to OutputStream
                dataWriteObject = new DataWriter(serialPort.OutputStream);

                // Load the text from the sendText input text box to the dataWriter object
                dataWriteObject.WriteString(msg);
                //dataWriteObject.WriteString(sendText.Text);

                // Launch an async task to complete the write operation
                storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

                UInt32 bytesWritten = await storeAsyncTask;
                if (bytesWritten > 0)
                {
                    status.Text = msg + ", ";
                    //status.Text = sendText.Text + ", ";
                    status.Text += "bytes written successfully!";
                }
                sendText.Text = ""; //??
            }
            else
            {
                status.Text = "Enter the text you want to write and then click on 'WRITE'";
            }
        }

        private async Task WriteChar(char ch)
        {
            string msg = "" + ch;
            await WriteAsync(msg);
        }
        
        string recvdtxt = "";
        bool listening = false;
        /// <summary>
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Listen()
        {

            ReadCancellationTokenSource = new CancellationTokenSource();
            try
            {
                if (serialPort != null)
                {
    

                    //Once the first trasmission, form this end, is received, this end IS connected.
                    SendCh((char)137);
                    _Mode = Mode.Connected;

                    dataReaderObject = new DataReader(serialPort.InputStream);
                    uint i = dataReaderObject.UnconsumedBufferLength;
                    if (i != 0)
                    {
                        byte[] bytes = new byte[i];
                        dataReaderObject.ReadBytes(bytes);
                    }
                    this.buttonStopRecv.IsEnabled = true;
                    this.buttonDisconnect.IsEnabled = false;
                    recvdtxt = "";
                    // keep reading the serial input
                    listening = true;
                    while (listening)
                    {
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {

                    this.buttonStopRecv.IsEnabled = false;
                    this.buttonStartRecv.IsEnabled = false;
                    this.buttonSend.IsEnabled = false;
                    this.buttonDisconnect.IsEnabled = false;
                    this.textBlockBTName.Text = "";
                    this.TxtBlock_SelectedID.Text = "";
                if (ex.GetType().Name == "TaskCanceledException")
                {
                    status.Text = "Reading task was cancelled, closing device and cleaning up";
                    CloseDevice();
                }
                else
                {
                    status.Text = "Listen: " + ex.Message;
                }
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }

            
        }

       

        /// <summary>
        /// ReadAsync: Task that waits on data and reads asynchronously from the serial device InputStream
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1024;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            // Create a task object to wait for data on the serialPort.InputStream
            loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

            // Launch the task and wait
            UInt32 bytesRead = await loadAsyncTask;
            if (bytesRead > 0)
            {
                try
                {
                    byte[] bytes  = new byte[bytesRead];
                    dataReaderObject.ReadBytes(bytes);
                    string currenbtRecvdText = Encoding.UTF8.GetString(bytes);
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => recvdText.Text = "" + ((char)bytes[0]));

                    recvdText.Text = currenbtRecvdText;

                    if (_Mode == Mode.Connected)
                    {
                        if ('0' == (char)bytes[0])
                        {
                            _Mode = Mode.ACK0;
                            recvdtxt = "";
                            SendCh('1');
                        }
                    }
                    else if (_Mode == Mode.ACK0)
                    {
                        if ('2' == (char)bytes[0])
                        {
                            _Mode = Mode.ACK2;
                            recvdtxt = "";
                            SendCh('3');
                        }
                    }
                    else if (_Mode == Mode.ACK2)
                    {
                        if ('4' == (char)bytes[0])
                        {
                            _Mode = Mode.ACK4;
                            SendCh('5');
                            //status.Text="Ready for Config. Press [Back] then on MainPage press [Load App Menu]";
                        }
                    }
                    else if (_Mode == Mode.ACK4)
                    {
                        if ('!' == (char)bytes[0])
                        {
                            _Mode = Mode.Ready;
                            SendCh('/');
                            //status.Text="Ready for Config. Press [Back] then on MainPage press [Load App Menu]";
                        }
                    }
                    else if (_Mode == Mode.Ready)
                    {
                        if ('/' == (char)bytes[0])
                        {
                            _Mode = Mode.Json1;
                            //Send Config
                            await WriteAsync(
"{\"Config\":[ [ { \"iWidth\": 120 },{ \"iHeight\": 100 },{ \"iSpace\": 5 },{ \"iCornerRadius\": 10 },{ \"iRows\": 2 },{ \"iColumns\": 5 },{ \"sComPortId\": \"\\\\\\\\?\\\\USB#VID_26BA&PID_0003#5543830353935161A112#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"sFTDIComPortId\": \"\\\\\\\\?\\\\FTDIBUS#VID_0403+PID_6001+FTG71BUIA#0000#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"iComportConnectDeviceNo\": -1 },{ \"iFTDIComportConnectDeviceNo\": 1 },{ \"sUseSerial\": \"BT\" } ] ] }~");
                        }
                    }
                    else if (_Mode == Mode.Json1)
                    {
                        if ('~' == (char)bytes[0])
                        {
                            _Mode = Mode.Json2;
                            //Send Menu
                            await WriteAsync(
"{\"MainMenu\":[ [ \"Something else\", \"Unload\", \"Show full list\", \"Setup Sockets\", \"The quick brown fox jumps over the lazy dog\" ],[ \"First\", \"Back\", \"Next\", \"Last\", \"Show All\" ] ] }~");
                            _Mode = Mode.Running;
                        }
                    }
                    else if (_Mode == Mode.Running)
                    {

                        if (listening)
                            switch ((char)bytes[0])
                            {
                                case '^':
                                    listening = false;
                                    break;
                                default:
                                    //Do app stuff here. For now just echo chars sent
                                    SendCh((char)bytes[0]);
                                    break;
                            }
                    }

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ReadAsync: " + ex.Message);
                }
                
            }
        }

        /// <summary>
        /// CancelReadTask:
        /// - Uses the ReadCancellationTokenSource to cancel read operations
        /// </summary>
        private void CancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }
        }

        /// <summary>
        /// CloseDevice:
        /// - Disposes SerialDevice object
        /// - Clears the enumerated device Id list
        /// </summary>
        private void CloseDevice()
        {
            if (serialPort != null)
            {
                serialPort.Dispose();
            }
            serialPort = null;

            _Mode = Mode.Disconnected;

        }


    }
}

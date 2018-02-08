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
using roundedbox;
using System.Text;
using System.Diagnostics;
using Windows.UI.Core;

namespace USBSerial
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
        string Title = "USB Serial Universal Windows App";
        private SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;

        ObservableCollection<DeviceInformation> listofDevices;
        private CancellationTokenSource ReadCancellationTokenSource;

        enum Mode
        {
            Disconnected,
            JustConnected,
            ACK0,
            ACK2,
            ACK4,
            Connected,
            AwaitJson,
            JsonConfig
        }
        Mode _Mode = Mode.Disconnected;

        public USBSerialTerminalPage()
        {
            this.NavigationCacheMode =
                    Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;
            this.InitializeComponent();
            MyTitle.Text = Title;         
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
                //if (dis.Any())
                //{
                //    var deviceId = dis[1].Id;
                //    var device = await SerialDevice.FromIdAsync(deviceId);

                //    if (device != null)
                //    {
                //        device.BaudRate = 57600;
                //        device.StopBits = SerialStopBitCount.One;
                //        device.DataBits = 8;
                //        device.Parity = SerialParity.None;
                //        device.Handshake = SerialHandshake.None;

                //        var reader = new DataReader(device.InputStream);
                //    }
                //}

                listofDevices.Clear();

                if (numDevices != 0)
                {
                    for (int i = 0; i < numDevices; i++)
                    {
                        if (dis[i].Name.Contains("USB"))
                            this.listofDevices.Add(dis[i]);
                    }

                    DeviceListSource.Source = this.listofDevices;
                    comPortInput.IsEnabled = true;
                    ConnectDevices.SelectedIndex = -1;
                    if (Commands.CheckComportIdSettingExists())
                    {
                        for (int i = 0; i < ConnectDevices.Items.Count; i++)
                        {
                            DeviceInformation di = (DeviceInformation)ConnectDevices.Items[i];
                            if (di.Id == Commands.ElementConfigStr[Commands.cComPortIdKey])
                            {
                                ConnectDevices.SelectedIndex = i;
                                comPortInput_Click(di, null);
                                done = true;
                            }
                        }
                    }
                    if(!done)
                    if (Commands.ElementConfigInt.ContainsKey(Commands.cComportConnectDeviceNoKey))
                    {
                        if (ConnectDevices.Items.Count > Commands.ElementConfigInt[Commands.cComportConnectDeviceNoKey])
                        {
                            int index = Commands.ElementConfigInt[Commands.cComportConnectDeviceNoKey];
                            if (index >= 0)
                            {
                                //If only one item then connect to it.
                                ConnectDevices.SelectedIndex = index;
                                DeviceInformation di = (DeviceInformation)ConnectDevices.SelectedItem;
                                if (di.Id == Commands.ElementConfigStr[Commands.cComPortIdKey])
                                {
                                    comPortInput_Click(di, null);
                                    done = true;
                                }
                            }
                            //Doesn't return to here
                        }
                    }
                    if (!done)
                        if (Commands.ElementConfigInt.ContainsKey(Commands.cFTDIComportConnectDeviceNoKey))
                        {
                            if (ConnectDevices.Items.Count > Commands.ElementConfigInt[Commands.cFTDIComportConnectDeviceNoKey])
                            {
                                int index = Commands.ElementConfigInt[Commands.cFTDIComportConnectDeviceNoKey];
                                if (index >= 0)
                                {
                                    //If only one item then connect to it.
                                    ConnectDevices.SelectedIndex = index;
                                    //var FTDIIdList = from n in Commands.ElementConfigStr where n.Key == Commands.cFTDIComPortIdKey select n;
                                    var FTDIIdList = Commands.ElementConfigStr.ElementAt(1);

                                    DeviceInformation di = (DeviceInformation)ConnectDevices.SelectedItem;
                                    //if (di.Id == Commands.ElementConfigStr[Commands.cFTDIComPortIdKey]) //This fails
                                    if (di.Id == FTDIIdList.Value)
                                    {
                                        comPortInput_Click(di, null);
                                        done = true;
                                    }

                                }
                                //Doesn't return to here
                            }
                        }
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
                _Mode = Mode.Connected;
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
                    while (true)
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
                    var fs = from n in bytes where n == 137 select n;
                    if (fs.Count() > 0)
                        bytes = fs.ToArray<byte>();
                    else
                    {
                        //VMDPV_1|1_VMDPV
                        string currenbtRecvdText1 = Encoding.UTF8.GetString(bytes);
                        //Need to remove the Arduino breakpoint looping messages
                        int ARDUINO_dbgMsg_Len = ARDUINO_DBGMSG.Length;
                        if (bytesRead >= ARDUINO_dbgMsg_Len)
                        {
                            if (currenbtRecvdText1.Substring(0, ARDUINO_dbgMsg_Len) == ARDUINO_DBGMSG)
                            {
                                currenbtRecvdText1 = currenbtRecvdText1.Substring(ARDUINO_dbgMsg_Len);
                                bytes = bytes.Skip(ARDUINO_dbgMsg_Len).Take((int)bytesRead - ARDUINO_dbgMsg_Len).ToArray();
                            }
                        }
                    }
                    string currenbtRecvdText = Encoding.UTF8.GetString(bytes);


                    recvdText.Text = currenbtRecvdText;

                    if (_Mode == Mode.JustConnected)
                    {
                        if (cFineStructure == bytes[0])
                        {
                            SendCh('0');
                            _Mode=Mode.ACK0;
                        }
                    }
                    else if (_Mode == Mode.ACK0)
                    {
                        if ('1' == (char)bytes[0])
                        {
                            recvdtxt = "";
                            SendCh('2');
                            _Mode = Mode.ACK2;
                        }
                    }
                    else if (_Mode == Mode.ACK2)
                    {
                        if ('3' == (char)bytes[0])
                        {
                            recvdtxt = "";
                            SendCh('4');
                            _Mode = Mode.ACK4;
                        }
                    }
                    else if (_Mode == Mode.ACK4)
                    {
                        if ('5' == (char)bytes[0])
                        {
                            status.Text="Ready for Config. Press [Back] then on MainPage press [Load App Menu]";
                            _Mode = Mode.Connected;

                        }
                    }
                    else if (_Mode == Mode.Connected)
                    {
                        byte byt = bytes[0];
                        switch (byt)
                        {
                            case 47:  //'!'
                                _Mode = Mode.JsonConfig;
                                recvdtxt = "";
                                SendCh('/');
                                break;
                            default:
                                recvdtxt = "" + (char)bytes[0];
                                await MainPage.MP.UpdateTextAsync(recvdtxt);//.Substring(0,recvdtxt.Length - 1));
                                recvdtxt = "";
                                System.Diagnostics.Debug.WriteLine("bytes read successfully!");
                                break;
                        }
                    }
                    else if (_Mode == Mode.JsonConfig)
                    {
                        recvdtxt += currenbtRecvdText;
                        if (recvdtxt.Substring(recvdtxt.Length - 1) == EOStringStr)
                        {
                            System.Diagnostics.Debug.WriteLine("Recvd: " + recvdtxt);
                            await MainPage.MP.UpdateTextAsync(recvdtxt);//.Substring(0,recvdtxt.Length - 1))

                            if (recvdtxt.Substring(0, "{\"Config\":".Length) == "{\"Config\":")
                                SendCh('~');
                            else if (recvdtxt.Substring(0, "{\"MainMenu\":".Length) == "{\"MainMenu\":")
                                _Mode = Mode.Connected;
                            else
                            {
                                //// Get stack trace for the exception with source file information
                                //var st = new StackTrace(ex, true);
                                //string thisFile = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
                                //var frame = st.GetFrame(0);
                                //// Get the line number from the stack frame
                                //var line = frame.GetFileLineNumber();
                                throw new System.Exception("BluetoothSerialTerminal.cs: ReadAsync() Getting JsonConfig. Shouldn't have reached this LOC"); //: { 0 }",line);

                            }
                            recvdtxt = "";
                        }
                        else
                            return;
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

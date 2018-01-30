// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using System.Threading;
using System.Threading.Tasks;
using roundedbox;
using System.Linq;

namespace Serial
{
    public sealed partial class SerialTerminalPage : Page
    {
        /// <summary>
        /// Private variables
        /// </summary>
		string Title = "Generic Bluetooth Serial Universal Windows App";
        private SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;

        private ObservableCollection<DeviceInformation> listOfDevices;
        private CancellationTokenSource ReadCancellationTokenSource;

        enum Mode
        {
            Disconnected,
            JustConnected,
            Connected,
            AwaitJson
        }
        Mode _Mode= Mode.Disconnected;

        public SerialTerminalPage()
        {


            this.NavigationCacheMode =
                Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;
            this.InitializeComponent();
			MyTitle.Text = Title;
            comPortInput.IsEnabled = false;
            sendTextButton.IsEnabled = false;
            listOfDevices = new ObservableCollection<DeviceInformation>();
            MainPage.SerialTerminalPage = this;
            _Mode = Mode.Disconnected;
        }

        

        /// <summary>
        /// ListAvailablePorts
        /// - Use SerialDevice.GetDeviceSelector to enumerate all serial devices
        /// - Attaches the DeviceInformation to the ListBox source so that DeviceIds are displayed
        /// </summary>
        private async void ListAvailablePorts()
        {
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


                listOfDevices.Clear();

                if (numDevices != 0)
                {
                    for (int i = 0; i < numDevices; i++)
                    {
                        listOfDevices.Add(dis[i]);
                    }

                    DeviceListSource.Source = listOfDevices;
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
                                return;
                            }
                        }
                    }

                    bool done = false;
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
            await ConnectSerial(entry);
        }
		
	    private void backButton_Click(object sender, RoutedEventArgs e)
        {
            if (_Mode == Mode.JustConnected)
            {
                _Mode = Mode.Connected;
                Send("ACK1");
            }

                this.Frame.GoBack();
            //this.Frame.Navigate(typeof(MainPage),null);

        }

        public async Task ConnectSerial(DeviceInformation entry)
        {

            try
            {
                serialPort = await SerialDevice.FromIdAsync(entry.Id);

                // Disable the 'Connect' button 
                comPortInput.IsEnabled = false;

                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.BaudRate = 115200;
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
                string lcdMsg = "~C" + "Serial Connected";
                //lcdMsg += "~" + ArduinoLCDDisplay.LCD.CMD_DISPLAY_LINE_2_CH + "PressBack/Select   ";
                //Send(lcdMsg);
                status.Text = "Serial Connected: Press [Back] or (Select)";

                Listen();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
                comPortInput.IsEnabled = true;
                sendTextButton.IsEnabled = false;
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

        //If sending key's code only
        public  void Send(int i) //1
        {
             Send(string.Format("~{0}",i));
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


        /// <summary>
        /// WriteAsync: Task that asynchronously writes data from the input text box 'sendText' to the OutputStream 
        /// </summary>
        /// <returns></returns>
        private async Task WriteAsync(string msg)
        {
            Task<UInt32> storeAsyncTask;

            if (msg == "")
                msg = sendText.Text; //??
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

        /// <summary>
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Listen()
        {
            try
            {
                if (serialPort != null)
                {
                    dataReaderObject = new DataReader(serialPort.InputStream);

                    // keep reading the serial input
                    while (true)
                    {
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == "TaskCanceledException")
                {
                    status.Text = "Reading task was cancelled, closing device and cleaning up";
                    CloseDevice();
                }
                else
                {
                    status.Text = ex.Message;
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
                    string recvdtxt = dataReaderObject.ReadString(bytesRead);
                    System.Diagnostics.Debug.WriteLine(recvdtxt);
                    this.recvdText.Text += recvdtxt;
                    if (_Mode == Mode.JustConnected)
                    {
                        if (recvdtxt.ToUpper() == "READY")
                        {
                            _Mode = Mode.AwaitJson;
                            recvdText.Text = "";
                            Send("ACK2");
                        }
                    }
                    else if (_Mode == Mode.AwaitJson)
                    {
                        if (recvdtxt.ToUpper().Substring(0, "JSON".Length)== "JSON")
                        {
                            recvdtxt = recvdtxt.Substring("JSON".Length);
                            MainPage.MP.Setup(recvdtxt);
                            recvdText.Text = "";
                            _Mode = Mode.Connected;
                            Send("ACK3");
                        }
                    }
                    else if (_Mode==Mode.Connected)
                    {
                        MainPage.MP.UpdateText(recvdtxt);
                        recvdText.Text = "";
                        status.Text = "bytes read successfully!";
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
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

        /// <summary>
        /// closeDevice_Click: Action to take when 'Disconnect and Refresh List' is clicked on
        /// - Cancel all read operations
        /// - Close and dispose the SerialDevice object
        /// - Enumerate connected devices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeDevice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                status.Text = "";
                CancelReadTask();

                refresh.IsEnabled = true;
                sendTextButton.IsEnabled = false;
                comPortInput.IsEnabled = true;
                cancelSendButton.IsEnabled = false;
                this.recvdText.Text = "";
                
                closeDevice.IsEnabled = false;
                //CloseDevice(); Is closed by reader cancel
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }



        private void ConnectDevices_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            comPortInput_Click(null, null);
            ////this.Frame.GoBack();
        }

        private void cancelSendTextButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void refreshDevice_Click(object sender, RoutedEventArgs e)
        {
 
            listOfDevices.Clear();
            ListAvailablePorts();
            
        }

        bool FirstVisit = true;
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (FirstVisit)
            {
                FirstVisit = false;
                ListAvailablePorts();
                await AwaitS("Ack1");
                Send("Ack1");
                bool res = GetJson();
                if (res)
                {
                    Send("Ack2");
                    backButton_Click(null, null);
                    Send("Ack3");
                }
            }
            else
            {
                closeDevice_Click(null, null);
                backButton_Click(null, null);
            }
        }

        private async Task AwaitS(string v)
        {
            //throw new NotImplementedException();
        }

        private bool GetJson()
        {
            //throw new NotImplementedException();
            return true;
        }
    }
}


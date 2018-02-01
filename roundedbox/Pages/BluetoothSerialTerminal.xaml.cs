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
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using System.Collections.ObjectModel;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using System.Threading.Tasks;
using System.Threading;
using roundedbox;
using System.Text;

namespace Bluetooth
{
    /// <summary>
    /// The Bluetooth Serial page for the app
    /// </summary>
    public sealed partial class BluetoothSerialTerminalPage : Page
    {
        const int PauseBtwSentCharsmS = 1000;
        public const string EOStringStr = "~";
        public const char EOStringChar = '~';
        public const byte EOStringByte = 126;

        string Title = "Generic Bluetooth Serial Universal Windows App";
        private Windows.Devices.Bluetooth.Rfcomm.RfcommDeviceService _service;
        private StreamSocket _socket;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;

        ObservableCollection<PairedDeviceInfo> _pairedDevices;
        private CancellationTokenSource ReadCancellationTokenSource;

        enum Mode
        {
            Disconnected,
            JustConnected,
            Connected,
            AwaitJson,
            GetString
        }
        Mode _Mode = Mode.Disconnected;

        public BluetoothSerialTerminalPage()
        {
            this.NavigationCacheMode =
                    Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;
            this.InitializeComponent();
            MyTitle.Text = Title;
           
            _pairedDevices = new ObservableCollection<PairedDeviceInfo>();
 			InitializeRfcommDeviceService();
            MainPage.BTTerminalPage = this;
            _Mode = Mode.Disconnected;
        }


        async void InitializeRfcommDeviceService()
        {
            try
            {
                DeviceInformationCollection DeviceInfoCollection = await DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort));

                DeviceInformationCollection DeviceInfoCollection2 = await DeviceInformation.FindAllAsync();
                var asdf = (from n in DeviceInfoCollection2 select n.Name).Distinct();

                var numDevices = DeviceInfoCollection.Count();

                // By clearing the backing data, we are effectively clearing the ListBox
                 _pairedDevices.Clear();

                if (numDevices == 0)
                {
                    //MessageDialog md = new MessageDialog("No paired devices found", "Title");
                    //await md.ShowAsync();
                    System.Diagnostics.Debug.WriteLine("InitializeRfcommDeviceService: No paired devices found.");
                }
                else
                {
                    // Found paired devices.
                    foreach (var deviceInfo in DeviceInfoCollection)
                    {
                        _pairedDevices.Add(new PairedDeviceInfo(deviceInfo));
                    }
                }
                PairedDevices.Source = _pairedDevices;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("InitializeRfcommDeviceService: " + ex.Message);
            }
        }

        async private void ConnectDevice_Click(object sender, RoutedEventArgs e)
        {
            DeviceInformation DeviceInfo = ((PairedDeviceInfo)ConnectDevices.SelectedItem).DeviceInfo;// await DeviceInformation.CreateFromIdAsync(this.TxtBlock_SelectedID.Text);
            bool success = true;
            try
            {
                _service = await RfcommDeviceService.FromIdAsync(DeviceInfo.Id);

                if (_socket != null)
                {
                    // Disposing the socket with close it and release all resources associated with the socket
                    _socket.Dispose();
                }

                _socket = new StreamSocket();
                try { 
                    // Note: If either parameter is null or empty, the call will throw an exception
                    await _socket.ConnectAsync(_service.ConnectionHostName, _service.ConnectionServiceName);
                }
                catch (Exception ex)
                {
                        success = false;
                        System.Diagnostics.Debug.WriteLine("Connect:" + ex.Message);
                }
                // If the connection was successful, the RemoteAddress field will be populated
                if (success)
                {
                    _Mode = Mode.JustConnected;
                    this.buttonDisconnect.IsEnabled = true;
                    this.buttonSend.IsEnabled = true;
                    this.buttonStartRecv.IsEnabled = true;
                    this.buttonStopRecv.IsEnabled = false;
                     SendCh('0');
                    //Send("ACK0#");
                    this.buttonStartRecv.IsEnabled = false;
                    this.buttonStopRecv.IsEnabled = true;
                    Listen();
                    System.Diagnostics.Debug.WriteLine("Connected");
                    //await md.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Overall Connect: " +ex.Message);
                _socket.Dispose();
                _socket = null;
            }
        }






        private void ConnectDevices_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            PairedDeviceInfo pairedDevice = (PairedDeviceInfo)ConnectDevices.SelectedItem;
            this.TxtBlock_SelectedID.Text = pairedDevice.ID;
            this.textBlockBTName.Text = pairedDevice.Name;
            ConnectDevice_Click(sender, e);
        }

        //Windows.Storage.Streams.Buffer InBuff;
        Windows.Storage.Streams.Buffer OutBuff;
        //private StreamSocket _socket;
        private async void button_Click(object sender, RoutedEventArgs e)
        {
            OutBuff = new Windows.Storage.Streams.Buffer(100);
            Button button = (Button)sender;
            if (button != null)
            {
                switch ((string)button.Content)
                {
                    case "Disconnect":
                        await this._socket.CancelIOAsync();
                        _socket.Dispose();
                        _socket = null;
                        this.textBlockBTName.Text = "";
                        this.TxtBlock_SelectedID.Text = "";
                        this.buttonDisconnect.IsEnabled = false;
                        this.buttonSend.IsEnabled = false;
                        this.buttonStartRecv.IsEnabled = false;
                        this.buttonStopRecv.IsEnabled = false;
                        break;
                    case "Send":
                        //await _socket.OutputStream.WriteAsync(OutBuff);
                        string az = this.textBoxSendText.Text;
                        Send(this.textBoxSendText.Text);
                        this.textBoxSendText.Text = "";
                        break;
                    case "Clear Send":
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
                        InitializeRfcommDeviceService();
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

        //If sending key's code only
        public  void Send(int i)
        {
             Send(string.Format("~{0}",i));
        }

        //Normally send key's text. Also some commands
        public async void Send(string msg)
        {
            //for (int i = 0; i < msg.Length; i++)
            //{
                try
                {
                    if (_socket.OutputStream != null)
                    {
                        // Create the DataWriter object and attach to OutputStream
                        dataWriteObject = new DataWriter(_socket.OutputStream);

                        //Launch the WriteAsync task to perform the write
                        await WriteAsync(msg); //.Substring(i, 1));
                    }
                    else
                    {
                        //status.Text = "Select a device and connect";
                    }
                }
                catch (Exception ex)
                {
                    //status.Text = "Send(): " + ex.Message;
                    System.Diagnostics.Debug.WriteLine(ex.Message);
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

            //}
            //Task.Delay(PauseBtwSentCharsmS).Wait();
        }

        public async void SendCh(char ch)
        {
            //for (int i = 0; i < msg.Length; i++)
            //{
            try
            {
                if (_socket.OutputStream != null)
                {
                    // Create the DataWriter object and attach to OutputStream
                    dataWriteObject = new DataWriter(_socket.OutputStream);

                    //Launch the WriteAsync task to perform the write
                    await WriteChar(ch); //.Substring(i, 1));
                }
                else
                {
                    //status.Text = "Select a device and connect";
                }
            }
            catch (Exception ex)
            {
                //status.Text = "Send(): " + ex.Message;
                System.Diagnostics.Debug.WriteLine(ex.Message);
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

            //}
            //Task.Delay(PauseBtwSentCharsmS).Wait();
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
                //dataWriteObject.WriteString(msg);
                byte[] bytes;
                //recvdtxt += Encoding.UTF8.GetString(rt);
                bytes = Encoding.UTF8.GetBytes(msg);
                dataWriteObject.WriteBytes(bytes);
                dataWriteObject.WriteByte(0);
                // Launch an async task to complete the write operation
                storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

                UInt32 bytesWritten = await storeAsyncTask;
                if (bytesWritten > 0)
                {
                    string status_Text = msg + ", ";
                    //status.Text = sendText.Text + ", ";
                    status_Text += "bytes written successfully!";
                    System.Diagnostics.Debug.WriteLine(status_Text);
                }
                //sendText.Text = "";
            }
            else
            {
                string status_Text2 = "Enter the text you want to write and then click on 'WRITE'";
                System.Diagnostics.Debug.WriteLine(status_Text2);
            }
        }

        private async Task WriteChar(char ch)
        {
            Task<UInt32> storeAsyncTask;

            // Load the text from the sendText input text box to the dataWriter object
            //dataWriteObject.WriteString(msg);
            // byte[] bytes = new byte[1];
            byte byt = (byte)ch; ;
            //bytes[0] = (byte) ch;
            //recvdtxt += Encoding.UTF8.GetString(rt);
            //bytes = Encoding.UTF8.GetBytes(msg);
            //dataWriteObject.WriteBytes(bytes);
            //dataWriteObject.WriteByte(0);
            dataWriteObject.WriteByte(byt);
           // Launch an async task to complete the write operation
           storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

            UInt32 bytesWritten = await storeAsyncTask;
            if (bytesWritten > 0)
            {
                string status_Text = "";
                status_Text += ch;

                //status.Text = sendText.Text + ", ";
                status_Text += " bytes written successfully!";
                System.Diagnostics.Debug.WriteLine(status_Text);
            }
            //sendText.Text = "";
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
            try
            {
                ReadCancellationTokenSource = new CancellationTokenSource();
                if (_socket.InputStream != null)
                {
                    dataReaderObject = new DataReader(_socket.InputStream);
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
                    while (true) //!ReadCancellationTokenSource.Token.IsCancellationRequested)
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
                    System.Diagnostics.Debug.WriteLine( "Listen: Reading task was cancelled, closing device and cleaning up");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Listen: " +ex.Message);
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
                    byte[] rt  = new byte[bytesRead];
                    dataReaderObject.ReadBytes(rt);

                    //recvdtxt += Encoding.UTF8.GetString(rt);
                    //recvdtxt += dataReaderObject.ReadString(bytesRead);
                    //System.Diagnostics.Debug.WriteLine(recvdtxt);
                    //if (recvdtxt.Substring(recvdtxt.Length - 1) == EOStringChar)
                    //{
                    //    System.Diagnostics.Debug.WriteLine("Recvd: " + recvdtxt);
                    //    //this.recvdText.Text += recvdtxt;
                    //}
                    //else
                    //    return;
                    if (_Mode == Mode.JustConnected)
                    {
                        if ('1' == (char)rt[0])
                        //if (recvdtxt.ToUpper() == "ACK1#")
                        {
                            _Mode = Mode.AwaitJson;
                            recvdtxt = "";
                            //Send("ACK2#");
                            SendCh('2');
                        }
                    }
                    else if (_Mode == Mode.AwaitJson)
                    {
                        if ('3' == (char)rt[0])
                        //if (recvdtxt.ToUpper() == "ACK3#")
                        {
                            //    if (recvdtxt.ToUpper().Substring(0, "JSON".Length)== "JSON")
                            //{
                            //    recvdtxt = recvdtxt.Substring("JSON".Length);
                            //    MainPage.MP.Setup(recvdtxt);
                            recvdtxt = "";
                            _Mode = Mode.Connected;
                            //Send("ACK4#");
                            SendCh('4');
                        }
                        else
                        {
                            string json = recvdtxt;
                        }
                    }
                    else if (_Mode == Mode.Connected)
                    {
                        byte byt = rt[0];
                        switch (byt)
                        {
                            case 47:  //'/'
                                _Mode = Mode.GetString;
                                recvdtxt = "";
                                SendCh('/');
                                break;
                            default:
                                recvdtxt = "" + (char)rt[0];
                                await MainPage.MP.UpdateTextAsync(recvdtxt);//.Substring(0,recvdtxt.Length - 1));
                                recvdtxt = "";
                                System.Diagnostics.Debug.WriteLine("bytes read successfully!");
                                break;
                        }
                    }
                    else if (_Mode == Mode.GetString)
                    {
                        recvdtxt += Encoding.UTF8.GetString(rt);
                        if (recvdtxt.Substring(recvdtxt.Length - 1) == EOStringStr)
                        {
                            System.Diagnostics.Debug.WriteLine("Recvd: " + recvdtxt);
                            await MainPage.MP.UpdateTextAsync(recvdtxt);//.Substring(0,recvdtxt.Length - 1))
                            _Mode = Mode.Connected;
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
        ///  Class to hold all paired device information
        /// </summary>
        public class PairedDeviceInfo
        {
            internal PairedDeviceInfo(DeviceInformation deviceInfo)
            {
                this.DeviceInfo = deviceInfo;
                this.ID = this.DeviceInfo.Id;
                this.Name = this.DeviceInfo.Name;
            }

            public string Name { get; private set; }
            public string ID { get; private set; }
            public DeviceInformation DeviceInfo { get; private set; }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}

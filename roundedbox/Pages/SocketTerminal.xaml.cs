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
using System.Diagnostics;

namespace Socket
{
    /// <summary>
    /// The Bluetooth Serial page for the app
    /// </summary>
    public sealed partial class SocketTerminalPage : Page
    {
        const int PauseBtwSentCharsmS = 1000;
        public const string EOStringStr = "~";
        public const char EOStringChar = '~';
        public const byte EOStringByte = 126;
        private const int cFineStructure = 137; //ASCII Per mille sign

        string Title = "Universal Windows Socket Terminal";
        private Windows.Devices.Bluetooth.Rfcomm.RfcommDeviceService _service;


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

        public SocketTerminalPage()
        {
            this.NavigationCacheMode =
                    Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;
            this.InitializeComponent();
            MyTitle.Text = Title;
           
            //_pairedDevices = new ObservableCollection<PairedDeviceInfo>();
            MainPage.SocketTerminalPage = this;
            _Mode = Mode.Disconnected;
        }
        

        
        Windows.Storage.Streams.Buffer OutBuff;
        
        private async void button_Click(object sender, RoutedEventArgs e)
        {
            OutBuff = new Windows.Storage.Streams.Buffer(100);
            Button button = (Button)sender;
            if (button != null)
            {
                switch ((string)button.Content)
                {
                    case "Disconnect":
                        roundedbox.Helpers.SynchronousSocketListener.CloseSocket();

                        this.textBlockBTName.Text = "";
                        this.TxtBlock_SelectedID.Text = "";
                        this.buttonDisconnect.IsEnabled = false;
                        this.buttonSend.IsEnabled = false;
                        this.buttonStartRecv.IsEnabled = false;
                        this.buttonStopRecv.IsEnabled = false;
                        break;
                    case "Send":
                        //await _socket.OutputStream.WriteAsync(OutBuff);
                        string az = this.sendText.Text;
                        Send(this.sendText.Text);
                        this.sendText.Text = "";
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
                    case "Connect":
                        //await roundedbox.Helpers.SynchronousSocketListener.StartClient();
                        this.StartSocketClient();
                        //Listen();
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

        //Normally send key's text. Also some commands
        public async void Send(string msg)
        {
            //for (int i = 0; i < msg.Length; i++)
            //{
                try
                {
                    ////if (_socket.OutputStream != null)
                    ////{
                    ////    // Create the DataWriter object and attach to OutputStream
                    ////    dataWriteObject = new DataWriter(_socket.OutputStream);

                    ////    //Launch the WriteAsync task to perform the write
                    ////    await WriteAsync(msg); //.Substring(i, 1));
                    ////}
                    ////else
                    ////{
                    ////    //status.Text = "Select a device and connect";
                    ////}
                }
                catch (Exception ex)
                {
                    //status.Text = "Send(): " + ex.Message;
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
                finally
                {
                    // Cleanup once complete
                    ////if (dataWriteObject != null)
                    ////{
                    ////    dataWriteObject.DetachStream();
                    ////    dataWriteObject = null;
                    ////}
                }

            //}
            //Task.Delay(PauseBtwSentCharsmS).Wait();
        }
        public async void SendCh(char ch)
        {
            try
            {
                await roundedbox.Helpers.SynchronousSocketListener.SendCh(ch);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {


            }
        }
        
        /// <summary>
        /// WriteAsync: Task that asynchronously writes data from the input text box 'sendText' to the OutputStream 
        /// </summary>
        /// <returns></returns>
        private async Task WriteAsync(string msg)
        {
            if (msg.Length != 0)
            {
                char[] charArr = msg.ToCharArray();
                int bytesWritten = await roundedbox.Helpers.SynchronousSocketListener.WriteAsync(charArr);
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

                this.buttonStopRecv.IsEnabled = true;
                this.buttonDisconnect.IsEnabled = false;
                recvdtxt = "";
                // keep reading the serial input
                while (true) //!ReadCancellationTokenSource.Token.IsCancellationRequested)
                {                 
                    await ReadAsync(ReadCancellationTokenSource.Token);
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
                    status.Text = "Listen: " +ex.Message;
                }
            }
            finally
            {
                // Cleanup once complete
                //if (dataReaderObject != null)
                //{
                //    dataReaderObject.DetachStream();
                //    dataReaderObject = null;
                //}
            }
        }  

        /// <summary>
        /// ReadAsync: Task that waits on data and reads asynchronously from the serial device InputStream
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ReadAsync(CancellationToken cancellationToken)
        {


            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available

            byte[] bytes = await roundedbox.Helpers.SynchronousSocketListener.ReadAsync();
            UInt32 bytesRead = (uint) bytes.Length;
            if (bytesRead > 0)
            {
                try
                {

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
        private Windows.Networking.Sockets.StreamSocket streamSocket= null;
        private async void StartSocketClient()
        {
            try
            {
                //// Create the StreamSocket and establish a connection to the echo server.
                //using (var streamSocket = new Windows.Networking.Sockets.StreamSocket())
                //{
                streamSocket = new Windows.Networking.Sockets.StreamSocket();
                    // The server hostname that we will be establishing a connection to. In this example, the server and client are in the same process.
                    var hostName = new Windows.Networking.HostName("192.168.0.137");

                    MainPage.MP.clientListBox.Items.Add("client is trying to connect...");

                    await streamSocket.ConnectAsync(hostName, "1234");

                    MainPage.MP.clientListBox.Items.Add("client connected");

                    // Send a request to the echo server.
                    string request = "Hello, World";
                    using (Stream outputStream = streamSocket.OutputStream.AsStreamForWrite())
                    {
                        using (var streamWriter = new StreamWriter(outputStream))
                        {
                            await streamWriter.WriteLineAsync(request);
                            await streamWriter.FlushAsync();



                            MainPage.MP.clientListBox.Items.Add(string.Format("client sent the request: \"{0}\"", request));

                            // Read data from the echo server.
                            string response;
                            string json1 = "";
                            int ch;
                            string json2 = "";
                            char[] chars = new char[2];
                            chars[1] = 'Z';
                            int responseLength;
                            using (Stream inputStream = streamSocket.InputStream.AsStreamForRead())
                            {
                                using (StreamReader streamReader = new StreamReader(inputStream))
                                {
                                    response = await streamReader.ReadLineAsync();
                                    //do
                                    //{
                                    //    ch = streamReader.Read();
                                    //} while (ch > 255);
                                    responseLength = await streamReader.ReadAsync(chars, 0, 1);



                                    if (chars[0] == '@')
                                    {
                                        await streamWriter.WriteAsync('0');
                                        await streamWriter.FlushAsync();
                                    }

                                    responseLength = await streamReader.ReadAsync(chars, 0, 1);
                                    if (chars[0] == '1')
                                    {
                                        await streamWriter.WriteAsync('2');
                                        await streamWriter.FlushAsync();
                                    }

                                    responseLength = await streamReader.ReadAsync(chars, 0, 1);
                                    if (chars[0] == '3')
                                    {
                                        await streamWriter.WriteAsync('4');
                                        await streamWriter.FlushAsync();
                                    }

                                    responseLength = await streamReader.ReadAsync(chars, 0, 1);
                                    if (chars[0] == '5')
                                    {
                                        await streamWriter.WriteAsync('!');
                                        await streamWriter.FlushAsync();
                                    }

                                    responseLength = await streamReader.ReadAsync(chars, 0, 1);
                                    if (chars[0] == '/')
                                    {
                                        await streamWriter.WriteAsync('/');
                                        await streamWriter.FlushAsync();

                                        json1 = await streamReader.ReadLineAsync();


                                        await streamWriter.WriteAsync('~');
                                        await streamWriter.FlushAsync();

                                        json2 = await streamReader.ReadLineAsync();
                                    }


                                    

                                }
                            }
                        

                        }
                    }

                   // MainPage.MP.clientListBox.Items.Add(string.Format("client received the response: \"{0}\" ", response));

                   
                    //using (Stream inputStream = streamSocket.InputStream.AsStreamForRead())
                    //{
                    //    using (StreamReader streamReader = new StreamReader(inputStream))
                    //    {
                    //        ch = streamReader.Read();
                    //    }
                    //}

                    //MainPage.MP.clientListBox.Items.Add(string.Format("client received the response: \"{0}\" ", chars[0]));
               // }//

                //MainPage.MP.clientListBox.Items.Add("client closed its socket");
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                MainPage.MP.clientListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
        }

    }
}

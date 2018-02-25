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
using Windows.UI.Core;

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

        private string Title = "Socket Client Terminal UI App - UWP";

        private CancellationTokenSource ReadCancellationTokenSource;

        enum Mode
        {
            Disconnected,
            JustConnected,
            ACK1,
            ACK3,
            ACK5,
            AwaitJson,
            JsonConfig,
            Running,
            Config
        }
        Mode _Mode = Mode.Disconnected;

        public SocketTerminalPage()
        {
            this.NavigationCacheMode =
                    Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;
            this.InitializeComponent();
            MyTitle.Text = Title;
            uartTitle.Text = Title;

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
                        this.CloseSocket();

                        this.tbSvrName.Text = "";
                        this.TxtBlock_SelectedID.Text = "";
                        this.buttonDisconnect.IsEnabled = false;
                        this.buttonSend.IsEnabled = false;
                        this.buttonStartRecv.IsEnabled = false;
                        this.buttonStopRecv.IsEnabled = false;
                        break;
                    case "Send":
                        //await _socket.OutputStream.WriteAsync(OutBuff);
                        string az = this.sendText.Text;
                        await Send(this.sendText.Text);
                        this.sendText.Text = "";
                        break;
                    case "Clear Send":
                        this.recvdText.Text = "";
                        break;
                    case "Start Listen":
                        this.buttonStartRecv.IsEnabled = false;
                        this.buttonStopRecv.IsEnabled = true;
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            status.Text = "Starting Listen";
                        });
                        Listen();
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            status.Text = "Conncted and Listening: Press [Back]";
                        });
                        break;
                    case "Stop Listen":
                        this.buttonStartRecv.IsEnabled = false;
                        this.buttonStopRecv.IsEnabled = false;
                        CancelReadTask();
                        break;
                    case "Connect":
                        await this.StartSocketClient();
                        break;
                    case "Back":
                        //this.Frame.GoBack();
                        backButton_Click(null, null);
                        break;
                }
            }
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            if (_Mode == Mode.JustConnected)
            {
            }
            this.Frame.GoBack(); ;

        }

        //Normally send key's text. Also some commands



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
                this.tbSvrName.Text = "";
                this.TxtBlock_SelectedID.Text = "";
                if (ex.GetType().Name == "TaskCanceledException")
                {
                    System.Diagnostics.Debug.WriteLine("Listen: Reading task was cancelled, closing device and cleaning up");
                }
                else
                {
                    status.Text = "Listen: " + ex.Message;
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
        private Windows.Networking.Sockets.StreamSocket streamSocket = null;
        private Stream outputStream = null;
        private StreamWriter streamWriter = null;
        private Stream inputStream=null;
        private StreamReader streamReader = null;

        private async Task StartSocketClient()
        {
            try
            {
                string port = tbPort.Text;
                string svr = tbSvrName.Text;
                //// Create the StreamSocket and establish a connection to the echo server.
                //using (var streamSocket = new Windows.Networking.Sockets.StreamSocket())
                //{
                streamSocket = new Windows.Networking.Sockets.StreamSocket();
                // The server hostname that we will be establishing a connection to. In this example, the server and client are in the same process.
                string host = tbSvrName.Text;
                port = tbPort.Text;
                var hostName = new Windows.Networking.HostName(host);

                MainPage.MP.clientListBox.Items.Add("client is trying to connect...");

                await streamSocket.ConnectAsync(hostName, port);

                _Mode = Mode.JustConnected;
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    status.Text = "Socket Connected. Getting config data";
                });

                MainPage.MP.clientListBox.Items.Add("client connected");

                string request = "Hello, World";
                outputStream = streamSocket.OutputStream.AsStreamForWrite();
                streamWriter = new StreamWriter(outputStream);
                await streamWriter.WriteLineAsync(request);
                await streamWriter.FlushAsync();

                MainPage.MP.clientListBox.Items.Add(string.Format("client sent the request: \"{0}\"", request));

                // Read data from the echo server.
                string response;
                string json1 = "";
                string json2 = "";
                char[] chars = new char[1000];
                chars[1] = 'Z';
                int responseLength;

                inputStream = streamSocket.InputStream.AsStreamForRead();
                streamReader = new StreamReader(inputStream);
                response = await streamReader.ReadLineAsync();
                recvdText.Text = "" + response;

                responseLength = await streamReader.ReadAsync(chars, 0, 1000);
                recvdText.Text = "" + chars[0];


                if (chars[0] == '@')
                {
                    _Mode = Mode.JustConnected;
                    await streamWriter.WriteAsync('0');
                    await streamWriter.FlushAsync();
                }
                

                responseLength = await streamReader.ReadAsync(chars, 0, 1);
                recvdText.Text = "" + chars[0];
                if (chars[0] == '1')
                {
                    _Mode = Mode.ACK1;
                    await streamWriter.WriteAsync('2');
                    await streamWriter.FlushAsync();
                }


                responseLength = await streamReader.ReadAsync(chars, 0, 1);
                recvdText.Text = "" + chars[0];
                if (chars[0] == '3')
                {
                    _Mode = Mode.ACK3;
                    await streamWriter.WriteAsync('4');
                    await streamWriter.FlushAsync();                  
                }

                responseLength = await streamReader.ReadAsync(chars, 0, 1);
                recvdText.Text = "" + chars[0];
                if (chars[0] == '5')
                {
                    _Mode = Mode.ACK5;
                    await streamWriter.WriteAsync('!');
                    await streamWriter.FlushAsync();
                }
                

                responseLength = await streamReader.ReadAsync(chars, 0, 1);
                recvdText.Text = "" + chars[0];
                if (chars[0] == '/')
                {
                    await streamWriter.WriteAsync('/');
                    await streamWriter.FlushAsync();

                    _Mode = Mode.AwaitJson;

                    json1 = await streamReader.ReadLineAsync();
                    recvdText.Text = json1;
                    _Mode = Mode.JsonConfig;

                    await MainPage.MP.UpdateTextAsync(json1);

                    await streamWriter.WriteAsync('~');
                    await streamWriter.FlushAsync();

                    json2 = await streamReader.ReadLineAsync();
                    recvdText.Text = "" + json2;
                    _Mode = Mode.Config;

                    await MainPage.MP.UpdateTextAsync(json2);
                    recvdText.Text = "";
                    _Mode = Mode.Running;


                    Listen();

                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        status.Text = "Config data received: Press [Back]";
                        this.buttonStartRecv.IsEnabled = false;
                        this.buttonStopRecv.IsEnabled = false;
                    });
                }
                MainPage.MP.clientListBox.Items.Add(string.Format("client received the response: \"{0}\" ", "Got Json"));
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                MainPage.MP.clientListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
        }

   
        public async Task SendCh(char ch)
        {

            char[] chars = new char[2];
            chars[0] = ch;
            try
            {
                await streamWriter.WriteAsync(ch);
                await streamWriter.FlushAsync();
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                MainPage.MP.clientListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
        }

        public async Task Send(string request)
        {
            try
            {
                await streamWriter.WriteLineAsync(request);
                await streamWriter.FlushAsync();
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                MainPage.MP.clientListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
        }


        public async Task<char> ReceiveCh()
        {
            char ch = ' ';
            char[] chars = new char[2];
            try
            {
                int responseLength = await streamReader.ReadAsync(chars, 0, 1);
                if (responseLength == 1)
                    ch = chars[0];
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                MainPage.MP.clientListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
            MainPage.MP.clientListBox.Items.Add("Sent " + ch + "\r\n");
            return ch;
        }

        public async Task<string> ReceiveLn()
        {
            string ret = "";
            char[] chars = new char[2];
            try
            {
                ret = await streamReader.ReadLineAsync();
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                MainPage.MP.clientListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
            return ret;
        }

        public async Task ReadAsync(CancellationToken cancellationToken)
        {
            char[] chars = new char[1024]; // streamReader.BaseStream.Length];
            int responseLength =  await streamReader.ReadAsync(chars,0,1024);
            byte[] bytes = Encoding.Unicode.GetBytes(chars);
            bytes = bytes = bytes.Skip(0).Take(responseLength).ToArray();
            string recvdtxt = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);

            if (_Mode == Mode.Running)
            {
                await MainPage.MP.UpdateTextAsync(recvdtxt);                      
                
            }
        }


        public  void CloseSocket()
        {
            try
            {
                outputStream = null;
                streamWriter = null;
                inputStream = null;
                streamReader = null;

                streamSocket.Dispose();
                _Mode = Mode.Disconnected;
                Status.Text = "Socekt Disconnected";
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                MainPage.MP.clientListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                status.Text = "Set Arduino IP and Port then Press [Connect]";
            });
        }


    }
}

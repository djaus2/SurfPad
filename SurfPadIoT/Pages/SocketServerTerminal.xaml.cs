using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SurfPadIoT.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SocketServerTerminalPage : Page
    {
        private CancellationTokenSource ReadCancellationTokenSource;
        private bool testing = false;

        enum Mode
        {
            NotStarted,
            Running,
            Disconnected,
            JustConnected,
            ACK0,
            ACK2,
            ACK4,
            Connected,
            AwaitJson,
            JsonConfig
        }
        Mode _Mode = Mode.NotStarted;

        public SocketServerTerminalPage()
        {
            this.InitializeComponent();
            MainPage.SocketTerminalPage = this;
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            this.buttonStartRecv.IsEnabled = true;
            this.buttonStopRecv.IsEnabled = false;

            //Retrieve the ConnectionProfile
            //We want the internet connection
            string connectionProfileInfo = string.Empty;
            ConnectionProfile InternetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
            var id = InternetConnectionProfile.NetworkAdapter.NetworkAdapterId;
            //Pass the returned object to a function that accesses the connection data  
            //connectionProfileInfo = GetConnectionProfileInfo(InternetConnectionProfile);

            foreach (HostName localHostName in NetworkInformation.GetHostNames())
            {
                if (localHostName.IPInformation != null)
                {
                    if (localHostName.Type == HostNameType.Ipv4)
                    {
                        if (localHostName.IPInformation.NetworkAdapter.NetworkAdapterId == id)
                        {
                            tbSvrName.Text = localHostName.ToString();
                            break;
                        }
                    }
                }
            }

            PortNumber = tbPort.Text;

        }

        Windows.Networking.Sockets.StreamSocketListener streamSocketListener = null;
        public static string PortNumber { get; private set; } = "1234";

        private async Task StartServer()
        {
            try
            {
                streamSocketListener = new Windows.Networking.Sockets.StreamSocketListener();

                // The ConnectionReceived event is raised when connections are received.
                streamSocketListener.ConnectionReceived += this.StreamSocketListener_ConnectionReceived;

                // Start listening for incoming TCP connections on the specified port. You can specify any port that's not currently in use.
                await streamSocketListener.BindServiceNameAsync(SocketServerTerminalPage.PortNumber);
                _Mode = Mode.JustConnected;

                status.Text = "Server is listening...";
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                status.Text = webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message;
            }
        }



        private async void StreamSocketListener_ConnectionReceived(Windows.Networking.Sockets.StreamSocketListener sender, Windows.Networking.Sockets.StreamSocketListenerConnectionReceivedEventArgs args)
        {
            //string request="";
            string response;
            char[] chars = new char[10];
            chars[1] = 'Z';
            int responseLength;

            try
            { 
                using (var streamReader = new StreamReader(args.Socket.InputStream.AsStreamForRead()))
                {
                    using (Stream outputStream = args.Socket.OutputStream.AsStreamForWrite())
                    {
                        using (var streamWriter = new StreamWriter(outputStream))
                        {
                            if (_Mode == Mode.JustConnected)
                            {
                                response = await streamReader.ReadLineAsync();

                                await streamWriter.WriteLineAsync(response);
                                await streamWriter.FlushAsync();

                                await streamWriter.WriteAsync('@');
                                await streamWriter.FlushAsync();


                                responseLength = await streamReader.ReadAsync(chars, 0, 10);
                                recvdText.Text = "" + chars;
                                if (chars[0] == '0')
                                {
                                    await streamWriter.WriteAsync('1');
                                    await streamWriter.FlushAsync();
                                }
                                _Mode = Mode.ACK0;

                                if (testing)
                                    response = await streamReader.ReadLineAsync();
                                responseLength = await streamReader.ReadAsync(chars, 0, 10);
                                recvdText.Text = "" + chars[0];
                                if (chars[0] == '2')
                                {
                                    await streamWriter.WriteAsync('3');
                                    await streamWriter.FlushAsync();
                                }

                                _Mode = Mode.ACK2;

                                if (testing)
                                    response = await streamReader.ReadLineAsync();
                                responseLength = await streamReader.ReadAsync(chars, 0, 10);
                                recvdText.Text = "" + chars[0];
                                if (chars[0] == '4')
                                {
                                    await streamWriter.WriteAsync('5');
                                    await streamWriter.FlushAsync();
                                }
                                _Mode = Mode.ACK4;

                                if (testing)
                                    response = await streamReader.ReadLineAsync();
                                responseLength = await streamReader.ReadAsync(chars, 0, 10);
                                recvdText.Text = "" + chars[0];
                                if (chars[0] == '!')
                                {
                                    await streamWriter.WriteAsync('/');
                                    await streamWriter.FlushAsync();
                                }

                                _Mode = Mode.AwaitJson;

           
                                responseLength = await streamReader.ReadAsync(chars, 0, 1);
                                recvdText.Text = "" + chars[0];
                                if (chars[0] == '/')
                                {
                                    _Mode = Mode.JsonConfig;
                                    await streamWriter.WriteLineAsync(
        "{\"Config\":[ [ { \"iWidth\": 120 },{ \"iHeight\": 100 },{ \"iSpace\": 5 },{ \"iCornerRadius\": 10 },{ \"iRows\": 2 },{ \"iColumns\": 5 },{ \"sComPortId\": \"\\\\\\\\?\\\\USB#VID_26BA&PID_0003#5543830353935161A112#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"sFTDIComPortId\": \"\\\\\\\\?\\\\FTDIBUS#VID_0403+PID_6001+FTG71BUIA#0000#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"iComportConnectDeviceNo\": -1 },{ \"iFTDIComportConnectDeviceNo\": 1 },{ \"sUseSerial\": \"BT\" } ] ] }~");
                                    await streamWriter.FlushAsync();

                                    if (testing)
                                        response = await streamReader.ReadLineAsync();
                                    responseLength = await streamReader.ReadAsync(chars, 0, 1);
                                    {
                                        if (chars[0] == '~')
                                        {
                                            await streamWriter.WriteLineAsync(
        "{\"MainMenu\":[ [ \"Something else\", \"Unload\", \"Show full list\", \"Setup Sockets\", \"The quick brown fox jumps over the lazy dog\" ],[ \"First\", \"Back\", \"Next\", \"Last\", \"Show All\" ] ] }~");
                                            await streamWriter.FlushAsync();
                                        }
                                    }
                                }

                                bool listening = true;
                                _Mode = Mode.Running;
                                while (listening)
                                {
                                    try
                                    {
                                        if (testing)
                                            response = await streamReader.ReadLineAsync();
                                        responseLength = await streamReader.ReadAsync(chars, 0, 1);
                                    }
                                    catch (Exception ex)
                                    {
                                        await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => status.Text += "\r\n" + "Lost connection:\r\n" + ex.Message);
                                        listening = false;
                                    }

                                    if (listening)
                                        switch (chars[0])
                                        {
                                            case '^':
                                                listening = false;
                                                break;
                                            default:
                                                //Do app stuff here. For now just echo chars sent
                                                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => status.Text += "" + chars[0]);
                                                try
                                                {
                                                    await streamWriter.WriteAsync(chars[0]);
                                                    await streamWriter.FlushAsync();
                                                }
                                                catch (Exception ex)
                                                {
                                                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => status.Text += "\r\n" + "Lost connection:\r\n" + ex.Message);
                                                    listening = false;
                                                }
                                                break;
                                        }
                                }
                            }
                        }
                    }
                }
            }      
            catch (Exception ex)
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => status.Text += "\r\n" + "Lost connection:\r\n" + ex.Message);
            }

            //await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => status.Text = string.Format("server sent back the response: \"{0}\"", request));
            sender.Dispose();

            //await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => status.Text = "server closed its socket"); ;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!previousLoaded)
            {
                await StartServer();
                this.buttonStartRecv.IsEnabled = false;
                this.buttonStopRecv.IsEnabled = true;
                previousLoaded = true;
            }
        }

        private bool previousLoaded = false;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

  
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                string content = (string)((Button)sender).Content;
                if (content != "")
                {
                    switch (content)
                    {
                        case "Start Listen":
                            PortNumber = tbPort.Text;
                            await StartServer();
                            this.buttonStartRecv.IsEnabled = false;
                            this.buttonStopRecv.IsEnabled = true;
                            break;
                        case "Stop Listen":
                            await streamSocketListener.CancelIOAsync();
                            streamSocketListener.Dispose();
                            status.Text = "Socket Server stopped. Press [Start listen]";
                            this.buttonStartRecv.IsEnabled = true;
                            this.buttonStopRecv.IsEnabled = false;
                            break;
                        case "Back":
                            //if (this.buttonStopRecv.IsEnabled)
                            //{
                            //    await streamSocketListener.CancelIOAsync();
                            //    streamSocketListener.Dispose();
                            //    status.Text = "Socket Server stopped. Press [Start listen]";
                            //    this.buttonStartRecv.IsEnabled = true;
                            //    this.buttonStopRecv.IsEnabled = false;
                            //}
                            this.Frame.GoBack();
                            break;
                    }
                }
            }
        }

        private void buttonBack_Click(object sender, RoutedEventArgs e)
        {
            //await streamSocketListener.CancelIOAsync();
            //streamSocketListener.Dispose();
            //status.Text = "Socket Server stopped. Press [Start listen]";
            //this.buttonStartRecv.IsEnabled = true;
            //this.buttonStopRecv.IsEnabled = false;
            this.Frame.GoBack();
        }
    }
}

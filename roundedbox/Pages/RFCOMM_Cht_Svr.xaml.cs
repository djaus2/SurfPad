//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace roundedbox

{
    public sealed partial class RFCOMM_ChatServer : Page
    {
        private StreamSocket socket;
        private DataWriter writer;
        private RfcommServiceProvider rfcommProvider;
        private StreamSocketListener socketListener;

        const int PauseBtwSentCharsmS = 1000;
        public const string EOStringStr = "~";
        public const char EOStringChar = '~';
        public const byte EOStringByte = 126;
        private const int cFineStructure = 137; //ASCII Per mille sign

        string Title = "Bluetooth RFCOMM Chat Svr Terminal UI App - UWP";

        enum Mode
        {
            Disconnected,
            JustConnected,
            Connected,
            ACK1,
            ACK3,
            ACK5,
            AwaitJson,
            JsonConfig,
            Config,
            Running
        }
        Mode _Mode = Mode.Disconnected;

        // A pointer back to the main page is required to display status messages.
        MainPage MP=null;

        public RFCOMM_ChatServer()
        {
            MP = MainPage.MP;
            MainPage.RFCOMM_ChatPage = this;
            this.InitializeComponent();
            _Mode = Mode.Disconnected;
            TitleTextBlock.Text = Title;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MP = MainPage.MP;
            ListenButton_Click(null, null);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            //Disconnect();
        }

        private void ListenButton_Click(object sender, RoutedEventArgs e)
        {
            InitializeRfcommServer();
        }

        /// <summary>
        /// Initializes the server using RfcommServiceProvider to advertise the Chat Service UUID and start listening
        /// for incoming connections.
        /// </summary>
        private async void InitializeRfcommServer()
        {
            ListenButton.IsEnabled = false;
            DisconnectButton.IsEnabled = true;

            try
            {
                rfcommProvider = await RfcommServiceProvider.CreateAsync(RfcommServiceId.FromUuid(Constants.RfcommChatServiceUuid));
            }
            // Catch exception HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE).
            catch (Exception ex) when ((uint)ex.HResult == 0x800710DF)
            {
                // The Bluetooth radio may be off.
                //MP..NotifyUser("Make sure your Bluetooth Radio is on: " + ex.Message, NotifyType.ErrorMessage);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    status.Text = string.Format("{0}: {1}", MainPage.NotifyType.ErrorMessage,
                    "Make sure your Bluetooth Radio is on: ");
                });
                ListenButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
                return;
            }


            // Create a listener for this service and start listening
            socketListener = new StreamSocketListener();
            socketListener.ConnectionReceived += OnConnectionReceived;
            var rfcomm = rfcommProvider.ServiceId.AsString();

            await socketListener.BindServiceNameAsync(rfcommProvider.ServiceId.AsString(),
                SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

            // Set the SDP attributes and start Bluetooth advertising
            InitializeServiceSdpAttributes(rfcommProvider);

            try
            {
                rfcommProvider.StartAdvertising(socketListener, true);
            }
            catch (Exception e)
            {
                // If you aren't able to get a reference to an RfcommServiceProvider, tell the user why.  Usually throws an exception if user changed their privacy settings to prevent Sync w/ Devices.  
                //MP.NotifyUser(e.Message, NotifyType.ErrorMessage);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    status.Text = string.Format("{0}: {1}", MainPage.NotifyType.ErrorMessage,
                    e.Message);
                });
                ListenButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
                return;
            }

            //MP.NotifyUser("Listening for incoming connections", NotifyType.StatusMessage);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                status.Text = string.Format("{0}: {1}",MainPage.NotifyType.StatusMessage,
                 "Listening for incoming connections");
            });
        }

        /// <summary>
        /// Creates the SDP record that will be revealed to the Client device when pairing occurs.  
        /// </summary>
        /// <param name="rfcommProvider">The RfcommServiceProvider that is being used to initialize the server</param>
        private void InitializeServiceSdpAttributes(RfcommServiceProvider rfcommProvider)
        {
            var sdpWriter = new DataWriter();

            // Write the Service Name Attribute.
            sdpWriter.WriteByte(Constants.SdpServiceNameAttributeType);

            // The length of the UTF-8 encoded Service Name SDP Attribute.
            sdpWriter.WriteByte((byte)Constants.SdpServiceName.Length);

            // The UTF-8 encoded Service Name value.
            sdpWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            sdpWriter.WriteString(Constants.SdpServiceName);

            // Set the SDP Attribute on the RFCOMM Service Provider.
            rfcommProvider.SdpRawAttributes.Add(Constants.SdpServiceNameAttributeId, sdpWriter.DetachBuffer());
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        public void KeyboardKey_Pressed(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                SendMessage();
            }
        }

        private async void SendMessage()
        {
            // There's no need to send a zero length message
            if (MessageTextBox.Text.Length != 0)
            {
                // Make sure that the connection is still up and there is a message to send
                if (socket != null)
                {
                    string message = MessageTextBox.Text;
                    writer.WriteUInt32((uint)message.Length);
                    writer.WriteString(message);

                    ConversationListBox.Items.Insert(0,"Sent: " + message);
                    // Clear the messageTextBox for a new message
                    MessageTextBox.Text = "";

                    await writer.StoreAsync();

                }
                else
                {
                    //MP.NotifyUser("No clients connected, please wait for a client to connect before attempting to send a message", NotifyType.StatusMessage);
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        status.Text = string.Format("{0}: {1}", MainPage.NotifyType.StatusMessage,
                        "No clients connected, please wait for a client to connect before attempting to send a message");
                    });
                }
            }
        }


        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();
            //MP.NotifyUser("Disconnected.", NotifyType.StatusMessage);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
            status.Text = string.Format("{0}: {1}", MainPage.NotifyType.StatusMessage,
                "Disconnected.");
            });


        }

        private async void Disconnect()
        {
            _Mode = Mode.Disconnected;
            if (rfcommProvider != null)
            {
                rfcommProvider.StopAdvertising();
                rfcommProvider = null;
            }

            if (socketListener != null)
            {
                socketListener.Dispose();
                socketListener = null;
            }

            if (writer != null)
            {
                writer.DetachStream();
                writer = null;
            }

            if (socket != null)
            {
                socket.Dispose();
                socket = null;
            }
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ListenButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
                ConversationListBox.Items.Clear();
            });
        }

        /// <summary>
        /// Invoked when the socket listener accepts an incoming Bluetooth connection.
        /// </summary>
        /// <param name="sender">The socket listener that accepted the connection.</param>
        /// <param name="args">The connection accept parameters, which contain the connected socket.</param>
        private async void OnConnectionReceived(
            StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            // Don't need the listener anymore
            socketListener.Dispose();
            socketListener = null;

            try
            {
                socket = args.Socket;
            }
            catch (Exception e)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    status.Text = string.Format("{0}: {1}", MainPage.NotifyType.ErrorMessage, e.Message);
                    //MP.NotifyUser(e.Message, NotifyType.ErrorMessage);
                });
                Disconnect();
                return;
            }

            // Note - this is the supported way to get a Bluetooth device from a given socket
            var remoteDevice = await BluetoothDevice.FromHostNameAsync(socket.Information.RemoteHostName);

            writer = new DataWriter(socket.OutputStream);
            var reader = new DataReader(socket.InputStream);
            bool remoteDisconnection = false;

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //MP.NotifyUser("Connected to Client: " + remoteDevice.Name, NotifyType.StatusMessage);
                status.Text = string.Format("{0}: {1}", MainPage.NotifyType.StatusMessage, "Connected to Client: ");

            });

            _Mode = Mode.JustConnected;
            // Infinite read buffer loop
            recvdtxt = "";
            while (true)
            {
                try
                {
                    // Based on the protocol we've defined, the first uint is the size of the message
                    uint readLength = await reader.LoadAsync(sizeof(uint));

                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                    if (readLength < sizeof(uint))
                    {
                        remoteDisconnection = true;
                        break;
                    }
                    uint currentLength = reader.ReadUInt32();

                    // Load the rest of the message since you already know the length of the data expected.  
                    readLength = await reader.LoadAsync(currentLength);

                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                    if (readLength < currentLength)
                    {
                        remoteDisconnection = true;
                        break;
                    }
                    string message = reader.ReadString(currentLength);
                    await ReadAsync(message);

                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        ConversationListBox.Items.Insert(0,"Received: " + message);
                    });
                }
                // Catch exception HRESULT_FROM_WIN32(ERROR_OPERATION_ABORTED).
                catch (Exception ex) when ((uint)ex.HResult == 0x800703E3)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        //MP.NotifyUser("Client Disconnected Successfully", NotifyType.StatusMessage);
                        status.Text = string.Format("{0}: {1}", MainPage.NotifyType.StatusMessage, "Client Disconnected Successfully");
                    });
                    break;
                }
            }

            reader.DetachStream();
            if (remoteDisconnection)
            {
                Disconnect();
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    //MP.NotifyUser("Client disconnected", NotifyType.StatusMessage);
                    status.Text = string.Format("{0}: {1}", MainPage.NotifyType.StatusMessage, "Client disconnected");
                    ListenButton_Click(null, null);
                });
            }
        }

        public void SendCh(char ch)
        {
            var t = Task.Run(async () =>
            {
                await SendChTask("" + ch);
            });
        }

        public async Task  SendChTask(string msg)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                MessageTextBox.Text = msg;
                SendMessage();
            });
        }

        string recvdtxt = "";
        private async Task ReadAsync(string msg)
        {

            if (msg != "")
            {
                try
                {
                    string currenbtRecvdText = msg;

                    
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        recvdText.Text = currenbtRecvdText;
                    });

                    if (_Mode == Mode.JustConnected)
                    {
                        //May get char(194) first
                        for (int i = 0; i < msg.Length; i++)
                        {
                            if ('@' == msg[i])
                            {
                                _Mode = Mode.Connected;
                                SendCh('0');
                            }
                        }
                    }
                    else if (_Mode == Mode.Connected)
                    {
                        if ('1' == msg[0])
                        {
                            _Mode = Mode.ACK1;
                            recvdtxt = "";
                            SendCh('2');
                        }
                    }
                    else if (_Mode == Mode.ACK1)
                    {
                        if ('3' == msg[0])
                        {
                            _Mode = Mode.ACK3;
                            recvdtxt = "";
                            SendCh('4');
                        }
                    }
                    else if (_Mode == Mode.ACK3)
                    {
                        if ('5' == msg[0])
                        {
                            _Mode = Mode.ACK5;
                            SendCh('!');
                            //status.Text="Ready for Config. Press [Back] then on MainPage press [Load App Menu]";
                        }
                    }
                    else if (_Mode == Mode.ACK5)
                    {
                        if ('/' == msg[0])
                        {
                            _Mode = Mode.AwaitJson;
                            recvdtxt = "";
                            SendCh('/');
                        }
                    }
                    else if (_Mode == Mode.AwaitJson)
                    {

                        recvdtxt += currenbtRecvdText;
                        if (recvdtxt.Substring(recvdtxt.Length - 1) == EOStringStr)
                        {
                            await MainPage.MP.UpdateTextAsync(recvdtxt);
                            recvdtxt = "";
                            _Mode = Mode.JsonConfig;
                            SendCh('~');
                        }
                    }
                    else if (_Mode == Mode.JsonConfig)
                    {
                        recvdtxt += currenbtRecvdText;
                        if (recvdtxt.Substring(recvdtxt.Length - 1) == EOStringStr)
                        {
                            System.Diagnostics.Debug.WriteLine("Recvd: " + recvdtxt);
                            _Mode = Mode.Config;
                            await MainPage.MP.UpdateTextAsync(recvdtxt);//.Substring(0,recvdtxt.Length - 1))
                            _Mode = Mode.Running;
                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                status.Text = "Config done. Press [Back]";
                                recvdtxt = "";
                                BacktButton.IsEnabled = true;
                            });

                        }

                        else
                            return;
                    }
                    else if (_Mode == Mode.Running)
                    {
                        recvdtxt = currenbtRecvdText;
                        await MainPage.MP.UpdateTextAsync(recvdtxt);
                    }

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ReadAsync: " + ex.Message);
                }

            }
        }

        private void BacktButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }
    }
}

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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static SurfPadIoT.MainPage;



namespace SurfPadIoT.Pages
{
    public sealed partial class RFCOMM_ChatClientPage : Page
    {
        // A pointer back to the main page is required to display status messages.
        private MainPage MP = MainPage.MP;

        const int PauseBtwSentCharsmS = 1000;
        public const string EOStringStr = "~";
        public const char EOStringChar = '~';
        public const byte EOStringByte = 126;
        private const int cFineStructure = 137; //ASCII Per mille sign

        string Title = "Bluetooth RFCOMM Chat Client Terminal UI App - UWP";

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


        // Used to display list of available devices to chat with
        public ObservableCollection<RfcommChatDeviceDisplay> ResultCollection
        {
            get;
            private set;
        }

        private DeviceWatcher deviceWatcher = null;
        private StreamSocket chatSocket = null;
        private DataWriter chatWriter = null;
        private RfcommDeviceService chatService = null;
        private BluetoothDevice bluetoothDevice;

        public RFCOMM_ChatClientPage()
        {
            this.InitializeComponent();
            App.Current.Suspending += App_Suspending;
            _Mode = Mode.Disconnected;
            TitleTextBlock.Text = Title;
            status.Text = "";
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MP = MainPage.MP;
            ResultCollection = new ObservableCollection<RfcommChatDeviceDisplay>();
            DataContext = this;
            RunButton_Click(null, null);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            StopWatcher();
        }

        private void StopWatcher()
        {
            if (null != deviceWatcher)
            {
                if ((DeviceWatcherStatus.Started == deviceWatcher.Status ||
                     DeviceWatcherStatus.EnumerationCompleted == deviceWatcher.Status))
                {
                    deviceWatcher.Stop();
                }
                deviceWatcher = null;
            }
        }

        void App_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            // Make sure we clean up resources on suspend.
            Disconnect("App Suspension disconnects");
        }

        /// <summary>
        /// When the user presses the run button, query for all nearby unpaired devices
        /// Note that in this case, the other device must be running the Rfcomm Chat Server before being paired.
        /// </summary>
        /// <param name="sender">Instance that triggered the event.</param>
        /// <param name="e">Event data describing the conditions that led to the event.</param>
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (deviceWatcher == null)
            {
                SetDeviceWatcherUI();
                StartUnpairedDeviceWatcher();
            }
            else
            {
                ResetMainUI();
            }
        }

        private void SetDeviceWatcherUI()
        {
            // Disable the button while we do async operations so the user can't Run twice.
            RunButton.Content = "Stop";
            UpdateStatus("Device watcher started", NotifyType.StatusMessage);
            resultsListView.Visibility = Visibility.Visible;
            resultsListView.IsEnabled = true;
        }

        private void ResetMainUI()
        {
            RunButton.Content = "Start";
            RunButton.IsEnabled = true;
            ConnectButton.Visibility = Visibility.Visible;
            resultsListView.Visibility = Visibility.Visible;
            resultsListView.IsEnabled = true;

            // Re-set device specific UX
            ChatBox.Visibility = Visibility.Collapsed;
            RequestAccessButton.Visibility = Visibility.Collapsed;
            if (ConversationList.Items != null) ConversationList.Items.Clear();
            StopWatcher();
        }

        private void StartUnpairedDeviceWatcher()
        {
            // Request additional properties
            string[] requestedProperties = new string[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            deviceWatcher = DeviceInformation.CreateWatcher("(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")",
                                                            requestedProperties,
                                                            DeviceInformationKind.AssociationEndpoint);

            // Hook up handlers for the watcher events before starting the watcher
            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, deviceInfo) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    // Make sure device name isn't blank
                    if (deviceInfo.Name != "")
                    {
                        var cv = deviceInfo.GetType();
                        var sdf = cv.GUID;
                        ResultCollection.Clear();
                        ResultCollection.Add(new RfcommChatDeviceDisplay(deviceInfo));
                        UpdateStatus(
                            String.Format("{0} devices found.", ResultCollection.Count),
                            NotifyType.StatusMessage);
                        ConnectButton_Click(null, null);
                    }

                });
            });

            deviceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    foreach (RfcommChatDeviceDisplay rfcommInfoDisp in ResultCollection)
                    {
                        if (rfcommInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            rfcommInfoDisp.Update(deviceInfoUpdate);
                            break;
                        }
                    }
                });
            });

            deviceWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    UpdateStatus(
                        String.Format("{0} devices found. Enumeration completed. Watching for updates...", ResultCollection.Count),
                        NotifyType.StatusMessage);
                });

            });

            deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    // Find the corresponding DeviceInformation in the collection and remove it
                    foreach (RfcommChatDeviceDisplay rfcommInfoDisp in ResultCollection)
                    {
                        if (rfcommInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            ResultCollection.Remove(rfcommInfoDisp);
                            break;
                        }
                    }

                    UpdateStatus(
                        String.Format("{0} devices found.", ResultCollection.Count),
                        NotifyType.StatusMessage);
                });
            });

            deviceWatcher.Stopped += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    ResultCollection.Clear();
                });
            });

            deviceWatcher.Start();
        }

        /// <summary>
        /// Invoked once the user has selected the device to connect to.
        /// Once the user has selected the device,
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // Make sure user has selected a device first
            if (ResultCollection.Count != 0) //.SelectedItem != null)
            {
                UpdateStatus("Connecting to remote device. Please wait...", NotifyType.StatusMessage);
            }
            else
            {
                UpdateStatus("Please select an item to connect to", NotifyType.ErrorMessage);
                return;
            }

            var coll = ResultCollection;

            //var xx = resultsListView.SelectedItem;
            RfcommChatDeviceDisplay deviceInfoDisp = ResultCollection[0]; // resultsListView.SelectedItem as RfcommChatDeviceDisplay;

            // Perform device access checks before trying to get the device.
            // First, we check if consent has been explicitly denied by the user.
            DeviceAccessStatus accessStatus = DeviceAccessInformation.CreateFromId(deviceInfoDisp.Id).CurrentStatus;
            if (accessStatus == DeviceAccessStatus.DeniedByUser)
            {
                UpdateStatus("This app does not have access to connect to the remote device (please grant access in Settings > Privacy > Other Devices", NotifyType.ErrorMessage);
                return;
            }
            // If not, try to get the Bluetooth device
            try
            {
                bluetoothDevice = await BluetoothDevice.FromIdAsync(deviceInfoDisp.Id);
            }
            catch (Exception ex)
            {
                UpdateStatus(ex.Message, NotifyType.ErrorMessage);
                //ResetMainUI();
                return;
            }
            // If we were unable to get a valid Bluetooth device object,
            // it's most likely because the user has specified that all unpaired devices
            // should not be interacted with.
            if (bluetoothDevice == null)
            {
                UpdateStatus("Bluetooth Device returned null. Access Status = " + accessStatus.ToString(), NotifyType.ErrorMessage);
                return;
            }

            // This should return a list of uncached Bluetooth services (so if the server was not active when paired, it will still be detected by this call
            var rfcommServices = await bluetoothDevice.GetRfcommServicesForIdAsync(
                RfcommServiceId.FromUuid(Constants.RfcommChatServiceUuid), BluetoothCacheMode.Uncached);

            if (rfcommServices.Services.Count > 0)
            {
                chatService = rfcommServices.Services[0];
            }
            else
            {
                UpdateStatus(
                   "Could not discover the chat service on the remote device",
                   NotifyType.StatusMessage);
                //ResetMainUI();
                return;
            }

            // Do various checks of the SDP record to make sure you are talking to a device that actually supports the Bluetooth Rfcomm Chat Service
            var attributes = await chatService.GetSdpRawAttributesAsync();
            if (!attributes.ContainsKey(Constants.SdpServiceNameAttributeId))
            {
                UpdateStatus(
                    "The Chat service is not advertising the Service Name attribute (attribute id=0x100). " +
                    "Please verify that you are running the BluetoothRfcommChat server.",
                    NotifyType.ErrorMessage);
                //ResetMainUI();
                return;
            }
            var attributeReader = DataReader.FromBuffer(attributes[Constants.SdpServiceNameAttributeId]);
            var attributeType = attributeReader.ReadByte();
            if (attributeType != Constants.SdpServiceNameAttributeType)
            {
                UpdateStatus(
                    "The Chat service is using an unexpected format for the Service Name attribute. " +
                    "Please verify that you are running the BluetoothRfcommChat server.",
                    NotifyType.ErrorMessage);
                //ResetMainUI();
                return;
            }
            var serviceNameLength = attributeReader.ReadByte();

            // The Service Name attribute requires UTF-8 encoding.
            attributeReader.UnicodeEncoding = UnicodeEncoding.Utf8;



            lock (this)
            {
                chatSocket = new StreamSocket();
            }
            try
            {
                await chatSocket.ConnectAsync(chatService.ConnectionHostName, chatService.ConnectionServiceName);

                SetChatUI(attributeReader.ReadString(serviceNameLength), bluetoothDevice.Name);
                chatWriter = new DataWriter(chatSocket.OutputStream);

                DataReader chatReader = new DataReader(chatSocket.InputStream);
                ReceiveStringLoop(chatReader);
                SendCh('@');
                _Mode = Mode.Connected;
                UpdateStatus(
                                "Chatting now.",
                                NotifyType.StatusMessage);
                StopWatcher();
                recvdtxt = "";
                listening = true;
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80070490) // ERROR_ELEMENT_NOT_FOUND
            {
                UpdateStatus("Please verify that you are running the BluetoothRfcommChat server.", NotifyType.ErrorMessage);
                //ResetMainUI();
                return;
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80072740) // WSAEADDRINUSE
            {
                UpdateStatus("Please verify that there is no other RFCOMM connection to the same device.", NotifyType.ErrorMessage);
                //ResetMainUI();
                return;
            }

        }

        /// <summary>
        ///  If you believe the Bluetooth device will eventually be paired with Windows,
        ///  you might want to pre-emptively get consent to access the device.
        ///  An explicit call to RequestAccessAsync() prompts the user for consent.
        ///  If this is not done, a device that's working before being paired,
        ///  will no longer work after being paired.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RequestAccessButton_Click(object sender, RoutedEventArgs e)
        {
            // Make sure user has given consent to access device
            DeviceAccessStatus accessStatus = await bluetoothDevice.RequestAccessAsync();

            if (accessStatus != DeviceAccessStatus.Allowed)
            {
                UpdateStatus(
                    "Access to the device is denied because the application was not granted access",
                    NotifyType.StatusMessage);
            }
            else
            {
                UpdateStatus(
                                    "Access granted, you are free to pair devices",
                                    NotifyType.StatusMessage);
            }
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

        /// <summary>
        /// Takes the contents of the MessageTextBox and writes it to the outgoing chatWriter
        /// </summary>
        private async void SendMessage()
        {
            try
            {
                if (MessageTextBox.Text.Length != 0)
                {
                    chatWriter.WriteUInt32((uint)MessageTextBox.Text.Length);
                    chatWriter.WriteString(MessageTextBox.Text);

                    ConversationList.Items.Insert(0,"Sent: " + MessageTextBox.Text);
                    MessageTextBox.Text = "";
                    await chatWriter.StoreAsync();

                }
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80072745)
            {
                // The remote device has disconnected the connection
                UpdateStatus("Remote side disconnect: " + ex.HResult.ToString() + " - " + ex.Message,
                    NotifyType.StatusMessage);
            }
        }

        private async void ReceiveStringLoop(DataReader chatReader)
        {
            try
            {
                uint size = await chatReader.LoadAsync(sizeof(uint));
                if (size < sizeof(uint))
                {
                    Disconnect("Remote device terminated connection - make sure only one instance of server is running on remote device");
                    return;
                }

                uint stringLength = chatReader.ReadUInt32();
                uint actualStringLength = await chatReader.LoadAsync(stringLength);
                if (actualStringLength != stringLength)
                {
                    // The underlying socket was closed before we were able to read the whole data
                    return;
                }
                string msg = chatReader.ReadString(stringLength);
                ConversationList.Items.Insert(0,"Received: " + msg);
                await ReadAsync(msg);
                ReceiveStringLoop(chatReader);
            }
            catch (Exception ex)
            {
                lock (this)
                {
                    if (chatSocket == null)
                    {
                        // Do not print anything here -  the user closed the socket.
                        if ((uint)ex.HResult == 0x80072745)
                            UpdateStatus("Disconnect triggered by remote device", NotifyType.StatusMessage);
                        else if ((uint)ex.HResult == 0x800703E3)
                            UpdateStatus("The I/O operation has been aborted because of either a thread exit or an application request.", NotifyType.StatusMessage);
                    }
                    else
                    {
                        Disconnect("Read stream failed with error: " + ex.Message);
                    }
                }
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            Disconnect("Disconnected");
        }


        /// <summary>
        /// Cleans up the socket and DataWriter and reset the UI
        /// </summary>
        /// <param name="disconnectReason"></param>
        private void Disconnect(string disconnectReason)
        {
            if (chatWriter != null)
            {
                chatWriter.DetachStream();
                chatWriter = null;
            }


            if (chatService != null)
            {
                chatService.Dispose();
                chatService = null;
            }
            lock (this)
            {
                if (chatSocket != null)
                {
                    chatSocket.Dispose();
                    chatSocket = null;
                }
            }

            UpdateStatus(disconnectReason, NotifyType.StatusMessage);
            ResetMainUI();
        }

        private void SetChatUI(string serviceName, string deviceName)
        {
            UpdateStatus("Connected", NotifyType.StatusMessage);
            ServiceName.Text = "Service Name: " + serviceName;
            DeviceName.Text = "Connected to: " + deviceName;
            RunButton.IsEnabled = false;
            ConnectButton.Visibility = Visibility.Collapsed;
            RequestAccessButton.Visibility = Visibility.Visible;
            resultsListView.IsEnabled = false;
            resultsListView.Visibility = Visibility.Collapsed;
            ChatBox.Visibility = Visibility.Visible;
        }

        private void UpdateStatus(string msg, NotifyType statusMessage)
        {
            var t = Task.Run(async () =>
            {
                await UpdateAsync(msg, statusMessage);
            });
        }

        private async Task UpdateAsync(string msg, NotifyType statusMessage)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {

                status.Text = string.Format("{0}: {1}", statusMessage,
                msg);
            });
        }

        private void ResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePairingButtons();
        }

        private void UpdatePairingButtons()
        {
            RfcommChatDeviceDisplay deviceDisp = (RfcommChatDeviceDisplay)resultsListView.SelectedItem;

            if (null != deviceDisp)
            {
                ConnectButton.IsEnabled = true;
            }
            else
            {
                ConnectButton.IsEnabled = false;
            }
        }

        string recvdtxt = "";
        bool listening = false;

        private async Task ReadAsync(string msg)
        {

            if (msg != "")
            {
                try
                {


                    string currenbtRecvdText = msg;
                    ////await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    ////    recvdText.Text = currenbtRecvdText;
                    ////));


                    if (_Mode == Mode.JustConnected)
                    {
                        if (cFineStructure == msg[0])
                        {
                            _Mode = Mode.Connected;
                            SendCh('0');
                        }
                    }
                    else if (_Mode == Mode.Connected)
                    {
                        if ('0' == msg[0])
                        {
                            _Mode = Mode.ACK0;
                            recvdtxt = "";
                            SendCh('1');
                        }
                    }
                    else if (_Mode == Mode.ACK0)
                    {
                        if ('2' == msg[0])
                        {
                            _Mode = Mode.ACK2;
                            recvdtxt = "";
                            SendCh('3');
                        }
                    }
                    else if (_Mode == Mode.ACK2)
                    {
                        if ('4' == msg[0])
                        {
                            _Mode = Mode.ACK4;
                            SendCh('5');
                            //status.Text="Ready for Config. Press [Back] then on MainPage press [Load App Menu]";
                        }
                    }
                    else if (_Mode == Mode.ACK4)
                    {
                        if ('!' == msg[0])
                        {
                            _Mode = Mode.Ready;
                            SendCh('/');
                            //status.Text="Ready for Config. Press [Back] then on MainPage press [Load App Menu]";
                        }
                    }
                    else if (_Mode == Mode.Ready)
                    {
                        if ('/' == msg[0])
                        {
                            _Mode = Mode.Json1;
                            //Send Config
                            await SendMsgTask(
"{\"Config\":[ [ { \"iWidth\": 120 },{ \"iHeight\": 100 },{ \"iSpace\": 5 },{ \"iCornerRadius\": 10 },{ \"iRows\": 2 },{ \"iColumns\": 5 },{ \"sComPortId\": \"\\\\\\\\?\\\\USB#VID_26BA&PID_0003#5543830353935161A112#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"sFTDIComPortId\": \"\\\\\\\\?\\\\FTDIBUS#VID_0403+PID_6001+FTG71BUIA#0000#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"iComportConnectDeviceNo\": -1 },{ \"iFTDIComportConnectDeviceNo\": 1 },{ \"sUseSerial\": \"BT\" } ] ] }~");
                        }
                    }
                    else if (_Mode == Mode.Json1)
                    {
                        if ('~' == msg[0])
                        {
                            _Mode = Mode.Json2;
                            //Send Menu
                            await SendMsgTask(
"{\"MainMenu\":[ [ \"Something else\", \"Unload\", \"Show full list\", \"Setup Sockets\", \"The quick brown fox jumps over the lazy dog\" ],[ \"First\", \"Back\", \"Next\", \"Last\", \"Show All\" ] ] }~");
                            _Mode = Mode.Running;
                        }
                    }
                    else if (_Mode == Mode.Running)
                    {

                        if (listening)
                            switch (msg[0])
                            {
                                case '^':
                                    listening = false;
                                    break;
                                default:
                                    //Do app stuff here. For now just echo chars sent
                                    SendCh(msg[0]);
                                    break;
                            }
                    }


                }
                catch (Exception ex)
                {
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => status.Text += "\r\n" + "Lost connection:\r\n" + ex.Message);
                    listening = false;
                }

            }
        }

        //private Task WriteAsync(string v)
        //{
        //    throw new NotImplementedException();
        //}

        private void SendCh(char ch)
        {
            var t = Task.Run(async () =>
            {
                await SendMsgTask("" + ch);
            });
        }

        private async Task SendMsgTask(string msg)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    MessageTextBox.Text = msg;
                    SendMessage();
                }
            );
        }
    }

        public class RfcommChatDeviceDisplay : INotifyPropertyChanged
    {
        private DeviceInformation deviceInfo;

        public RfcommChatDeviceDisplay(DeviceInformation deviceInfoIn)
        {
            deviceInfo = deviceInfoIn;
            UpdateGlyphBitmapImage();
        }

        public DeviceInformation DeviceInformation
        {
            get
            {
                return deviceInfo;
            }

            private set
            {
                deviceInfo = value;
            }
        }

        public string Id
        {
            get
            {
                return deviceInfo.Id;
            }
        }

        public string Name
        {
            get
            {
                return deviceInfo.Name;
            }
        }

        public BitmapImage GlyphBitmapImage
        {
            get;
            private set;
        }

        public void Update(DeviceInformationUpdate deviceInfoUpdate)
        {
            deviceInfo.Update(deviceInfoUpdate);
            UpdateGlyphBitmapImage();
        }

        private async void UpdateGlyphBitmapImage()
        {
            DeviceThumbnail deviceThumbnail = await deviceInfo.GetGlyphThumbnailAsync();
            BitmapImage glyphBitmapImage = new BitmapImage();
            await glyphBitmapImage.SetSourceAsync(deviceThumbnail);
            GlyphBitmapImage = glyphBitmapImage;
            OnPropertyChanged("GlyphBitmapImage");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }




    }
}

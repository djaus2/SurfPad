using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using SocketClientServer;
using Windows.UI.Core;
using Windows.Networking.Connectivity;


namespace SoxStreams
{
    public sealed partial class MainPage : Page
    {
        #region SocketClasses Instances
        public SocketListenerSvr SocketListenerSvr;
        public SocketClient SocketClient;
        public SocketSend_ClientToListner SocketSend_ClientToLister;
        #endregion

        #region Properties
        private string _remoteSocketListenerSvrName = "192.168.0.27";
        public string RemoteSocketListenerSvrName
        {
            get
            {
                return _remoteSocketListenerSvrName;
            }
            set
            {
                _remoteSocketListenerSvrName = value;       
            }
        }

        private string _port = "22112";
        public string Port
        {
            get
            { return _port; }
            set
            {
                _port = value;
                prevPortStr = value;
            }
        }

        private string prevPortStr = "22112";

        public bool BindToAny { get; set; } = true;
        public string Param { get; private set; } = "0";

        public string Log
        {
            set
            {
                LogTb.Text = value + "\r\n" + LogTb.Text;
            }
        }

        public object LocalSocketListenerSvrName { get; private set; }
        public string ZoomFactor { get; private set; } = "0.0";
        public string Passkey { get; private set; }="22113A22113";
        public bool IsListener { get; private set; } = false;
        #endregion

        public MainPage()
        {
            this.InitializeComponent();
            Port = App.Port;
            RemoteSocketListenerSvrName = App.RemoteListenerName;
            SocketClientServer.Sox.Start();
            SocketClientServer.Sox.Instance.PropertyChanged += Instance_PropertyChanged;
            LocalListenerTb.Text = CurrentIPAddress();
        }

        /// <summary>
        /// Get IPAddress of this system
        /// </summary>
        /// <returns></returns>
        public static string CurrentIPAddress()
        {
            var icp = NetworkInformation.GetInternetConnectionProfile();

            if (icp != null && icp.NetworkAdapter != null)
            {
                var hostname2 =     NetworkInformation.GetHostNames();
                int count = hostname2.Count();
                foreach (Windows.Networking.HostName hnn in hostname2)
                {
                }
                var hostname = from hn in hostname2 where ((hn.IPInformation != null) 
                               && (hn.Type== Windows.Networking.HostNameType.Ipv4) 
                               && (hn.IPInformation.NetworkAdapter != null)) select hn;

                if (hostname != null)
                {
                    // the ip address
                    return hostname.First().CanonicalName;
                }
            }

            return "127.0.0.1"; // string.Empty;
        }

        private async void Instance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            string msg = SocketClientServer.Sox.Instance.LogMsg;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                Log = msg;
            });
        }


        #region CameraKeypadConstructs
        Dictionary<string, string> KeypadKeys = new Dictionary<string, string>() {
            { "1", "C" },
            { "2", "Z" },
            { "3", "V" },
            { "4", "N" },
            { "5", "A" },
            { "6", "F" },
            { "7", "a" },
            { "8", "m" },
            { "9", "G" },
            { "*", "Con" },
            { "0", "T" },
            { "#", "Cls" },
            { "T", "TKy" },
            { "C", "Cnl" }
        };

        Dictionary<string, string> Actions = new Dictionary<string, string>() {
            { "1", "Toggle Camera" },
            { "2", "Zoom" },
            { "3", "Video" },
            { "4", "None" },
            { "5", "Auto" },
            { "6", "Flash" },
            { "7", "auto Focus" },
            { "8", "manual Focus" },
            { "9", "GPS" },
            { "*", "Connect" },
            { "0", "Take Photo" },
            { "#", "Close" },
            { "T", "Toggle Keys" },
            { "C", "Cancel" },
        };



        List<string> DoNotsend = new List<string>(){"T", "*", "#","C"};
        List<string> ParamRequired = new List<string>() { "f", "Z"};

        enum ButtonState
        { num,act,lit}

        #endregion


        #region KeypadHandler

        ButtonState buttonState = ButtonState.num;
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                Button butt = (Button)sender;
                if (butt != null)
                {
                    object otag = butt.Tag;
                    if (otag != null)
                    {
                        string tag = (string)otag;
                        if (KeypadKeys.Keys.Contains(tag))
                        {
                            string action = KeypadKeys[tag];
                            if (!DoNotsend.Contains(tag))
                                SendAction(action);
                            else
                            {
                                switch (tag)
                                {
                                    case "T":
                                        ToggleKeypadText();
                                        break;
                                    case "9":
                                        //Do something else (Calls GPS in phone)
                                        break;
                                    case "*":
                                        await StartAndConnectSocket();
                                        break;
                                    case "#":
                                        DisconnectFromListener();
                                        LogTb.Text = "";
                                        break;
                                    case "C":
                                        if (SocketClientServer.Sox.AppMode == AppMode.ListenerStarted)
                                            DisconnectFromListener();
                                        else
                                            SocketClientServer.Sox.CancelActions();
                                        break;

                                }
                            }
                        }
                    }

                }
            }
        }

        private void ToggleKeypadText()
        {
            SocketClientServer.Sox.Instance.Log(buttonState, NotifyType.ErrorMessage);
            foreach (var ctrl in Keypad.Children)
            {
                if (ctrl is Button)
                {
                    Button butt = (Button)ctrl;
                    if (butt != null)
                    {
                        object otag = butt.Tag;
                        if (otag != null)
                        {
                            string tag = (string)otag;
                            switch (buttonState)
                            {
                                case ButtonState.num:
                                    if (KeypadKeys.Keys.Contains(tag))
                                    {
                                        butt.Content = KeypadKeys[tag];
                                    }
                                    butt.Width = 45;
                                    Col1.Width = new GridLength(50);
                                    Col2.Width = new GridLength(50);
                                    Col3.Width = new GridLength(50);
                                    Col4.Width = new GridLength(50);
                                    break;
                                case ButtonState.lit:
                                    butt.Content = tag;
                                    butt.Width = 45;
                                    Col1.Width = new GridLength(50);
                                    Col2.Width = new GridLength(50);
                                    Col3.Width = new GridLength(50);
                                    Col4.Width = new GridLength(50);
                                    break;
                                case ButtonState.act:
                                    if (Actions.Keys.Contains(tag))
                                    {
                                        butt.Content = Actions[tag];
                                    }
                                    butt.Width = 125;
                                    Col1.Width = new GridLength(130);
                                    Col2.Width = new GridLength(130);
                                    Col3.Width = new GridLength(130);
                                    Col4.Width = new GridLength(130);
                                    break;
                            }
                        }
                    }
                }

            }

            switch (buttonState)
            {
                case ButtonState.num:
                    buttonState = ButtonState.act;
                    break;
                case ButtonState.act:
                    buttonState = ButtonState.lit;
                    break;
                case ButtonState.lit:
                    buttonState = ButtonState.num;
                    break;

            }
        }

    
        #endregion


        #region Sockets
        private void Start_LocalListenerSvr(object sender, RoutedEventArgs e)
        {
            if (SocketListenerSvr == null)
            {
                SocketListenerSvr = SocketListenerSvr.CreateNew(RemoteSocketListenerSvrName, Port, BindToAny, Passkey);
            }
            if (SocketListenerSvr != null)
                SocketListenerSvr.StartListener();
        }

        private void DisconnectFromListener()
        {

            try
            {
                SocketClose.CloseSockets();
                SocketSend_ClientToLister = null;
                SocketClient = null;
                SocketListenerSvr = null;
            }
            catch (Exception ex)
            {
                SocketClientServer.Sox.Instance.Log(ex.Message);
            }
        }

        private async Task StartAndConnectSocket()
        {
            if (!IsListener)
            {
                if (SocketClient == null)
                    SocketClient =  SocketClient.CreateNew(RemoteSocketListenerSvrName, Port, BindToAny, Passkey);
                if(SocketClient != null)
                    await (SocketClient.ConnectSocket());
            }
            else
            {
                if (SocketListenerSvr == null)
                {
                    SocketListenerSvr = SocketListenerSvr.CreateNew(RemoteSocketListenerSvrName, Port, BindToAny, Passkey);
                    SocketListenerSvr.ListenerConnection += SocketListenerSvr_ListenerConnection;
                }
                if (SocketListenerSvr != null)
                    SocketListenerSvr.StartListener();
            }
        }

        private void SocketListenerSvr_ListenerConnection(char command, string param)
        {
            Log =
                string.Format("(MainPage)Listener Connection Event Occurred Command: '{0}' Param: \"{1}\".", command, param);
        }

        private async void SendAction(string action)
        {
            if (SocketSend_ClientToLister == null)
            {
                SocketSend_ClientToLister = SocketSend_ClientToListner.CreateNew(Passkey);
            }
            if (SocketSend_ClientToLister != null)
            {
                if (ParamRequired.Contains(action))
                    action += Param;
                bool res = await  SocketSend_ClientToLister.Send(action);
                if (res)
                    SocketClientServer.Sox.Instance.Log("Sent: " + action, NotifyType.StatusMessage);
                else
                    SocketClientServer.Sox.Instance.Log("Send of: " + action + " was cancelled", NotifyType.StatusMessage);
            }
        }


        #endregion




        #region UIControlsChangedValues
        private void Port_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SocketClientServer.Sox.IsNumeric_uint(PortTb.Text))
            {
                Port = PortTb.Text;
            }
            else
            {
                PortTb.IsEnabled = false;
                PortTb.Text = prevPortStr;
                PortTb.IsEnabled = true;
            }
        }
        
        private void BindToAnyRemoteCB_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (BindToAnyRemoteCB.IsChecked == true)
                BindToAny = true;
            else
                BindToAny = false;
        }


        private void RemoteListenerTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            RemoteSocketListenerSvrName = RemoteListenerTb.Text;
        }

        private void LocalListenerTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            LocalSocketListenerSvrName = LocalListenerTb.Text;
        }

        private void ZoomFactorTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            ZoomFactor = ZoomFactorTb.Text;
        }

        private void PasskeyTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            Passkey = PasskeyTb.Text;
        }

        private void IsPhoneCB_Checked(object sender, RoutedEventArgs e)
        {
            if (IsPhoneCB.IsChecked == true)
                IsListener = true;
            else
                IsListener = false;
            SocketClientServer.SocketClose.CloseSockets();
        }

        #endregion


    }
}

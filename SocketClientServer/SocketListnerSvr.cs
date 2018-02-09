using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;


namespace SocketClientServer
{

    //Used with ListenerConnection event which is fired when a valid command is received:
    public delegate void ListenerConnection(char command, string param);

    public class SocketListenerSvr
    {

        public string PortOrServicenameForListener = "22112";
        public string RemoteSocketListenerSvrName = "";

        public event ListenerConnection ListenerConnection;

        

        // List containing all available local HostName endpoints
        private List<LocalHostItem> localHostItems = new List<LocalHostItem>();

        private bool BindToAny_IsChecked = true;
        private bool BindToAddress_IsChecked = false;
        private bool BindToAdapter_IsChecked = false;

        //This must be recvd as the prefix of any command message that is sent to this service
        public string Passkey { get; private set; }

        //We only use this constructor:
        public SocketListenerSvr(string remoteSocketListenerSvrName, string port, bool bindtoany, string passkey)
        {
            Passkey = passkey;
            if (Sox.IsNumeric_uint(port))
            {
                BindToAny_IsChecked = bindtoany;
                RemoteSocketListenerSvrName = remoteSocketListenerSvrName;
                PortOrServicenameForListener = port;
                //Hook up a local ListenerConnection Event handler 
                this.ListenerConnection += SocketListenerSvr_ListenerConnection;
            }
            else
                Sox.Instance.Log(
                    "Invalid Port string for listener service.",
                    NotifyType.ErrorMessage);
        }

        //A local handler of the ListenerConnection Event
        private void SocketListenerSvr_ListenerConnection(char command, string param)
        {
            Sox.Instance.Log(
                string.Format("(In Listener)Listener Connection Event Occurred Command: '{0}' Param: \"{1}\".",command,param),
                NotifyType.StatusMessage);
        }


        //Call this to create a new instance of this listener class, rather than the constructor
        public static SocketListenerSvr CreateNew(string remoteSocketListenerSvrName, string port, bool bindtoany, string passkey)
        {
            SocketListenerSvr _SocketListenerSvr = null;
            if (Sox.AppMode == AppMode.None)
            {
                if (Sox.IsNumeric_uint(port))
                {
                    _SocketListenerSvr = new SocketListenerSvr(remoteSocketListenerSvrName,  port,  bindtoany,  passkey);
                    if (_SocketListenerSvr != null)
                        Sox.AppMode = AppMode.NewListner;
                }
                else
                    Sox.Instance.Log(
                        "Invalid Port string for listener service.",
                        NotifyType.ErrorMessage);
            }
            else
                Sox.Instance.Log(
                    "Unable to create SocketListner. Incorrect AppMode.",
                    NotifyType.ErrorMessage);
      
            return _SocketListenerSvr;
        }




        /// <summary>
        /// This is the click handler for the 'StartListener' button.
        /// </summary>
        /// <param name="sender">Object for which the event was generated.</param>
        /// <param name="e">Event's parameters.</param>
        public async void StartListener()
        {
            if (Sox.AppMode != AppMode.NewListner)
            {
                if (Sox.AppMode == AppMode.ListenerStarted)
                    Sox.Instance.Log(
                        "Start listening failed with warning: App Listener Socket already started",
                        NotifyType.WarningMessage);
                else
                    Sox.Instance.Log(
                        "Start listening failed with error: App not in Listner Mode",
                        NotifyType.ErrorMessage);
            }
            else
            {
                // Overriding the listener here is safe as it will be deleted once all references to it are gone.
                // However, in many cases this is a dangerous pattern to override data semi-randomly (each time user
                // clicked the button) so we block it here.
                if (CoreApplication.Properties.ContainsKey("listener"))
                {
                    Sox.Instance.Log(
                        "This step has already been executed.",
                        NotifyType.ErrorMessage);
                    return;
                }

                if (String.IsNullOrEmpty(PortOrServicenameForListener))
                {
                    Sox.Instance.Log("Please provide a service name.", NotifyType.ErrorMessage);
                    return;
                }

                CoreApplication.Properties.Remove("serverAddress");


                LocalHostItem selectedLocalHost = null;
                if ((BindToAddress_IsChecked == true) || (BindToAdapter_IsChecked == true))
                {
                    selectedLocalHost = new LocalHostItem(RemoteSocketListenerSvrName);
                    if (selectedLocalHost == null)
                    {
                        Sox.Instance.Log("Please select an address / adapter.", NotifyType.ErrorMessage);
                        return;
                    }

                    // The user selected an address. For demo purposes, we ensure that connect will be using the same
                    // address.
                    CoreApplication.Properties.Add("serverAddress", selectedLocalHost.LocalHost.CanonicalName);
                }

                StreamSocketListener listener = new StreamSocketListener();
                listener.ConnectionReceived += OnConnection;

                // If necessary, tweak the listener's control options before carrying out the bind operation.
                // These options will be automatically applied to the connected StreamSockets resulting from
                // incoming connections (i.e., those passed as arguments to the ConnectionReceived event handler).
                // Refer to the StreamSocketListenerControl class' MSDN documentation for the full list of control options.
                listener.Control.KeepAlive = false;



                // Start listen operation.
                try
                {
                    if (BindToAny_IsChecked == true)
                    {
                        // Don't limit traffic to an address or an adapter.
                        Sox.StartToken();
                        await listener.BindServiceNameAsync(PortOrServicenameForListener).AsTask(Sox.CancelToken);
                        Sox.Instance.Log("Listening", NotifyType.StatusMessage);
                    }
                    else if (BindToAddress_IsChecked == true)
                    {
                        // Try to bind to a specific address.
                        Sox.StartToken();
                        await listener.BindEndpointAsync(selectedLocalHost.LocalHost, PortOrServicenameForListener).AsTask(Sox.CancelToken);
                        Sox.Instance.Log(
                            "Listening on address " + selectedLocalHost.LocalHost.CanonicalName,
                            NotifyType.StatusMessage);
                        Sox.Instance.Log(
                            "On Port " + PortOrServicenameForListener,
                            NotifyType.StatusMessage);
                    }
                    else if (BindToAdapter_IsChecked == true)
                    {
                        // Try to limit traffic to the selected adapter.
                        // This option will be overridden by interfaces with weak-host or forwarding modes enabled.
                        NetworkAdapter selectedAdapter = selectedLocalHost.LocalHost.IPInformation.NetworkAdapter;

                        // For demo purposes, ensure that we use the same adapter in the client connect scenario.
                        CoreApplication.Properties.Add("adapter", selectedAdapter);
                        Sox.StartToken();
                        await listener.BindServiceNameAsync(
                            PortOrServicenameForListener,
                            SocketProtectionLevel.PlainSocket,
                            selectedAdapter).AsTask(Sox.CancelToken);

                        Sox.Instance.Log(
                            "Listening on adapter " + selectedAdapter.NetworkAdapterId,
                            NotifyType.StatusMessage);
                    }
                    // Save the socket, so subsequent steps can use it.
                    CoreApplication.Properties.Add("listener", listener);
                    Sox.AppMode = AppMode.ListenerStarted;
                }
                catch (TaskCanceledException)
                {
                    Sox.Instance.Log("Cancelled.", NotifyType.StatusMessage);
                    Sox.AppMode = AppMode.NewListner;
                    if (CoreApplication.Properties.ContainsKey("listener"))
                    {
                        // Remove the listener from the list of application properties as it wasn't connected.
                        CoreApplication.Properties.Remove("listener");
                    }
                }

                catch (Exception exception)
                {
                    if (CoreApplication.Properties.ContainsKey("listener"))
                    {
                        // Remove the listener from the list of application properties as it wasn't connected.
                        CoreApplication.Properties.Remove("listener");
                    }
                    Sox.AppMode = AppMode.NewListner;

                    // If this is an unknown status it means that the error is fatal and retry will likely fail.
                    if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                    {
                        throw;
                    }

                    Sox.Instance.Log(
                        "Start listening failed with error: " + exception.Message,
                        NotifyType.ErrorMessage);
                }
            }
        }

        /// <summary>
        /// Invoked once a connection is accepted by StreamSocketListener.
        /// </summary>
        /// <param name="sender">The listener that accepted the connection.</param>
        /// <param name="args">Parameters associated with the accepted connection.</param>
        private async void OnConnection(
            StreamSocketListener sender,
            StreamSocketListenerConnectionReceivedEventArgs args)
        {
            DataReader reader = new DataReader(args.Socket.InputStream);
            try
            {
                //Windows.Storage.Streams.Buffer Out = new Windows.Storage.Streams.Buffer(100);
                //await args.Socket.OutputStream.WriteAsync(Out);
                while (true)
                {
                    // Read first 4 bytes (length of the subsequent string).
                    Sox.StartToken();
                    uint sizeFieldCount = await reader.LoadAsync(sizeof(uint)).AsTask(Sox.CancelToken);
                    if (sizeFieldCount != sizeof(uint))
                    {
                        // The underlying socket was closed before we were able to read the whole data.
                        return;
                    }

                    // Read the string.
                    uint stringLength = reader.ReadUInt32();
                    Sox.StartToken();
                    uint actualStringLength = await reader.LoadAsync(stringLength).AsTask(Sox.CancelToken);
                    if (stringLength != actualStringLength)
                    {
                        // The underlying socket was closed before we were able to read the whole data.
                        return;
                    }
                    string data = reader.ReadString(actualStringLength);
                    await NotifyUserFromAsyncThread(
                            String.Format("Received data: \"{0}\"", data),
                            NotifyType.StatusMessage);
                    //data should be the string Passkey + comamnd char + Parameter string (can be "")
                    //If of that format that get the command and parameter and fire the ListenerConnection Event
                    if (data.Length > 0)
                    {
                        if (data.Length >= Passkey.Length)
                        {
                            if (Passkey == data.Substring(0, Passkey.Length))
                            {
                                char command = data[Passkey.Length];
                                string param = "";
                                if (data.Length> Passkey.Length +1)
                                    param = data.Substring(Passkey.Length + 1);
                                string msg = string.Format("(InConnection) Command: '{0}' Param= \"{1}\" recvd", command, param);
                                Sox.Instance.Log(msg, NotifyType.StatusMessage);
                                //Fire the ListenerConnection Event
                                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                    () =>
                                    {
                                        ListenerConnection.Invoke(command, param);
                                    }).AsTask(Sox.CancelToken);
                            }
                            else
                                Sox.Instance.Log("Null Command recvd.", NotifyType.StatusMessage);
                        }
                        else
                            Sox.Instance.Log("Null Message recvd.", NotifyType.ErrorMessage);
                    }
                    else
                        Sox.Instance.Log("Null data recvd.", NotifyType.ErrorMessage);
                }
            }
            catch (TaskCanceledException)
            {
                Sox.Instance.Log("Cancelled.", NotifyType.StatusMessage);
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                await NotifyUserFromAsyncThread(
                    "Read stream failed with error: " + exception.Message,
                    NotifyType.ErrorMessage);
            }
        }


        private async Task NotifyUserFromAsyncThread(string strMessage, NotifyType type)
        {
            Sox.StartToken();
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    Sox.Instance.Log(strMessage, type);
                }).AsTask(Sox.CancelToken);
        }
    }

    /// <summary>
    /// Helper class describing a NetworkAdapter and its associated IP address
    /// </summary>
    class LocalHostItem
    {
        public string DisplayString
        {
            get;
            private set;
        }

        public HostName LocalHost
        {
            get;
            private set;
        }

        public void SetLocalHost( string hostName)
        {
            LocalHost = new HostName(hostName);
        }

        public LocalHostItem(string localHostNameStr)
        {
            LocalHost = new HostName(localHostNameStr);
            if (LocalHost == null)
            {
                throw new ArgumentNullException("localHostName");
            }

            if (LocalHost.IPInformation == null)
            {
                throw new ArgumentException("Adapter information not found");
            }

            
            this.DisplayString = "Address: " + LocalHost.DisplayName +
                " Adapter: " + LocalHost.IPInformation.NetworkAdapter.NetworkAdapterId;
        }


        public LocalHostItem(HostName localHostName)
        {
            if (localHostName == null)
            {
                throw new ArgumentNullException("localHostName");
            }

            if (localHostName.IPInformation == null)
            {
                throw new ArgumentException("Adapter information not found");
            }

            this.LocalHost = localHostName;
            this.DisplayString = "Address: " + localHostName.DisplayName +
                " Adapter: " + localHostName.IPInformation.NetworkAdapter.NetworkAdapterId;
        }
    }
}


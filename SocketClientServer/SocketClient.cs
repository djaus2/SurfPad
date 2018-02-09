
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SocketClientServer
{
    /// <summary>
    /// A page for second scenario.
    /// </summary>
    public class SocketClient
    {

        // Limit traffic to the same adapter that the listener is using to demonstrate client adapter-binding.
        NetworkAdapter adapter = null;

        string RemoteSocketListenerSvrName = "";
        string PortOrServicenameForListener = "";

        public string Passkey { get; private set; }

        //string HostNameForConnect { get; set; } = "";

        //public string ServiceNameForConnect { get; set; } = "";

        public SocketClient()
        {
    
        }

        public SocketClient(string remoteSocketListenerSvrName, string port, bool bindtoany)
        {
            if (Sox.IsNumeric_uint(port))
            {
                RemoteSocketListenerSvrName = remoteSocketListenerSvrName;
                PortOrServicenameForListener = port;
            }
            else
                Sox.Instance.Log(
                    "Invalid Port string for client.",
                    NotifyType.ErrorMessage);
        }

        public SocketClient(string remoteSocketListenerSvrName, string port, bool bindtoany, string passkey)
        {
            Passkey = passkey;
            if (Sox.IsNumeric_uint(port))
            {
                RemoteSocketListenerSvrName = remoteSocketListenerSvrName;
                PortOrServicenameForListener = port;
            }
            else
                Sox.Instance.Log(
                    "Invalid Port string for client.",
                    NotifyType.ErrorMessage);
        }

        public static SocketClient CreateNew(string remoteSocketListenerSvrName, string port, bool bindtoany, string passkey)
        {
            SocketClient _SocketClient = null;
            if (Sox.AppMode == AppMode.None)
            {
                
                if (Sox.IsNumeric_uint(port))
                {
                    _SocketClient = new SocketClient(remoteSocketListenerSvrName, port, bindtoany, passkey);
                    if (_SocketClient != null)
                        Sox.AppMode = AppMode.NewClient;
                }
                else
                    Sox.Instance.Log(
                        "Invalid Port string for client.",
                        NotifyType.ErrorMessage);
            }
            else
                Sox.Instance.Log(
                     "Unable to create SocketClient: Incorrect AppMode.",
                     NotifyType.ErrorMessage);
            return _SocketClient;
        }



        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        public void SetUp()
        {
            // Make sure we're using the correct server address if an adapter was selected in scenario 1.
            object serverAddress;
            if (CoreApplication.Properties.TryGetValue("serverAddress", out serverAddress))
            {
                if (serverAddress is string)
                {
                    //HostNameForConnect = rootPage.listener;// serverAddress as string;
                }
            }

            adapter = null;
            object networkAdapter;
            if (CoreApplication.Properties.TryGetValue("adapter", out networkAdapter))
            {
                adapter = (NetworkAdapter)networkAdapter;
            }
        }

        /// <summary>
        /// This is the click handler for the 'ConnectSocket' button.
        /// </summary>
        /// <param name="sender">Object for which the event was generated.</param>
        /// <param name="e">Event's parameters.</param>
        public async Task ConnectSocket()
        {
            if (Sox.AppMode != AppMode.NewClient)
            {
                if (Sox.AppMode == AppMode.ClientSocketConnected)
                    Sox.Instance.Log(
                        "Start client failed with warning: App Client Socket already connected",
                        NotifyType.WarningMessage);
                else
                    Sox.Instance.Log(
                        "Start client failed with error: App not in new Client Mode",
                        NotifyType.ErrorMessage);
            }
            else
            { 
                HostName hostName = null;
                if (CoreApplication.Properties.ContainsKey("clientSocket"))
                {
                    Sox.Instance.Log(
                        "This step has already been executed.",
                        NotifyType.ErrorMessage);
                    //return;
                }
                else
                {
                    if (string.IsNullOrEmpty(PortOrServicenameForListener)) //(String.IsNullOrEmpty(ServiceNameForConnect))
                    {
                        Sox.Instance.Log("Please provide a service name.", NotifyType.ErrorMessage);
                        //return;
                    }
                    else
                    {
                        bool returnNow = false;
                        if (string.IsNullOrEmpty(RemoteSocketListenerSvrName)) //(String.IsNullOrEmpty(ServiceNameForConnect))
                        {
                            Sox.Instance.Log("Please provide a server name.", NotifyType.ErrorMessage);
                            //return;
                            returnNow = true;
                        }
                        // By default 'HostNameForConnect' is disabled and host name validation is not required. When enabling the
                        // text box validating the host name is required since it was received from an untrusted source
                        // (user input). The host name is validated by catching ArgumentExceptions thrown by the HostName
                        // constructor for invalid input.
                        else
                        {


                            try
                            {
                                hostName = new HostName(RemoteSocketListenerSvrName);// HostNameForConnect.Text);
                            }
                            catch (TaskCanceledException)
                            {
                                Sox.Instance.Log("Canceled.", NotifyType.StatusMessage);
                            }
                            catch (ArgumentException)
                            {
                                Sox.Instance.Log("Error: Invalid host name.", NotifyType.ErrorMessage);
                                //return;
                                returnNow = true;
                            }
                        }
                        if (!returnNow)
                        {

                            StreamSocket socket = new StreamSocket();

                            // If necessary, tweak the socket's control options before carrying out the connect operation.
                            // Refer to the StreamSocketControl class' MSDN documentation for the full list of control options.
                            socket.Control.KeepAlive = false;


                            try
                            {
                                if (adapter == null)
                                {

                                    Sox.Instance.Log("Connecting to: " + hostName.DisplayName, NotifyType.StatusMessage);
                                    Sox.Instance.Log("On Port: " + PortOrServicenameForListener, NotifyType.StatusMessage);

                                    Sox.StartToken();
                                    // Connect to the server (by default, the listener we created in the previous step).
                                    await socket.ConnectAsync(hostName, PortOrServicenameForListener).AsTask(Sox.CancelToken);

                                    Sox.Instance.Log("Connected", NotifyType.StatusMessage);
                                    CoreApplication.Properties.Add("clientSocket", socket);
                                }
                                else
                                {
                                    Sox.Instance.Log(
                                        "Connecting to: " + RemoteSocketListenerSvrName +
                                        " using network adapter " + adapter.NetworkAdapterId,
                                        NotifyType.StatusMessage);

                                    // Connect to the server (by default, the listener we created in the previous step)
                                    // limiting traffic to the same adapter that the user specified in the previous step.
                                    // This option will be overridden by interfaces with weak-host or forwarding modes enabled.
                                    Sox.StartToken();
                                    await socket.ConnectAsync(
                                        hostName,
                                        PortOrServicenameForListener,//                        ServiceNameForConnect.Text, 
                                        SocketProtectionLevel.PlainSocket,
                                        adapter).AsTask(Sox.CancelToken);

                                    // Save the socket, so subsequent steps can use it.
                                   

                                    Sox.Instance.Log(
                                        "Connected using network adapter " + adapter.NetworkAdapterId,
                                        NotifyType.StatusMessage);
                                }

                                // Mark the socket as connected. Set the value to null, as we care only about the fact that the 
                                // property is set.
                                CoreApplication.Properties.Add("connected", null);
                            }
                            catch (TaskCanceledException)
                            {
                                if (CoreApplication.Properties.ContainsKey("connected"))
                                {
                                    CoreApplication.Properties.Remove("connected");
                                }
                                Sox.AppMode = AppMode.NewClient;
                                Sox.Instance.Log("Cancelled.", NotifyType.StatusMessage);
                                
                            }
                            catch (Exception exception)
                            {
                                if (CoreApplication.Properties.ContainsKey("connected"))
                                {
                                    CoreApplication.Properties.Remove("connected");
                                }
                                Sox.AppMode = AppMode.NewClient;
                                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                                {
                                    throw;
                                }

                                Sox.Instance.Log("Connect failed with error: " + exception.Message, NotifyType.ErrorMessage);
                            }
                        }
                    }
                }
                }
            }
        }


 
}


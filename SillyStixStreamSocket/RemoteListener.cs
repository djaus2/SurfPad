using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using SelfyStix.Interface;
using System.Threading;

namespace SelfyStix
{
    class RemoteListener
    {
    }

    public enum NotifyType
    {
        StatusMessage,
        ErrorMessage
    };

    public class Listener
    {
        Camera.CameraPage Camera = null;
        public string ServiceNameForListener = "22112";

        // List containing all available local HostName endpoints
        private List<LocalHostItem> localHostItems = new List<LocalHostItem>();

        private bool BindToAny_IsChecked = true;
        private bool BindToAddress_IsChecked = false;
        private bool BindToAdapter_IsChecked = false;

        public Listener()
        {
            BindToAny_IsChecked = true;
            ServiceNameForListener = "22112";
        }

        bool IsNumeric(string val)
        {
            uint value;
            bool res = uint.TryParse(val, out value);
            return res;
        }

        public Listener(string port)
        {
            if (IsNumeric(port))
            {
                BindToAny_IsChecked = true;
                ServiceNameForListener = port;
            }
            else
                System.Diagnostics.Debug.WriteLine(
                    "Invalid Port string for listener service.",
                    NotifyType.ErrorMessage);
        }

        public Listener(Camera.CameraPage camera, string port)
        {
            if (IsNumeric(port))
            {
                BindToAny_IsChecked = true;
                ServiceNameForListener = port;
            }
            else
                System.Diagnostics.Debug.WriteLine(
                    "Invalid Port string for listener service.",
                    NotifyType.ErrorMessage);
            Camera = camera;
        }



        /// <summary>
        /// This is the click handler for the 'StartListener' button.
        /// </summary>
        /// <param name="sender">Object for which the event was generated.</param>
        /// <param name="e">Event's parameters.</param>
        public async void StartListener()
        {
            // Overriding the listener here is safe as it will be deleted once all references to it are gone.
            // However, in many cases this is a dangerous pattern to override data semi-randomly (each time user
            // clicked the button) so we block it here.
            if (CoreApplication.Properties.ContainsKey("listener"))
            {
                System.Diagnostics.Debug.WriteLine(
                    "This step has already been executed. Please move to the next one.",
                    NotifyType.ErrorMessage);
                return;
            }

            if (String.IsNullOrEmpty(ServiceNameForListener))
            {
                System.Diagnostics.Debug.WriteLine("Please provide a service name.", NotifyType.ErrorMessage);
                return;
            }

            CoreApplication.Properties.Remove("serverAddress");


            LocalHostItem selectedLocalHost = null;
            if ((BindToAddress_IsChecked == true) || (BindToAdapter_IsChecked == true))
            {
                selectedLocalHost = null; //ToDo
                if (selectedLocalHost == null)
                {
                    System.Diagnostics.Debug.WriteLine("Please select an address / adapter.", NotifyType.ErrorMessage);
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

            // Save the socket, so subsequent steps can use it.
            CoreApplication.Properties.Add("listener", listener);

            // Start listen operation.
            try
            {
                if (BindToAny_IsChecked == true)
                {
                    // Don't limit traffic to an address or an adapter.
                    await listener.BindServiceNameAsync(ServiceNameForListener);
                    System.Diagnostics.Debug.WriteLine("Listening", NotifyType.StatusMessage);
                }
                else if (BindToAddress_IsChecked == true)
                {
                    // Try to bind to a specific address.
                    await listener.BindEndpointAsync(selectedLocalHost.LocalHost, ServiceNameForListener);
                    System.Diagnostics.Debug.WriteLine(
                        "Listening on address " + selectedLocalHost.LocalHost.CanonicalName,
                        NotifyType.StatusMessage);
                }
                else if (BindToAdapter_IsChecked == true)
                {
                    // Try to limit traffic to the selected adapter.
                    // This option will be overridden by interfaces with weak-host or forwarding modes enabled.
                    NetworkAdapter selectedAdapter = selectedLocalHost.LocalHost.IPInformation.NetworkAdapter;

                    // For demo purposes, ensure that we use the same adapter in the client connect scenario.
                    CoreApplication.Properties.Add("adapter", selectedAdapter);

                    await listener.BindServiceNameAsync(
                        ServiceNameForListener,
                        SocketProtectionLevel.PlainSocket,
                        selectedAdapter);

                    System.Diagnostics.Debug.WriteLine(
                        "Listening on adapter " + selectedAdapter.NetworkAdapterId,
                        NotifyType.StatusMessage);
                }
            }
            catch (Exception exception)
            {
                CoreApplication.Properties.Remove("listener");

                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                System.Diagnostics.Debug.WriteLine(
                    "Start listening failed with error: " + exception.Message,
                    NotifyType.ErrorMessage);
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
                while (true)
                {
                    // Read first 4 bytes (length of the subsequent string).
                    uint sizeFieldCount = await reader.LoadAsync(sizeof(uint));
                    if (sizeFieldCount != sizeof(uint))
                    {
                        // The underlying socket was closed before we were able to read the whole data.
                        return;
                    }

                    // Read the string.
                    uint stringLength = reader.ReadUInt32();
                    uint actualStringLength = await reader.LoadAsync(stringLength);
                    if (stringLength != actualStringLength)
                    {
                        // The underlying socket was closed before we were able to read the whole data.
                        return;
                    }
                    string data = reader.ReadString(actualStringLength);
                    await NotifyUserFromAsyncThread(
                            String.Format("Received data: \"{0}\"", data),
                            NotifyType.StatusMessage);
                    if (data.Length>0)
                    {
                        char command = data[0];
                        string param = data.Substring(1);
                        switch (command)
                        {
                            case 'C':
                                await RemoteControl.RemoteActions(Enums.Item.CameraToggle);
                                break;
                            case 'T':
                                await RemoteControl.RemoteActions(Enums.Item.TakePhoto);
                                break;
                            case 'N':
                                await RemoteControl.RemoteActions(Enums.Item.NoFlash);
                                break;
                            case 'A':
                                await RemoteControl.RemoteActions(Enums.Item.AutoFlash);
                                break;
                            case 'F':
                                await RemoteControl.RemoteActions(Enums.Item.Flash);
                                break;
                            case 'V':
                                await RemoteControl.RemoteActions(Enums.Item.Video);
                                break;
                            case 'a':
                                await RemoteControl.RemoteActions(Enums.Item.AutoFocus);
                                break;
                            case 'm':
                                if (param !="")
                                    await RemoteControl.RemoteActions(Enums.Item.ManualFocus);
                                else
                                    await RemoteControl.RemoteActions(Enums.Item.ManualFocus, param);
                                break;
                            case 'G':
                                await RemoteControl.RemoteActions(Enums.Item.GPS);
                                break;
                            case 'Z':
                                await RemoteControl.RemoteActions(Enums.Item.Zoom,param);
                                break;
                        }

                    }
                    //});
                    // Display the string on the screen. The event is invoked on a non-UI thread, so we need to marshal
                    // the text back to the UI thread.

                }
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
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    System.Diagnostics.Debug.WriteLine(strMessage, type);
                });
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


using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SocketClientServer
{
    /// <summary>
    /// A page for third scenario.
    /// </summary>
    public class SocketSend_ClientToListner: INotifyPropertyChanged
    {

        public SocketSend_ClientToListner()
        {

        }

        public SocketSend_ClientToListner(string passkey)
        {
            Passkey = passkey;
        }

        public static SocketSend_ClientToListner CreateNew(string passkey)
        {

            SocketSend_ClientToListner _SocketSend_ClientToListner = null;
            if (Sox.AppMode == AppMode.ClientSocketConnected)
                Sox.Instance.Log(
                    "Client socket ready to send set up failed: App not client connected mode.",
                    NotifyType.ErrorMessage);
            else
            {
                _SocketSend_ClientToListner = new SocketSend_ClientToListner(passkey);
                if (_SocketSend_ClientToListner != null)
                    Sox.AppMode = AppMode.ClientSocketReadyToSend;
            }
            return _SocketSend_ClientToListner;
        }


        private string _sendOutput = "";
        public string SendOutput {
            get
            {
                return _sendOutput;
            }
            set
            {
                _sendOutput = value;
                RaisePropertyChanged("SendOutput");
            }
        }

        public string Passkey { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

 



        /// <summary>
        /// This is the click handler for the 'SendHello' button.
        /// </summary>
        /// <param name="sender">Object for which the event was generated.</param>
        /// <param name="e">Event's parameters.</param>
        public async Task<bool> Send(string SendData)
        {
            if (Sox.AppMode != AppMode.ClientSocketReadyToSend)
            {
                if (Sox.AppMode == AppMode.ClientSocketConnected)
                    Sox.Instance.Log(
                        "Client send failed with warning: App Client Socket connected but not ready to send.",
                        NotifyType.ErrorMessage);
                else
                    Sox.Instance.Log(
                        "Client send failed with error: App not in new Client Ready To Send Mode",
                        NotifyType.ErrorMessage);
                return false;
            }
            string stringToSend = SendData;// "Hello";

            if (!CoreApplication.Properties.ContainsKey("connected"))
            {
                Sox.Instance.Log("Client not connected.", NotifyType.ErrorMessage);
                return false;
            }
            else
            {
                object outValue;
                StreamSocket socket;
            if (!CoreApplication.Properties.TryGetValue("clientSocket", out outValue))
            {
                Sox.Instance.Log("Client socket not enabled.", NotifyType.ErrorMessage);
                return false;
            }
            else
            {
                socket = (StreamSocket)outValue;

                    // Create a DataWriter if we did not create one yet. Otherwise use one that is already cached.
                    DataWriter writer;
                    if (!CoreApplication.Properties.TryGetValue("clientDataWriter", out outValue))
                    {
                        writer = new DataWriter(socket.OutputStream);
                        CoreApplication.Properties.Add("clientDataWriter", writer);
                    }
                    else
                    {
                        writer = (DataWriter)outValue;
                    }

                    // Write first the length of the string as UINT32 value followed up by the string. 
                    // Writing data to the writer will just store data in memory.

                    string actualStringToSend = Passkey + stringToSend;

                    writer.WriteUInt32(writer.MeasureString(actualStringToSend));
                    writer.WriteString(actualStringToSend);

                    // Write the locally buffered data to the network.
                    try
                    {
                        Sox.StartToken();
                        await writer.StoreAsync().AsTask(Sox.CancelToken);
                        SendOutput = "\"" + stringToSend + "\" sent successfully.";
                    }
                    catch (TaskCanceledException)
                    {
                        Sox.Instance.Log("Canceled.", NotifyType.StatusMessage);
                        return false;
                    }
                    catch (Exception exception)
                    {
                        // If this is an unknown status it means that the error if fatal and retry will likely fail.
                        if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                        {
                            throw;
                        }

                        Sox.Instance.Log("Send failed with error: " + exception.Message, NotifyType.ErrorMessage);
                        return false;
                    }
                }
            }
            return true;
        }


    }
}

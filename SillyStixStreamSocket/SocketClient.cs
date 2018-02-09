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

//using SDKTemplate;
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

namespace SoxStreams
{
    /// <summary>
    /// A page for second scenario.
    /// </summary>
    public class SocketClient
    {
        // A pointer back to the main page.  This is needed if you want to call methods in MainPage such
        // as NotifyUser()
        MainPage rootPage = MainPage.Current;

        // Limit traffic to the same adapter that the listener is using to demonstrate client adapter-binding.
        NetworkAdapter adapter = null;

        //string HostNameForConnect { get; set; } = "";

        //public string ServiceNameForConnect { get; set; } = "";

        public SocketClient()
        {
    
        }

        public SocketClient(MainPage current)
        {
            rootPage = current;
            rootPage.SocketClient = this;
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
            HostName hostName = null;
            if (CoreApplication.Properties.ContainsKey("clientSocket"))
            {
                Util.Log(
                    "This step has already been executed. Please move to the next one.",
                    NotifyType.ErrorMessage);
                //return;
            }
            else
            {
                if (string.IsNullOrEmpty(rootPage.Port)) //(String.IsNullOrEmpty(ServiceNameForConnect))
                {
                    Util.Log("Please provide a service name.", NotifyType.ErrorMessage);
                    //return;
                }
                else
                {
                    bool returnNow = false;
                    if (string.IsNullOrEmpty(rootPage.RemoteSocketListenerSvrName)) //(String.IsNullOrEmpty(ServiceNameForConnect))
                    {
                        Util.Log("Please provide a server name.", NotifyType.ErrorMessage);
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
                            hostName = new HostName(rootPage.RemoteSocketListenerSvrName);// HostNameForConnect.Text);
                        }
                        catch (ArgumentException)
                        {
                            Util.Log("Error: Invalid host name.", NotifyType.ErrorMessage);
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

                        // Save the socket, so subsequent steps can use it.
                        CoreApplication.Properties.Add("clientSocket", socket);
                        try
                        {
                            if (adapter == null)
                            {

                                Util.Log("Connecting to: " + hostName.DisplayName, NotifyType.StatusMessage);
                                Util.Log("On Port: " + rootPage.Port, NotifyType.StatusMessage);

                                // Connect to the server (by default, the listener we created in the previous step).
                                await socket.ConnectAsync(hostName, rootPage.Port);

                                Util.Log("Connected", NotifyType.StatusMessage);
                            }
                            else
                            {
                                Util.Log(
                                    "Connecting to: " + rootPage.SocketListenerSvr +
                                    " using network adapter " + adapter.NetworkAdapterId,
                                    NotifyType.StatusMessage);

                                // Connect to the server (by default, the listener we created in the previous step)
                                // limiting traffic to the same adapter that the user specified in the previous step.
                                // This option will be overridden by interfaces with weak-host or forwarding modes enabled.
                                await socket.ConnectAsync(
                                    hostName,
                                    rootPage.Port,//                        ServiceNameForConnect.Text, 
                                    SocketProtectionLevel.PlainSocket,
                                    adapter);

                                Util.Log(
                                    "Connected using network adapter " + adapter.NetworkAdapterId,
                                    NotifyType.StatusMessage);
                            }

                            // Mark the socket as connected. Set the value to null, as we care only about the fact that the 
                            // property is set.
                            CoreApplication.Properties.Add("connected", null);
                        }
                        catch (Exception exception)
                        {
                            // If this is an unknown status it means that the error is fatal and retry will likely fail.
                            if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                            {
                                throw;
                            }

                            Util.Log("Connect failed with error: " + exception.Message, NotifyType.ErrorMessage);
                        }
                    }
                }
                }
            }
        }


 
}


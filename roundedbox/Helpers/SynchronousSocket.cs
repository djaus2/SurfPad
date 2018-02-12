
//This is not used by teh project by was use as the starting point for the Socket code development in Pages\SocketTerminal.xaml.cs
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace roundedbox.Helpers { 


    public static class SynchronousSocketListener
    {

        private static Windows.Networking.Sockets.StreamSocket streamSocket = null;

        public  static async Task StartClient()
        {
            try
            {
                // Create the StreamSocket and establish a connection to the echo server.
                streamSocket = new Windows.Networking.Sockets.StreamSocket();
        
                // The server hostname that we will be establishing a connection to. In this example, the server and client are in the same process.
                var hostName = new Windows.Networking.HostName("192.168.0.137");

                MainPage.MP.clientListBox.Items.Add("client is trying to connect...");

                await streamSocket.ConnectAsync(hostName, "1234");

                MainPage.MP.clientListBox.Items.Add("client connected");

                

            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                MainPage.MP.clientListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
        }

        public static async Task SendCh(char ch)
        {
            char[] chars = new char[] { ch };
            await WriteAsync(chars);
        }

        public static async Task<int> WriteAsync(char[] chars)
        {
            int ret = -1;
            try
            {


                    using (Stream outputStream = streamSocket.OutputStream.AsStreamForWrite())
                    {
                        using (var streamWriter = new StreamWriter(outputStream))
                        {
                            await streamWriter.WriteAsync(chars, 0, chars.Length);
                            await streamWriter.FlushAsync();
                            ret = chars.Length;
                        }
                    }

                    MainPage.MP.clientListBox.Items.Add(string.Format("client sent the request: \"{0}\"", ("" + chars + "")));


            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                MainPage.MP.clientListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
            return ret;
        }

        public static async Task<byte[]> ReadAsync()
        {
            byte[] bytes = new byte[0];
            try
            {

                    int ReadBufferLength = 1024;
                    char[] chars  = new char[ReadBufferLength];


                    // Read data from the echo server.
                    int responseLength;
                    using (Stream inputStream = streamSocket.InputStream.AsStreamForRead())
                    {
                        using (StreamReader streamReader = new StreamReader(inputStream))
                        {
                            responseLength = await streamReader.ReadAsync(chars, 0, ReadBufferLength);
                            //responseLength = await streamReader.ReadAsync(chars, 0, ReadBufferLength).WithCancellation(CancellationToken.None, request.Abort, true))
                        }
                    }
                //Truncate the array 
                bytes = Encoding.Unicode.GetBytes(chars);
                bytes = bytes = bytes.Skip(0).Take(responseLength).ToArray();
                string s = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                MainPage.MP.clientListBox.Items.Add(string.Format("client received the response: \"{0}\" ", s));

            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                MainPage.MP.clientListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }

            return bytes;
        }

        public static async Task<string> ReadStrAsync()
        {
            string ret = "";
            try
            {

                int ReadBufferLength = 1024;
                char[] chars = new char[ReadBufferLength];


                // Read data from the echo server.
                int responseLength;
                using (Stream inputStream = streamSocket.InputStream.AsStreamForRead())
                {
                    using (StreamReader streamReader = new StreamReader(inputStream))
                    {
                        responseLength = await streamReader.ReadAsync(chars, 0, ReadBufferLength);
                    }
                }
                //Truncate the array 
                byte[] bytes = Encoding.Unicode.GetBytes(chars);
                bytes = bytes = bytes.Skip(0).Take(responseLength).ToArray();
                ret = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                MainPage.MP.clientListBox.Items.Add(string.Format("client received the response: \"{0}\" ", ret));

            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                MainPage.MP.clientListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }

            return ret;
        }

        public static async Task<char> GetCh()
        {
            char[] bytes = null;
            char ch = '\0';
            try
            {

                int ReadBufferLength = 1;
                bytes = new char[ReadBufferLength];
                int responseLength;

                // Read data from the echo server.
                do
                {

                    using (Stream inputStream = streamSocket.InputStream.AsStreamForRead())
                    {
                        using (StreamReader streamReader = new StreamReader(inputStream))
                        {
                            responseLength = await streamReader.ReadAsync(bytes, 0, ReadBufferLength);
                        }
                    }
                } while (responseLength != 1);

                        //Truncate the array 
                        ch = bytes[0];
                MainPage.MP.clientListBox.Items.Add(string.Format("client received the response: \"{0}\" ", (("" + bytes) + "")));

            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                MainPage.MP.clientListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }

            return ch;
        }

       

        public static  void CloseSocket()
        {
            try
            {
                streamSocket.Dispose();

            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                MainPage.MP.clientListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
        }

                //Ref: https://stackoverflow.com/questions/28626575/can-i-cancel-streamreader-readlineasync-with-a-cancellationtoken
                /*
                 var request = (HttpWebRequest)WebRequest.Create(url);

                        using (var response = await request.GetResponseAsync())
                        {
                            . . .
                        }
                        will become:
                        var request = (HttpWebRequest)WebRequest.Create(url);

                        using (WebResponse response = await request.GetResponseAsync().WithCancellation(CancellationToken.None, request.Abort, true))
                        {
                            . . .
                        }
                 */
                public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken, Action action, bool useSynchronizationContext = true)
                {
                    using (cancellationToken.Register(action, useSynchronizationContext))
                    {
                        try
                        {
                            return await task;
                        }
                        catch (Exception ex)
                        {

                            if (cancellationToken.IsCancellationRequested)
                            {
                                // the Exception will be available as Exception.InnerException
                                throw new OperationCanceledException(ex.Message, ex, cancellationToken);
                            }

                            // cancellation hasn't been requested, rethrow the original Exception
                            throw;
                        }
                    }
                }

            }


        }
 
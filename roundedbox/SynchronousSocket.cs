
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

namespace roundedbox {


    public static class SynchronousSocketListener
    {

        //public static async  Task Qwerty()
        //{
            
        //    HostName serverHost = new HostName("192.168.0.137");
        //    StreamSocket clientSocket = new Windows.Networking.Sockets.StreamSocket();
        //    // Try to connect to the remote host
        //    await clientSocket.ConnectAsync(serverHost, "1234");
         

        //    Task<UInt32> loadAsyncTask;

        //    int ReadBufferLength = 1024;
        //    char[] bytes = new char[ReadBufferLength];

        //    System.IO.Stream inputStream = clientSocket.InputStream.AsStreamForRead();
        //    StreamReader streamReader = new StreamReader(inputStream);
        //    var response = await streamReader.ReadAsync(bytes, 0, ReadBufferLength);

        //    DataReader dataReaderObject = new DataReader(clientSocket.InputStream);

        //    CancellationTokenSource ReadCancellationTokenSource = new CancellationTokenSource();
        //    var cancellationToken = ReadCancellationTokenSource.Token;
            

        //    // If task cancellation was requested, comply
        //    cancellationToken.ThrowIfCancellationRequested();

        //    // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
        //    dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

        //    // Create a task object to wait for data on the serialPort.InputStream
        //    loadAsyncTask = dataReaderObject.LoadAsync((uint)ReadBufferLength).AsTask(cancellationToken);

        //    // Launch the task and wait
        //    UInt32 bytesRead = await loadAsyncTask;
        //    if (bytesRead > 0)
        //    {
        //    }


        //    }

        public  static async Task StartClient()
        {
            try
            {
                // Create the StreamSocket and establish a connection to the echo server.
                using (var streamSocket = new Windows.Networking.Sockets.StreamSocket())
                {
                    // The server hostname that we will be establishing a connection to. In this example, the server and client are in the same process.
                    var hostName = new Windows.Networking.HostName("192.168.0.137");

                    MainPage.MP.clientListBox.Items.Add("client is trying to connect...");

                    await streamSocket.ConnectAsync(hostName, "1234");

                    MainPage.MP.clientListBox.Items.Add("client connected");

                    // Send a request to the echo server.
                    string request = "Hello, World!";
                    using (Stream outputStream = streamSocket.OutputStream.AsStreamForWrite())
                    {
                        using (var streamWriter = new StreamWriter(outputStream))
                        {
                            await streamWriter.WriteLineAsync(request);
                            await streamWriter.FlushAsync();
                        }
                    }

                    MainPage.MP.clientListBox.Items.Add(string.Format("client sent the request: \"{0}\"", request));

                }

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

        public static async Task WriteAsync(char[] chars)
        {
            try
            {


                    using (Stream outputStream = streamSocket.OutputStream.AsStreamForWrite())
                    {
                        using (var streamWriter = new StreamWriter(outputStream))
                        {
                            await streamWriter.WriteAsync(chars, 0, chars.Length);
                            await streamWriter.FlushAsync();
                        }
                    }

                    MainPage.MP.clientListBox.Items.Add(string.Format("client sent the request: \"{0}\"", ("" + chars + "")));


            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                MainPage.MP.clientListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
        }

        public static async Task<char[]> ReadAsync()
        {
            char[] bytes = null;
            try
            {

                    int ReadBufferLength = 1024;
                    bytes = new char[ReadBufferLength];


                    // Read data from the echo server.
                    int responseLength;
                    using (Stream inputStream = streamSocket.InputStream.AsStreamForRead())
                    {
                        using (StreamReader streamReader = new StreamReader(inputStream))
                        {
                            responseLength = await streamReader.ReadAsync(bytes, 0, ReadBufferLength);
                        }
                    }
                    //Truncate the array 
                    bytes = bytes = bytes.Skip(0).Take(responseLength).ToArray();
                MainPage.MP.clientListBox.Items.Add(string.Format("client received the response: \"{0}\" ", (("" + bytes) + "")));

            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                MainPage.MP.clientListBox.Items.Add(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }

            return bytes;
        }

        public static async Task<char> GetAChar()
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

        private  static Windows.Networking.Sockets.StreamSocket streamSocket;

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




    }


}
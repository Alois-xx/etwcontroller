using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETWController.Network
{
    /// <summary>
    /// Client which connects to a server to send message to it.
    /// </summary>
    public class MessageSender : IDisposable
    {
        /// <summary>
        /// Internal buffer size for send and receive.
        /// </summary>
        internal const int SendReceiveBufferSize = 1024;

        NetworkProtocolType Protocol;
        string Host;
        int Port;

        internal TcpClient Client;
        IPEndPoint RemoteIpEndPoint;
        static object Lock = new object();

        public ThreadLocal<byte[]> SendBuffer = new ThreadLocal<byte[]>(() => new byte[SendReceiveBufferSize]);

        private NetworkStream NetworkSend;

        /// <summary>
        /// Create a connection to a remove machine to send data to it.
        /// </summary>
        /// <param name="host">Hostname</param>
        /// <param name="port">Port</param>
        /// <param name="protocol">Protocol. Currently only TCP is supported</param>
        /// <exception cref="ArgumentException">When host is null or empty or the port number is 0 or negative.</exception>
        /// <exception cref="NotSupportedException">When another protocol than TCP is used.</exception>
        /// <exception cref="SocketException">Server does not exist or it refused the connection because no server is running on that port.</exception>
        public MessageSender(string host, int port, NetworkProtocolType protocol)
        {
            if(String.IsNullOrEmpty(host) )
            {
                throw new ArgumentException("host is null or empty");
            }

            if( port <= 0 )
            {
                throw new ArgumentException("port must be > 0");
            }

            if (protocol != NetworkProtocolType.TCP)
            {
                throw new NotSupportedException(String.Format("This protocol {0} is currently not supported", protocol));
            }

            Host = host;
            Port = port;
            Protocol = protocol;

            RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, port);

            if( protocol == NetworkProtocolType.TCP)
            {
                const int Retries = 2;

                // Try to connect to server with 2 retries and a short sleep in between
                for (int i = 0; i < Retries; i++)
                {
                    try
                    {
                        Client = new TcpClient(Host, Port);
                        ConfigureSocket(Client.Client);
                        this.NetworkSend = Client.GetStream();
                        break;
                    }
                    catch(SocketException)
                    {
                        if (i == Retries-1)
                        {
                            throw;
                        }
                        Thread.Sleep(10);
                    }
                }
            }
        }


        void ConfigureSocket(Socket socket)
        {
            // The socket will linger for 10 seconds after 
            // Socket.Close is called.
            socket.LingerState = new LingerOption (true, 10);

            // Disable the Nagle Algorithm for this tcp socket.
            socket.NoDelay = true;

            // Set the receive buffer size to 8k
            socket.ReceiveBufferSize = 8192;

            // Set the timeout for synchronous receive methods to 
            // 1 second (1000 milliseconds.)
            socket.ReceiveTimeout = 2000;

            // Set the send buffer size to 8k.
            socket.SendBufferSize = 8192;

            // Set the timeout for synchronous send methods
            // to 1 second (1000 milliseconds.)            
            socket.SendTimeout = 2000;
        }

        /// <summary>
        /// Send a size prefixed message as UTF8 over the wire.
        /// </summary>
        /// <param name="message">Message to send</param>
        public void Send(string message)
        {
            var utfMsg = Encoding.UTF8.GetBytes(message);

            Array.Clear(SendBuffer.Value, 0, SendBuffer.Value.Length);
            Array.Copy(utfMsg, 0, SendBuffer.Value, 4, utfMsg.Length);

            if (NetworkSend != null)
            {
                // Prefix message with message string length
                SendBuffer.Value[0] = (byte)((utfMsg.Length >> 24) & 0xff);
                SendBuffer.Value[1] = (byte)((utfMsg.Length >> 16) & 0xff);
                SendBuffer.Value[2] = (byte)((utfMsg.Length >> 8) & 0xff);
                SendBuffer.Value[3] = (byte)((utfMsg.Length) & 0xff);

                lock (Lock) // do not mess up the network shared by ohter threads.
                {
                    NetworkSend.Write(SendBuffer.Value, 0, utfMsg.Length + 4);
                    ReadAcknowledge();
                }
            }
            else
            {
                throw new NotSupportedException("No Network connection configured!");
            }
        }

        /// <summary>
        /// Fake async version to not block hooked keyboard in case of sloppy network. 
        /// Otherwise the input processing of the complete machine hangs until the network packets
        /// get through it or never if the remote machine does not respond or the remote host is configured wrong.
        /// </summary>
        /// <param name="message">message to send.</param>
        /// <returns></returns>
        public Task SendAsync(string message)
        {
            return Task.Run(() => Send(message));
        }

        /// <summary>
        /// Read response from the server that the message has arrived and it has been processed.
        /// </summary>
        private void ReadAcknowledge()
        {
            byte ackValue = (byte) NetworkSend.ReadByte();
            if (ackValue != MessageReceiver.AcknowledgeByte)
            {
                throw new InvalidOperationException(String.Format("Wrong ACK byte received. Expected {0} but got {1}", MessageReceiver.AcknowledgeByte, ackValue));
            }
        }

        /// <summary>
        /// Close the connection to the server
        /// </summary>
        public void Dispose()
        {
            if (Client != null)
            {
                Client.Close();
                Client = null;
            }
        }
    }
}

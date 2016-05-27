using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETWControler.Network
{
    /// <summary>
    /// Server class which receives messages from on or more clients
    /// </summary>
    public class MessageReceiver : IDisposable
    {
        Task ConnectionAcceptor;
        CancellationToken CancelToken;
        CancellationTokenSource CancelSource;
        TcpListener Listener;
        internal int OpenListeners;
        Exception LastStartException = null;


        public const byte AcknowledgeByte = 0x25;

        int Port;

        event Action<string> OnMessageReceivedInternal;

        /// <summary>
        /// Register on this event to get all received messages on a thread pool thread deliverd from potentially 
        /// severl clients.
        /// </summary>
        public event Action<string> OnMessageReceived
        {
            add
            {
                OnMessageReceivedInternal += value;
            }
            remove
            {

            }
        }

        /// <summary>
        /// Create a message receiver server which can accept connection from many clients
        /// </summary>
        /// <param name="port"></param>
        /// <param name="protocol"></param>
        public MessageReceiver(int port, NetworkProtocolType protocol)
        {
            if (port <= 0)
            {
                throw new ArgumentException("port must be > 0");
            }

            if (protocol != NetworkProtocolType.TCP)
            {
                throw new NotSupportedException(String.Format("This protocol {0} is currently not supported", protocol));
            }

            Port = port;

            using (Barrier untilStartedListening = new Barrier(2))
            {
                Listener = new TcpListener(IPAddress.Any, Port);
                CancelSource = new CancellationTokenSource();
                CancelToken = CancelSource.Token;
                ConnectionAcceptor = Task.Factory.StartNew( ()=> StartAcceptingConnections(untilStartedListening), CancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                if( untilStartedListening.SignalAndWait(5000) == false)
                {
                    throw new InvalidOperationException($"Could not start network receiver on port {Port}. LastStartException: {LastStartException}");
                }
            }
        }


        void StartAcceptingConnections(Barrier untilStartedListening)
        {
            bool bHasWaited = false;

            try
            {
                while (!CancelToken.IsCancellationRequested)
                {
                    Listener.Start();
                    if (!bHasWaited)
                    {
                        untilStartedListening.SignalAndWait();
                        bHasWaited = true;
                    }

                    var client = Listener.AcceptTcpClient();

                    if (CancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    Task.Factory.StartNew((o)=>Receive((TcpClient)o), client);
                }
            }
            catch(SocketException ex)
            {
                Debug.Print("Got SocketException in server: {0}", ex);
                LastStartException = ex;
                throw;
            }
            catch(Exception ex)
            {
                LastStartException = ex;
                throw;
            }
        }

        
        /// <summary>
        /// Every message is prefixed with the length in bytes.
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <param name="receiveBuffer">Buffer to put the data into to.</param>
        /// <returns>Length of pending data excluding the size of the lenth int.</returns>
        int ReadLength(NetworkStream stream, byte[] receiveBuffer)
        {
            int nRead = 1;
            int nTotal = 0;
            while (nRead > 0 && nTotal != 4 && !CancelToken.IsCancellationRequested)
            {
                nRead = stream.Read(receiveBuffer, nTotal, 4-nTotal);
                nTotal += nRead;
            }

            if (nTotal == 4)
            {
                return (receiveBuffer[0] << 24) +
                       (receiveBuffer[1] << 16) +
                       (receiveBuffer[2] << 8) +
                        receiveBuffer[3];
            }
            else if( nTotal > 4 )
            {
                throw new NotSupportedException(String.Format("Did read more data than exptected 4 but was {0}", nTotal));
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Start receiving messages from the clients
        /// </summary>
        /// <param name="client"></param>
        void Receive(TcpClient client)
        {
            try
            {
                Interlocked.Increment(ref OpenListeners);

                var stream = client.GetStream();
                byte[] receiveBuffer = new byte[MessageSender.SendReceiveBufferSize];
                int nRead = 0;
                while (!CancelToken.IsCancellationRequested)
                {
                    Array.Clear(receiveBuffer, 0, receiveBuffer.Length);

                    int msgSize = ReadLength(stream, receiveBuffer);
                    if (msgSize == -1 || CancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if( msgSize > 50*1000) // received potentially a partial message from an already closed socket
                    {
                        throw new InvalidOperationException(String.Format("Invalid buffer size during connect found: {0}", msgSize));
                    }

                    int read = 0;
                    while (read < msgSize)
                    {
                        nRead = stream.Read(receiveBuffer, read, msgSize - read);
                        if (nRead == 0)
                        {
                            break;
                        }
                        read += nRead;
                    }

                    string readStr = Encoding.UTF8.GetString(receiveBuffer, 0, msgSize);
                    //    Debug.Print("Did read {0} bytes with message: {1}", nRead, read);
                    if (OnMessageReceivedInternal != null)
                    {
                        OnMessageReceivedInternal(readStr);
                    }

                    SendAck(stream);
                }
            }
            finally
            {
                Interlocked.Decrement(ref OpenListeners);
            }
        }

        private void SendAck(NetworkStream stream)
        {
            stream.WriteByte(MessageReceiver.AcknowledgeByte);
        }

        public void Dispose()
        {
            OnMessageReceivedInternal = null;
            CancelSource.Cancel();
            Listener.Stop();
            ConnectionAcceptor.Wait();
        }
    }
}

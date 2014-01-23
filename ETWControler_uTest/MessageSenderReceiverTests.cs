using ETWControler;
using ETWControler.Network;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETWControler_uTest
{
    [TestFixture]
    public class MessageSenderReceiverTests
    {
        /// <summary>
        /// The trivial use use. Open Server, Open Sender send one message and check if server
        /// has received it. 
        /// </summary>
        [Test]
        public void Can_Send_And_Receive_On_LocalHost()
        {
            string message = "Hi Network";

            using(MessageReceiver rec = new MessageReceiver(5600, NetworkProtocolType.TCP))
            {
                using (MessageSender sender = new MessageSender("127.0.0.1", 5600, NetworkProtocolType.TCP))
                {
                    using (Barrier b = new Barrier(2))
                    {
                        var task = Task.Factory.StartNew(() => sender.Send(message));
                        string received = null;
                        rec.OnMessageReceived += (recMsg) =>
                            {
                                received = recMsg;
                                b.SignalAndWait(100);
                                Console.WriteLine("Got: {0}", recMsg);
                            };
                        b.SignalAndWait(100);
                        Assert.AreEqual(message, received);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public void Can_Close_Connection_And_Reconnect()
        {
            string message = "Hi Network";
            string secondMessage = "Hi Other Network";

            using (var rec = new MessageReceiver(5600, NetworkProtocolType.TCP))
            {
                string received = null;
                rec.OnMessageReceived += (recv) => received = recv;

                var sendTask = Task.Factory.StartNew(() =>
                    {
                        using (MessageSender sender = new MessageSender("127.0.0.1", 5600, NetworkProtocolType.TCP))
                        {
                            sender.Send(message);
                        }
                    });

                sendTask.Wait();
                Thread.Sleep(50);
                Assert.AreEqual(message, received);
            }

            using (var rec2 = new MessageReceiver(5600, NetworkProtocolType.TCP))
            {
                string received = null;
                rec2.OnMessageReceived += (rec) => received = rec;

                var sendTask = Task.Factory.StartNew(() =>
                {
                    using (MessageSender sender = new MessageSender("127.0.0.1", 5600, NetworkProtocolType.TCP))
                    {
                        sender.Send(secondMessage);
                    }
                });

                sendTask.Wait();

                Thread.Sleep(50);
                Assert.AreEqual(secondMessage, received);
            }
        }


        [Test]
        public void Connect_To_Not_Existing_Server_Throws_SocketException()
        {
            Assert.Throws<System.Net.Sockets.SocketException>( () =>
                {
                    using (var receiver = new MessageSender("localhsot", 5900, NetworkProtocolType.TCP))
                    { }
                }
            );
        }

        /// <summary>
        /// Verify that sending and receiving one message works when the server exits immediately after the message was sent.
        /// When sending a message afer the server has exited it must
        ///  a) Not wait arbitrary long (this can happen easy with sockets)
        ///     To make this work I needed to ack every sent message explicetly. 
        ///     TCP KeepAlive did not help either. But that is ok since we do want to measure round trip times anyway. 
        ///  b) Throw some IOException
        /// </summary>
        [Test]
        public void Server_Exited()
        {
            const string MsgText = "First Message";

            using (var receiver = new MessageReceiver(5950, NetworkProtocolType.TCP))
            {
                var sender = new MessageSender("localhost", 5950, NetworkProtocolType.TCP);
                string received = null;
                using (Barrier b = new Barrier(2))
                {
                    receiver.OnMessageReceived += (msg) =>
                        {
                            received = msg;
                            Console.WriteLine("Received: {0}", msg);
                            b.SignalAndWait(500);
                        };
                    sender.Send(MsgText);
                    receiver.Dispose();
                    b.SignalAndWait(500);
                    Assert.AreEqual(MsgText, received);
                }

                Assert.Throws<IOException>(() =>
                        sender.Send("Hi to close"));
            }
        }


        /// <summary>
        /// Send from 5 senders in parallel 1000 messages and ensure that all are received. 
        /// </summary>
        [Test]
        public void Receive_5000_Messages()
        {
            using(var receiver = new MessageReceiver(5600, NetworkProtocolType.TCP ))
            {
                var received = new List<string> ();
               
                receiver.OnMessageReceived += (recv) =>
                    {
                        lock (received)
                        {
                            received.Add(recv);
                        }
                    };

                int sends = 0;

                Action Send = () =>
                    {
                        using (var sender = new MessageSender("localhost", 5600, NetworkProtocolType.TCP))
                        {
                            for (int i = 0; i < 1000; i++)
                            {
                                sender.Send(String.Format("Message {0}", Interlocked.Increment(ref sends)));
                            }
                        }
                    };

                Parallel.Invoke(Send, Send, Send, Send, Send);
                
                while(received.Count != 5000)
                {
                    Thread.Sleep(1);
                }

                Console.WriteLine("Got {0} messages. Last 100: {1}", received.Count,
                    String.Join(Environment.NewLine, received.Skip(4900)));

                Assert.AreEqual(5000, received.Count);
                Assert.AreEqual("Message 5000", received.Last());
            }
        }
    }
}

using NTDLS.StreamFraming;
using System.Net;
using System.Net.Sockets;
using TestHarness.Payloads;

namespace TestHarness
{
    internal class TestServer
    {
        private readonly int _listenPort;
        private bool _keepRunning = false;
        private readonly List<PeerConnection> _peerConnections = new();
        private readonly Thread _listenerThread;
        private readonly TcpListener _listener;

        private class PeerConnection
        {
            public TcpClient TcpClient { get; set; }
            public Thread Thread { get; set; }

            public PeerConnection(TcpClient tcpClient, Thread thread)
            {
                TcpClient = tcpClient;
                Thread = thread;
            }
        }

        public TestServer(int listenPort)
        {
            _listenPort = listenPort;
            _listenerThread = new Thread(ListenerThreadProc);
            _listener = new TcpListener(IPAddress.Any, _listenPort);
        }

        public void StartServer()
        {
            _keepRunning = true;
            _listener.Start();
            _listenerThread.Start();
        }

        public void Shutdown()
        {
            _keepRunning = false;

            _listener.Stop();
            _listenerThread.Join();

            while (true) //Wait on all peer threads to stop.
            {
                PeerConnection? peerConnection = null;

                lock (_peerConnections)
                {
                    if (_peerConnections.Count > 0)
                    {
                        peerConnection = _peerConnections[0];
                    }
                    else break;
                }

                try
                {
                    peerConnection?.TcpClient.Close();
                    peerConnection?.Thread.Join();
                }
                catch { }
            }
        }

        private void ListenerThreadProc()
        {
            try
            {
                while (_keepRunning)
                {
                    var tcpClient = _listener.AcceptTcpClient(); //Wait for an inbound connection.
                    if (tcpClient.Connected)
                    {
                        var peerThread = new Thread(AcceptedClientThreadProc);
                        lock (_peerConnections)
                        {
                            _peerConnections.Add(new PeerConnection(tcpClient, peerThread));
                        }
                        peerThread.Start(tcpClient);
                    }
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode != SocketError.Interrupted && ex.SocketErrorCode != SocketError.Shutdown)
                {
                    Console.WriteLine($"Error in ListenerThreadProc: '{ex.Message}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ListenerThreadProc: '{ex.Message}'");
            }
        }


        private void AcceptedClientThreadProc(object? param)
        {
            try
            {
                var frameBuffer = new FrameBuffer();

                using (var tcpClient = param as TcpClient)
                {
                    if (tcpClient == null)
                    {
                        throw new Exception("tcpClient can not be null.");
                    }

                    using (var tcpStream = tcpClient.GetStream())
                    {
                        while (tcpClient.Connected)
                        {
                            string text = $"Hello Client! The time is: {DateTime.Now.ToLongTimeString()}.";

                            //Assemble a message frame and write it to the stream:
                            tcpStream.WriteNotification(new MyMessage(text));

                            Thread.Sleep(1000);
                        }
                        tcpStream.Close();
                    }

                    tcpClient.Close();
                }
            }
            catch (IOException)
            {
                //Closing the connection.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AcceptedClientThreadProc: '{ex.Message}'");
            }
            finally
            {
                lock (_peerConnections)
                {
                    _peerConnections.RemoveAll(o => o.Thread.ManagedThreadId == Environment.CurrentManagedThreadId);
                }
            }
        }
    }
}

using NTDLS.StreamFraming;
using NTDLS.StreamFraming.Payloads;
using System.Net.Sockets;
using TestHarness.Payloads;

namespace TestHarness
{
    internal class TestClient
    {
        private readonly string _connectAddress;
        private readonly int _connectPort;
        private readonly Thread _peerThread;
        private readonly TcpClient _tcpClient;

        public TestClient(string connectAddress, int connectPort)
        {
            _connectAddress = connectAddress;
            _connectPort = connectPort;

            _tcpClient = new TcpClient();
            _peerThread = new Thread(ClientThreadProc);
        }

        public void ConnectToServer()
        {
            _tcpClient.Connect(_connectAddress, _connectPort);
            _peerThread.Start();
        }

        public void Disconnect()
        {
            _tcpClient.Close();
            _peerThread.Join();
        }

        private void ClientThreadProc()
        {
            try
            {
                var frameBuffer = new FrameBuffer();

                using (var tcpStream = _tcpClient.GetStream())
                {
                    while (_tcpClient.Connected)
                    {
                        //Read from the stream, assemble the bytes into the original messages and call the handler for each message that was received.
                        if (tcpStream.ReadAndProcessFrames(frameBuffer, ProcessFrameNotification, ProcessFrameQuery) == false)
                        {
                            //If ReadAndProcessFrames() returns false then we have been disconnected.
                            break;
                        }
                    }
                    tcpStream.Close();
                }
                _tcpClient.Close();
            }
            catch (IOException)
            {
                //Close the connection.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ClientThreadProc: '{ex.Message}'");
            }
        }

        private static void ProcessFrameNotification(IFrameNotification payload)
        {
            //We recevied a message, see if it is of the type "MyMessage".
            if (payload is MyMessage myMessage)
            {
                Console.WriteLine($"Received [Notification] from server: '{myMessage.Text}'");
            }
        }

        private static IFrameQueryReply ProcessFrameQuery(IFrameQuery payload)
        {
            //We recevied a message, see if it is of the type "MyQuery", if so reply with the type of MyQueryReply.
            if (payload is MyQuery myQuery)
            {
                Console.WriteLine($"Received [Query] from server: '{myQuery.Text}'");

                return new MyQueryReply("Hello Server!");
            }

            throw new Exception("The query type was unhandled.");
        }
    }
}
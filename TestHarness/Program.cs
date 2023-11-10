namespace TestHarness
{
    internal class Program
    {
        static void Main()
        {
            int tcpPort = 36985;

            var server = new TestServer(tcpPort);
            server.StartServer();

            var client = new TestClient("localhost", tcpPort);
            client.ConnectToServer();

            Console.WriteLine("Press [enter] to shutdown...");
            Console.ReadLine();

            client.Disconnect();
            server.Shutdown();
        }
    }
}
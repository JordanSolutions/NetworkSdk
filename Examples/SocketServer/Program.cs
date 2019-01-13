/*
 This is a simple example meant to run over the loopback address and send/receive small packages that require no fragmentation. 
 Outputs connections made, echoes back received packages for the purpose of demonstrating each individual IProtocol and ISocket implementation.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JordanSdk.Network.Core;
namespace SocketServer
{
    class Program
    {
        #region Private Fields
        static List<IProtocol> servers;
       

        const int TCP_UDP_PORT = 4098;
        const int WS_PORT = 4099;

        static bool running = true;

        #endregion


        [MTAThread]
        static void Main(string[] args)
        {
            Console.Title = "Example Multi-Socket Server";
            Console.WriteLine("Starting Servers:");

            StartSocketServer();

            Console.WriteLine("Servers Started, waiting for incoming connections.");
            while (running)
            {
                ProcessInput(Console.ReadLine());
            }

            foreach (IProtocol server in servers)
            {
                server.StopListening();
                server.Dispose();
            }

        }

        private static async void StartSocketServer()
        {
            servers = new List<IProtocol>();
            //When no IP address is specified for TCP or UDP, the default address is set to IV4 loopback
            servers.Add(new JordanSdk.Network.Tcp.TcpProtocol() { Port = TCP_UDP_PORT } as IProtocol);
            servers.Add(new JordanSdk.Network.Udp.UdpProtocol() { Port = TCP_UDP_PORT } as IProtocol);
            //When no address is specified for web sockets, the default listening URI is: http://localhost/server/
            servers.Add(new JordanSdk.Network.WebSocket.WebSocketProtocol() { Port = WS_PORT } as IProtocol);

            foreach (IProtocol server in servers)
            {
                server.OnConnectionRequested += Server_OnConnectionRequested;
                await server.ListenAsync(false);
                Console.WriteLine("{0} Server started using address: {1} and port: {2}", server is JordanSdk.Network.Tcp.TcpProtocol ? "TCP" : server is JordanSdk.Network.Udp.UdpProtocol ? "UDP" : "Web Sockets", server.Address, server.Port);
            }
        }

        private static void Server_OnConnectionRequested(ISocket socket)
        {
            Console.WriteLine($"Socket with id: {socket.Id.ToString()} has connected.");
            socket.OnSocketDisconnected += Socket_OnSocketDisconnected;
            var throwAway = StartReceiving(socket);
        }

        private static void Socket_OnSocketDisconnected(ISocket socket)
        {
            Console.WriteLine($"Socket with id {socket.Id} Disconnected.");
        }

        private static void ProcessInput(string input)
        {
            switch (input.ToLower())
            {
                case "stop":
                    running = false;
                    break;
            }
        }

        private static async Task StartReceiving(ISocket socket)
        {
            while (running)
            {
                var data = await socket.ReceiveAsync();
                if (data != null)
                {
                    var receivedText = Encoding.UTF8.GetString(data);
                    Console.WriteLine($"Data received from {socket.Id.ToString()}: {receivedText}");
                    receivedText = $"Echo: {receivedText}";
                    socket.Send(Encoding.UTF8.GetBytes(receivedText));
                }
            }
        } 

    }
}

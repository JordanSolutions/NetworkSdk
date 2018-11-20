/*
 * Example socket client that simply attempts to connect using the specified protocol to a server over the specified IP or URL (for web sockets)
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JordanSdk.Network.Core;
namespace SocketClient
{
    class Program
    {
        #region Private fields

        static string connectionType = "tcp";
        const int TCP_UDP_PORT = 4098;
        const int WS_PORT = 4099;
        static bool running = true;
        static string ip = null;
        static int port = 0;
        static ISocket client;

        #endregion

        #region Main

        [MTAThread]
        static int Main(string[] args)
        {
            Console.Title = "Example Jordan SDK Client";

            Console.WriteLine("JordanSdk.Network Client Example");
            if (args.Length == 1 && args[0].Trim().ToLower() == "--help")
            {
                return PrintHelp();
            }
            else if ((args.Length > 0 && ProcessArgs(args)) || RequestInput() > 0)
            {
                if (StartClient().Result)
                {
                    StartReadingThread();
                    while (running)
                    {
                        ProcessInput(Console.ReadLine());
                    }
                }
                else
                    return -1;
            }
            return 0;
        }

        #endregion

        #region Connection Parameter Input

        private static int RequestInput()
        {
            Console.WriteLine("Please select your connection protocol.");
            Console.WriteLine("For TCP, type \"tcp\" (without quotes) and press enter.");
            Console.WriteLine("For Datagrams type \"udp\"  (without quotes)  and press enter.");
            Console.WriteLine("For Web Sockets type \"ws\"  (without quotes)  and press enter.");
            int control = 0;
            while (control == 0)
            {
                control = SelectProtocol();
                if (control < 0)
                    return control;
            }
            control = 0;
            Console.WriteLine("Please type the server {0} and press enter, or live it blank and press enter to use the default {0}.", connectionType == "ws" ? "URL": "IP");
            while (control == 0)
            {
                control = SelectIP();
                if (control < 0)
                    return control;
            }
            control = 0;
            Console.WriteLine("Please type the server port number and press enter, or live it blank and press enter to use the default port.");
            while (control == 0)
            {
                control = SelectPort();
                if (control < 0)
                    return control;
            }
            return 1;
        }

        private static int SelectProtocol()
        {
            Console.Write("Protocol? ");
            var input = Console.ReadLine();
            if (input.Trim().Length > 0)
            {
                switch (input.Trim().ToLower())
                {
                    case "udp":
                        connectionType = "udp";
                        return 1;
                    case "tcp":
                        connectionType = "tcp";
                        return 1;
                    case "ws":
                        connectionType = "ws";
                        return 1;
                    case "--quit":
                    case "--stop":
                        return -1;
                    default:
                        Console.WriteLine("Invalid protocol entered");
                        return 0;
                }
            }
            else
                return 1;
        }

        private static int SelectIP()
        {
            Console.Write( connectionType == "ws" ? "URL: " : "IP: ");
            var input = Console.ReadLine();
            if (input.Trim().Length > 0)
            {
                switch (input.Trim().ToLower())
                {
                    case "--quit":
                    case "--stop":
                        return -1;
                    default:
                        ip = input;
                        break;
                }
            }
            else
                ip = connectionType == "ws" ? "ws://localhost/server" : "127.0.0.1";
            return 1;
        }

        private static int SelectPort()
        {
            Console.Write("Port: ");
            var input = Console.ReadLine();
            if (input.Trim().Length > 0)
            {
                switch (input.Trim().ToLower())
                {
                    case "--quit":
                    case "--stop":
                        return -1;
                    default:
                        if (int.TryParse(input, out port))
                            return 1;
                        else
                        {
                            Console.WriteLine("Invalid Port Number.");
                            return 0;
                        }
                }
            }
            else
                port = connectionType == "ws" ? WS_PORT : TCP_UDP_PORT;
            return 1;
        }

        #endregion

        #region Startup Argument Parser

        private static bool ProcessArgs(string[] args)
        {
            Dictionary<string, string> commands = new Dictionary<string, string>();
            string currentArg = null, currentValue = null;

            foreach (string s in args)
            {
                if (s.StartsWith("-"))
                {
                    var arg = s.Split('=');
                    if (arg.Length == 1)
                        arg = s.Split(':');
                    currentArg = arg[0];
                    if (arg.Length > 1) currentValue = arg[1];
                    else currentValue = null;
                }
                else
                    currentValue = s;
                if (currentArg != null)
                    if (commands.ContainsKey(currentArg)) commands[currentArg] = currentValue;
                    else commands.Add(currentArg, currentValue);
            }

            foreach (KeyValuePair<string, string> command in commands)
            {
                switch (command.Key.ToLower())
                {
                    case "--ip":
                    case "-address":
                    case "-ipaddress":
                        ip = command.Value?.Trim();
                        break;
                    case "--p":
                    case "-port":
                        if(command.Value != null)
                            int.TryParse(command.Value?.Trim(), out port);
                        break;
                    case "--type":
                    case "-protocol":
                        connectionType = command.Value ?? "tcp";
                        connectionType = connectionType.Trim().ToLower();
                        break;
                }
            }
            if(connectionType.Length == 0)
                connectionType = "tcp";

            if(connectionType == "tcp" || connectionType == "udp")
            {
                port = port > 0 ? port : TCP_UDP_PORT;
                ip = ip != null && ip.Length > 0 ? ip : "127.0.0.1";
            }
            else
            {
                port = port > 0 ? port : WS_PORT;
                ip = ip != null && ip.Length > 0 ? ip : "ws://localhost/server";
            }
            return commands.Count > 0;
        }

        #endregion

        #region Client Socket Connection  

        private static async Task<bool> StartClient()
        {
            IProtocol selected = null;
            string address = null;
            int port = 0;
            switch (connectionType)
            {
                case "tcp":
                    port = TCP_UDP_PORT;
                    selected = new JordanSdk.Network.Tcp.TcpProtocol();
                    address = "127.0.0.1";
                    break;
                case "udp":
                    port = TCP_UDP_PORT;
                    selected = new JordanSdk.Network.Udp.UdpProtocol();
                    address = "127.0.0.1";
                    break;
                case "ws":
                    port = WS_PORT;
                    selected = new JordanSdk.Network.WebSocket.WebSocketProtocol();
                    address = "ws://localhost/server"; 
                    break;
            }
            client = await selected.ConnectAsync(address, port, false);
            if (client.Connected)
                Console.WriteLine("Connected.");
            else
            {
                Console.WriteLine("Unable to connect.");
                return false;
            }
            return true;
        }

        private static void StartReadingThread()
        {
            client.ReceiveAsync((data) =>
            {
                if (data != null && data.Length > 0)
                    Console.WriteLine($"Received: {Encoding.UTF8.GetString(data)}");
                if(running)
                    StartReadingThread();
            });
        } 

        private static void ProcessInput(string input)
        {
            if (input.Trim().ToLower() == "--stop" || input.Trim().ToLower() == "--quit")
            {
                running = false;
                if (client != null && client.Connected)
                    client.Disconnect();
                return;
            }
            var sent = client?.Send(Encoding.UTF8.GetBytes(input));
            Console.WriteLine($"{sent} bytes sent.");

        }

        #endregion

        #region Print Help

        private static int PrintHelp()
        {
            Console.WriteLine("Valid command line arguments are:");
            Console.WriteLine("--ip, -address, -ipaddress for the IP Address or URL (web socket).");
            Console.WriteLine("--p, -port, for the port to use when connecting.");
            Console.WriteLine("--type, -protocol, to specify the kind of connection to use.");
            Console.WriteLine("To disconnect, simply type --stop or --quit and press enter at any time.");
            return 0;
        }

        #endregion
    }
}

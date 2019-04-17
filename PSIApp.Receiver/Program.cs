using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSIApp.Receiver
{
    public struct UdpState
    {
        public UdpClient u;
        public IPEndPoint e;
    }

    /// <summary>
    /// PSIApp.Receiver
    /// </summary>
    class Program
    {
        const int ListeningPort = 5555;
        const int TargetPort = 8888;

        public static bool messageReceived = false;


        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to PSI File Receiver Program");

            //Get info:
            Console.Write($"Entry listening port [{ListeningPort}]: ");
            string port_string = Console.ReadLine();

            int port = string.IsNullOrEmpty(port_string) ? ListeningPort : Convert.ToInt32(port_string);

            UdpFileReceiver receiver = new UdpFileReceiver();
            receiver.Port = port;

            receiver.Connected += OnReceiverConnected;
            receiver.FileReceived += OnReceiverFileReceived;
            
            Console.WriteLine("Listening at port {0}", port);

            Console.Write($"Desired Packet Length [{UdpFileReceiver.DefaultPacketLength}]: ");
            string pck_length_str = Console.ReadLine();
            receiver.MaxPacketLength = string.IsNullOrEmpty(pck_length_str) ? UdpFileReceiver.DefaultPacketLength : Convert.ToUInt32(pck_length_str);

            Console.Write($"Desired Packet Count [{UdpFileReceiver.DefaultPacketCount}]: ");
            string pck_count_str = Console.ReadLine();
            receiver.MaxPackets = string.IsNullOrEmpty(pck_count_str) ? UdpFileReceiver.DefaultPacketCount : Convert.ToUInt32(pck_count_str);
            

            Console.WriteLine("Waiting for connection");
            while(!receiver.IsConnected)
            { }
            Console.WriteLine("Connected to {0}:{1}", receiver.Target.Address, receiver.Target.Port);

            Console.WriteLine("Waiting for transfer...");
            while (!receiver.IsTransfering)
            { }
            Console.WriteLine("Transfer started.");
            Console.WriteLine($"Max Packet length: {receiver.MaxPacketLength}");
            Console.WriteLine($"Window size:       {receiver.MaxPackets}");

            while (receiver.IsTransfering)
            { }
            Console.WriteLine("Transfer finished, ending application.");



            ////dispose of connection

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        private static void OnReceiverFileReceived(object sender, FileReceivedEventArgs e)
        {
          
        }

        private static void OnReceiverConnected(object sender, EventArgs e)
        {
           
        }
    }
}

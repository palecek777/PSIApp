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
            //Get info:
            Console.Write("Entry listening port: ");
            string port_string = Console.ReadLine();

            int port = string.IsNullOrEmpty(port_string) ? ListeningPort : Convert.ToInt32(port_string);

            UdpFileReceiver receiver = new UdpFileReceiver();
            receiver.Port = port;

            receiver.Connected += OnReceiverConnected;
            receiver.FileReceived += OnReceiverFileReceived;
            
            Console.WriteLine("Listening at port {0}", port);

            Console.WriteLine("Waiting for connection");
            while(!receiver.IsConnected)
            { }
            Console.WriteLine("Connected...");

            Console.WriteLine("Waiting for transfer...");
            while (!receiver.IsTransfering)
            { }
            Console.WriteLine("Transfer started");
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

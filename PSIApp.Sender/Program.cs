using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSIApp
{
    /// <summary>
    /// PSIApp.Sender
    /// </summary>
    class Program
    {
        const int ListeningPort = 8888;
        const int TargetPort = 5555;

        static UdpClient Client;

        static bool Quit = false;

        static UdpFileSender file_sender;

        static void Main(string[] args)
        {
            file_sender = new UdpFileSender();
            //file_sender.Port = ListeningPort;

            // get IPAddress and port
            Console.Write("Entry IP Address [127.0.0.1]: ");
            string ip_string = Console.ReadLine();
            if (string.IsNullOrEmpty(ip_string)) ip_string = "127.0.0.1";

            //Console.WriteLine($"Target IP Address: {ip_string}");

            Console.Write($"Entry Port [{TargetPort}]: ");
            string port_string = Console.ReadLine();
            //if (string.IsNullOrEmpty(port_string)) port_string = "8888";
            int target_port = string.IsNullOrEmpty(port_string) ? TargetPort : Convert.ToInt32(port_string);
            //Console.WriteLine($"Target Port: {target_port}");


            IPEndPoint target_endpoint = new IPEndPoint(IPAddress.Parse(ip_string), target_port);

            Console.Write($"Desired Packet Length [{UdpFileSender.DefaultPacketLength}]: ");
            string pck_length_str = Console.ReadLine();
            uint pck_length = string.IsNullOrEmpty(pck_length_str) ? UdpFileSender.DefaultPacketLength : Convert.ToUInt32(pck_length_str);

            Console.Write($"Desired Packet Count [{UdpFileSender.DefaultPacketCount}]: ");
            string pck_count_str = Console.ReadLine();
            uint pck_count = string.IsNullOrEmpty(pck_count_str) ? UdpFileSender.DefaultPacketCount : Convert.ToUInt32(pck_count_str);



            file_sender = new UdpFileSender();
            file_sender.MaxPacketLength = pck_length;
            file_sender.MaxPackets = pck_count;
            file_sender.Port = ListeningPort;
            file_sender.Target = target_endpoint;

            Console.Write("Enter filename [test_img.png]: ");

            string filename = Console.ReadLine();
            filename = string.IsNullOrEmpty(filename) ? "test_img.png" : filename;

            Console.WriteLine($"Max Packet length: {file_sender.MaxPacketLength}");
            Console.WriteLine($"Window size:       {file_sender.MaxPackets}");

            file_sender.SendFile(filename);

            Console.WriteLine("File sent.");

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
    }
}

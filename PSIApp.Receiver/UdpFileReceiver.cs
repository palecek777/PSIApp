﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSIApp
{
    public class UdpFileReceiver
    {
        public const uint DefaultPacketLength = 2048;
        public const uint DefaultPacketCount = 128;

        private SmartUdpClient _client;
        private IPEndPoint _target;
        private int _port;

        private uint _max_packet_length;
        private uint _max_packets;

        private bool _is_transfering;
        private bool _is_connected;

        private uint ReceivedCount { get; set; }
        private DataPacket[] ReceivedPackets { get; set; }
        private uint AwaitedPacketNum { get; set; }
        private string FileName { get; set; }
        private uint TotalPacketCount { get; set; }


        public SmartUdpClient Client
        {
            get { return _client; }
        }

        //prijimame prave ted neco?
        public bool IsTransfering
        {
            get { return _is_transfering; }
        }

        //jsme pripojeni k senderovi?
        public bool IsConnected
        {
            get { return _is_connected; }
        }

        //port na kterem klient pracuje
        public int Port
        {
            get { return _port; }
            set
            {
                if (!IsTransfering)
                {
                    _port = value;
                    SetUpClient();
                }
            }
        }

        //maximalni delka packetu
        public uint MaxPacketLength
        {
            get { return _max_packet_length; }
            set
            {
                if (!IsTransfering)
                {
                    _max_packet_length = value;
                }
            }
        }

        //maximalni pocet packetu
        public uint MaxPackets
        {
            get { return _max_packets; }
            set
            {
                if (!IsTransfering)
                {
                    _max_packets = value;
                }
            }
        }

        public UdpFileReceiver()
        {
            _max_packets = DefaultPacketCount;
            _max_packet_length = DefaultPacketLength;

            file_mutex = new Mutex();
        }


        //korektne ukonci komunikcaci s receiverem
        private void Disconnect()
        {
            _client.DataReceived -= OnClientDataReceived;
            _client.Dispose();
            _client = null;
        }

        //nastartuje Udp klienta
        private void SetUpClient()
        {
            if (IsTransfering) return;

            if (_client != null)
            {
                Disconnect();
            }
            _client = new SmartUdpClient(Port);

            Client.DataReceived += OnClientDataReceived;
        }

        private void OnClientDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!MessageConstructor.ValidateMessage(e.Data))
            {
                return;
            }

            if (MessageConstructor.IsHandshake(e.Data))
            {
                Connect(BitConverter.ToUInt32(e.Data, 1), BitConverter.ToUInt32(e.Data, 1 + sizeof(int)), e.EndPoint);
                return;
            }

            if (IsTransfering && MessageConstructor.IsFileData(e.Data))
            {
                ReceivePacket(MessageConstructor.GetPacket(e.Data));
                return;
            }

            if (MessageConstructor.IsFileMeta(e.Data))
            {
                TotalPacketCount = BitConverter.ToUInt32(e.Data, 1 + sizeof(long));


                string fn = Encoding.ASCII.GetString(e.Data, 1 + sizeof(long) + sizeof(uint), e.Data.Length - 1 - sizeof(long) - sizeof(uint) - sizeof(uint));
                PrepareFile(fn);

                ReceivedPackets = new DataPacket[TotalPacketCount];

                Console.WriteLine($"Receiving file '{fn}', total of {TotalPacketCount} packets.");
                WriteStatus(0, TotalPacketCount, false);
                Client.Send(e.Data, e.Data.Length);
            }

            if (IsTransfering && MessageConstructor.IsFileEnd(e.Data))
            {

                WriteFile();

                uint hash = BitConverter.ToUInt32(e.Data, 1);

                uint computed = MessageConstructor.GetFileHash(FileName);

                if (hash == computed)
                {
                    FileReceived?.Invoke(this, new FileReceivedEventArgs() { FileName = FileName });
                    _is_transfering = false;
                }
                else
                {
                    Console.WriteLine("Something is wrong, received different hash.");
                    _is_transfering = false;
                }
            }
        }


        //pripoji Clienta na adresu receivera
        private void Connect(uint length, uint count, IPEndPoint ip)
        {
            MaxPackets = Math.Min(MaxPackets, count);
            MaxPacketLength = Math.Min(MaxPacketLength, length);
            byte[] message = MessageConstructor.GetHandshake(MaxPacketLength, MaxPackets);
            Client.Connect(ip);
            Client.Send(message, message.Length);

            _is_connected = true;
            Connected?.Invoke(this, new EventArgs());

        }


        //Dictionary<uint, DataPacket> received_packets;
        //DataPacket[] packets;
        private void ReceivePacket(DataPacket packet)
        {
            ////je to packet, ktery ocekavam
            //if (packet.Number == aw_packet)// && !(aw_packet > total_packet_count))
            //{
            //    WriteData(packet);
            //    //WriteStatus(packet.Number, total_packet_count);
            //    aw_packet++;

            //    while (received_packets.Count > 0 && received_packets.Keys.Contains(aw_packet))
            //    {
            //        WriteData(received_packets[aw_packet]);
            //        received_packets.Remove(aw_packet);
            //        aw_packet++;
            //    }
            //}
            ////je to packet, ktery nyni necekam, dam si ho do ordered listu
            //else
            //{
            //    received_packets[packet.Number] = packet;
            //}

            ////if (packet.Number < total_packet_count)
            ////    _packets[packet.Number] = packet;

            //try
            //{
            //    _packets[packet.Number] = packet;
            //}
            //catch
            //{
            //    Console.WriteLine("Invalid packet number");
            //}

            if (ReceivedPackets[packet.Number] is null)
            {
                ReceivedPackets[packet.Number] = packet;
            }
            else
            {
                return;
            }

            UpdateAwaitedNum();

            WriteStatus(AwaitedPacketNum, TotalPacketCount);

            byte[] resp = MessageConstructor.GetDataReceived(packet.Number, AwaitedPacketNum);
            Client.Send(resp, resp.Length);
        }

        private void UpdateAwaitedNum()
        {
            while(AwaitedPacketNum < TotalPacketCount && ReceivedPackets[AwaitedPacketNum] != null)
            {
                AwaitedPacketNum++;
            }
        }

        private void PrepareFile(string filename)
        {
            //ReceivedPackets = new DataPacket[TotalPacketCount];
            File.Create(filename).Close();
            FileName = filename;
            AwaitedPacketNum = 0;
            _is_transfering = true;
        }

        Mutex file_mutex;
        private void WriteData(DataPacket packet)
        {
            return;

            file_mutex.WaitOne();
            WriteStatus(packet.Number, TotalPacketCount);
            using (BinaryWriter writer = new BinaryWriter(File.Open(FileName, FileMode.Append, FileAccess.Write)))
            {
                writer.Write(packet.Data, DataPacket.DataOffset, packet.DataLength);
            }
            file_mutex.ReleaseMutex();
        }

        private void WriteFile()
        {
            file_mutex.WaitOne();
            using (BinaryWriter writer = new BinaryWriter(File.Open(FileName, FileMode.Append, FileAccess.Write)))
            {
                for (int i = 0; i < ReceivedPackets.Length; ++i)
                {
                    ReceivedPackets[i].AckCount++;
                    writer.Write(ReceivedPackets[i].Data, 1 + sizeof(uint) + sizeof(uint), ReceivedPackets[i].Data.Length - 1 - sizeof(uint) - sizeof(uint) - sizeof(uint));
                }
            }
            file_mutex.ReleaseMutex();
        }


        private void WriteStatus(uint value, uint target, bool flush = true)
        {

            int width = Console.WindowWidth;

            if (flush)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                //for (int i = 0; i < width; ++i) Console.Write(" ");
            }


            Console.Write("[{0,6}/{1,6}] : ", value, target);
            double perc = (double)value / (double)target;

            for (int i = 0; i < (width - 30) * perc; ++i)
                Console.Write("#");

            Console.WriteLine(" {0} %", (int)(perc * 100));
        }

        public event FileReceivedEventHandler FileReceived;
        public event EventHandler Connected;
    }

    public delegate void FileReceivedEventHandler(object sender, FileReceivedEventArgs e);
    public class FileReceivedEventArgs : EventArgs
    {
        public string FileName { get; set; }
    }

}

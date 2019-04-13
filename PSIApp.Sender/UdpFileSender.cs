﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSIApp
{
    //1. vlakno dela pooling a kontroluje, jestli neprisla data - begin receive / if available - receive
    //2. vlakno posila pakety, jak jen muze

    public class UdpFileSender
    {
        public const uint DefaultPacketLength = 6000;
        public const uint DefaultPacketCount = 32;


        private SmartUdpClient _client;
        private IPEndPoint _target;
        private int _port;

        private uint _max_packet_length = DefaultPacketLength;
        private uint _max_packets = DefaultPacketCount;

        private bool _is_transfering = false;
        private bool _is_connected = false;

        private Mutex ClientMutex { get; set; }
        private Mutex PacketsMutex { get; set; }
        private HashSet<int> AckPackets { get; set; }

        private DataPacket[] _packets;

        //posilame prave ted neco?
        public bool IsTransfering
        {
            get { return _is_transfering; }
        }

        //jsme pripojeni k receiverovy?
        public bool IsConnected
        {
            get { return _is_connected; }
        }

        //objekt pouzivany pro komunikaci
        //to same jako zakladni UPD client, pridany event, ktery hlasi prijeti dat
        public SmartUdpClient Client
        {
            get { return _client; }
        }

        //adresa receivera
        // IP a Port
        public IPEndPoint Target
        {
            get { return _target; }
            set
            {
                if (!IsTransfering)
                {
                    _target = value;
                    Connect();
                }
            }
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

        //pripravene packety
        public DataPacket[] Packets
        {
            get { return _packets; }
        }

        public UdpFileSender()
        {
            ClientMutex = new Mutex();
            PacketsMutex = new Mutex();

            _max_packets = DefaultPacketCount;
            _max_packet_length = DefaultPacketLength;
        }

        //pripoji Clienta na adresu receivera
        byte[] handshake_response = null;
        private void Connect()
        {
            _is_connected = false;
            try
            {
                Client.Connect(Target);
                // navrhnu svoje hodnoty delky packetu a poctu packetu
                byte[] message = MessageConstructor.GetHandshake(MaxPacketLength, MaxPackets);
                Client.Send(message, message.Length);

                while (handshake_response == null)
                { }
                //TODO pridat nejaky timeout na handshake

                //if (MessageConstructor.ValidateMessage(handshake_response) &&  MessageConstructor.IsHandshake(handshake_response))
                //{
                //    // bud prijme moje => vrati mnou poslane hodnoty, nebo je snizi => Math.Min(...)
                //    MaxPacketLength = Math.Min(MaxPacketLength, BitConverter.ToInt32(handshake_response, 1));
                //    MaxPackets = Math.Min(MaxPackets, BitConverter.ToInt32(handshake_response, 1 + sizeof(int)));
                //}
                //else
                //{
                //    // pokud nevyjde CRC nebo message neni typu Hanshake, je neco spatne...
                //    throw new Exception("Invalid hanshake message from target");
                //}

                MaxPacketLength = Math.Min(MaxPacketLength, BitConverter.ToUInt32(handshake_response, 1));
                MaxPackets = Math.Min(MaxPackets, BitConverter.ToUInt32(handshake_response, 1 + sizeof(int)));

                handshake_response = null;

                //dispose of older packets
                if (Packets != null)
                {
                    DisposePackets();
                }


                _packets = new DataPacket[MaxPackets];
                for(int i = 0; i < MaxPackets; ++i)
                {
                    _packets[i] = new DataPacket();
                    _packets[i].Timeout += OnPacketTimeout;
                }

                _is_connected = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error during connection to '{Target}': {e.Message}");                
            }
        }

        // zrusi prepripravene packety
        private void DisposePackets()
        {
            foreach (DataPacket pck in Packets)
            {
                pck.Timeout -= OnPacketTimeout;
            }
        }

        // zavola se, kdyz je timeout packetu
        private void OnPacketTimeout(object sender, EventArgs e)
        {
            DataPacket packet = (DataPacket)sender;
            Client.SendPacket(packet);
        }

        //korektne ukonci komunikcaci s receiverem
        private void Disconnect()
        {
            DisposePackets();
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

            if (Target != null)
                Connect();
        }

        //called if there are new data
        private void OnClientDataReceived(object sender, DataReceivedEventArgs e)
        {
            //neplatne zpravy zahazujeme
            if (!MessageConstructor.ValidateMessage(e.Data))
            {
                return;
            }

            if (MessageConstructor.IsHandshake(e.Data))
            {
                handshake_response = e.Data;
                return;
            }

            if (MessageConstructor.IsDataReceived(e.Data))
            {
                uint packet_num = BitConverter.ToUInt32(e.Data, 1);

                bool is_pending = false;

                PacketsMutex.WaitOne();
                for(int i = 0; i < MaxPackets; ++i)
                {
                    if (Packets[i].AckCount == -1)
                        continue;
                    //if (Packets[i].Number == packet_num)
                    //{
                    //    is_pending = true;
                    //    Packets[i].OnReceive();
                    //    packet_num--;
                    //    AckPackets.Add(i);
                    //}

                    if (Packets[i].Number < packet_num)
                    {
                        Packets[i].OnReceive();
                        packet_num--;
                        AckPackets.Add(i);
                        ack_count++;
                    }
                    else if (Packets[i].Number == packet_num)
                    {
                        Packets[i].AckCount++;
                        if (Packets[i].AckCount >= 3)
                        {
                            Client.SendPacket(Packets[i]);
                        }
                        is_pending = true;
                    }
                }

                if (!is_pending)
                {

                }

                PacketsMutex.ReleaseMutex();
                //Console.WriteLine($"Received ACK of {packet_num}");
                return;
            }

            if (MessageConstructor.IsFileMeta(e.Data))
            {
                meta_response = e.Data;
                return;
            }
            
        }

        //spusti posilani souboru
        byte[] meta_response = null;
        uint ack_count = 0;
        uint last_ack = 0;
        public void SendFile(string path)
        {
            ack_count = 0;
            _is_transfering = true;
            string file_name = Path.GetFileName(path);

            // poslu FileMeta msg a pockam na odpoved - Handshake se stejnymi hodnotami
            FileInfo finfo = new FileInfo(path);
            uint pack_cnt = (uint) (finfo.Length / MaxPacketLength + (finfo.Length % MaxPacketLength > 0 ? 1 : 0));

            //zkratit file_name, aby se veslo do maximalni delky packetu
            if (file_name.Length > (MaxPacketLength - sizeof(byte) - sizeof(long) - sizeof(int) - sizeof(uint)))
            {
                file_name = file_name.Substring((int)(MaxPacketLength - sizeof(byte) - sizeof(long) - sizeof(int) - sizeof(uint)));
            }

            byte[] msg = MessageConstructor.GetFileMeta(finfo.Length, pack_cnt, file_name);
            Client.Send(msg, msg.Length);

            while(meta_response == null)
            { }

            if (!Extensions.AreSameArrays(msg, meta_response))
            {
                meta_response = null;
                throw new Exception("Invalid FileMeta response");
            }

            meta_response = null;

            // kdykoliv to bude mozne, tak budu posilat packety...
            // array - vsechny aktualni nepotvrzene packety
            uint packet_num = 0;
            // v tomto HashSetu jsou indexy packetu, ktere jsou potvrzene, tudiz je mozne je pouzit
            AckPackets = new HashSet<int>();
            for (int i = 0; i < MaxPackets; ++i) AckPackets.Add(i);


            int buffer_size = (int) MaxPacketLength - //packet length
                              1 -               //packet type
                              sizeof(uint) -     //number of packet
                              sizeof(uint) -     //data length
                              sizeof(uint);     //CRC

            //indkuje, ze je potreba zrusit nejaky cyclus, pouziti ruzne
            bool break_cycle;

            WriteStatus(ack_count, pack_cnt, false);

            using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read)))
            {
                byte[] read_buffer = new byte[buffer_size];
                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    reader.Read(read_buffer, 0, buffer_size);

                    // cekam na volny packet
                    int idx;
                    while (true)
                    {
                        PacketsMutex.WaitOne();
                        break_cycle = AckPackets.Count > 0;
                        PacketsMutex.ReleaseMutex();
                        if (break_cycle) break;
                        Thread.Sleep(20);
                    }

                    PacketsMutex.WaitOne();
                    idx = AckPackets.Pop();
                    PacketsMutex.ReleaseMutex();

                    //Nastvaim hodnoty volneho packetu
                    // nemelo by byt potreba nastavovat PacketMutext, protoze jina vlakna by v tento moment nemela pristupovat k datum ktera chci upravovat
                    //PacketsMutex.WaitOne();

                    Packets[idx].Data = MessageConstructor.GetFileData(packet_num, read_buffer);
                    Packets[idx].Number = packet_num;
                    Packets[idx].AckCount = 0;

                    //PacketsMutex.ReleaseMutex();

                    Client.SendPacket(Packets[idx]);

                    //zvysuji cislo packetu
                    packet_num++;

                    if (packet_num % 2 == 0)
                    {
                        WriteStatus(ack_count, pack_cnt);
                    }
                }
            }

            //v tento moment jsou vsechnz packety dorucene ci na ceste, cekam na ack vsech prave posilanych
            
            while (true)
            {
                PacketsMutex.WaitOne();
                // tedy vsechny packety jsou volne
                break_cycle = AckPackets.Count >= MaxPackets;
                PacketsMutex.ReleaseMutex();
                if (break_cycle) break;
                Thread.Sleep(20);
            }

            // poslu FileEnd msg
            //TODO: vypocet File hash
            byte[] file_end = MessageConstructor.GetFileEnd(MessageConstructor.GetFileHash(file_name));
            Client.Send(file_end, file_end.Length);

            // jine vlakno prijme reakci na file_end message a pokud to nebude uspokojiva odpoved, zopakuje zaslani...
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

            for (int i = 0; i < (width - 30)* perc; ++i)
                Console.Write("#");

            Console.WriteLine(" {0} %", (int)(perc * 100));
        }
    }
}
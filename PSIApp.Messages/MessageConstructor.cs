using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CRC;

namespace PSIApp
{
    /// <summary>
    /// Vyrabi zpravy
    /// </summary>
    public static class MessageConstructor
    {
        // typy zprav                 // Format zpravy
        const byte Handshake = 0; // [Handshake, [MaxPacketLength - 4B], [MaxPackets - 4B], [CRC/Hash... - 4B]
        const byte FileMeta = 1; // [FileMeta, [Length - 8B], [PacketCount - 4B], [Name - ...], [CRC/Hash... - 4B]
        const byte FileData = 2; // [FileData, [Packet # - 4B]], [Data Length - 4B], [Data - ...], [CRC/Hash... - 4B]
        const byte FileEnd = 3; // [FileEnd, [Hash - 4B], [CRC/Hash... - 4B]
        const byte ConnectionEnd = 4; // [ConnectionEnd, [CRC/Hash... - 4B]
        const byte DataReceived = 5; // [DataReceived, [Packet # 4B], [CRC/Hash... - 4B]

        // otestuje hodnotu Type bytu
        public static bool IsHandshake(byte[] msg) {  return msg[0] == Handshake; }
        public static bool IsFileMeta(byte[] msg) {  return msg[0] == FileMeta; }
        public static bool IsFileData(byte[] msg) { return msg[0] == FileData; }
        public static bool IsFileEnd(byte[] msg) {  return msg[0] == FileEnd; }
        public static bool IsConnectionEnd(byte[] msg) {  return msg[0] == ConnectionEnd; }
        public static bool IsDataReceived(byte[] msg) {  return msg[0] == DataReceived; }

        static uint ComputeCrc(byte[] data)
        {
            return Crc32.Compute(data);
        }

        public static uint GetFileHash(string path)
        {
            byte[] hash = Crc32.ComputeFileHash(path);
            return BitConverter.ToUInt32(hash, 0);
        }

        public static bool ValidateMessage(byte[] message)
        {
            uint rec = BitConverter.ToUInt32(message, message.Length - sizeof(uint));


            byte[] data = new byte[message.Length];
            message.CopyTo(data, 0);
            for (int i = 1; i < sizeof(uint) + 1; ++i) data[data.Length - i] = 0;

            uint crc = ComputeCrc(data);
            return rec == crc;
            //uint val = rec ^ crc;

            //return (rec ^ crc) == 0;
        }

        public static byte[] GetHandshake(uint packet_length, uint packet_count)
        {
            byte[] message = new byte[1 +               // msg type         - byte (char)
                                      sizeof(uint) +     // packet length    - int
                                      sizeof(uint) +     // max packet count - int
                                      sizeof(uint)      // CRC              - uint
                                      ];

            message[0] = Handshake;

            uint i = ComputeCrc(message);

            BitConverter.GetBytes(packet_length).CopyTo(message, 1);
            BitConverter.GetBytes(packet_count).CopyTo(message, 1 + sizeof(uint));
            BitConverter.GetBytes(ComputeCrc(message)).CopyTo(message, 1 + sizeof(uint) + sizeof(uint));

            return message;
        }

        public static byte[] GetFileMeta(long length, uint packet_count, string name)
        {
            byte[] message = new byte[1 +               // msg type         - byte (char)
                                      sizeof(long) +    // file size        - long
                                      sizeof(uint) +     // packet count     - int
                                      name.Length +     // file name        - char[]
                                      sizeof(uint)      // CRC              - uint
                                      ];

            message[0] = FileMeta;
            BitConverter.GetBytes(length).CopyTo(message, 1);
            BitConverter.GetBytes(packet_count).CopyTo(message, 1 + sizeof(long));
            Encoding.ASCII.GetBytes(name).CopyTo(message, 1 + sizeof(long) + sizeof(uint));
            BitConverter.GetBytes(ComputeCrc(message)).CopyTo(message, 1 + sizeof(long) + sizeof(uint) + name.Length);

            return message;
        }
        
        public static byte[] GetFileData(uint packet_number, byte[] data)
        {
            byte[] message = new byte[1 +               // msg type         - byte (char)
                                      sizeof(uint) +    // packet number    - uint
                                      sizeof(uint) +    // data length      - uint
                                      data.Length +     // data             - byte[] (char[])  
                                      sizeof(uint)      // CRC              - uint
                                      ];

            message[0] = FileData;
            BitConverter.GetBytes(packet_number).CopyTo(message, 1);
            BitConverter.GetBytes((uint)data.Length).CopyTo(message, 1 + sizeof(uint));
            data.CopyTo(message, 1 + sizeof(uint) + sizeof(uint));
            BitConverter.GetBytes(ComputeCrc(message)).CopyTo(message, 1 + sizeof(uint) + sizeof(uint) + data.Length);
            return message;
        }
        
        public static byte[] GetFileEnd(uint hash)
        {
            byte[] message = new byte[1 +               // msg type         - byte (char)
                                      sizeof(uint) +     // file hash        - int
                                      sizeof(uint)      // CRC              - uint
                                      ];

            message[0] = FileEnd;
            BitConverter.GetBytes(hash).CopyTo(message, 1);            
            BitConverter.GetBytes(ComputeCrc(message)).CopyTo(message, 1 + sizeof(uint));
            return message;
        }
        
        public static byte[] GetConnectioNEnd()
        {
            byte[] message = new byte[1 + sizeof(uint)];
            message[0] = ConnectionEnd;
            BitConverter.GetBytes(ComputeCrc(message)).CopyTo(message, 1);
            return message;
        }

        public static byte[] GetDataReceived(uint packet)
        {
            byte[] message = new byte[1 + sizeof(uint) + sizeof(uint)];
            message[0] = DataReceived;
            BitConverter.GetBytes(packet).CopyTo(message, 1);
            BitConverter.GetBytes(ComputeCrc(message)).CopyTo(message, 1 + sizeof(int));
            return message;
        }

        public static DataPacket GetPacket(byte[] message)
        {
            DataPacket packet = new DataPacket();
            packet.Data = message;
            packet.Number = BitConverter.ToUInt32(message, 1);

            return packet;
        }
    }
}

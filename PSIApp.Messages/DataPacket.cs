using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;

namespace PSIApp
{
    public class DataPacket
    {
        //const byte FileData = 2; // [FileData, [Packet # - 4B]], [Data Length - 4B], [Data - ...], [CRC/Hash... - 4B]

        public const int DataOffset = sizeof(byte) + sizeof(uint) + sizeof(uint);
        public const int MiscLength = DataOffset + sizeof(uint);

        // v ms
        const double TimeoutInterval = 3000;

        // poradove cislo packetu
        public uint Number { get; set; }

        // obsah packetu
        public byte[] Data { get; set; }

        public int DataLength { get; set; }

        // pocet neprijeti od receivera
        // pokud prekroci magickou hranici 3 - posle se znovu i kdyz nebude timeout
        public int AckCount { get; set; }

        public event EventHandler Timeout;

        private Timer _timeout_timer;

        public DataPacket()
        {
            _timeout_timer = new Timer();
            _timeout_timer.Elapsed += TimeoutTimerElapsed;
            _timeout_timer.Interval = TimeoutInterval;
        }

        private void TimeoutTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _timeout_timer.Stop();
            Timeout?.Invoke(this, new EventArgs());
        }

        public void OnSend()
        {
            AckCount = 0;
            _timeout_timer.Start();
        }

        public void OnReceive()
        {
            AckCount = -1;
            _timeout_timer.Stop();
        }

        public override string ToString()
        {
            return string.Format("Packet : {0}, ACK {1}", Number, AckCount);
        }
    }

}

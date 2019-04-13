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
    public class SmartUdpClient : UdpClient
    {
        protected Mutex Mutex {get;set;}
        protected System.Timers.Timer PoolingTimer { get; set; }

        const double PoolingInterval = 2;

        #region Construction
        public SmartUdpClient()
        {
            Init();
        }

        public SmartUdpClient(AddressFamily family) : base(family)
        {
            Init();
        }

        public SmartUdpClient(int port) : base(port)
        {
            Init();
        }

        public SmartUdpClient(IPEndPoint localEP) : base(localEP)
        {
            Init();
        }

        public SmartUdpClient(int port, AddressFamily family) : base(port, family)
        {
            Init();
        }

        public SmartUdpClient(string hostname, int port) : base(hostname, port)
        {
            Init();
        }

        protected void Init()
        {
            Mutex = new Mutex();
            PoolingTimer = new System.Timers.Timer();
            PoolingTimer.Interval = PoolingInterval;
            PoolingTimer.Elapsed += OnPoolingElapsed;
            PoolingTimer.Start();
        }

        #endregion Construction

        public event DataReceivedEventHandler DataReceived;

        private void OnPoolingElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if(Available > 0)
            {
                OnDataReceived();
            }
        }

        //posle jeden packet pomoci klienta
        public void SendPacket(DataPacket packet)
        {
            // Mutex - thread safety 
            Mutex.WaitOne();
            // posilam data
            Send(packet.Data, packet.Data.Length);
            packet.OnSend();
            Mutex.ReleaseMutex();

            //Console.WriteLine($"Sending packet: {packet.Number}");
        }

        protected virtual void OnDataReceived()
        {
            IPEndPoint sender = default(IPEndPoint);
            byte[] msg = Receive(ref sender);

            DataReceived?.Invoke(this, new DataReceivedEventArgs() {Data = msg, EndPoint = sender });
        }

        protected override void Dispose(bool disposing)
        {
            PoolingTimer.Stop();
            PoolingTimer.Dispose();
            base.Dispose(disposing);
        }
    }

    public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);
    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
        public IPEndPoint EndPoint { get; set; }
    }
}

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
    public class SmartUdpClient : IDisposable// : UdpClient
    {
        protected Mutex Mutex {get;set;}
        protected System.Timers.Timer PoolingTimer { get; set; }

        protected UdpClient Client { get; set; }

        const double PoolingInterval = 2;

        public double ErrorRate = -1.0;
        public double DropRate = -1.0;

        #region Construction
        public SmartUdpClient()
        {
            Client = new UdpClient();
            Init();
        }

        public SmartUdpClient(AddressFamily family)// : base(family)
        {
            Client = new UdpClient(family);
            Init();
        }

        public SmartUdpClient(int port)// : base(port)
        {
            Client = new UdpClient(port);
            Init();
        }

        public SmartUdpClient(IPEndPoint localEP)// : base(localEP)
        {
            Client = new UdpClient(localEP);
            Init();
        }

        public SmartUdpClient(int port, AddressFamily family)// : base(port, family)
        {
            Client = new UdpClient(port, family);
            Init();
        }

        public SmartUdpClient(string hostname, int port)// : base(hostname, port)
        {
            Client = new UdpClient(hostname, port);
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
            if(Client.Available > 0)
            {
                OnDataReceived();
            }
        }

        //posle jeden packet pomoci klienta
        public void SendPacket(DataPacket packet, IPEndPoint ip)
        {
            // Mutex - thread safety 
            Mutex.WaitOne();
            // posilam data
            Send(packet.Data, packet.Data.Length, ip);
            packet.OnSend();
            Mutex.ReleaseMutex();

            //Console.WriteLine($"Sending packet: {packet.Number}");
        }

        protected virtual void OnDataReceived()
        {
            IPEndPoint sender = default(IPEndPoint);
            byte[] msg = Client.Receive(ref sender);

            DataReceived?.Invoke(this, new DataReceivedEventArgs() {Data = msg, EndPoint = sender });
        }

        public virtual void Connect(IPEndPoint host)
        {
            Client.Connect(host);
        }

        public virtual void Send(byte[] data, int count)
        {
            data = MessageErrorGenerator.ProccessMessage(data, ErrorRate, DropRate);

            if (data == null) return;

            Client.Send(data, count);
        }

        public virtual void Send(byte[] data, int count, IPEndPoint host)
        {
            data = MessageErrorGenerator.ProccessMessage(data, ErrorRate, DropRate);

            if (data == null) return;

            Client.Send(data, count, host);
        }

        public virtual void Send(byte[] data, int count, string host, int port)
        {
            data = MessageErrorGenerator.ProccessMessage(data, ErrorRate, DropRate);

            if (data == null) return;

            Client.Send(data, count, host, port);
        }

        public void Dispose()
        {
            PoolingTimer.Stop();
            PoolingTimer.Dispose();

            Client.Dispose();

            Mutex.Dispose();
        }

        //protected override void Dispose(bool disposing)
        //{
        //    PoolingTimer.Stop();
        //    PoolingTimer.Dispose();
        //    base.Dispose(disposing);
        //}
    }

    public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);
    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
        public IPEndPoint EndPoint { get; set; }
    }
}

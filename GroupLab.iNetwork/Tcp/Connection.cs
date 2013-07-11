using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using GroupLab.iNetwork.Service;

namespace GroupLab.iNetwork.Tcp
{
    #region Connection Event Delegates
    #region Delegates
    public delegate void SingleConnectionDiscoveryEventHandler(Connection connection);

    public delegate void MultipleConnectionDiscoveryEventHandler(List<Connection> connections);
    #endregion
    #endregion

    #region Class 'Connection'
    public class Connection
    {
        #region Static Class Members
        private static object SynchronizationObject = new object();
        #endregion

        #region Class Members
        #region Network Class Members
        private Server _serv;

        private TcpClient _sock;
        #endregion

        #region Address Class Members
        private IPAddress _ipAddress;

        private int _port;

        private IPEndPoint _endPoint;
        #endregion

        #region Stream Class Members
        private Stream _netStrm;

        private StreamReader _reader;

        private StreamWriter _writer;
        #endregion

        #region Control Class Members
        private bool _running = false;

        private bool _remote = false;
        #endregion

        #region Thread Class Members
        private Thread _receivingThread;
        #endregion
        #endregion

        #region Events
        public event ConnectionEventHandler Connected;

        public event ConnectionMessageEventHandler MessageReceived;

        internal event ConnectionMessageEventHandler InternalMessageReceived;
        #endregion

        #region Constructors
        public Connection(string ipAddress, int port)
            : this(IPAddress.Parse(ipAddress), port)
        { }

        public Connection(IPAddress ipAddress, int port)
        {
            this._serv = null;
            this._remote = true;

            this._ipAddress = ipAddress;
            this._port = port;
        }

        internal Connection(Server server, TcpClient socket)
        {
            this._serv = server;
            this._sock = socket;

            this._endPoint = (IPEndPoint)socket.Client.RemoteEndPoint;
            this._ipAddress = this._endPoint.Address;
            this._port = this._endPoint.Port;

            this._remote = false;
        }
        #endregion

        #region Properties
        internal IPEndPoint RemoteEndPoint
        {
            get { return this._endPoint; }
        }

        internal Server Server
        {
            get { return this._serv; }
        }

        internal bool IsRunning
        {
            get { return this._running; }
        }

        internal bool IsRemote
        {
            get { return this._remote; }
        }
        #endregion

        #region Start/Stop Methods
        public void Start()
        {
            if (!(this._running))
            {
                if (this._remote)
                {
                    this._sock = new TcpClient(this._ipAddress.ToString(), this._port);
                    this._endPoint = (IPEndPoint)this._sock.Client.RemoteEndPoint;
                }

                this._netStrm = this._sock.GetStream();
                this._reader = new StreamReader(this._netStrm);
                this._writer = new StreamWriter(this._netStrm);

                this._running = true;

                this._receivingThread = new Thread(new ThreadStart(this.Receive));
                this._receivingThread.IsBackground = true;
                this._receivingThread.Name = "TcpConnection#receivingThread_0";
                this._receivingThread.Start();

                if (this._remote
                    && Connected != null)
                {
                    Connected(this, new ConnectionEventArgs(
                        this, ConnectionEvents.Connect));
                }
            }
        }

        public void Stop()
        {
            this._running = false;
            if (this._reader != null)
            {
                try
                {
                    this._reader.Close();
                    this._reader.Dispose();
                }
                catch (Exception)
                { }
                finally
                {
                    this._reader = null;
                }
            }

            if (this._writer != null)
            {
                try
                {
                    this._writer.Close();
                    this._writer.Dispose();
                }
                catch (Exception)
                { }
                finally
                {
                    this._writer = null;
                }
            }

            if (this._netStrm != null)
            {
                try
                {
                    this._netStrm.Close();
                    this._netStrm.Dispose();
                }
                catch (Exception)
                { }
                finally
                {
                    this._netStrm = null;
                }
            }

            if (this._sock != null)
            {
                try
                {
                    this._sock.Close();
                }
                catch (Exception)
                { }
                finally
                {
                    this._sock = null;
                }
            }

            if (this._receivingThread != null)
            {
                try
                {
                    this._receivingThread.Join(100);
                    if (this._receivingThread != null
                        && this._receivingThread.IsAlive)
                    {
                        this._receivingThread.Abort();
                    }
                }
                catch (Exception)
                { }
                finally
                {
                    this._receivingThread = null;
                }
            }

            if (this._remote
                && Connected != null)
            {
                Connected(this, new ConnectionEventArgs(this, ConnectionEvents.Disconnect));
            }
        }

        internal void Remove()
        {
            this._serv.RemoveConnection(this);
        }
        #endregion

        #region Send/Receive Methods
        #region Send Methods
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SendMessage(Message msg)
        {
            try
            {
                byte[] sendBytes = msg.ToByteArray();
                this._netStrm.Write(sendBytes, 0, sendBytes.Length);
            }
            catch (Exception)
            {
                this._running = false;
                if (this._serv != null)
                {
                    this._serv.RemoveConnection(this);
                }
            }
        }
        #endregion

        #region Receive Methods
        #region Read Message Methods
        private byte[] ReadBytesFromStream(int length, Stream strm)
        {
            byte[] bytes = new byte[length];

            int read = 0;
            while (read < length)
            {
                int n = strm.Read(bytes, read, length - read);
                if (n < 0)
                {
                    throw new IOException("Can't read the message data");
                }
                read += n;
            }
            return bytes;
        }

        private MessageHeader ReadMessageHeader(Stream strm)
        {
            int totalLength = strm.ReadByte() << 24;
            totalLength |= (strm.ReadByte() << 16);
            totalLength |= (strm.ReadByte() << 8);
            totalLength |= strm.ReadByte();

            int controlFlag = strm.ReadByte();

            int nameLength = strm.ReadByte() << 8;
            nameLength |= strm.ReadByte();

            byte[] nameBytes = ReadBytesFromStream(nameLength, strm);
            string name = Encoding.UTF8.GetString(nameBytes);

            return (new MessageHeader(totalLength, (controlFlag == 1), name));
        }
        #endregion

        #region Thread Methods
        private void OnMessageReceived(Message message)
        {
            if (!(message.IsInternal)
                && MessageReceived != null)
            {
                MessageReceived(this, message);
            }
            else if (message.IsInternal
                && InternalMessageReceived != null)
            {
                InternalMessageReceived(this, message);
            }
        }

        private void UpdateMessages(object obj)
        {
            if (obj is Message)
            {
                OnMessageReceived((Message)obj);
            }

            obj = null;
            GC.Collect();
        }

        private void Receive()
        {
            while (this._running)
            {
                try
                {
                    MessageHeader header = ReadMessageHeader(this._netStrm);
                    byte[] content = ReadBytesFromStream(header.ContentLength, this._netStrm);

                    Message message = Message.FromStream(header, content);
                    if (message != null)
                    {
                        if (NetworkConstants.MultiThreaded)
                        {
                            Thread updater = new Thread(new ParameterizedThreadStart(UpdateMessages));
                            updater.IsBackground = true;
                            updater.Start(message);
                        }
                        else
                        {
                            OnMessageReceived(message);

                            message = null;
                            GC.Collect();
                        }
                    }
                }
                catch (Exception)
                {
                    if (NetworkConstants.PrintLog)
                    {
                        Console.WriteLine("Connection closed...");
                    }

                    this._running = false;
                    if (this._serv != null)
                    {
                        this._serv.RemoveConnection(this);
                    }
                }
            }
        }
        #endregion
        #endregion
        #endregion

        #region Discovery Methods
        #region Discover 'ONE' Methods
        public static void Discover(SingleConnectionDiscoveryEventHandler eventHandler)
        {
            Connection.Discover(NetworkConstants.DiscoveryTimeout,
                null, eventHandler);
        }

        public static void Discover(int timeout,
            SingleConnectionDiscoveryEventHandler eventHandler)
        {
            Connection.Discover(NetworkConstants.DiscoveryTimeout,
                null, eventHandler);
        }

        public static void Discover(string serverName,
            SingleConnectionDiscoveryEventHandler eventHandler)
        {
            Connection.Discover(NetworkConstants.DiscoveryTimeout,
                serverName, eventHandler);
        }

        public static void Discover(int timeout, string serverName,
            SingleConnectionDiscoveryEventHandler eventHandler)
        {
            DiscoveryAgent agent = new DiscoveryAgent(serverName);
            agent.Tag = eventHandler;
            agent.DiscoveryUpdate += new DiscoveryEventHandler(OnDiscoveryUpdate);
            agent.Discover(DiscoveryType.Single, ServerType.Tcp, timeout);
        }
        #endregion

        #region Discover 'ALL' Methods
        public static void DiscoverAll(MultipleConnectionDiscoveryEventHandler eventHandler)
        {
            Connection.DiscoverAll(NetworkConstants.DiscoveryTimeout, eventHandler);
        }

        public static void DiscoverAll(int timeout,
            MultipleConnectionDiscoveryEventHandler eventHandler)
        {
            Connection.DiscoverAll(NetworkConstants.DiscoveryTimeout, null, eventHandler);
        }

        public static void DiscoverAll(string serverName,
            MultipleConnectionDiscoveryEventHandler eventHandler)
        {
            Connection.DiscoverAll(NetworkConstants.DiscoveryTimeout, serverName, eventHandler);
        }

        public static void DiscoverAll(int timeout, string serverName,
            MultipleConnectionDiscoveryEventHandler eventHandler)
        {
            DiscoveryAgent agent = new DiscoveryAgent(serverName);
            agent.Tag = eventHandler;
            agent.DiscoveryUpdate += new DiscoveryEventHandler(OnDiscoveryUpdate);
            agent.Discover(DiscoveryType.Multiple, ServerType.Tcp, timeout);
        }
        #endregion

        #region Event Handler
        private static void OnDiscoveryUpdate(object sender, DiscoveryEventArgs e)
        {
            if (sender is DiscoveryAgent)
            {
                DiscoveryAgent agent = sender as DiscoveryAgent;
                if (e.Type == DiscoveryType.Single
                    && agent.Tag != null
                    && agent.Tag is SingleConnectionDiscoveryEventHandler)
                {
                    if (e.Results.Count > 0)
                    {
                        ((SingleConnectionDiscoveryEventHandler)agent.Tag).Invoke(
                            new Connection(e.Results[0].IPAddress, e.Results[0].Port));
                    }
                    else
                    {
                        ((SingleConnectionDiscoveryEventHandler)agent.Tag).Invoke(null);
                    }
                }
                else if (e.Type == DiscoveryType.Multiple
                    && agent.Tag != null
                    && agent.Tag is MultipleConnectionDiscoveryEventHandler)
                {
                    List<Connection> connections = new List<Connection>();
                    foreach (DiscoveryResult result in e.Results)
                    {
                        connections.Add(new Connection(result.IPAddress, result.Port));
                    }

                    ((MultipleConnectionDiscoveryEventHandler)agent.Tag).Invoke(connections);
                }
            }
        }
        #endregion
        #endregion

        #region OVerridden Methods (Object)
        public override string ToString()
        {
            return this._endPoint.Address.ToString() + ":" 
                + this._endPoint.Port.ToString();
        }
        #endregion
    }
    #endregion
}

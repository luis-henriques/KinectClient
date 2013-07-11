using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using GroupLab.iNetwork.Service;

namespace GroupLab.iNetwork.Tcp
{
    #region Class 'Server'
    public class Server
    {
        #region Class Members
        #region Network Class Members
        private TcpListener _servSock;

        private List<Connection> _connections;

        private NetworkConfiguration _netCfg;

        private DiscoveryAgent _discoveryAgent;

        private string _name;
        #endregion

        #region Control Class Members
        private bool _running = false;
        #endregion

        #region Thread Class Members
        private Thread _acceptThread;
        #endregion
        #endregion

        #region Events
        public event ConnectionEventHandler Connection;
        #endregion

        #region Constructors
        public Server(string name)
            : this(name, -1)
        { }

        public Server(string name, int port)
        {
            if (name == null
                || name.Equals(""))
            {
                throw new ConfigurationException("The 'Server' must have a name.");
            }

            this._name = name;

            IPAddress[] ipAddrs = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            IPAddress correctAddress = null;
            for (int i = 0; i < ipAddrs.Length; i++)
            {
                if (!(ipAddrs[i].IsIPv6LinkLocal)
                    && (ipAddrs[i].GetAddressBytes().Length == 4))
                {
                    correctAddress = ipAddrs[i];
                }
            }

            int correctPort = InitializeServer(correctAddress, port);
            this._netCfg = new NetworkConfiguration(correctAddress, correctPort);
        }

        public Server(string name, IPAddress ipAddress)
            : this(name, ipAddress, -1)
        { }

        public Server(string name, IPAddress ipAddress, int port)
        {
            if (name == null
                || name.Equals(""))
            {
                throw new ConfigurationException("The 'Server' must have a name.");
            }

            this._name = name;
            
            int correctPort = InitializeServer(ipAddress, port);
            this._netCfg = new NetworkConfiguration(ipAddress, correctPort);
        }
        #endregion

        #region Initialization
        private int InitializeServer(IPAddress ipAddr)
        {
            return (InitializeServer(ipAddr, -1));
        }

        private int InitializeServer(IPAddress ipAddr, int port)
        {
            if (port < 1)
            {
                port = NetworkConstants.StartPort;
            }

            bool portValid = false;
            while (!(portValid))
            {
                try
                {
                    // this._servSock = new TcpListener(ipAddr, port);
                    this._servSock = new TcpListener(IPAddress.Any, port);
                    this._servSock.Start();
                    portValid = true;

                    return port;
                }
                catch (Exception)
                {
                    portValid = false;
                    port += NetworkConstants.PortStepping;
                }
            }

            return -1;
        }
        #endregion

        #region Properties
        public NetworkConfiguration Configuration
        {
            get { return this._netCfg; }
        }

        public bool IsRunning
        {
            get { return this._running; }
        }

        public bool IsDiscoverable
        {
            get { return this._discoveryAgent != null; }
            set
            {
                if (value && this._discoveryAgent == null)
                {
                    this._discoveryAgent = new DiscoveryAgent(
                        this._name, this._netCfg, ServerType.Tcp);
                    if (!(this._discoveryAgent.IsConnected))
                    {
                        throw new Exception("Could not start the service 'Discovery'...");
                    }
                }
                else if (!(value) && this._discoveryAgent != null)
                {
                    this._discoveryAgent.Shutdown();
                }
            }
        }

        internal List<Connection> Connections
        {
            get { return this._connections; }
        }
        #endregion

        #region Static Properties
        public static IPAddress[] AllAddresses
        {
            get { return Dns.GetHostEntry(Dns.GetHostName()).AddressList; }
        }
        #endregion

        #region Start/Stop Methods
        public void Start()
        {
            this._connections = new List<Connection>();

            this._acceptThread = new Thread(new ThreadStart(this.Accept));
            this._acceptThread.Name = "TcpServer_"
                + this._netCfg.IPAddress + "_" + this._netCfg.Port;
            this._acceptThread.IsBackground = true;
            this._acceptThread.Start();
        }

        public void Stop()
        {
            if (this._running)
            {
                this._running = false;

                for (int i = 0; i < this._connections.Count; i++)
                {
                    Connection connection = this._connections[i];
                    connection.Stop();
                    connection = null;
                }

                if (this._discoveryAgent != null)
                {
                    this._discoveryAgent.Shutdown();
                    this._discoveryAgent = null;
                }

                if (this._servSock != null)
                {
                    this._servSock.Stop();
                    this._servSock = null;
                }

                if (this._acceptThread != null)
                {
                    this._acceptThread.Join(100);
                    if (this._acceptThread != null
                        && this._acceptThread.IsAlive)
                    {
                        this._acceptThread.Abort();
                        this._acceptThread = null;
                    }
                }
            }
        }
        #endregion

        #region Connection Methods
        #region Broadcast Methods
        public void BroadcastMessage(Message message, Connection excludedReceiver)
        {
            List<Connection> excludedReceivers = null;
            if (excludedReceiver != null)
            {
                excludedReceivers = new List<Connection>();
                excludedReceivers.Add(excludedReceiver);
            }

            BroadcastMessage(message, excludedReceivers);
        }

        public void BroadcastMessage(Message message, List<Connection> excludedReceivers = null)
        {
            lock (this)
            {
                foreach (Connection connection in this._connections)
                {
                    if (excludedReceivers == null
                        || !(excludedReceivers.Contains(connection)))
                    {
                        connection.SendMessage(message);
                    }
                }
            }
        }
        #endregion

        #region Accept Methods
        private void Accept()
        {
            this._running = true;

            if (NetworkConstants.PrintLog)
            {
                Console.WriteLine("'Server' started on '"
                    + this._netCfg.IPAddress.ToString() + "' [port: " + this._netCfg.Port + "]");
            }

            while (this._running)
            {
                try
                {
                    TcpClient client = this._servSock.AcceptTcpClient();
                    Connection conn = new Connection(this, client);

                    if (NetworkConstants.PrintLog)
                    {
                        Console.WriteLine("Client connected... ["
                            + ((IPEndPoint)client.Client.RemoteEndPoint).Address
                            + ":" + this._netCfg.Port + "]");
                    }

                    this._connections.Add(conn);
                    conn.Start();

                    if (Connection != null)
                    {
                        Connection(this, new ConnectionEventArgs(conn,
                            ConnectionEvents.Connect));
                    }
                }
                catch (Exception)
                {
                    this._running = false;
                }
            }
        }
        #endregion

        #region Delete Methods
        internal void RemoveConnection(Connection conn)
        {
            if (this._connections.Contains(conn))
            {
                if (NetworkConstants.PrintLog)
                {
                    Console.WriteLine("Client disconnected... ["
                        + conn.RemoteEndPoint.Address
                        + ":" + this._netCfg.Port + "]");
                }

                conn.Stop();
                this._connections.Remove(conn);

                if (Connection != null)
                {
                    Connection(this, new ConnectionEventArgs(
                        conn, ConnectionEvents.Disconnect));
                }
            }
        }
        #endregion
        #endregion
    }
    #endregion
}

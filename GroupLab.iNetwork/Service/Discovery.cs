using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using GroupLab.iNetwork.Udp;

namespace GroupLab.iNetwork.Service
{
    #region Enumerations
    [Flags]
    internal enum ServerType : int
    {
        All = 1,
        Tcp = 2,
        Heap = 4
    }

    [Flags]
    internal enum DiscoveryType : int
    {
        Single = 1,
        Multiple = 2
    }
    #endregion

    #region Discovery Event Classes
    #region Delegates
    internal delegate void DiscoveryEventHandler(object sender, DiscoveryEventArgs e);
    #endregion

    #region Event Declarations
    internal class DiscoveryEventArgs : EventArgs
    {
        #region Class Members
        private List<DiscoveryResult> _results;

        private DiscoveryType _type;
        #endregion

        #region Constructors
        internal DiscoveryEventArgs(DiscoveryType type,
            List<DiscoveryResult> results)
        {
            this._type = type;
            this._results = results;
        }
        #endregion

        #region Properties
        internal DiscoveryType Type
        {
            get { return this._type; }
        }

        internal List<DiscoveryResult> Results
        {
            get { return this._results; }
        }
        #endregion
    }
    #endregion
    #endregion

    #region Class 'DiscoveryResult'
    internal class DiscoveryResult
    {
        #region Class Members
        private IPAddress _ipAddress;

        private int _port;

        private ServerType _type;
        #endregion

        #region Constructors
        internal DiscoveryResult(IPAddress ipAddress,
            int port, ServerType type)
        {
            this._ipAddress = ipAddress;
            this._port = port;
            this._type = type;
        }
        #endregion

        #region Properties
        internal IPAddress IPAddress
        {
            get { return this._ipAddress; }
        }

        internal int Port
        {
            get { return this._port; }
        }

        internal ServerType ServerType
        {
            get { return this._type; }
        }
        #endregion
    }
    #endregion

    #region Class 'DiscoveryAgent'
    internal class DiscoveryAgent
    {
        #region Static Class Members
        private const string DiscoveryLookup = "LU";

        private const string DiscoveryResponse = "RE";

        private static string Delimiter = ";";
        #endregion

        #region Class Members
        private MulticastSender _mcSender;

        private MulticastReceiver _mcReceiver;

        private bool _connected = false;

        private NetworkConfiguration _netCfg;

        private ServerType _serverType;

        private string _serverName;

        private object _tag;

        private List<DiscoveryResult> _discoveries;

        private DiscoveryType _discoveryType;

        private System.Timers.Timer _timer;

        private object _syncObj = new object();
        #endregion

        #region Events
        internal event DiscoveryEventHandler DiscoveryUpdate;
        #endregion

        #region Constructors
        internal DiscoveryAgent(string serverName)
            : this(serverName, new NetworkConfiguration(-1), ServerType.All)
        { }

        internal DiscoveryAgent(string serverName, NetworkConfiguration netCfg, ServerType serverType)
        {
            this._serverName = serverName;

            this._netCfg = netCfg;
            this._serverType = serverType;

            Initialize();
        }
        #endregion

        #region Properties
        internal bool IsConnected
        {
            get { return this._connected; }
        }

        internal object Tag
        {
            get { return this._tag; }
            set { this._tag = value; }
        }
        #endregion

        #region Initialization
        internal void Initialize()
        {
            this._discoveries = new List<DiscoveryResult>();
            if (!this._connected)
            {
                if (InitializeMulticast())
                {
                    this._connected = true;
                    if (NetworkConstants.PrintLog)
                    {
                        Console.WriteLine("The service 'Discovery' has been started...");
                    }
                }
                else
                {
                    this._connected = false;
                    if (NetworkConstants.PrintLog)
                    {
                        Console.WriteLine("Could not start the service 'Discovery'...");
                    }
                    Shutdown();
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private bool InitializeMulticast()
        {
            IPAddress address = IPAddress.Parse(NetworkConstants.MulticastAddress);

            this._mcSender = new MulticastSender(address, NetworkConstants.MulticastPort, 64);
            this._mcReceiver = new MulticastReceiver(address, NetworkConstants.MulticastPort);

            this._mcReceiver.MulticastMessageReceived
                += new MulticastMessageEventHandler(OnMulticastMessageReceived);

            if ((!(this._mcSender.Initialize())) || (!(this._mcReceiver.Initialize())))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion

        #region Shutdown Methods
        internal void Shutdown()
        {
            StopMulticast();
            if (NetworkConstants.PrintLog)
            {
                Console.WriteLine("The service 'Discovery' has been shut down...");
            }
        }

        private void StopMulticast()
        {
            this._mcReceiver.MulticastMessageReceived -= new MulticastMessageEventHandler(OnMulticastMessageReceived);

            this._mcSender.Close();
            this._mcReceiver.Close();
        }
        #endregion

        #region Discovery Methods
        internal void Discover(DiscoveryType type)
        {
            Discover(type, NetworkConstants.DiscoveryTimeout);
        }

        internal void Discover(DiscoveryType type, int timeout)
        {
            Discover(type, this._serverType, timeout);
        }

        internal void Discover(DiscoveryType type, ServerType serverType, int timeout)
        {
            this._discoveries = new List<DiscoveryResult>();
            this._discoveryType = type;

            if (this._mcSender.IsConnected
                && this._mcReceiver.IsConnected)
            {
                if (NetworkConstants.PrintLog)
                {
                    Console.WriteLine("Sending Discovery Request...");
                }

                /* string discoveryMessage = DiscoveryAgent.DiscoveryLookup + DiscoveryAgent.Delimiter +
                    ((int)serverType).ToString() + DiscoveryAgent.Delimiter +
                    this._serverName + DiscoveryAgent.Delimiter +
                    (this._netCfg.IPAddress == null ? "" : this._netCfg.IPAddress.ToString()) + DiscoveryAgent.Delimiter +
                    this._netCfg.Port.ToString();

                this._mcSender.SendMessage(discoveryMessage); */

                Message discoveryMessage = new Message(DiscoveryAgent.DiscoveryLookup);
                discoveryMessage.AddField("type", (int)serverType);
                discoveryMessage.AddField("name", this._serverName);
                discoveryMessage.AddField("ip", (this._netCfg.IPAddress == null 
                    ? (string)null : this._netCfg.IPAddress.ToString()));
                discoveryMessage.AddField("port", this._netCfg.Port);

                this._mcSender.SendMessage(discoveryMessage);

                if (this._timer != null
                    && this._timer.Enabled)
                {
                    this._timer.Stop();
                }

                this._timer = new System.Timers.Timer();
                this._timer.Interval = timeout;
                this._timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimerElapsed);
                this._timer.Start();
            }
        }

        internal void SendResponse()
        {
            if (this._mcSender.IsConnected && this._mcReceiver.IsConnected)
            {
                if (NetworkConstants.PrintLog)
                {
                    Console.WriteLine("Sending Discovery Response...");
                }

                /* string discoveryMessage = DiscoveryAgent.DiscoveryResponse + DiscoveryAgent.Delimiter +
                    ((int)this._serverType).ToString() + DiscoveryAgent.Delimiter +
                    this._serverName + DiscoveryAgent.Delimiter +
                    (this._netCfg.IPAddress == null ? "" : this._netCfg.IPAddress.ToString()) + DiscoveryAgent.Delimiter +
                    this._netCfg.Port.ToString();

                this._mcSender.SendMessage(discoveryMessage); */

                Message discoveryMessage = new Message(DiscoveryAgent.DiscoveryResponse);
                discoveryMessage.AddField("type", (int)this._serverType);
                discoveryMessage.AddField("name", this._serverName);
                discoveryMessage.AddField("ip", (this._netCfg.IPAddress == null
                    ? (string)null : this._netCfg.IPAddress.ToString()));
                discoveryMessage.AddField("port", this._netCfg.Port);

                this._mcSender.SendMessage(discoveryMessage);
            }
        }
        #endregion

        #region Check Methods
        private bool IsOwnRequest(NetworkConfiguration requestCfg, ServerType serverType)
        {
            return (requestCfg.IPAddress != null
                && requestCfg.Equals(this._netCfg)
                && this._serverType == serverType);
        }
        #endregion

        #region Event Handler
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void OnMulticastMessageReceived(object sender, Message msg)
        {
            if (msg.Name != null
                && (msg.Name.Equals(DiscoveryAgent.DiscoveryLookup)
                    || msg.Name.Equals(DiscoveryAgent.DiscoveryResponse)))
            {
                ServerType serverType = (ServerType)Enum.ToObject(
                    typeof(ServerType), msg.GetIntField("type"));

                IPAddress ipAddress = null;
                int port = -1;
                string name = null;

                if (msg.ContainsField("ip", TransferType.String))
                {
                    ipAddress = IPAddress.Parse(msg.GetStringField("ip"));
                }

                if (msg.ContainsField("port", TransferType.Int))
                {
                    port = msg.GetIntField("port");
                }

                if (msg.ContainsField("name", TransferType.String))
                {
                    name = msg.GetStringField("name");
                }

                bool isOwn = IsOwnRequest(new NetworkConfiguration(ipAddress, port), serverType);
                if (isOwn)
                {
                    return;
                }

                if (NetworkConstants.PrintLog)
                {
                    Console.WriteLine("Received a Discovery Message '" + msg.Name + "' ... [" + ipAddress
                        + ", " + port + "]");
                }

                if (this._mcSender.IsConnected
                    && this._mcReceiver.IsConnected)
                {
                    switch (msg.Name)
                    {
                        case DiscoveryAgent.DiscoveryLookup:
                            if (this._serverType == serverType
                                && (name == null || name.Equals(this._serverName)))
                            {
                                try
                                {
                                    Monitor.Wait(this, 50);
                                }
                                catch (ThreadInterruptedException) { }
                                SendResponse();
                            }
                            break;
                        case DiscoveryAgent.DiscoveryResponse:
                            if (!(isOwn))
                            {
                                lock (this._syncObj)
                                {
                                    if (this._timer.Enabled)
                                    {
                                        this._discoveries.Add(new DiscoveryResult(
                                            ipAddress, port, serverType));

                                        if (this._discoveryType == DiscoveryType.Single)
                                        {
                                            EndDiscovery();
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        /* private void OnMulticastMessageReceived(object sender, string msg)
        {
            string[] elements = msg.Split(new string[] { DiscoveryAgent.Delimiter },
                StringSplitOptions.None);

            string messageType = elements[0];
            ServerType serverType = (ServerType)Enum.ToObject(
                    typeof(ServerType), Convert.ToInt32(elements[1]));
            string name = (elements[2] == null || elements[2].Equals("") ?
                null : elements[2]);

            IPAddress ipAddress = null;
            if (elements[3] != null
                && !(elements[3].Equals("")))
            {
                ipAddress = IPAddress.Parse(elements[3]);
            }
            int port = Convert.ToInt32(elements[4]);
            
            if (messageType != null
                && (messageType.Equals(DiscoveryAgent.DiscoveryLookup)
                    || messageType.Equals(DiscoveryAgent.DiscoveryResponse)))
            {
                bool isOwn = IsOwnRequest(new NetworkConfiguration(ipAddress, port), serverType);
                if (isOwn)
                {
                     return;
                }

                if (NetworkConstants.PrintLog)
                {
                    Console.WriteLine("Received a Discovery Message '" + messageType + "' ... [" + ipAddress
                        + ", " + port + "]");
                }

                if (this._mcSender.IsConnected
                    && this._mcReceiver.IsConnected)
                {
                    switch (messageType)
                    {
                        case DiscoveryAgent.DiscoveryLookup:
                            if (this._serverType == serverType
                                && (name == null || name.Equals(this._serverName)))
                            {
                                try
                                {
                                    Monitor.Wait(this, 50);
                                }
                                catch (ThreadInterruptedException) { }
                                SendResponse();
                            }
                            break;
                        case DiscoveryAgent.DiscoveryResponse:
                            if (!(isOwn))
                            {
                                lock (this._syncObj)
                                {
                                    if (this._timer.Enabled)
                                    {
                                        this._discoveries.Add(new DiscoveryResult(
                                            ipAddress, port, serverType));

                                        if (this._discoveryType == DiscoveryType.Single)
                                        {
                                            EndDiscovery();
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        } */

        private void EndDiscovery()
        {
            lock (this._syncObj)
            {
                if (this._timer.Enabled)
                {
                    this._timer.Stop();
                    this.Shutdown();

                    if (DiscoveryUpdate != null)
                    {
                        DiscoveryUpdate(this, new DiscoveryEventArgs(
                            this._discoveryType, this._discoveries));
                    }
                }
            }
        }

        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            EndDiscovery();
        }
        #endregion
    }
    #endregion
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using GroupLab.iNetwork.Service;
using GroupLab.iNetwork.Tcp;

namespace GroupLab.iNetwork.PubSub
{
    #region Subscription Event Delegates
    #region Delegates
    public delegate void SingleSubscriptionDiscoveryEventHandler(Subscription subscription);

    public delegate void MultipleSubscriptionDiscoveryEventHandler(List<Subscription> subscriptions);
    #endregion
    #endregion

    #region Class 'Subscription'
    public class Subscription
    {
        #region Static Class Members
        private const string RegisterTemplateMessageName = "RegTemp";

        private const string UnregisterTemplateMessageName = "UnregTemp";

        private const string TemplateFieldName = "temp";

        private static object SynchronizationObject = new object();
        #endregion

        #region Class Members
        private Connection _conn;

        private List<Template> _templates;

        private Dictionary<Template, SubscriptionMessageEventHandler> _messageHandlers;

        private DiscoveryAgent _agent;

        private bool _startsAutomatically;
        #endregion

        #region Events
        public event SubscriptionEventHandler Subscribed;

        public event SubscriptionMessageEventHandler MessageReceived;

        internal event SubscriptionMessageEventHandler InternalMessageReceived;
        #endregion

        #region Constructors
        public Subscription(string ipAddress, int port)
            : this(IPAddress.Parse(ipAddress), port)
        { }

        public Subscription(IPAddress ipAddress, int port)
        {
            this._conn = new Connection(ipAddress, port);
            this._conn.Connected += new ConnectionEventHandler(OnConnected);

            this._messageHandlers = new Dictionary<Template, SubscriptionMessageEventHandler>();

            Initialize();
        }

        internal Subscription(Connection connection)
        {
            this._conn = connection;
            Initialize();
        }
        #endregion

        #region Initialization
        private void Initialize()
        {
            this._templates = new List<Template>();

            this._conn.MessageReceived += new ConnectionMessageEventHandler(OnMessageReceived);
            this._conn.InternalMessageReceived += new ConnectionMessageEventHandler(OnInternalMessageReceived);
        }
        #endregion

        #region Properties
        internal Connection Connection
        {
            get { return this._conn; }
        }

        public List<Template> Templates
        {
            get { return this._templates; }
        }
        #endregion

        #region Start/Stop Methods
        public void Start()
        {
            this._conn.Start();
        }

        public void Stop()
        {
            this._conn.Stop();
        }
        #endregion

        #region Send Methods
        public void SendMessage(Message message)
        {
            this._conn.SendMessage(message);
        }
        #endregion

        #region Template Methods
        internal bool AcceptsTemplate(Template template)
        {
            lock (this._templates)
            {
                return this._templates.Contains(template);
            }
        }

        public void RegisterTemplate(Template template, SubscriptionMessageEventHandler eventHandler = null)
        {
            lock (this._templates)
            {
                if (!(this._templates.Contains(template)))
                {
                    this._templates.Add(template);
                    if (this._conn.IsRemote)
                    {
                        if (eventHandler != null)
                        {
                            this._messageHandlers.Add(template, eventHandler);
                        }

                        Message templateMsg = new Message(Subscription.RegisterTemplateMessageName, true);
                        templateMsg.AddField("temp", template);

                        SendMessage(templateMsg);
                    }
                }
            }
        }

        public void UnregisterTemplate(Template template, ConnectionMessageEventHandler eventHandler = null)
        {
            lock (this._templates)
            {
                if (this._templates.Contains(template))
                {
                    this._templates.Remove(template);
                    if (this._conn.IsRemote)
                    {
                        if (eventHandler != null)
                        {
                            this._messageHandlers.Remove(template);
                        }

                        Message templateMsg = new Message(Subscription.UnregisterTemplateMessageName, true);
                        templateMsg.AddField("temp", template);

                        SendMessage(templateMsg);
                    }
                }
            }
        }
        #endregion

        #region Discovery Methods
        #region Discover 'ONE' Methods
        public static void Discover(SingleSubscriptionDiscoveryEventHandler eventHandler)
        {
            Subscription.Discover(NetworkConstants.DiscoveryTimeout,
                null, eventHandler);
        }

        public static void Discover(int timeout,
            SingleSubscriptionDiscoveryEventHandler eventHandler)
        {
            Subscription.Discover(NetworkConstants.DiscoveryTimeout,
                null, eventHandler);
        }

        public static void Discover(string serverName,
            SingleSubscriptionDiscoveryEventHandler eventHandler)
        {
            Subscription.Discover(NetworkConstants.DiscoveryTimeout,
                serverName, eventHandler);
        }

        public static void Discover(int timeout, string serverName,
            SingleSubscriptionDiscoveryEventHandler eventHandler)
        {
            DiscoveryAgent agent = new DiscoveryAgent(serverName);
            agent.Tag = eventHandler;
            agent.DiscoveryUpdate += new DiscoveryEventHandler(OnDiscoveryUpdate);
            agent.Discover(DiscoveryType.Single, ServerType.Heap, timeout);
        }
        #endregion

        #region Discover 'ALL' Methods
        public static void DiscoverAll(MultipleSubscriptionDiscoveryEventHandler eventHandler)
        {
            Subscription.DiscoverAll(NetworkConstants.DiscoveryTimeout, eventHandler);
        }

        public static void DiscoverAll(int timeout,
            MultipleSubscriptionDiscoveryEventHandler eventHandler)
        {
            Subscription.DiscoverAll(NetworkConstants.DiscoveryTimeout, null, eventHandler);
        }

        public static void DiscoverAll(string serverName,
            MultipleSubscriptionDiscoveryEventHandler eventHandler)
        {
            Subscription.DiscoverAll(NetworkConstants.DiscoveryTimeout, serverName, eventHandler);
        }

        public static void DiscoverAll(int timeout, string serverName,
            MultipleSubscriptionDiscoveryEventHandler eventHandler)
        {
            DiscoveryAgent agent = new DiscoveryAgent(serverName);
            agent.Tag = eventHandler;
            agent.DiscoveryUpdate += new DiscoveryEventHandler(OnDiscoveryUpdate);
            agent.Discover(DiscoveryType.Multiple, ServerType.Heap, timeout);
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
                    && agent.Tag is SingleSubscriptionDiscoveryEventHandler)
                {
                    if (e.Results.Count > 0)
                    {
                        ((SingleSubscriptionDiscoveryEventHandler)agent.Tag).Invoke(
                            new Subscription(e.Results[0].IPAddress, e.Results[0].Port));
                    }
                    else
                    {
                        ((SingleSubscriptionDiscoveryEventHandler)agent.Tag).Invoke(null);
                    }
                }
                else if (e.Type == DiscoveryType.Multiple
                    && agent.Tag != null
                    && agent.Tag is MultipleSubscriptionDiscoveryEventHandler)
                {
                    List<Subscription> subscriptions = new List<Subscription>();
                    foreach (DiscoveryResult result in e.Results)
                    {
                        subscriptions.Add(new Subscription(result.IPAddress, result.Port));
                    }

                    ((MultipleSubscriptionDiscoveryEventHandler)agent.Tag).Invoke(subscriptions);
                }
            }
        }
        #endregion
        #endregion

        #region Event Handler
        private void OnConnected(object sender, ConnectionEventArgs e)
        {
            if (e.Connection != null
                && e.Connection.Equals(this._conn))
            {
                if (e.ConnectionEvent == ConnectionEvents.Connect
                    && Subscribed != null)
                {
                    if (this._startsAutomatically)
                    {
                        this._conn.Start();
                    }

                    Subscribed(this, new SubscriptionEventArgs(this, SubscriptionEvents.Subscribe));
                }
                else if (e.ConnectionEvent == ConnectionEvents.Disconnect
                    && Subscribed != null)
                {
                    Subscribed(this, new SubscriptionEventArgs(this, SubscriptionEvents.Unsubscribe));
                }
            }
        }

        private void OnMessageReceived(object sender, Message msg)
        {
            // pass it on
            // is it a special event handler?
            Template template = Publisher.GetTemplate(msg);
            if (this._messageHandlers != null
                && this._messageHandlers.ContainsKey(template))
            {
                this._messageHandlers[template].Invoke(this, msg);
            }
            else
            {
                if (MessageReceived != null)
                {
                    MessageReceived(this, msg);
                }
            }
        }

        private void OnInternalMessageReceived(object sender, Message msg)
        {
            switch (msg.Name)
            {
                case Subscription.RegisterTemplateMessageName:
                    RegisterTemplate((Template)msg.GetField("temp", typeof(Template)));
                    break;
                case Subscription.UnregisterTemplateMessageName:
                    UnregisterTemplate((Template)msg.GetField("temp", typeof(Template)));
                    break;
                default:
                    if (InternalMessageReceived != null)
                    {
                        InternalMessageReceived(this, msg);
                    }
                    break;
            }
        }
        #endregion

        #region Overridden Methods (Object)
        public override string ToString()
        {
            string str = ((IPEndPoint)this._conn.RemoteEndPoint).Address
                + (this._conn.Server != null ? " ["
                + this._conn.Server.Configuration.Port + "]" : "");

            return str;
        }
        #endregion
    }
    #endregion
}

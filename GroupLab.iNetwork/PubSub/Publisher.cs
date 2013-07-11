using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GroupLab.iNetwork.Service;
using GroupLab.iNetwork.Tcp;

namespace GroupLab.iNetwork.PubSub
{
    #region Class 'Publisher'
    public class Publisher
    {
        #region Class Members
        private Server _server;

        private DiscoveryAgent _discoveryAgent;

        private string _name;

        private List<Subscription> _subscribers;
        #endregion

        #region Events
        public event SubscriptionEventHandler Subscription;
        #endregion

        #region Constructors
        public Publisher(string name)
            : this(name, -1)
        { }

        public Publisher(string name, int port)
        {
            if (name == null
                || name.Equals(""))
            {
                throw new ConfigurationException("The 'MessageHeap' must have a name.");
            }

            this._name = name;

            this._server = new Server(name, port);
            this._server.Connection += new ConnectionEventHandler(OnServerConnection);
        }
        #endregion

        #region Properties
        public bool IsDiscoverable
        {
            get { return this._discoveryAgent != null; }
            set
            {
                if (value && this._discoveryAgent == null)
                {
                    this._discoveryAgent = new DiscoveryAgent(
                        this._name, this._server.Configuration, ServerType.Heap);
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
        #endregion

        #region Start/Stop Methods
        public void Start()
        {
            this._subscribers = new List<Subscription>();
            this._server.Start();
        }

        public void Stop()
        {
            this._server.Stop();
        }
        #endregion

        #region Template Methods
        internal static Template GetTemplate(Message message)
        {
            List<Descriptor> descriptors = message.Descriptors;
            Template template = new Template(message.Name);

            foreach (Descriptor descriptor in descriptors)
            {
                template.Fields.Add(new Field(
                    descriptor.Name, descriptor.Type));
            }

            return template;
        }
        #endregion

        #region Event Handler
        private Subscription GetSubscriber(Connection conn)
        {
            foreach (Subscription subscriber in this._subscribers)
            {
                if (subscriber.Connection != null
                    && subscriber.Connection.Equals(conn))
                {
                    return subscriber;
                }
            }
            return null;
        }

        private void OnServerConnection(object sender, ConnectionEventArgs e)
        {
            if (e.ConnectionEvent == ConnectionEvents.Connect
                && e.Connection != null)
            {
                Subscription subscription = new Subscription(e.Connection);
                this._subscribers.Add(subscription);

                subscription.MessageReceived += new SubscriptionMessageEventHandler(
                    OnConnectionMessageReceived);

                if (Subscription != null)
                {
                    Subscription(this, new SubscriptionEventArgs(subscription,
                        SubscriptionEvents.Subscribe));
                }
            }
            else if (e.ConnectionEvent == ConnectionEvents.Disconnect)
            {
                Subscription subscription = GetSubscriber(e.Connection);
                if (subscription != null)
                {
                    this._subscribers.Remove(subscription);
                    subscription.MessageReceived -= new SubscriptionMessageEventHandler(
                        OnConnectionMessageReceived);

                    if (Subscription != null)
                    {
                        Subscription(this, new SubscriptionEventArgs(subscription,
                            SubscriptionEvents.Unsubscribe));
                    }
                }
            }
        }

        private void OnConnectionMessageReceived(object sender, Message message)
        {
            // ok, now we check whether we should broadcast it
            // check for each connection
            lock (this)
            {
                Template messageTemplate = Publisher.GetTemplate(message);
                foreach (Subscription subscription in this._subscribers)
                {
                    if (message.IsInternal
                        || subscription.AcceptsTemplate(messageTemplate))
                    {
                        subscription.SendMessage(message);
                    }
                    else
                    {
                        Console.WriteLine("Unsupported Message [" + messageTemplate.ToString() + "]");
                    }
                }
            }
        }
        #endregion
    }
    #endregion
}

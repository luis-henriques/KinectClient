using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GroupLab.iNetwork.PubSub;
using GroupLab.iNetwork.Service;
using GroupLab.iNetwork.Tcp;

namespace GroupLab.iNetwork
{
    #region Class 'Subscriber'
    public class Subscriber
    {
        #region Events
        public event SingleSubscriptionDiscoveryEventHandler SingleSubscriptionDiscovered;

        public event MultipleSubscriptionDiscoveryEventHandler MultipleSubscriptionsDiscovered;
        #endregion

        #region Constructors
        public Subscriber() { }
        #endregion

        #region Discovery Methods
        #region Discover 'ONE' Methods
        public void Discover()
        {
            Discover(NetworkConstants.DiscoveryTimeout);
        }

        public void Discover(int timeout)
        {
            Discover(timeout, null);
        }

        public void Discover(string serverName)
        {
            Discover(NetworkConstants.DiscoveryTimeout, serverName);
        }

        public void Discover(int timeout, string serverName)
        {
            DiscoveryAgent agent = new DiscoveryAgent(serverName);
            agent.DiscoveryUpdate += new DiscoveryEventHandler(OnSubscriptionDiscoveryUpdate);
            agent.Discover(DiscoveryType.Single, ServerType.Heap, timeout);
        }
        #endregion

        #region Discover 'ALL' Methods
        public void DiscoverAll()
        {
            DiscoverAll(NetworkConstants.DiscoveryTimeout);
        }

        public void DiscoverAll(int timeout)
        {
            DiscoverAll(timeout, null);
        }

        public void DiscoverAll(string serverName)
        {
            DiscoverAll(NetworkConstants.DiscoveryTimeout, serverName);
        }

        public void DiscoverAll(int timeout, string serverName)
        {
            DiscoveryAgent agent = new DiscoveryAgent(serverName);
            agent.DiscoveryUpdate += new DiscoveryEventHandler(OnSubscriptionDiscoveryUpdate);
            agent.Discover(DiscoveryType.Multiple, ServerType.Heap, timeout);
        }
        #endregion

        #region Event Handler
        private void OnSubscriptionDiscoveryUpdate(object sender, DiscoveryEventArgs e)
        {
            if (sender is DiscoveryAgent)
            {
                DiscoveryAgent agent = sender as DiscoveryAgent;
                if (e.Type == DiscoveryType.Single
                    && SingleSubscriptionDiscovered != null)
                {
                    if (e.Results.Count > 0)
                    {
                        SingleSubscriptionDiscovered(new Subscription(
                            e.Results[0].IPAddress, e.Results[0].Port));
                    }
                    else
                    {
                        SingleSubscriptionDiscovered(null);
                    }
                }
                else if (e.Type == DiscoveryType.Multiple
                    && MultipleSubscriptionsDiscovered != null)
                {
                    List<Subscription> subscriptions = new List<Subscription>();
                    foreach (DiscoveryResult result in e.Results)
                    {
                        subscriptions.Add(new Subscription(result.IPAddress, result.Port));
                    }

                    MultipleSubscriptionsDiscovered(subscriptions);
                }
            }
        }
        #endregion
        #endregion
    }
    #endregion

    #region Class 'Connector'
    public class Connector
    {
        #region Events
        public event SingleConnectionDiscoveryEventHandler SingleConnectionDiscovered;

        public event MultipleConnectionDiscoveryEventHandler MultipleConnectionsDiscovered;
        #endregion

        #region Constructors
        public Connector() { }
        #endregion

        #region Connection Discovery Methods
        #region Discover 'ONE' Methods
        public void Discover()
        {
            Discover(NetworkConstants.DiscoveryTimeout);
        }

        public void Discover(int timeout)
        {
            Discover(timeout, null);
        }

        public void Discover(string serverName)
        {
            Discover(NetworkConstants.DiscoveryTimeout, serverName);
        }

        public void Discover(int timeout, string serverName)
        {
            DiscoveryAgent agent = new DiscoveryAgent(serverName);
            agent.DiscoveryUpdate += new DiscoveryEventHandler(OnConnectionDiscoveryUpdate);
            agent.Discover(DiscoveryType.Single, ServerType.Tcp, timeout);
        }
        #endregion

        #region Discover 'ALL' Methods
        public void DiscoverAll()
        {
            DiscoverAll(NetworkConstants.DiscoveryTimeout);
        }

        public void DiscoverAll(int timeout)
        {
            DiscoverAll(timeout, null);
        }

        public void DiscoverAll(string serverName)
        {
            DiscoverAll(NetworkConstants.DiscoveryTimeout, serverName);
        }

        public void DiscoverAll(int timeout, string serverName)
        {
            DiscoveryAgent agent = new DiscoveryAgent(serverName);
            agent.DiscoveryUpdate += new DiscoveryEventHandler(OnConnectionDiscoveryUpdate);
            agent.Discover(DiscoveryType.Multiple, ServerType.Tcp, timeout);
        }
        #endregion

        #region Event Handler
        private void OnConnectionDiscoveryUpdate(object sender, DiscoveryEventArgs e)
        {
            if (sender is DiscoveryAgent)
            {
                DiscoveryAgent agent = sender as DiscoveryAgent;
                if (e.Type == DiscoveryType.Single
                    && SingleConnectionDiscovered != null)
                {
                    if (e.Results.Count > 0)
                    {
                        SingleConnectionDiscovered(new Connection(
                            e.Results[0].IPAddress, e.Results[0].Port));
                    }
                    else
                    {
                        SingleConnectionDiscovered(null);
                    }
                }
                else if (e.Type == DiscoveryType.Multiple
                    && MultipleConnectionsDiscovered != null)
                {
                    List<Connection> connections = new List<Connection>();
                    foreach (DiscoveryResult result in e.Results)
                    {
                        connections.Add(new Connection(result.IPAddress, result.Port));
                    }

                    MultipleConnectionsDiscovered(connections);
                }
            }
        }
        #endregion
        #endregion
    }
    #endregion
}

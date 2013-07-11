using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GroupLab.iNetwork.PubSub
{
    #region Enumerations
    #region Enumeration 'SubscriptionEvents'
    [Flags]
    public enum SubscriptionEvents
    {
        Subscribe = 1,
        Unsubscribe = 2
    }
    #endregion
    #endregion

    #region Event Definitions
    #region Delegates
    public delegate void SubscriptionMessageEventHandler(object sender, Message msg);

    public delegate void SubscriptionEventHandler(object sender, SubscriptionEventArgs e);
    #endregion

    #region Event Arguments Classes
    public class SubscriptionEventArgs : EventArgs
    {
        #region Class Members
        private Subscription _sub;

        private SubscriptionEvents _subEvent;
        #endregion

        #region Constructors
        public SubscriptionEventArgs(Subscription sub, SubscriptionEvents evt)
        {
            this._sub = sub;
            this._subEvent = evt;
        }
        #endregion

        #region Properties
        public Subscription Subscriber
        {
            get { return this._sub; }
        }

        public SubscriptionEvents SubscriptionEvent
        {
            get { return this._subEvent; }
        }
        #endregion
    }
    #endregion
    #endregion
}

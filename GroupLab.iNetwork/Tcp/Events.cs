using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GroupLab.iNetwork.Tcp
{
    #region Enumerations
    #region Enumeration 'ConnectionEvents'
    [Flags]
    public enum ConnectionEvents
    {
        Connect = 1,
        Disconnect = 2
    }
    #endregion
    #endregion

    #region Event Definitions
    #region Delegates
    public delegate void ConnectionMessageEventHandler(object sender, Message msg);

    public delegate void ConnectionEventHandler(object sender, ConnectionEventArgs e);
    #endregion

    #region Event Arguments Classes
    public class ConnectionEventArgs : EventArgs
    {
        #region Class Members
        private Connection _conn;

        private ConnectionEvents _connEvent;
        #endregion

        #region Constructors
        public ConnectionEventArgs(Connection conn, ConnectionEvents evt)
        {
            this._conn = conn;
            this._connEvent = evt;
        }
        #endregion

        #region Properties
        public Connection Connection
        {
            get { return this._conn; }
        }

        public ConnectionEvents ConnectionEvent
        {
            get { return this._connEvent; }
        }
        #endregion
    }
    #endregion
    #endregion
}

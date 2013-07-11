using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace GroupLab.iNetwork
{
    #region Class 'NetworkConfiguration'
    public class NetworkConfiguration
    {
        #region Class Members
        private IPAddress _ipAddr;

        private int _port;
        #endregion

        #region Constructors
        public NetworkConfiguration()
            : this(null, -1)
        { }

        public NetworkConfiguration(int port)
            : this(Dns.GetHostAddresses(Dns.GetHostName())[0], port)
        { }

        public NetworkConfiguration(IPAddress ipAddr, int port)
        {
            this._ipAddr = ipAddr;
            this._port = port;
        }
        #endregion

        #region Properties
        public IPAddress IPAddress
        {
            get { return this._ipAddr; }
        }

        public int Port
        {
            get { return this._port; }
        }
        #endregion

        #region Overridden Methods
        public override string ToString()
        {
            return ("NetworkConfiguration: IP = " + this._ipAddr.ToString()
                + ", Port = " + this._port);
        }

        public override int GetHashCode()
        {
            return (this._port ^ this._port);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is NetworkConfiguration))
            {
                return false;
            }
            else
            {
                return (((this._ipAddr == null && ((NetworkConfiguration)obj).IPAddress == null)
                    || (this._ipAddr.Equals(((NetworkConfiguration)obj).IPAddress))
                    && this._port == ((NetworkConfiguration)obj).Port));
            }
        }
        #endregion
    }
    #endregion

    #region Class 'NetwrokConstants'
    internal class NetworkConstants
    {
        #region Port Class Members
        internal const int StartPort = 10001;

        internal const int PortStepping = 2;
        #endregion

        #region Multicast Class Members
        internal static readonly string MulticastAddress = "224.0.1.141";

        internal static readonly int MulticastPort = 2541;

        internal static readonly int MulticastMaxDataLength = 512;
        #endregion

        #region Thread Constants
        internal static readonly bool MultiThreaded = false;

        internal static readonly int DiscoveryTimeout = 1000;
        #endregion

        #region Debug Constants
#if DEBUG
        internal static bool Debug = true;
        internal static bool PrintLog = true;
#else
        internal static bool Debug = false;
        internal static bool PrintLog = false;
#endif
        #endregion
    }
    #endregion
}

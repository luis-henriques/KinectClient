using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GroupLab.iNetwork.Udp
{
    #region Class 'MulticastSender'
    internal class MulticastSender
    {
        #region Class Members
        private Socket _sock;

        private IPEndPoint _ipEndPoint;

        private IPAddress _mcGroup;

        private int _port;

        private int _ttl;
        #endregion

        #region Constructors
        internal MulticastSender(IPAddress multicastGroup, int port, int timeToLive)
        {
            this._mcGroup = multicastGroup;
            this._port = port;
            this._ttl = timeToLive;
        }
        #endregion

        #region Initialization
        internal bool Initialize()
        {
            try
            {
                this._sock = new Socket(AddressFamily.InterNetwork,
                    SocketType.Dgram, ProtocolType.Udp);
                this._sock.SetSocketOption(SocketOptionLevel.IP,
                    SocketOptionName.MulticastTimeToLive, this._ttl);
                this._sock.SetSocketOption(SocketOptionLevel.IP,
                    SocketOptionName.MulticastLoopback, true);

                this._ipEndPoint = new IPEndPoint(this._mcGroup, this._port);
                this._sock.Connect(this._ipEndPoint);

                return true;
            }
            catch (SocketException)
            {
                this._sock = null;
                return false;
            }
        }
        #endregion

        #region Properties
        internal bool IsConnected
        {
            get { return this._sock != null; }
        }
        #endregion

        #region Send Methods
        /* internal void SendMessage(string msg)
        {
            try
            {
                byte[] bytesToSend = Encoding.UTF8.GetBytes(msg);
                byte[] lengthBytes = ObjectConverter.Encode(bytesToSend.Length, 2);

                List<byte> bytes = new List<byte>();
                bytes.AddRange(lengthBytes);
                bytes.AddRange(bytesToSend);

                byte[] transportData = bytes.ToArray();

                this._sock.SendTo(transportData, 0, 
                    transportData.Length, SocketFlags.None, this._ipEndPoint);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\n" + e.StackTrace);
            }
        } */

        internal void SendMessage(Message msg)
        {
            try
            {
                byte[] bytesToSend = msg.ToByteArray();
                this._sock.SendTo(bytesToSend, 0,
                    bytesToSend.Length, SocketFlags.None, this._ipEndPoint);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\n" + e.StackTrace);
            }
        }
        #endregion

        #region Close Methods
        internal void Close()
        {
            if (this._sock != null)
            {
                try
                {
                    this._sock.Shutdown(SocketShutdown.Send);
                    this._sock.Close();

                    this._sock = null;
                    this._ipEndPoint = null;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + "\n" + e.StackTrace);
                }
            }
        }
        #endregion
    }
    #endregion
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GroupLab.iNetwork.Udp
{
    #region Class 'MulticastReceiver'
    internal class MulticastReceiver
    {
        #region Class Members
        private Socket _sock;

        private IPEndPoint _ipEndPoint;

        private IPAddress _mcGroup;

        private int _port;

        private List<byte> _buffer;

        // private int _length = -1;

        private int _totalLength = -1;

        private int _controlFlag = -1;

        private int _nameLength = -1;

        private string _name = null;
        #endregion

        #region Events
        internal event MulticastMessageEventHandler MulticastMessageReceived;
        #endregion

        #region Constructors
        internal MulticastReceiver(IPAddress multicastGroup, int port)
        {
            this._mcGroup = multicastGroup;
            this._port = port;

            this._buffer = new List<byte>();
        }
        #endregion

        #region Initialization
        internal bool Initialize()
        {
            try
            {
                this._sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                this._ipEndPoint = new IPEndPoint(IPAddress.Any, this._port);

                this._sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

                this._sock.Bind(this._ipEndPoint);
                this._sock.SetSocketOption(SocketOptionLevel.IP,
                    SocketOptionName.AddMembership,
                    new MulticastOption(this._mcGroup, IPAddress.Any));

                Thread receiver = new Thread(new ThreadStart(this.Run));
                receiver.IsBackground = false;
                receiver.Name = "Multicast Receiver";
                receiver.Start();

                return true;
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.Message + "\n" + se.StackTrace);

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

        #region Read Message Methods
        private void ProcessBuffer()
        {
            while (true)
            {
                if (this._totalLength == -1
                    || this._controlFlag == -1
                    || this._nameLength == -1)
                {
                    // we don't have the header yet
                    if (this._buffer.Count >= 7)
                    {
                        this._totalLength = this._buffer[0] << 24;
                        this._totalLength |= this._buffer[1] << 16;
                        this._totalLength |= this._buffer[2] << 8;
                        this._totalLength |= this._buffer[3];

                        this._controlFlag = this._buffer[4];

                        this._nameLength = this._buffer[5] << 8;
                        this._nameLength |= this._buffer[6];

                        this._buffer.RemoveRange(0, 7);
                    }
                    else
                    {
                        // not enough bytes yet...
                        break;
                    }
                }

                if (this._name == null 
                    && this._buffer.Count >= this._nameLength)
                {
                    byte[] nameBytes = this._buffer.GetRange(0, this._nameLength).ToArray();
                    this._name = Encoding.UTF8.GetString(nameBytes, 0, this._nameLength);

                    this._buffer.RemoveRange(0, this._nameLength);
                }
                else
                {
                    break;
                }

                if (this._buffer.Count >= this._totalLength)
                {
                    MessageHeader header = new MessageHeader(this._totalLength,
                        (this._controlFlag == 1), this._name);

                    byte[] contentBytes = this._buffer.GetRange(0, this._totalLength).ToArray();
                    Message message = Message.FromStream(header, contentBytes);

                    this._buffer.RemoveRange(0, this._totalLength);

                    if (message != null
                        && MulticastMessageReceived != null)
                    {
                        MulticastMessageReceived(this, message);
                    }

                    this._totalLength = -1;
                    this._controlFlag = -1;
                    this._nameLength = -1;
                    this._name = null;
                }
                else
                {
                    break;
                }
            }
        }

        /* private void ProcessBuffer()
        {
            while (true)
            {
                if (this._length == -1)
                {
                    if (this._buffer.Count >= 2)
                    {
                        this._length = this._buffer[0] << 8;
                        this._length |= (this._buffer[1]);

                        this._buffer.RemoveRange(0, 2);
                    }
                    else
                    {
                        break;
                    }
                }

                if (this._buffer.Count >= this._length)
                {
                    byte[] rawBytes = new byte[this._length];
                    this._buffer.CopyTo(0, rawBytes, 0, this._length);

                    string message = Encoding.UTF8.GetString(rawBytes);

                    if (message != null
                        && MulticastMessageReceived != null)
                    {
                        MulticastMessageReceived(this, message);
                    }

                    this._buffer.RemoveRange(0, this._length);
                    this._length = -1;
                }
            }
        } */
        #endregion

        #region Thread Methods
        internal void Run()
        {
            bool running = true;
            while (this._sock != null && running)
            {
                try
                {
                    EndPoint tmpRecvPt = new IPEndPoint(IPAddress.Any, 0);

                    byte[] buffer = new byte[NetworkConstants.MulticastMaxDataLength];
                    int length = this._sock.ReceiveFrom(buffer, 0,
                        NetworkConstants.MulticastMaxDataLength, SocketFlags.None, ref tmpRecvPt);

                    if (length > 0)
                    {
                        byte[] data = new byte[length];
                        Array.Copy(buffer, data, length);
                        this._buffer.AddRange(data);

                        ProcessBuffer();
                    }
                }
                catch (Exception)
                {
                    running = false;
                }
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
                    this._sock.Shutdown(SocketShutdown.Receive);
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

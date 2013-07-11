using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GroupLab.iNetwork
{
    #region Class 'MessageHeader'
    internal class MessageHeader
    {
        #region Class Members
        private int _contentLength;

        private bool _internal;

        private string _name;
        #endregion

        #region Constructors
        internal MessageHeader(int contentLength, bool isInternal, string name)
        {
            this._contentLength = contentLength;
            this._internal = isInternal;
            this._name = name;
        }
        #endregion

        #region Properties
        internal int ContentLength
        {
            get { return this._contentLength; }
        }

        internal bool IsInternal
        {
            get { return this._internal; }
        }

        internal string Name
        {
            get { return this._name; }
        }
        #endregion
    }
    #endregion

    #region Class 'Message'
    public class Message
    {
        #region Class Members
        private NetworkStreamInfo _content;

        private string _name;

        private bool _internal = false;
        #endregion

        #region Constructors
        public Message(string messageName)
            : this(messageName, false)
        { }

        internal Message(string messageName, bool isInternal)
        {
            this._content = new NetworkStreamInfo();
            this._name = messageName;
            this._internal = isInternal;
        }

        private Message(MessageHeader header)
        {
            this._internal = header.IsInternal;
            this._name = header.Name;
        }
        #endregion

        #region Properties
        internal bool IsInternal
        {
            get { return this._internal; }
            set { this._internal = value; }
        }

        internal NetworkStreamInfo Content
        {
            get { return this._content; }
            set { this._content = value; }
        }

        internal List<Descriptor> Descriptors
        {
            get { return this._content.Descriptors; }
        }

        public string Name
        {
            get { return this._name; }
        }
        #endregion

        #region Check Methods
        public bool ContainsField(string name, TransferType type)
        {
            lock (this)
            {
                return (this.ContainsField(new Descriptor(name, type)));
            }
        }

        internal bool ContainsField(Descriptor descriptor)
        {
            lock (this)
            {
                return (this._content.ContainsKey(descriptor));
            }
        }
        #endregion

        #region Add/Get Methods
        #region Add Methods
        public void AddField(string name, bool value)
        {
            AddField(name, (object)value);
        }

        public void AddField(string name, byte value)
        {
            AddField(name, (object)value);
        }

        public void AddField(string name, double value)
        {
            AddField(name, (object)value);
        }

        public void AddField(string name, float value)
        {
            AddField(name, (object)value);
        }

        public void AddField(string name, int value)
        {
            AddField(name, (object)value);
        }

        public void AddField(string name, long value)
        {
            AddField(name, (object)value);
        }

        public void AddField(string name, short value)
        {
            AddField(name, (object)value);
        }

        public void AddField(string name, string value)
        {
            AddField(name, (object)value);
        }

        public void AddField(string name, byte[] value)
        {
            AddField(name, (object)value);
        }

        public void AddField(string name, object value)
        {
            this._content.AddValue(name, value);
        }
        #endregion

        #region Get Methods
        public bool GetBoolField(string name)
        {
            return (bool)GetField(name, typeof(bool));
        }

        public byte GetByteField(string name)
        {
            return (byte)GetField(name, typeof(byte));
        }

        public double GetDoubleField(string name)
        {
            return (double)GetField(name, typeof(double));
        }

        public float GetFloatField(string name)
        {
            return (float)GetField(name, typeof(float));
        }

        public int GetIntField(string name)
        {
            return (int)GetField(name, typeof(int));
        }

        public long GetLongField(string name)
        {
            return (long)GetField(name, typeof(long));
        }

        public short GetShortField(string name)
        {
            return (short)GetField(name, typeof(short));
        }

        public string GetStringField(string name)
        {
            return (string)GetField(name, typeof(string));
        }

        public byte[] GetBinaryField(string name)
        {
            return (byte[])GetField(name, typeof(byte[]));
        }

        public object GetField(string name, Type type)
        {
            return this._content.GetValue(name, type);
        }
        #endregion
        #endregion

        #region Encoding / Decoding Methods
        #region Ecndoing Methods
        private byte[] CreateHeader(int messageLength)
        {
            List<byte> header = new List<byte>();
            byte[] encodedName = Encoding.UTF8.GetBytes(this.Name);

            // start with the message length (INT = 4 bytes)
            header.AddRange(ObjectConverter.Encode(messageLength, 4));

            // continue with the control flag (BYTE = 1 byte)
            header.AddRange(ObjectConverter.Encode((this.IsInternal ? 1 : 0), 1));

            // continue with the name length (SHORT = 2 bytes)
            header.AddRange(ObjectConverter.Encode(encodedName.Length, 2));

            // continue with the name (STRING)
            header.AddRange(encodedName);

            return header.ToArray();
        }

        internal byte[] ToByteArray()
        {
            // ok, create the message
            byte[] content = this._content.Serialize();
            int totalLength = content.Length;

            byte[] header = CreateHeader(totalLength);

            List<byte> rawData = new List<byte>();
            rawData.AddRange(header);
            rawData.AddRange(content);

            return (rawData.ToArray());
        }
        #endregion

        #region Decoding Methods
        internal static Message FromStream(MessageHeader header, byte[] rawData)
        {
            Message message = new Message(header);

            NetworkStreamInfo info = new NetworkStreamInfo();
            info.Deserialize(rawData);

            message.Content = info;

            return message;
        }
        #endregion
        #endregion
    }
    #endregion
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GroupLab.iNetwork
{
    #region Enumerations
    #region Enumeration 'TransferType'
    public enum TransferType : int
    {
        Unknown = 0,
        Bool = 1,
        Byte = 2,
        Double = 3,
        Float = 4,
        Int = 5,
        Long = 6,
        Short = 7,
        String = 8,
        Binary = 9,
        Object = 10,
        Null = 11
    }
    #endregion
    #endregion

    #region Class 'Descriptor'
    internal class Descriptor
    {
        #region Class Members
        private string _name;

        private TransferType _type;
        #endregion

        #region Constructor
        internal Descriptor(string name, TransferType type)
        {
            this._name = name;
            this._type = type;
        }
        #endregion

        #region Properties
        internal string Name
        {
            get { return this._name; }
        }

        internal TransferType Type
        {
            get { return this._type; }
        }
        #endregion

        #region Overridden Methods (Object)
        public override int GetHashCode()
        {
            return 100;
        }

        public override bool Equals(object obj)
        {
            if (obj is Descriptor)
            {
                Descriptor descriptor = obj as Descriptor;
                if (descriptor.Type == TransferType.Unknown
                    || this.Type == TransferType.Unknown)
                {
                    return (descriptor.Name != null
                        && descriptor.Name.Equals(this.Name));
                }
                else
                {
                    return (descriptor.Name != null
                        && descriptor.Name.Equals(this.Name));
                }
            }

            return false;
        }
        #endregion
    }
    #endregion

    #region Class 'NetworkStreamInfo'
    public class NetworkStreamInfo
    {
        #region Class Members
        private Dictionary<Descriptor, byte[]> _data;
        #endregion

        #region Constructors
        public NetworkStreamInfo()
        {
            this._data = new Dictionary<Descriptor, byte[]>();
        }
        #endregion

        #region Properties
        internal List<Descriptor> Descriptors
        {
            get
            {
                List<Descriptor> descriptors = new List<Descriptor>();
                foreach (Descriptor descriptor in this._data.Keys)
                {
                    descriptors.Add(descriptor);
                }
                return descriptors;
            }
        }
        #endregion

        #region Check Methods
        internal bool ContainsKey(Descriptor descriptor)
        {
            if (this._data != null
                && this._data.ContainsKey(descriptor))
            {
                return true;
            }
            return false;
        }
        #endregion

        #region Add/Get Methods
        #region Add Methods
        public void AddValue(string name, bool value)
        {
            AddValue(name, (object)value);
        }

        public void AddValue(string name, byte value)
        {
            AddValue(name, (object)value);
        }

        public void AddValue(string name, double value)
        {
            AddValue(name, (object)value);
        }

        public void AddValue(string name, float value)
        {
            AddValue(name, (object)value);
        }

        public void AddValue(string name, int value)
        {
            AddValue(name, (object)value);
        }

        public void AddValue(string name, long value)
        {
            AddValue(name, (object)value);
        }

        public void AddValue(string name, short value)
        {
            AddValue(name, (object)value);
        }

        public void AddValue(string name, string value)
        {
            AddValue(name, (object)value);
        }

        public void AddValue(string name, object value)
        {
            if (name == null
                || name.Equals(""))
            {
                throw new EncodingException("The given name '" + name
                    + "' is invalid. Names cannot be 'null' and "
                    + "must contain at least one character!");
            }
            else if (ContainsKey(new Descriptor(name, TransferType.Unknown)))
            {
                throw new EncodingException("The given name '" + name
                    + "' already exists in this context. "
                    + "Names must be unique!");
            }

            TransferType internalType = TransferType.Unknown;
            byte[] bytes = null;

            if (value == null)
            {
                internalType = TransferType.Null;
                bytes = new byte[] { 0 };
            }
            else
            {
                internalType = ObjectConverter.GetTransferType(value);
                if (internalType != TransferType.Unknown)
                {
                    bytes = ObjectConverter.ToBytes(value, internalType);
                }
                else
                {
                    throw new EncodingException("There was an error encoding the object "
                        + "[internal type is 'Unknown', name = '" + name + "']");
                }
            }

            if (bytes != null)
            {
                this._data.Add(new Descriptor(name, internalType), bytes);
            }
        }
        #endregion

        #region Get Methods
        private byte[] GetData(string name)
        {
            IEnumerator<Descriptor> keys = this._data.Keys.GetEnumerator();
            while (keys.MoveNext())
            {
                Descriptor key = keys.Current;
                if (key.Name != null
                    && key.Name.Equals(name))
                {
                    return this._data[key];
                }
            }
            return null;
        }

        public bool GetBool(string name)
        {
            return (bool)GetValue(name, typeof(bool));
        }

        public byte GetByte(string name)
        {
            return (byte)GetValue(name, typeof(byte));
        }

        public double GetDouble(string name)
        {
            return (double)GetValue(name, typeof(double));
        }

        public float GetFloat(string name)
        {
            return (float)GetValue(name, typeof(float));
        }

        public int GetInt(string name)
        {
            return (int)GetValue(name, typeof(int));
        }

        public long GetLong(string name)
        {
            return (long)GetValue(name, typeof(long));
        }

        public short GetShort(string name)
        {
            return (short)GetValue(name, typeof(short));
        }

        public string GetString(string name)
        {
            return (string)GetValue(name, typeof(string));
        }

        public byte[] GetBinary(string name)
        {
            return (byte[])GetValue(name, typeof(byte[]));
        }

        public object GetValue(string name, Type type)
        {
            if (name == null
                || name.Equals(""))
            {
                throw new DecodingException("The given name '" + name
                    + "' is invalid. Names cannot be 'null' and "
                    + "must contain at least one character!");
            }

            byte[] data = GetData(name);
            if (data == null)
            {
                throw new DecodingException("The given name '" + name + "' does not exist in this context.");
            }

            TransferType internalType = ObjectConverter.GetTransferType(type);
            object value = null;

            if (data != null && data.Length > 0
                && internalType != TransferType.Unknown)
            {
                if (data.Length == 1 && data[0] == 0
                    && internalType != TransferType.Bool)
                {
                    value = null;
                }
                else
                {
                    value = ObjectConverter.FromBytes(data, type);
                }
            }
            else
            {
                throw new DecodingException("There was an error decoding this object " 
                    + "[internal type is 'Unknown', name = '" + name + "']");
            }

            return value;
        }
        #endregion
        #endregion

        #region Serialization / Deserialization Methods
        #region Serialization Methods
        private byte[] CreateHeader(string name, TransferType type, int contentLength)
        {
            List<byte> header = new List<byte>();

            byte[] encodedName = Encoding.UTF8.GetBytes(name);

            // start with the name length (SHORT = 2 bytes)
            header.AddRange(ObjectConverter.Encode(encodedName.Length, 2));

            // continue with the name (STRING)
            header.AddRange(encodedName);

            // continue with the content type bytes (BYTE = 1 byte)
            header.AddRange(ObjectConverter.Encode((int)type, 1));

            // finalize with the content length bytes (INT = 4 bytes)
            header.AddRange(ObjectConverter.Encode(contentLength, 4));

            return header.ToArray();
        }

        internal byte[] Serialize()
        {
            int totalLength = 0;
            List<byte> bytes = new List<byte>();

            IEnumerator<Descriptor> keys = this._data.Keys.GetEnumerator();
            while (keys.MoveNext())
            {
                Descriptor key = keys.Current;
                byte[] content = this._data[key];

                byte[] header = CreateHeader(key.Name, key.Type, content.Length);

                bytes.AddRange(header);
                bytes.AddRange(content);

                totalLength += (header.Length + content.Length);
            }

            bytes.InsertRange(0, ObjectConverter.Encode(totalLength, 4));

            return bytes.ToArray();
        }
        #endregion

        #region Deserialization Methods
        internal int Deserialize(byte[] rawData)
        {
            int counter = 0;

            // read length of NetworkStreamInfo
            int totalLength = rawData[counter] << 24;
            totalLength |= rawData[counter + 1] << 16;
            totalLength |= rawData[counter + 2] << 8;
            totalLength |= rawData[counter + 3];

            counter += 4;

            while (counter - 4 < totalLength)
            {
                int nameLength = rawData[counter] << 8;
                nameLength |= rawData[counter + 1];
                counter += 2;

                byte[] nameBytes = new byte[nameLength];
                Array.Copy(rawData, counter, nameBytes, 0, nameLength);
                counter += nameLength;

                Descriptor descriptor = new Descriptor(
                    Encoding.UTF8.GetString(nameBytes),
                    (TransferType)Enum.ToObject(typeof(TransferType),
                        (int)rawData[counter]));
                counter++;

                int contentLength = rawData[counter] << 24;
                contentLength |= rawData[counter + 1] << 16;
                contentLength |= rawData[counter + 2] << 8;
                contentLength |= rawData[counter + 3];
                counter += 4;

                // content
                byte[] contentBytes = new byte[contentLength];
                Array.Copy(rawData, counter, contentBytes, 0, contentLength);

                this._data.Add(descriptor, contentBytes);

                counter += contentLength;
            }

            return totalLength;
        }
        #endregion
        #endregion
    }
    #endregion
}

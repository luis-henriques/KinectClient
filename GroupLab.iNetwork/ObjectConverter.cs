using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GroupLab.iNetwork
{
    #region Class 'ObjectConverter'
    internal class ObjectConverter
    {
        #region Type Conversion Methods
        internal static TransferType GetTransferType(object value)
        {
            if (value != null)
            {
                return GetTransferType(value.GetType());
            }
            return TransferType.Unknown;
        }

        internal static TransferType GetTransferType(Type type)
        {
            if (type.Equals(typeof(bool)))
            {
                return TransferType.Bool;
            }
            else if (type.Equals(typeof(byte)))
            {
                return TransferType.Byte;
            }
            else if (type.Equals(typeof(double)))
            {
                return TransferType.Double;
            }
            else if (type.Equals(typeof(float)))
            {
                return TransferType.Float;
            }
            else if (type.Equals(typeof(int)))
            {
                return TransferType.Int;
            }
            else if (type.Equals(typeof(long)))
            {
                return TransferType.Long;
            }
            else if (type.Equals(typeof(short)))
            {
                return TransferType.Short;
            }
            else if (type.Equals(typeof(string)))
            {
                return TransferType.String;
            }
            else if (type.Equals(typeof(byte[])))
            {
                return TransferType.Binary;
            }
            else if (type.GetInterfaces().Contains(typeof(ITransferable)))
            {
                return TransferType.Object;
            }
            return TransferType.Unknown;
        }
        #endregion

        #region Serialization Methods
        internal static byte[] Encode(int value, int length)
        {
            byte[] bytes = new byte[length];
            for (int i = 0; i < length - 1; i++)
            {
                bytes[i] = (byte)(value >> ((length - (i + 1)) * 8));
            }
            bytes[length - 1] = (byte)(value & 0xff);

            return bytes;
        }

        internal static byte[] ToBytes(object value, TransferType type)
        {
            List<byte> bytes = new List<byte>();
            switch (type)
            {
                case TransferType.Bool:
                    bytes.AddRange(BitConverter.GetBytes((bool)value));
                    break;
                case TransferType.Byte:
                    bytes.Add((byte)value);
                    break;
                case TransferType.Double:
                    bytes.AddRange(BitConverter.GetBytes((double)value));
                    break;
                case TransferType.Float:
                    bytes.AddRange(BitConverter.GetBytes((float)value));
                    break;
                case TransferType.Int:
                    bytes.AddRange(BitConverter.GetBytes((int)value));
                    break;
                case TransferType.Long:
                    bytes.AddRange(BitConverter.GetBytes((long)value));
                    break;
                case TransferType.Short:
                    bytes.AddRange(BitConverter.GetBytes((short)value));
                    break;
                case TransferType.String:
                    bytes.AddRange(Encoding.UTF8.GetBytes((string)value));
                    break;
                case TransferType.Binary:
                    bytes.AddRange((byte[])value);
                    break;
                case TransferType.Object:
                    NetworkStreamInfo info = new NetworkStreamInfo();
                    ((ITransferable)value).GetStreamData(info);
                    bytes.AddRange(info.Serialize());
                    break;
                default:
                    break;
            }

            if (bytes == null
                || bytes.Count == 0)
            {
                return new byte[] { 0 };
            }

            return bytes.ToArray();
        }
        #endregion

        #region Deserialization Methods
        internal static byte DecodeByte(byte[] bytes)
        {
            byte value = bytes[0];
            return value;
        }

        internal static short DecodeShort(byte[] bytes)
        {
            int value = bytes[0] << 8;
            value |= bytes[1];

            return (short)value;
        }

        internal static int DecodeInt(byte[] bytes)
        {
            int value = bytes[0] << 24;
            value |= (bytes[1] << 16);
            value |= (bytes[2] << 8);
            value |= bytes[3];

            return value;
        }

        internal static byte[] GetBytes(byte[] source, int offset, int length)
        {
            byte[] bytes = new byte[length];
            Array.Copy(source, offset, bytes, 0, length);

            return bytes;
        }

        internal static object FromBytes(byte[] bytes, Type type)
        {
            TransferType transferType = GetTransferType(type);

            object value = null;
            switch (transferType)
            {
                case TransferType.Bool:
                    value = BitConverter.ToBoolean(bytes, 0);
                    break;
                case TransferType.Byte:
                    value = bytes[0];
                    break;
                case TransferType.Double:
                    value = BitConverter.ToDouble(bytes, 0);
                    break;
                case TransferType.Float:
                    value = BitConverter.ToSingle(bytes, 0);
                    break;
                case TransferType.Int:
                    value = BitConverter.ToInt32(bytes, 0);
                    break;
                case TransferType.Long:
                    value = BitConverter.ToInt64(bytes, 0);
                    break;
                case TransferType.Short:
                    value = BitConverter.ToInt16(bytes, 0);
                    break;
                case TransferType.String:
                    value = Encoding.UTF8.GetString(bytes);
                    break;
                case TransferType.Binary:
                    value = bytes;
                    break;
                case TransferType.Object:
                    NetworkStreamInfo info = new NetworkStreamInfo();
                    info.Deserialize(bytes);
                    
                    ConstructorInfo constructor = type.GetConstructor(
                        new Type[] { typeof(NetworkStreamInfo) });
                    value = constructor.Invoke(new object[] { info });
                    break;
                default:
                    break;
            }

            return value;
        }
        #endregion
    }
    #endregion
}

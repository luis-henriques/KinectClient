using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GroupLab.iNetwork.PubSub
{
    #region Class 'Field'
    public class Field : ITransferable
    {
        #region Class Members
        private string _name;

        private TransferType _type;
        #endregion

        #region Constructors
        public Field(string name)
            : this(name, TransferType.Unknown)
        { }

        public Field(string name, TransferType type)
        {
            this._name = name;
            this._type = type;
        }

        public Field(NetworkStreamInfo info)
        {
            this._name = info.GetString("name");
            this._type = (TransferType)Enum.ToObject(
                typeof(TransferType), info.GetInt("type"));
        }
        #endregion

        #region Transform Methods
        public void GetStreamData(NetworkStreamInfo info)
        {
            info.AddValue("name", this._name);
            info.AddValue("type", (int)this._type);
        }
        #endregion

        #region Properties
        public string Name
        {
            get { return this._name; }
        }

        public TransferType Type
        {
            get { return this._type; }
        }
        #endregion

        #region Overridden Methods (Object)
        public override bool Equals(object obj)
        {
            if (obj == null 
                || !(obj is Field))
            {
                return false;
            }

            Field field = obj as Field;

            bool equal = (this.Name != null && this.Name.Equals(field.Name));
            equal = equal && (this.Type == TransferType.Unknown
                || field.Type == TransferType.Unknown
                || (field.Type == TransferType.Null && (this.Type == TransferType.Null
                || this.Type == TransferType.String || this.Type == TransferType.Binary))
                || (this.Type == TransferType.Null && (field.Type == TransferType.Null
                || field.Type == TransferType.String || field.Type == TransferType.Binary))
                || (this.Type == field.Type));

            return equal;
        }

        public override int GetHashCode()
        {
            return (this.Name.Length * ((int)this.Type + 1));
        }

        public override string ToString()
        {
            string str = this.Name + " [" + this.Type + "]";
            return str;
        }
        #endregion
    }
    #endregion

    #region Class 'Template'
    public class Template : ITransferable
    {
        #region Class Members
        private string _name;

        private List<Field> _fields;
        #endregion

        #region Constructors
        public Template(string name)
        {
            this._name = name;
            this._fields = new List<Field>();
        }

        public Template(NetworkStreamInfo info)
        {
            this._name = info.GetString("name");
            this._fields = new List<Field>();
            
            int numFields = info.GetInt("num");
            for (int i = 0; i < numFields; i++)
            {
                this._fields.Add((Field)info.GetValue(
                    i.ToString(), typeof(Field)));
            }
        }
        #endregion

        #region Transform Methods
        public void GetStreamData(NetworkStreamInfo info)
        {
            info.AddValue("name", this._name);
            info.AddValue("num", this._fields.Count);

            for (int i = 0; i < this._fields.Count; i++)
            {
                info.AddValue(i.ToString(), this._fields[i]);
            }
        }
        #endregion

        #region Properties
        public string Name
        {
            get { return this._name; }
        }

        public List<Field> Fields
        {
            get { return this._fields; }
        }
        #endregion

        #region Field Methods
        public void AddField(string name)
        {
            AddField(name, TransferType.Unknown);
        }

        public void AddField(string name, TransferType type)
        {
            AddField(new Field(name, type));
        }

        public void AddField(Field field)
        {
            lock (this._fields)
            {
                if (!(this._fields.Contains(field)))
                {
                    this._fields.Add(field);
                }
            }
        }

        public void RemoveField(string name)
        {
            RemoveField(name, TransferType.Unknown);
        }

        public void RemoveField(string name, TransferType type)
        {
            RemoveField(new Field(name, type));
        }

        public void RemoveField(Field field)
        {
            lock (this._fields)
            {
                if (this._fields.Contains(field))
                {
                    this._fields.Add(field);
                }
            }
        }
        #endregion

        #region Overridden Methods (Object)
        public override bool Equals(object obj)
        {
            if (!(obj is Template))
            {
                return false;
            }

            Template template = obj as Template;
            bool equal = (this.Name != null && this.Name.Equals(template.Name));

            if (equal)
            {
                foreach (Field field in this._fields)
                {
                    if (!(template.Fields.Contains(field)))
                    {
                        equal = false;
                        break;
                    }
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            int code = this.Name.Length;

            foreach (Field field in this._fields)
            {
                code += field.GetHashCode();
            }

            return code;
        }

        public override string ToString()
        {
            string str = "Template: '" + this.Name + "' { ";
            for (int i = 0; i < this._fields.Count; i++)
            {
                str += this._fields[i].ToString() + (i < this._fields.Count - 1 ? ", " : "");
            }
            str += " }";
            return str;
        }
        #endregion
    }
    #endregion
}

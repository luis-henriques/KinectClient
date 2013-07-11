using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GroupLab.iNetwork
{
    #region Exceptions
    #region Class 'ConfigurationException'
    public class ConfigurationException : Exception
    {
        #region Constructors
        public ConfigurationException(string message)
            : base(message)
        { }
        #endregion
    }
    #endregion
    #endregion

    #region Class 'EncodingException'
    public class EncodingException : Exception
    {
        internal EncodingException()
            : base()
        { }

        internal EncodingException(string message)
            : base(message)
        { }
    }
    #endregion

    #region Class 'DecodingException'
    public class DecodingException : Exception
    {
        internal DecodingException()
            : base()
        { }

        internal DecodingException(string message)
            : base(message)
        { }
    }
    #endregion
}

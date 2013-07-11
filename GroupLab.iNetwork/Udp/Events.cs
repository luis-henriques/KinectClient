using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GroupLab.iNetwork.Udp
{
    #region Event Definitions
    #region Delegates
    // internal delegate void MulticastMessageEventHandler(object sender, string msg);
    
    internal delegate void MulticastMessageEventHandler(object sender, Message msg);
    #endregion
    #endregion
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GroupLab.iNetwork
{
    #region Interface 'ITransferable'
    public interface ITransferable
    {
        void GetStreamData(NetworkStreamInfo info);
    }
    #endregion
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusRTUDemo.Communication
{
    /// <summary>
    /// 串口接收数据事件的参数
    /// </summary>
    public class ReceiveDataEventArg : EventArgs
    {
        /// <summary>
        /// 串口接收到的数据
        /// </summary>
        public byte[] Data { get; set; }
    }
}

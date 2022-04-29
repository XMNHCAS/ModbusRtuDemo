using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusRTUDemo.Communication
{
    /// <summary>
    /// 自定义串口消息接收事件委托
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ReceiveDataEventHandler(object sender, ReceiveDataEventArg e);

    class SerialPortHelper
    {
        /// <summary>
        /// 自定义串口消息接收事件
        /// </summary>
        public event ReceiveDataEventHandler ReceiveDataEvent;

        //串口字段
        private SerialPort serialPort;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SerialPortHelper()
        {
            serialPort = new SerialPort();
        }

        /// <summary>
        /// 串口状态
        /// </summary>
        public bool Status { get => serialPort.IsOpen; }

        /// <summary>
        /// 获取当前计算机所有的串行端口名
        /// </summary>
        /// <returns></returns>
        public static string[] GetPortArray()
        {
            return SerialPort.GetPortNames();
        }

        /// <summary>
        /// 串口参数
        /// </summary>
        public void SetSerialPort(string portName, int baudrate, Parity parity, int databits, StopBits stopBits)
        {
            //端口名
            serialPort.PortName = portName;

            //波特率
            serialPort.BaudRate = baudrate;

            //奇偶校验
            serialPort.Parity = parity;

            //数据位
            serialPort.DataBits = databits;

            //停止位
            serialPort.StopBits = stopBits;

            //串口接收数据事件
            serialPort.DataReceived += ReceiveDataMethod;
        }

        /// <summary>
        /// 打开串口
        /// </summary>
        public void Open()
        {
            //打开串口
            serialPort.Open();
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        public void Close()
        {
            serialPort.Close();
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data">要发送的数据</param>
        public void SendDataMethod(byte[] data)
        {
            //获取串口状态，true为已打开，false为未打开
            bool isOpen = serialPort.IsOpen;

            if (!isOpen)
            {
                Open();
            }

            //发送字节数组
            //参数1：包含要写入端口的数据的字节数组。
            //参数2：参数中从零开始的字节偏移量，从此处开始将字节复制到端口。
            //参数3：要写入的字节数。 
            serialPort.Write(data, 0, data.Length);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data">要发送的数据</param>
        public void SendDataMethod(string data)
        {
            //获取串口状态，true为已打开，false为未打开
            bool isOpen = serialPort.IsOpen;

            if (!isOpen)
            {
                Open();
            }

            //直接发送字符串
            serialPort.Write(data);
        }

        /// <summary>
        /// 串口接收到数据触发此方法进行数据读取
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReceiveDataMethod(object sender, SerialDataReceivedEventArgs e)
        {
            ReceiveDataEventArg arg = new ReceiveDataEventArg();

            //读取串口缓冲区的字节数据
            arg.Data = new byte[serialPort.BytesToRead];
            serialPort.Read(arg.Data, 0, serialPort.BytesToRead);

            //触发自定义消息接收事件，把串口数据发送出去
            if (ReceiveDataEvent != null && arg.Data.Length != 0)
            {
                ReceiveDataEvent.Invoke(null, arg);
            }
        }
    }
}
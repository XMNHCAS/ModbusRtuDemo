using Modbus.Data;
using Modbus.Device;
using Modbus.Message;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace NModbus
{
    class Program
    {
        static void Main(string[] args)
        {
            //串口实例
            SerialPort sport = new SerialPort("COM11", 9600, Parity.None, 8, StopBits.One);

            //NModbus4实例
            ModbusMaster master = ModbusSerialMaster.CreateRtu(sport);

            //打开串口
            sport.Open();

            #region 读取线圈

            //功能码01 请求报文
            ReadCoilsInputsRequest readCoilsReq = new ReadCoilsInputsRequest(0x01, 0x01, 0, 10);
            //获取响应报文
            var readCoilsRes = master.ExecuteCustomMessage<ReadCoilsInputsResponse>(readCoilsReq);
            //输出结果
            CWRecvData("读取线圈", readCoilsRes.Data);

            //功能码02 请求报文
            ReadCoilsInputsRequest readInputCoilsReq = new ReadCoilsInputsRequest(0x02, 0x01, 0, 10);
            //获取响应报文
            var readInputCoilsRes = master.ExecuteCustomMessage<ReadCoilsInputsResponse>(readInputCoilsReq);
            //输出结果
            CWRecvData("读取输入线圈", readInputCoilsRes.Data);

            #endregion

            #region 读取寄存器

            //功能码03 请求报文
            ReadHoldingInputRegistersRequest readRegistersReq = new ReadHoldingInputRegistersRequest(0x03, 0x01, 0, 10);
            //获取响应报文
            var readRegistersRes = master.ExecuteCustomMessage<ReadHoldingInputRegistersResponse>(readRegistersReq);
            //输出结果
            CWRecvData("读取保持型寄存器", readRegistersRes.Data);

            //功能码04 请求报文
            ReadHoldingInputRegistersRequest readInputRegistersReq = new ReadHoldingInputRegistersRequest(0x04, 0x01, 0, 10);
            //获取响应报文
            var readInputRegistersRes = master.ExecuteCustomMessage<ReadHoldingInputRegistersResponse>(readInputRegistersReq);
            //输出结果
            CWRecvData("读取输入寄存器", readInputRegistersRes.Data);

            #endregion

            #region 写入线圈

            //写入单个线圈
            WriteSingleCoilRequestResponse writeSingleCoilsReq = new WriteSingleCoilRequestResponse(1, 0, true);
            //获取响应报文
            var writeSingleCoilsRes = master.ExecuteCustomMessage<WriteSingleCoilRequestResponse>(writeSingleCoilsReq);
            //输出响应报文
            CWRecvData("写入单个线圈的响应报文(无校验码)", writeSingleCoilsRes.SlaveAddress, writeSingleCoilsRes.ProtocolDataUnit);

            //批量写入线圈
            //写入的值
            DiscreteCollection writeMultipleCoilsParam = new DiscreteCollection(new List<bool> { true, true });
            //获取请求报文
            WriteMultipleCoilsRequest writeMultipleCoilsReq = new WriteMultipleCoilsRequest(1, 1, writeMultipleCoilsParam);
            //获取响应报文
            var writeMultipleCoilsRes = master.ExecuteCustomMessage<WriteMultipleCoilsResponse>(writeMultipleCoilsReq);
            //输出响应报文
            CWRecvData("批量写入线圈的响应报文(无校验码)", writeMultipleCoilsRes.SlaveAddress, writeMultipleCoilsRes.ProtocolDataUnit);

            #endregion

            #region 写入寄存器

            //写入单个寄存器
            WriteSingleRegisterRequestResponse writeSingleRegisterReq = new WriteSingleRegisterRequestResponse(1, 0, 33);
            //获取响应报文
            var writeSingleRegisterRes = master.ExecuteCustomMessage<WriteSingleRegisterRequestResponse>(writeSingleRegisterReq);
            //输出响应报文
            CWRecvData("写入单个寄存器的响应报文(无校验码)", writeSingleRegisterRes.SlaveAddress, writeSingleRegisterRes.ProtocolDataUnit);

            //批量写入寄存器
            //写入的值
            RegisterCollection writeMultipleParam = new RegisterCollection(new List<ushort> { 11, 22 });
            //获取请求报文
            WriteMultipleRegistersRequest writeMultipleRegistersReq = new WriteMultipleRegistersRequest(1, 1, writeMultipleParam);
            //获取响应报文
            var writeMultipleRegistersRes = master.ExecuteCustomMessage<WriteMultipleRegistersResponse>(writeMultipleRegistersReq);
            //输出响应报文
            CWRecvData("批量写入寄存器的响应报文(无校验码)", writeMultipleRegistersRes.SlaveAddress, writeMultipleRegistersRes.ProtocolDataUnit);

            #endregion

            #region 读与写寄存器

            //写入的值
            RegisterCollection rwMultipleParam = new RegisterCollection(new List<ushort> { 11, 22 });
            //获取读写寄存器的读与写的请求报文
            ReadWriteMultipleRegistersRequest rwMultipleRegistersReq = new ReadWriteMultipleRegistersRequest(1, 0, 10, 5, rwMultipleParam);

            //获取读取的结果
            var rMultipleRegistersReq = master.ExecuteCustomMessage<ReadHoldingInputRegistersResponse>(rwMultipleRegistersReq.ReadRequest);
            //输出结果
            CWRecvData("读取寄存器读取值", rMultipleRegistersReq.Data);

            //获取写入的响应报文
            var wMultipleRegistersReq = master.ExecuteCustomMessage<WriteMultipleRegistersResponse>(rwMultipleRegistersReq.WriteRequest);
            //输出响应报文
            CWRecvData("写入寄存器响应报文(无校验码)", wMultipleRegistersReq.SlaveAddress, wMultipleRegistersReq.ProtocolDataUnit);

            #endregion

            #region 直接使用报文读取

            //01 03 00 00 00 0A C5 CD
            //读取报文
            byte[] readMsg = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xC5, 0xCD };
            //获取请求报文
            var readMsgReq = ModbusMessageFactory.CreateModbusRequest(readMsg);
            //获取响应报文
            var readMsgRes = master.ExecuteCustomMessage<ReadHoldingInputRegistersResponse>(readMsgReq);
            //输出结果
            CWRecvData("直接使用报文读取", readMsgRes.Data);

            #endregion

            //关闭串口
            sport.Close();

            Console.ReadKey();
        }

        /// <summary>
        /// 将接收到的数据打印到控制台
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str">内容说明</param>
        /// <param name="data">接收到的数据</param>
        public static void CWRecvData<T>(string str, ICollection<T> data)
        {
            Console.WriteLine($"{str}:");
            foreach (var item in data)
            {
                Console.Write($"{item} ");
            }

            Console.WriteLine("\n");
        }

        /// <summary>
        /// 显示报文
        /// </summary>
        /// <param name="str">内容说明</param>
        /// <param name="slaveID">从站ID</param>
        /// <param name="msg">报文主体</param>
        public static void CWRecvData(string str, byte slaveID, byte[] msg)
        {
            Console.WriteLine($"{str}:");
            Console.Write($"{slaveID.ToString("X2")} ");
            foreach (var item in msg)
            {
                Console.Write($"{item.ToString("X2")} ");
            }

            Console.WriteLine("\n");
        }
    }
}
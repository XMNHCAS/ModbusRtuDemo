using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusRTUDemo.Message
{
    class AnalysisMessage
    {
        /// <summary>
        /// 解析线圈数据
        /// </summary>
        /// <param name="receiveMsg">接收到的报文</param>
        /// <returns></returns>
        public static BitArray GetCoil(byte[] receiveMsg)
        {
            //获取线圈状态
            BitArray bitArray = new BitArray(receiveMsg.Skip(3).Take(Convert.ToInt32(receiveMsg[2])).ToArray());

            return bitArray;
        }

        /// <summary>
        /// 解析寄存器数据
        /// </summary>
        /// <param name="receiveMsg">接收到的报文</param>
        /// <returns></returns>
        public static List<short> GetRegister(byte[] receiveMsg)
        {
            List<short> result = new List<short>();
            //获取字节数
            int count = Convert.ToInt32(receiveMsg[2]);
            int index = 0;
            for (int i = 3; i < count + 3; i += 2)
            {
                index++;
                //每个地址所属的字节数组
                byte[] temp = new byte[] { receiveMsg[i + 1], receiveMsg[i] };

                //获取整型结果
                result.Add(BitConverter.ToInt16(temp, 0));
            }

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusRTUDemo.Message
{
    class CheckSum
    {
        /// <summary>
        /// CRC16校验码计算
        /// </summary>
        /// <param name="data">要计算的报文</param>
        /// <returns></returns>
        public static byte[] CRC16(byte[] data)
        {
            int len = data.Length;
            if (len > 0)
            {
                ushort crc = 0xFFFF;

                for (int i = 0; i < len; i++)
                {
                    crc = (ushort)(crc ^ (data[i]));
                    for (int j = 0; j < 8; j++)
                    {
                        crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
                    }
                }
                byte hi = (byte)((crc & 0xFF00) >> 8); //高位置
                byte lo = (byte)(crc & 0x00FF); //低位置

                return BitConverter.IsLittleEndian ? new byte[] { lo, hi } : new byte[] { hi, lo };
            }
            return new byte[] { 0, 0 };
        }

        /// <summary>
        /// CRC16校验码计算
        /// </summary>
        /// <param name="data">要计算的报文</param>
        /// <returns></returns>
        public static byte[] CRC16(List<byte> data)
        {
            int len = data.Count;
            if (len > 0)
            {
                ushort crc = 0xFFFF;

                for (int i = 0; i < len; i++)
                {
                    crc = (ushort)(crc ^ (data[i]));
                    for (int j = 0; j < 8; j++)
                    {
                        crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
                    }
                }
                byte hi = (byte)((crc & 0xFF00) >> 8); //高位置
                byte lo = (byte)(crc & 0x00FF); //低位置

                return BitConverter.IsLittleEndian ? new byte[] { lo, hi } : new byte[] { hi, lo };
            }
            return new byte[] { 0, 0 };
        }
    }
}

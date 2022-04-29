using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusRTUDemo.Message
{
    class MessageGenerationModule
    {
        /// <summary>
        /// 生成读取报文
        /// </summary>
        /// <param name="slaveStation">从站地址</param>
        /// <param name="mode">读取模式</param>
        /// <param name="startAdr">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <returns></returns>
        public static byte[] MessageGeneration(int slaveStation, ReadMode mode, short startAdr, short length)
        {
            return GetReadMessage(slaveStation, mode, startAdr, length);
        }

        /// <summary>
        /// 生成写入报文
        /// </summary>
        /// <param name="slaveStation">从站地址</param>
        /// <param name="mode">写入模式</param>
        /// <param name="startAdr">起始地址</param>
        /// <param name="value">写入值</param>
        /// <returns></returns>
        //public static byte[] MessageGeneration(int slaveStation, WriteMode mode, short startAdr, object value)
        //{
        //    //C# 8.0以下版本的写法：
        //    switch (mode)
        //    {
        //        case WriteMode.Write01:
        //            return GetSingleBoolWriteMessage(slaveStation, startAdr, (bool)value);
        //        case WriteMode.Write03:
        //            return GetSingleDataWriteMessage(slaveStation, startAdr, Convert.ToInt16(value));
        //        case WriteMode.Write01s:
        //            return GetArrayBoolWriteMessage(slaveStation, startAdr, (IEnumerable<bool>)value);
        //        case WriteMode.Write03s:
        //            return GetArrayDataWriteMessage(slaveStation, startAdr, (IEnumerable<short>)value);
        //        default:
        //            return null;
        //    }
        //}

        public static byte[] MessageGeneration(int slaveStation, WriteMode mode, short startAdr, object value) => mode switch
        {
            //C# 8.0开始支持此写法，具体可查阅微软官方文档，switch表达式
            WriteMode.Write01 => GetSingleBoolWriteMessage(slaveStation, startAdr, (bool)value),
            WriteMode.Write03 => GetSingleDataWriteMessage(slaveStation, startAdr, Convert.ToInt16(value)),
            WriteMode.Write01s => GetArrayBoolWriteMessage(slaveStation, startAdr, (IEnumerable<bool>)value),
            WriteMode.Write03s => GetArrayDataWriteMessage(slaveStation, startAdr, (IEnumerable<short>)value),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), $"Not expected WriteMode value: {mode}"),
        };

        /// <summary>
        /// 获取读取数据请求报文
        /// </summary>
        /// <param name="slaveStation">从站地址</param>
        /// <param name="mode">读取模式</param>
        /// <param name="startAdr">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <returns></returns>
        private static byte[] GetReadMessage(int slaveStation, ReadMode mode, short startAdr, short length)
        {
            //定义临时字节列表
            List<byte> temp = new List<byte>();

            //依次放入头两位字节（站地址和读取模式）
            temp.Add((byte)slaveStation);
            temp.Add((byte)mode);

            //获取起始地址及读取长度
            byte[] start = BitConverter.GetBytes(startAdr);
            byte[] count = BitConverter.GetBytes(length);

            //判断系统是否为小端存储
            //如果为true，BitConverter.GetBytes方法会返回低字节在前，高字节在后的字节数组，
            //而ModbusRTU则需要高字节在前，低字节在后，所以需要做一次反转操作。
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(start);
                Array.Reverse(count);
            }

            //依次放入起始地址和读取长度
            temp.AddRange(start);
            temp.AddRange(count);

            //获取校验码并在最后放入
            temp.AddRange(CheckSum.CRC16(temp));

            return temp.ToArray();
        }

        /// <summary>
        /// 获取写入单个线圈的报文
        /// </summary>
        /// <param name="slaveStation">从站地址</param>
        /// <param name="startAdr">线圈地址</param>
        /// <param name="value">写入值</param>
        /// <returns>写入单个线圈的报文</returns>
        private static byte[] GetSingleBoolWriteMessage(int slaveStation, short startAdr, bool value)
        {
            //创建字节列表
            List<byte> temp = new List<byte>();

            //插入站地址及功能码
            temp.Add((byte)slaveStation);
            temp.Add(0x05);

            //获取线圈地址
            byte[] start = BitConverter.GetBytes(startAdr);
            //根据计算机大小端存储方式进行高低字节转换
            if (BitConverter.IsLittleEndian) Array.Reverse(start);
            //插入线圈地址
            temp.Add(start[0]);
            temp.Add(start[1]);

            //插入写入值
            temp.Add((byte)(value ? 0xFF : 0x00));
            temp.Add(0x00);

            //转换为字节数组
            byte[] result = temp.ToArray();

            //计算校验码并拼接，返回最后的报文结果
            return result.Concat(CheckSum.CRC16(temp)).ToArray();
        }

        /// <summary>
        /// 获取写入单个寄存器的报文
        /// </summary>
        /// <param name="slaveStation">从站地址</param>
        /// <param name="startAdr">寄存器地址</param>
        /// <param name="value">写入值</param>
        /// <returns>写入单个寄存器的报文</returns>
        private static byte[] GetSingleDataWriteMessage(int slaveStation, short startAdr, short value)
        {
            //从站地址
            byte station = (byte)slaveStation;

            //功能码
            byte type = 0x06;

            //寄存器地址
            byte[] start = BitConverter.GetBytes(startAdr);

            //值
            byte[] valueBytes = BitConverter.GetBytes(value);

            //根据计算机大小端存储方式进行高低字节转换
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(start);
                Array.Reverse(valueBytes);
            }

            //拼接报文
            byte[] result = new byte[] { station, type };
            result = result.Concat(start.Concat(valueBytes).ToArray()).ToArray();

            //计算校验码并拼接，返回最后的报文结果
            return result.Concat(CheckSum.CRC16(result)).ToArray();
        }

        /// <summary>
        /// 获取写入多个线圈的报文
        /// </summary>
        /// <param name="slaveStation">从站地址</param>
        /// <param name="startAdr">起始地址</param>
        /// <param name="value">写入值</param>
        /// <returns>写入多个线圈的报文</returns>
        private static byte[] GetArrayBoolWriteMessage(int slaveStation, short startAdr, IEnumerable<bool> value)
        {
            //定义报文临时存储字节集合
            List<byte> tempList = new List<byte>();

            //插入从站地址
            tempList.Add((byte)slaveStation);

            //插入功能码
            tempList.Add(0x0F);

            //获取起始地址
            byte[] start = BitConverter.GetBytes(startAdr);

            //获取写入线圈数量
            byte[] length = BitConverter.GetBytes(Convert.ToInt16(value.Count()));

            //根据计算机大小端存储方式进行高低字节转换
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(start);
                Array.Reverse(length);
            }

            //插入起始地址
            tempList.Add(start[0]);
            tempList.Add(start[1]);

            //插入写入线圈数量
            tempList.Add(length[0]);
            tempList.Add(length[1]);

            //定义写入值字节集合
            List<byte> valueTemp = new List<byte>();

            //由于一个字节只有八个位，所以如果需要写入的值超过了八个，
            //则需要生成一个新的字节用以存储，
            //所以循环截取输入的值，然后生成对应的写入值字节
            for (int i = 0; i < value.Count(); i += 8)
            {
                //写入值字节临时字节集合
                List<bool> temp = value.Skip(i).Take(8).ToList();

                //剩余位不足八个，则把剩下的所有位都放到同一个字节里
                if (temp.Count != 8)
                {
                    //取余获取剩余的位的数量
                    int m = value.Count() % 8;
                    //截取位放入临时字节集合中
                    temp = value.Skip(i).Take(m).ToList();
                }

                //获取位生成的写入值字节
                byte tempByte = GetBitArray(temp);

                //将生成的写入值字节拼接到写入值字节集合中
                valueTemp.Add(tempByte);
            }

            //获取写入值的字节数
            byte bytecount = (byte)valueTemp.Count;

            //插入写入值的字节数
            tempList.Add(bytecount);

            //插入值字节集合
            tempList.AddRange(valueTemp);

            //根据报文字节集合计算CRC16校验码，并拼接到最后，然后转换为字节数组并返回
            return tempList.Concat(CheckSum.CRC16(tempList)).ToArray();
        }

        /// <summary>
        /// 获取写入多个寄存器的报文
        /// </summary>
        /// <param name="slaveStation">从站地址</param>
        /// <param name="startAdr">起始地址</param>
        /// <param name="value">写入值</param>
        /// <returns>写入多个寄存器的报文</returns>
        private static byte[] GetArrayDataWriteMessage(int slaveStation, short startAdr, IEnumerable<short> value)
        {
            //定义报文临时存储字节集合
            List<byte> tempList = new List<byte>();

            //插入从站地址
            tempList.Add((byte)slaveStation);

            //插入功能码
            tempList.Add(0x10);

            //获取起始地址
            byte[] start = BitConverter.GetBytes(startAdr);

            //获取写入值的数量
            byte[] length = BitConverter.GetBytes(Convert.ToInt16(value.Count()));

            //根据计算机大小端存储方式进行高低字节转换
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(start);
                Array.Reverse(length);
            }

            //插入起始地址
            tempList.AddRange(start);

            //插入写入值数量
            tempList.AddRange(length);

            //创建写入值字节集合
            List<byte> valueBytes = new List<byte>();

            //将需要插入的每个值转换为字节数组，
            //并根据计算机大小端存储方式进行高低字节转换
            //然后插入到值的字节集合中
            foreach (var item in value)
            {
                byte[] temp = BitConverter.GetBytes(item);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(temp);
                }
                valueBytes.AddRange(temp);
            }

            //获取写入值的字节数
            byte count = Convert.ToByte(valueBytes.Count);

            //插入写入值的字节数
            tempList.Add(count);

            //插入写入值字节集合
            tempList.AddRange(valueBytes);

            //根据报文字节集合计算CRC16校验码，并拼接到最后，然后转换为字节数组并返回
            return tempList.Concat(CheckSum.CRC16(tempList)).ToArray();
        }

        /// <summary>
        /// 反转顺序并生成字节
        /// </summary>
        /// <param name="data">位数据</param>
        /// <returns></returns>
        private static byte GetBitArray(IEnumerable<bool> data)
        {
            //把位数据集合反转
            data.Reverse();

            //定义初始字节，值为0000 0000
            byte temp = 0x00;

            //循环计数
            int index = 0;

            //循环位集合
            foreach (bool item in data)
            {
                //判断每一位的数据，为true则左移一个1到对应的位置
                if (item) temp = (byte)(temp | (0x01 << index));

                //计数+1
                index++;
            }

            //返回最后使用位数据集合生成的二进制字节
            return temp;
        }
    }
}
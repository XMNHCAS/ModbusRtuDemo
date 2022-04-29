using Modbus.Device;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NModbusDemo
{
    public partial class DemoForm : Form
    {
        /// <summary>
        /// 私有串口实例
        /// </summary>
        private SerialPort serialPort = new SerialPort();

        /// <summary>
        /// 私有ModbusRTU主站字段
        /// </summary>
        private static IModbusMaster master;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DemoForm()
        {
            InitializeComponent();            
        }

        /// <summary>
        /// 窗体加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DemoForm_Load(object sender, EventArgs e)
        {
            //设置可选串口
            cbxPort.Items.AddRange(SerialPort.GetPortNames());
            //设置可选波特率
            cbxBaudRate.Items.AddRange(new object[] { 9600, 19200 });
            //设置可选奇偶校验
            cbxParity.Items.AddRange(new object[] { "None", "Odd", "Even", "Mark", "Space" });
            //设置可选数据位
            cbxDataBits.Items.AddRange(new object[] { 5, 6, 7, 8 });
            //设置可选停止位
            cbxStopBits.Items.AddRange(new object[] { 1, 1.5, 2 });
            //设置读写模式
            cbxMode.Items.AddRange(new object[] {
                "读取输出线圈",
                "读取离散输入",
                "读取保持型寄存器",
                "读取输入寄存器",
                "写入单个线圈",
                "写入多个线圈",
                "写入单个寄存器",
                "写入多个寄存器"
            });

            //设置默认选中项
            cbxPort.SelectedIndex = 1;
            cbxBaudRate.SelectedIndex = 0;
            cbxParity.SelectedIndex = 0;
            cbxDataBits.SelectedIndex = 3;
            cbxStopBits.SelectedIndex = 0;
            cbxMode.SelectedIndex = 0;

            nudLength.Minimum = 1;
            nudSlaveID.Minimum = 1;
            nudStartAdr.Minimum = 0;

            //设置为默认输入法，即为英文半角
            rbxRWMsg.ImeMode = ImeMode.Disable;
        }

        /// <summary>
        /// 模式切换事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbxMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            rbxRWMsg.Clear();
            if (cbxMode.SelectedItem.ToString().Contains("读取"))
            {
                btnRW.Text = "读取";
                rbxRWMsg.Enabled = false;
                nudLength.Enabled = true;
            }
            else
            {
                btnRW.Text = "写入";
                rbxRWMsg.Enabled = true;
                nudLength.Enabled = false;
            }
        }

        /// <summary>
        /// 读写事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRW_Click(object sender, EventArgs e)
        {
            //设定串口参数
            serialPort.PortName = cbxPort.SelectedItem.ToString();
            serialPort.BaudRate = (int)cbxBaudRate.SelectedItem;
            serialPort.Parity = GetSelectedParity();
            serialPort.DataBits = (int)cbxDataBits.SelectedItem;
            serialPort.StopBits = GetSelectedStopBits();

            //创建ModbusRTU主站实例
            master = ModbusSerialMaster.CreateRtu(serialPort);

            //打开串口
            if (!serialPort.IsOpen) serialPort.Open();
            
            //根据选择的模式进行读写
            switch (cbxMode.SelectedItem.ToString())
            {
                case "读取输出线圈":
                    SetMsg(ReadCoils().ToList());
                    break;
                case "读取离散输入":
                    SetMsg(ReadInputs().ToList());
                    break;
                case "读取保持型寄存器":
                    SetMsg(ReadHoldingRegisters().ToList());
                    break;
                case "读取输入寄存器":
                    SetMsg(ReadInputRegisters().ToList());
                    break;
                case "写入单个线圈":
                    if (rbxRWMsg.Text.Contains(","))
                    {
                        MessageBox.Show("输入值过多");
                        serialPort.Close();
                        return;
                    }
                    WriteSingleCoil();
                    break;
                case "写入多个线圈":
                    WriteArrayCoil();
                    break;
                case "写入单个寄存器":
                    if (rbxRWMsg.Text.Contains(","))
                    {
                        MessageBox.Show("输入值过多");
                        serialPort.Close();
                        return;
                    }
                    WriteSingleRegister();
                    break;
                case "写入多个寄存器":
                    WriteArrayRegister();
                    break;
                default:
                    break;
            }

            //关闭串口
            serialPort.Close();
        }

        /// <summary>
        /// 更新写入值计数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbxRWMsg_TextChanged(object sender, EventArgs e)
        {
            nudLength.Value = Regex.Matches(rbxRWMsg.Text, ",").Count + 1;
        }

        /// <summary>
        /// 获取窗体选中的奇偶校验
        /// </summary>
        /// <returns></returns>
        private Parity GetSelectedParity()
        {
            switch (cbxParity.SelectedItem.ToString())
            {
                case "Odd":
                    return Parity.Odd;
                case "Even":
                    return Parity.Even;
                case "Mark":
                    return Parity.Mark;
                case "Space":
                    return Parity.Space;
                case "None":
                default:
                    return Parity.None;
            }
        }

        /// <summary>
        /// 获取窗体选中的停止位
        /// </summary>
        /// <returns></returns>
        private StopBits GetSelectedStopBits()
        {
            switch (Convert.ToDouble(cbxStopBits.SelectedItem))
            {
                case 1:
                    return StopBits.One;
                case 1.5:
                    return StopBits.OnePointFive;
                case 2:
                    return StopBits.Two;
                default:
                    return StopBits.One;
            }
        }

        /// <summary>
        /// 写入单个线圈
        /// </summary>
        private void WriteSingleCoil()
        {
            bool result = false;
            if (rbxRWMsg.Text.Equals("true", StringComparison.OrdinalIgnoreCase) || rbxRWMsg.Text.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                result = true;
            }
            master.WriteSingleCoil((byte)nudSlaveID.Value, (ushort)nudStartAdr.Value, result);
        }

        /// <summary>
        /// 批量写入线圈
        /// </summary>
        private void WriteArrayCoil()
        {
            List<string> strList = rbxRWMsg.Text.Split(',').ToList();

            List<bool> result = new List<bool>();

            strList.ForEach(m => result.Add(m.Equals("true", StringComparison.OrdinalIgnoreCase) || m.Equals("1", StringComparison.OrdinalIgnoreCase)));

            master.WriteMultipleCoils((byte)nudSlaveID.Value, (ushort)nudStartAdr.Value, result.ToArray());
        }

        /// <summary>
        /// 写入单个寄存器
        /// </summary>
        private void WriteSingleRegister()
        {
            ushort result = Convert.ToUInt16(rbxRWMsg.Text);

            master.WriteSingleRegister((byte)nudSlaveID.Value, (ushort)nudStartAdr.Value, result);
        }

        /// <summary>
        /// 批量写入寄存器
        /// </summary>
        private void WriteArrayRegister()
        {
            List<string> strList = rbxRWMsg.Text.Split(',').ToList();

            List<ushort> result = new List<ushort>();

            strList.ForEach(m => result.Add(Convert.ToUInt16(m)));

            master.WriteMultipleRegisters((byte)nudSlaveID.Value, (ushort)nudStartAdr.Value, result.ToArray());
        }

        /// <summary>
        /// 读取输出线圈
        /// </summary>
        /// <returns></returns>
        private bool[] ReadCoils()
        {
            return master.ReadCoils((byte)nudSlaveID.Value, (ushort)nudStartAdr.Value, (ushort)nudLength.Value);
        }

        /// <summary>
        /// 读取输入线圈
        /// </summary>
        /// <returns></returns>
        private bool[] ReadInputs()
        {
            return master.ReadInputs((byte)nudSlaveID.Value, (ushort)nudStartAdr.Value, (ushort)nudLength.Value);
        }

        /// <summary>
        /// 读取保持型寄存器
        /// </summary>
        /// <returns></returns>
        private ushort[] ReadHoldingRegisters()
        {
            return master.ReadHoldingRegisters((byte)nudSlaveID.Value, (ushort)nudStartAdr.Value, (ushort)nudLength.Value);
        }

        /// <summary>
        /// 读取输入寄存器
        /// </summary>
        /// <returns></returns>
        private ushort[] ReadInputRegisters()
        {
            return master.ReadInputRegisters((byte)nudSlaveID.Value, (ushort)nudStartAdr.Value, (ushort)nudLength.Value);
        }

        /// <summary>
        /// 界面显示读取结果
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        private void SetMsg<T>(List<T> result)
        {
            string msg = string.Empty;

            result.ForEach(m => msg += $"{m} ");

            rbxRWMsg.Text = msg.Trim();
        }
    }
}
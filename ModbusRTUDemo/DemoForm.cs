using ModbusRTUDemo.Communication;
using ModbusRTUDemo.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace ModbusRTUDemo
{
    public partial class DemoForm : Form
    {
        #region Field

        /// <summary>
        /// 串口类
        /// </summary>
        SerialPortHelper conHelper = new SerialPortHelper();

        /// <summary>
        /// 是否为写入模式
        /// </summary>
        private bool isWrite = false;

        /// <summary>
        /// 是否读写线圈
        /// </summary>
        private bool isCoil = true;

        /// <summary>
        /// 是否读写单个值
        /// </summary>
        private bool isSingleData = true;

        /// <summary>
        /// 读写模式
        /// </summary>
        private object readWriteMode = null;

        #endregion

        #region Ctor

        public DemoForm()
        {
            InitializeComponent();
        }

        #endregion

        #region FormEvent

        /// <summary>
        /// 窗体加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DemoForm_Load(object sender, EventArgs e)
        {
            //设置可选串口
            cbxPort.Items.AddRange(SerialPortHelper.GetPortArray());
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

            //显示连接状态
            tbxStatus.Text = conHelper.Status ? "连接成功" : "未连接";

            //从站地址默认为1
            nudStation.Value = 1;

            //设置为默认输入法，即为英文半角
            tbxValue.ImeMode = ImeMode.Disable;

            //初始化禁用读写按钮（未打开串口连接）
            btnRW.Enabled = false;

            //注册接收消息的事件
            conHelper.ReceiveDataEvent += ReceiveDataEvent;
        }

        /// <summary>
        /// 打开或者关闭串口连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCon_Click(object sender, EventArgs e)
        {
            if (!conHelper.Status)
            {
                //串口号
                string port = cbxPort.SelectedItem.ToString();
                //波特率
                int baudrate = (int)cbxBaudRate.SelectedItem;
                //奇偶校验
                Parity parity = GetSelectedParity();
                //数据位
                int databits = (int)cbxDataBits.SelectedItem;
                //停止位
                StopBits stopBits = GetSelectedStopBits();

                //设定串口参数
                conHelper.SetSerialPort(port, baudrate, parity, databits, stopBits);
                //打开串口
                conHelper.Open();

                Thread.Sleep(200);

                //刷新状态
                tbxStatus.Text = conHelper.Status ? "连接成功" : "未连接";

                //启用读写按钮
                btnRW.Enabled = true;

                btnCon.Text = "关闭串口";
            }
            else
            {
                //关闭串口
                conHelper.Close();

                tbxStatus.Text = conHelper.Status ? "连接成功" : "未连接";

                //禁用读写按钮
                btnRW.Enabled = false;

                btnCon.Text = "打开串口";
            }
        }

        /// <summary>
        /// 读写按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRW_Click(object sender, EventArgs e)
        {
            //生成的报文
            byte[] message = null;
            //从站地址
            short station = (short)nudStation.Value;
            //起始地址
            short stratAdr = (short)nudAddress.Value;
            //读写数量
            short count = (short)nudCount.Value;

            if (isWrite)
            {
                //生成写入报文
                WriteMode mode = (WriteMode)readWriteMode;

                //生成单个或多个值的写入报文
                if (isSingleData)
                {
                    //判断是否输入单个值
                    if (tbxValue.Text.IndexOf(",") != -1)
                    {
                        MessageBox.Show("输入值过多");
                        return;
                    }

                    //生成写入单个值的写入报文
                    if (isCoil)
                    {
                        //生成写入单个线圈的报文
                        bool value = false;
                        if (string.Equals(tbxValue.Text, "True", StringComparison.OrdinalIgnoreCase) || tbxValue.Text == "1")
                        {
                            value = true;
                        }
                        else if (string.Equals(tbxValue.Text, "False", StringComparison.OrdinalIgnoreCase) || tbxValue.Text == "0")
                        {
                            value = false;
                        }
                        else
                        {
                            MessageBox.Show("输入值只能是1、0或者true、false");
                            return;
                        }
                        message = MessageGenerationModule.MessageGeneration(station, mode, stratAdr, value);
                    }
                    else
                    {
                        //生成写入单个寄存器的报文
                        try
                        {
                            message = MessageGenerationModule.MessageGeneration(station, mode, stratAdr, short.Parse(tbxValue.Text));
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("输入有误");
                            return;
                        }
                    }
                }
                else
                {
                    //输入值数组
                    string[] arr = tbxValue.Text.Split(",");

                    if (isCoil)
                    {
                        //生成写入多个线圈的报文
                        List<bool> value = new List<bool>();
                        for (int i = 0; i < arr.Length; i++)
                        {
                            bool temp = false;
                            if (string.Equals(arr[i], "True", StringComparison.OrdinalIgnoreCase) || arr[i] == "1")
                            {
                                temp = true;
                            }
                            else if (string.Equals(tbxValue.Text, "False", StringComparison.OrdinalIgnoreCase) || arr[i] == "0")
                            {
                                temp = false;
                            }
                            else
                            {
                                MessageBox.Show("输入值只能是1、0或者true、false");
                                return;
                            }

                            value.Add(temp);
                        }
                        message = MessageGenerationModule.MessageGeneration(station, mode, stratAdr, value);
                    }
                    else
                    {
                        //生成写入多个寄存器的报文
                        try
                        {
                            List<short> value = new List<short>();
                            for (int i = 0; i < arr.Length; i++)
                            {
                                value.Add(short.Parse(arr[i]));
                            }
                            message = MessageGenerationModule.MessageGeneration(station, mode, stratAdr, value);
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("输入有误");
                            return;
                        }
                    }
                }
            }
            else
            {
                //生成读取报文
                ReadMode mode = (ReadMode)readWriteMode;
                message = MessageGenerationModule.MessageGeneration(station, mode, stratAdr, count);
            }

            //发送报文
            conHelper.SendDataMethod(message);

            //将发送的报文显示在窗体中
            string msgStr = "";
            for (int i = 0; i < message.Length; i++)
            {
                msgStr += message[i].ToString("X2") + " ";
            }
            rbxSendMsg.Text = msgStr;
        }


        /// <summary>
        /// 读写模式切换事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbxMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            //更新状态字段
            GetReadWriteMode();

            //计数复位
            nudCount.Value = 1;
            //清空输入值
            tbxValue.Clear();
            //是否显示提示文本
            labTip.Visible = isSingleData ? false : true;
            //是否可输入值
            tbxValue.Enabled = isWrite ? true : false;
            //是否可修改计数
            nudCount.Enabled = isWrite ? false : true;
            //读写按钮显示文本
            btnRW.Text = isWrite ? "写入" : "读取";
        }

        /// <summary>
        /// 根据输入值的数量同步刷新窗体计数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbxValue_TextChanged(object sender, EventArgs e)
        {
            nudCount.Value = Regex.Matches(tbxValue.Text, ",").Count + 1;
        }

        #endregion

        #region Event

        /// <summary>
        /// 接收消息的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">事件参数</param>
        private void ReceiveDataEvent(object sender, ReceiveDataEventArg e)
        {
            //在窗体上显示接收到的报文
            string msgStr = "";
            for (int i = 0; i < e.Data.Length; i++)
            {
                msgStr += e.Data[i].ToString("X2") + " ";
            }
            rbxRecMsg.Invoke(new Action(() => { rbxRecMsg.Text = msgStr; }));

            //如果是读取数据，则对接收到的消息进行解析
            if (!isWrite)
            {
                string result = "";

                if (isCoil)
                {
                    BitArray bitArray = AnalysisMessage.GetCoil(e.Data);

                    int count = Convert.ToInt32(nudCount.Value);

                    for (int i = 0; i < count; i++)
                    {
                        result += bitArray[i].ToString() + ",";
                    }
                }
                else
                {
                    List<short> list = AnalysisMessage.GetRegister(e.Data);

                    list.ForEach(m => { result += m.ToString() + ","; });
                }

                tbxValue.Invoke(new Action(() => { tbxValue.Text = result.Remove(result.LastIndexOf(","), 1); }));
            }
        }

        #endregion

        #region Methods

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
        /// 根据选中的读写模式更新字段值
        /// </summary>
        private void GetReadWriteMode()
        {
            switch (cbxMode.SelectedItem.ToString())
            {
                case "读取输出线圈":
                default:
                    isWrite = false;
                    isSingleData = false;
                    isCoil = true;
                    readWriteMode = ReadMode.Read01;
                    break;

                case "读取离散输入":
                    isWrite = false;
                    isSingleData = false;
                    isCoil = true;
                    readWriteMode = ReadMode.Read02;
                    break;

                case "读取保持型寄存器":
                    isWrite = false;
                    isSingleData = false;
                    isCoil = false;
                    readWriteMode = ReadMode.Read03;
                    break;

                case "读取输入寄存器":
                    isWrite = false;
                    isSingleData = false;
                    isCoil = false;
                    readWriteMode = ReadMode.Read04;
                    break;

                case "写入单个线圈":
                    isWrite = true;
                    isSingleData = true;
                    isCoil = true;
                    readWriteMode = WriteMode.Write01;
                    break;

                case "写入多个线圈":
                    isWrite = true;
                    isSingleData = false;
                    isCoil = true;
                    readWriteMode = WriteMode.Write01s;
                    break;

                case "写入单个寄存器":
                    isWrite = true;
                    isSingleData = true;
                    isCoil = false;
                    readWriteMode = WriteMode.Write03;
                    break;

                case "写入多个寄存器":
                    isWrite = true;
                    isSingleData = false;
                    isCoil = false;
                    readWriteMode = WriteMode.Write03s;
                    break;
            }
        }

        #endregion       
    }
}
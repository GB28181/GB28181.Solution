using GB28181.Net;
using GB28181.Servers;
using GB28181.Servers.SIPMessage;
using GB28181.Servers.SIPMonitor;
using GB28181;
using GB28181.Sys;
using GB28181.Config;
using GB28181.Sys.Model;
using GB28181.Sys.XML;
using GB28181.Logger4Net;
using StreamingKit;
using StreamingKit.Media.TS;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using GB28181.WinTool.Player.Analyzer;
using GB28181.App;
using GB28181.Cache;
using System.Threading.Tasks;
using SIPSorcery.SIP;
namespace GB28181.WinTool
{
    public partial class WinTool : Form
    {
        #region 私有字段
        private Queue<Catalog> _catalogQueue = new Queue<Catalog>();
        private Thread _cataThread;
        private static ILog logger = AppState.logger;
        private bool _isStop = true;
        //private string _dir = AppDomain.CurrentDomain.BaseDirectory + "Config";
        private DateTime _keepaliveTime;
        private Thread _keepaliveThread;
        private Queue<Keep> _keepQueue = new Queue<Keep>();
        private int _count = 0;
        private SIPMessageCore _messageCore;
        private readonly CancellationTokenSource _registryServiceToken = new CancellationTokenSource();

        #endregion

        #region 私有属性
        /// <summary>
        /// 选择的设备键
        /// </summary>
        private MonitorKey DevKey
        {
            get
            {
                var devItem = new ListViewItem();
                foreach (var item in lvDev.SelectedItems.Cast<ListViewItem>())
                {
                    devItem = item;
                }
                var key = new MonitorKey()
                {
                    DeviceID = devItem.ImageKey,
                    CmdType = CommandType.Play
                };
                return key;
            }
        }
        #endregion

        #region 委托回调
        /// <summary>
        /// 设置服务状态回调
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="state"></param>
        public delegate void SetSIPServiceText(string msg, ServiceStatus state);
        /// <summary>
        /// 设置设备查询目录回调
        /// </summary>
        /// <param name="cata"></param>
        public delegate void SetCatalogText(Catalog cata);
        /// <summary>
        /// 设置录像文件查询回调
        /// </summary>
        /// <param name="record"></param>
        public delegate void SetRecordText(RecordInfo record);
        /// <summary>
        /// 设置心跳消息回调
        /// </summary>
        /// <param name="remoteEP"></param>
        /// <param name="keepalive"></param>
        public delegate void SetKeepaliveText(string remoteEP, KeepAlive keepalive);
        #endregion

        #region 初始化服务
        public WinTool()
        {
            InitializeComponent();
        }

        private void Initialize()
        {
            _devList = new List<ListViewItem>();
            txtStartTime.Text = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd 8:00:00");
            txtStopTime.Text = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd 9:00:00");
            //txtDragDrop.Text = DateTime.Now.ToString("yyyy-MM-dd 14:00:00");

            Dictionary<string, string> configType = new Dictionary<string, string>();
            configType.Add("BasicParam", "基本参数配置");
            configType.Add("VideoParamOpt", "视频参数范围");
            configType.Add("SVACEncodeConfig", "SVAC编码配置");
            configType.Add("SVACDecodeConfig", "SVAC解码配置");
            BindingSource bs = new BindingSource();
            bs.DataSource = configType;

            cbxConfig.DataSource = bs;
            cbxConfig.DisplayMember = "Value";
            cbxConfig.ValueMember = "Key";

            Dictionary<string, string> recordType = new Dictionary<string, string>();
            recordType.Add("time", "time");
            recordType.Add("alarm", "alarm");
            recordType.Add("manual", "manual");
            recordType.Add("all", "all");
            BindingSource bsRecord = new BindingSource();
            bsRecord.DataSource = recordType;

            cbxRecordType.DataSource = bsRecord;
            cbxRecordType.DisplayMember = "Value";
            cbxRecordType.ValueMember = "Key";

            //SIPSqlite.Instance.Read();
            //  var cameras = new List<CameraInfo>();
            // var account = SIPSqlite.Instance.Accounts.First();
            //if (account == null)
            //{
            //    logger.Error("Account Config NULL,SIP not started");
            //    return;
            //}
            _keepaliveTime = DateTime.Now;
            _cataThread = new Thread(new ThreadStart(HandleCata));
            _keepaliveThread = new Thread(new ThreadStart(HandleKeepalive));
            // _messageCore = new SIPMessageCore(cameras, account);




            SIPTransport m_sipTransport;

            m_sipTransport = new SIPTransport(new SIPTransactionEngine(), false)
            {
                PerformanceMonitorPrefix = SIPSorceryPerformanceMonitor.REGISTRAR_PREFIX
            };
            SIPAccount account = SipStorage.Instance.Accounts.FirstOrDefault();
            var sipChannels = SIPTransportConfig.ParseSIPChannelsNode(account);
            m_sipTransport.AddSIPChannel(sipChannels);

            SipStorage sipAccountStorage = new SipStorage();
            IMemoCache<Camera> memocache = new DeviceObjectCache();
            SIPRegistrarCore sipRegistrarCore = new SIPRegistrarCore(m_sipTransport, sipAccountStorage, memocache, true, true);
            // _messageCore = new SIPMessageCore(m_sipTransport, SIPConstants.SIP_SERVER_STRING);
            _messageCore = new SIPMessageCore(sipRegistrarCore, m_sipTransport, sipAccountStorage, memocache);

            Task.Factory.StartNew(() => sipRegistrarCore.ProcessRegisterRequest(), _registryServiceToken.Token);

        }
        #endregion

        #region 其他消息回调
        private void MediaStatusReceived(SIPEndPoint endPoint, MediaStatus mediaStatus)
        {
            //this.txtMsg.Text = "GB28181Platform --------------> 发送媒体文件完成。";
        }

        #endregion

        #region 开始/停止服务
        private void btnStart_Click(object sender, System.EventArgs e)
        {
            //_tn = new TreeNode();
            //tvCalatog.Nodes.Add(_tn);
            //_bg = new TreeNode();
            //_tn.Nodes.Add(_bg);

            //tvCalatog.Nodes.Add(_tn);
            _keepaliveTime = DateTime.Now;
            playerWin = new GB28181.WinTool.PlayerControl();
            playerWin.Start();
            Initialize();

            _cataThread.Start();
            _keepaliveThread.Start();

            _messageCore.Start();
            _messageCore.OnServiceChanged += SIPServiceChangeHandle;
            _messageCore.OnCatalogReceived += m_msgCore_OnCatalogReceived;
            _messageCore.OnNotifyCatalogReceived += MessageCore_OnNotifyCatalogReceived;
            _messageCore.OnAlarmReceived += MessageCore_OnAlarmReceived;

            _messageCore.OnRecordInfoReceived += MessageCore_OnRecordInfoReceived;
            _messageCore.OnKeepaliveReceived += MessageCore_OnKeepaliveReceived;
            _messageCore.OnDeviceStatusReceived += DeviceStatusReceived;
            _messageCore.OnDeviceInfoReceived += DeviceInfoReceived;
            _messageCore.OnMediaStatusReceived += MediaStatusReceived;
            _messageCore.OnPresetQueryReceived += _messageCore_OnPresetQueryReceived;
            _messageCore.OnDeviceConfigDownloadReceived += _messageCore_OnDeviceConfigDownloadReceived;
            lblStatus.Text = "sip服务已启动。。。";
            lblStatus.ForeColor = Color.FromArgb(0, 192, 0);
        }

        void _messageCore_OnDeviceConfigDownloadReceived(SIPEndPoint arg1, DeviceConfigDownload config)
        {
            Invoke(new Action(() =>
            {
                txtMsg.Text = DeviceConfigDownload.Instance.Save<DeviceConfigDownload>(config);
            }));
        }



        private void btnStop_Click(object sender, EventArgs e)
        {
            playerWin.Stop();
            _isStop = false;
            _messageCore.Stop();
            _messageCore.OnServiceChanged -= SIPServiceChangeHandle;
            _messageCore.OnCatalogReceived -= m_msgCore_OnCatalogReceived;
            _messageCore.OnNotifyCatalogReceived -= MessageCore_OnNotifyCatalogReceived;
            _messageCore.OnRecordInfoReceived -= MessageCore_OnRecordInfoReceived;
            _messageCore.OnKeepaliveReceived -= MessageCore_OnKeepaliveReceived;
            lvDev.Items.Clear();
            lblStatus.Text = "sip服务已停止。。。";
            lblStatus.ForeColor = Color.Blue;
        }
        #endregion

        #region 心跳消息
        private void HandleKeepalive()
        {
            while (_isStop)
            {
                if (_keepQueue.Count > 0)
                {
                    Keep keep = null;
                    lock (_keepQueue)
                    {
                        keep = _keepQueue.Dequeue();
                    }
                    if (keep != null)
                    {
                        if (lblKeepalive.InvokeRequired)
                        {
                            SetKeepaliveText keepTxt = new SetKeepaliveText(SetKeepalive);
                            this.Invoke(keepTxt, new object[] { keep.RemoteEP.ToHost(), keep.Heart });
                        }
                        else
                        {
                            SetKeepalive(keep.RemoteEP.ToHost(), keep.Heart);
                        }
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        private void SetKeepalive(string remoteEP, KeepAlive keepalive)
        {
            lblKeepalive.Text = string.Format("序号:{0},状态:{1},心跳时间:{2}", keepalive.SN, keepalive.Status, _keepaliveTime.ToString());
            lblKeepalive.ForeColor = Color.FromArgb(new Random().Next(1, int.MaxValue));
        }

        void MessageCore_OnKeepaliveReceived(SIPEndPoint remoteEP, KeepAlive keapalive, string devId)
        {
            _keepaliveTime = DateTime.Now;
            Keep keep = new Keep()
            {
                RemoteEP = remoteEP,
                Heart = keapalive
            };
            _keepQueue.Enqueue(keep);
        }
        #endregion

        #region 目录查询/通知
        private void HandleCata()
        {
            while (_isStop)
            {
                if (_catalogQueue.Count > 0)
                {
                    lock (_catalogQueue)
                    {
                        SetDevText(_catalogQueue.Dequeue());
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        /// <summary>
        /// 报警通知回调
        /// </summary>
        /// <param name="alarm"></param>
        void MessageCore_OnAlarmReceived(Alarm alarm)
        {
            string msg = "DeviceID:" + alarm.DeviceID +
               "\r\nSN:" + alarm.SN +
               "\r\nCmdType:" + alarm.CmdType +
               "\r\nAlarmPriority:" + alarm.AlarmPriority +
               "\r\nAlarmMethod:" + alarm.AlarmMethod +
               "\r\nAlarmTime:" + alarm.AlarmTime +
               "\r\nAlarmDescription:" + alarm.AlarmDescription;
            Invoke(new Action(() =>
            {
                MessageBox.Show(msg);
                var key = new MonitorKey()
                {
                    CmdType = CommandType.Play,
                    DeviceID = alarm.DeviceID
                };
                _messageCore.NodeMonitorService[key.ToString()].AlarmResponse(alarm);
                //txtMsg.Text = msg;
            }));
        }
        /// <summary>
        /// 目录通知回调
        /// </summary>
        /// <param name="obj"></param>
        void MessageCore_OnNotifyCatalogReceived(NotifyCatalog notify)
        {
            if (notify.DeviceList == null)
            {
                return;
            }
            new Action(() =>
            {
                foreach (var item in notify.DeviceList.Items)
                {
                    var listview = lvDev.Items.Cast<ListViewItem>();
                    foreach (var view in listview)
                    {
                        if (item.DeviceID == view.ImageKey)
                        {
                            view.ForeColor = Color.Red;
                            view.SubItems[2].Text = "OFF";
                        }
                    }
                }
            }).BeginInvoke(null, null);

        }

        /// <summary>
        /// 目录查询回调
        /// </summary>
        /// <param name="cata"></param>
        private void m_msgCore_OnCatalogReceived(Catalog cata)
        {
            _catalogQueue.Enqueue(cata);
        }

        int _devSN = 1;
        private TreeNode _tn;
        private TreeNode _bg;
        private List<ListViewItem> _devList;
        /// <summary>
        /// 设置设备目录
        /// </summary>
        /// <param name="cata">设备目录</param>
        private void SetDevText(Catalog cata)
        {
            foreach (Catalog.Item item in cata.DeviceList.Items)
            {
                DevCataType devCata = DevType.GetCataType(item.DeviceID);
                ListViewItem lvItem;
                switch (devCata)
                {
                    case DevCataType.UnKnown:
                        break;
                    case DevCataType.ProviceCata:
                    case DevCataType.CityCata:
                    case DevCataType.AreaCata:
                    case DevCataType.BasicUnit:
                    case DevCataType.SystemCata:
                    case DevCataType.BusinessGroupCata:
                    case DevCataType.VirtualGroupCata:  //目录类型
                        lvItem = new ListViewItem(new string[] { item.DeviceID, devCata.ToString(), item.Name });
                        lvItem.ImageKey = item.DeviceID;
                        this.Invoke(new Action(() =>
                        {
                            lvCata.Items.Add(lvItem);
                            //if (devCata == DevCataType.SystemCata)
                            //{
                            //    //_tn.Name = item.Name + "[" + item.DeviceID + "]";
                            //    //_tn.Text = item.Name + "[" + item.DeviceID + "]";
                            //    //_tn.Tag = devCata.ToString();
                            //}
                            //else if (devCata == DevCataType.CityCata)
                            //{
                            //    _tn.Name = item.Name + "[" + item.DeviceID + "]";
                            //    _tn.Text = item.Name + "[" + item.DeviceID + "]";
                            //    _tn.Tag = devCata.ToString();
                            //}
                            //else if (devCata == DevCataType.BusinessGroupCata)
                            //{
                            //    _bg.Name = item.Name + "[" + item.DeviceID + "]";
                            //    _bg.Text = item.Name + "[" + item.DeviceID + "]";
                            //    _bg.Tag = devCata.ToString();

                            //    //_tn.Nodes.Add(_bg);
                            //}
                            //else if (devCata == DevCataType.VirtualGroupCata)
                            //{
                            //    var vg = new TreeNode();
                            //    vg.Name = item.Name + "[" + item.DeviceID + "]";
                            //    vg.Text = item.Name + "[" + item.DeviceID + "]";
                            //    vg.Tag = devCata.ToString();

                            //    _bg.Nodes.Add(vg);
                            //}

                        }));
                        break;
                    case DevCataType.Device:    //设备类型
                        lvItem = new ListViewItem(new string[] { _devSN.ToString(), item.Name, item.Status.ToString() });
                        lvItem.ImageKey = item.DeviceID;
                        _devList.Add(lvItem);
                        this.Invoke(new Action(() =>
                        {
                            lvDev.Items.Add(lvItem);
                        }));
                        _devSN++;
                        break;
                }
            }
        }
        #endregion

        #region 服务状态改变
        private void SIPServiceChangeHandle(string msg, ServiceStatus state)
        {
            if (lblStatus.InvokeRequired)
            {
                SetSIPServiceText sipService = new SetSIPServiceText(SetSIPService);
                this.Invoke(sipService, msg, state);
            }
            else
            {
                SetSIPService(msg, state);
            }
        }

        /// <summary>
        /// 设置sip服务状态
        /// </summary>
        /// <param name="state">sip状态</param>
        private void SetSIPService(string msg, ServiceStatus state)
        {
            if (state == ServiceStatus.Wait)
            {
                lblStatus.Text = msg + "-SIP服务未初始化完成";
                lblStatus.ForeColor = Color.YellowGreen;
            }
            else
            {
                lblStatus.Text = msg + "-SIP服务已初始化完成";
                lblStatus.ForeColor = Color.Green;
            }
        }
        #endregion

        #region 其他接口

        //系统设备配置查询
        private void btnConfigQuery_Click(object sender, EventArgs e)
        {
            string configType = cbxConfig.SelectedValue.ToString();
            _messageCore.NodeMonitorService[DevKey.ToString()].DeviceConfigQuery(configType);
        }

        //设备状态查询
        private void btnStateSearch_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].DeviceStateQuery();
        }

        //设备详细信息查询
        private void btnDevSearch_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].DeviceInfoQuery();
        }

        //设备信息查询回调函数
        private void DeviceInfoReceived(SIPEndPoint remoteEP, DeviceInfo device)
        {
            string msg = "DeviceID:" + device.DeviceID +
                "\r\nResult:" + device.Result +
                "\r\nModel:" + device.Model;
            Invoke(new Action(() =>
            {
                txtMsg.Text = msg;
            }));
        }

        //设备状态查询回调函数
        private void DeviceStatusReceived(SIPEndPoint remoteEP, DeviceStatus device)
        {
            string msg = "DeviceID:" + device.DeviceID +
                "\r\nResult:" + device.Result +
                "\r\nOnline:" + device.Online +
                "\r\nState:" + device.Status;
            this.Invoke(new Action(() =>
            {
                this.txtMsg.Text = msg;
            }));
        }

        //设备重启
        private void brnRenboot_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].DeviceReboot();
        }

        //设备配置
        private void btnDevConfig_Click(object sender, EventArgs e)
        {
            // string name = utf8_gb2312(txtDevName.Text);
            // name = txtDevName.Text;
            // name = "\u56fd\u6807\u6d4b\u8bd5\u8bbe\u5907";
            //string name=System.Text.Encoding.GetEncoding("GB2312").GetString()
            string name = txtDevName.Text;
            int expiration = (int)numIntervalTimeOut.Value;
            int heartBeatInterval = (int)numIntervalTime.Value;
            int heartBeatToal = (int)numIntervalTotal.Value;
            _messageCore.NodeMonitorService[DevKey.ToString()].DeviceConfig(name, expiration, heartBeatInterval, heartBeatToal);
        }
        public byte[] ConvertUnicodeToUTF8(string message)
        {
            // var utf8 = Encoding.GetEncoding("utf-8");
            byte[] array = Encoding.Default.GetBytes(message);
            byte[] s4 = Encoding.Convert(System.Text.Encoding.GetEncoding("gb2312"), Encoding.UTF8, array);
            return s4;

        }




        #endregion

        #region 报警事件

        //事件订阅
        private void btnEventSubscribe_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].DeviceEventSubscribe();
        }

        //布防
        private void btnSetGuard_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].DeviceControlSetGuard();
        }

        //撤防
        private void btResetGuard_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].DeviceControlResetGuard();
        }

        //报警复位
        private void btnAlarmReset_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].DeviceControlResetAlarm();
        }
        #endregion

        #region 目录操作
        //目录订阅
        private void btnCatalogSubscribe_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].DeviceCatalogSubscribe(true);
        }

        //目录查询
        private void btnCatalogSearch_Click(object sender, EventArgs e)
        {
            _devSN = 1;
            lvDev.Items.Clear();
            //lvCata.Items.Clear();
            _devList.Clear();
            //_bg.Nodes.Clear();
            _messageCore.DeviceCatalogQuery();
        }
        #endregion

        #region 实时视频
        //开始实时视频
        private void BtnReal_Click(object sender, EventArgs e)
        {
            SIPAccount account = SipStorage.Instance.Accounts.FirstOrDefault();

            int[] mediaPort =  { account.MediaPort };
            string ip = account.MediaIP.ToString();
            _analyzer = new StreamAnalyzer();
            _messageCore.NodeMonitorService[DevKey.ToString()].RealVideoReq(mediaPort, ip, true);
            //_messageCore.NodeMonitorService[DevKey.ToString()].RealVideoReq();
            _messageCore.NodeMonitorService[DevKey.ToString()].OnStreamReady += Form1_OnStreamReady;
        }

        //停止实时视频
        private void BtnBye_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].OnStreamReady -= Form1_OnStreamReady;
            _messageCore.NodeMonitorService[DevKey.ToString()].ByeVideoReq();
        }

        FileStream m_fs;
        void Form1_OnStreamReady(RTPFrame rtpFrame)
        {
            //byte[] buffer = rtpFrame.GetFramePayload();
            //if (this.m_fs == null)
            //{
            //    this.m_fs = new FileStream("D:\\" + "111111" + ".h264", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 8 * 1024);
            //}
            //m_fs.Write(buffer, 0, buffer.Length);
            //m_fs.Flush();


            PsToH264(rtpFrame.GetFramePayload());
        }
        #endregion

        #region 录像点播
        int _recordSN = 1;

        /// <summary>
        /// 设置录像文件
        /// </summary>
        /// <param name="record"></param>
        private void SetRecord(RecordInfo record)
        {
            foreach (var item in record.RecordItems.Items)
            {
                ListViewItem lvItem = new ListViewItem(new string[] { _recordSN.ToString(), item.StartTime, item.EndTime });
                lvRecord.Items.Add(lvItem);
                _recordSN++;
            }
        }

        /// <summary>
        /// 录像查询回调
        /// </summary>
        /// <param name="record"></param>
        private void MessageCore_OnRecordInfoReceived(RecordInfo record)
        {
            if (lvRecord.InvokeRequired)
            {
                SetRecordText recordText = new SetRecordText(SetRecord);
                this.Invoke(recordText, record);
            }
            else
            {
                SetRecord(record);
            }
        }

        //录像播放(点播)
        private void btnRecord_Click(object sender, EventArgs e)
        {
            _analyzer = new StreamAnalyzer();
            var key = DevKey;
            key.CmdType = CommandType.Playback;

            DateTime startTime = DateTime.Parse(txtStartTime.Text.Trim());
            DateTime stopTime = DateTime.Parse(txtStopTime.Text.Trim());
            _messageCore.NodeMonitorService[key.ToString()].BackVideoReq(startTime, stopTime);
            //      _messageCore.NodeMonitorService[key.ToString()].OnStreamReady += Form1_OnStreamReady;
        }

        //终止点播(停止)
        private void btnStopRecord_Click(object sender, EventArgs e)
        {
            var key = DevKey;
            key.CmdType = CommandType.Playback;
            _messageCore.NodeMonitorService[key.ToString()].ByeVideoReq();
            _messageCore.NodeMonitorService[key.ToString()].OnStreamReady -= Form1_OnStreamReady;
        }

        //暂停播放
        private void btnPause_Click(object sender, EventArgs e)
        {
            var key = DevKey;
            key.CmdType = CommandType.Playback;
            _messageCore.NodeMonitorService[key.ToString()].BackVideoPauseControlReq();
        }
        //继续播放
        private void btnPlay_Click(object sender, EventArgs e)
        {
            var key = DevKey;
            key.CmdType = CommandType.Playback;
            _messageCore.NodeMonitorService[key.ToString()].BackVideoContinuePlayingControlReq();
        }
        //播放倍数设置
        private void btnSdu_Click(object sender, EventArgs e)
        {
            var key = DevKey;
            key.CmdType = CommandType.Playback;
            string range = txtScale.Text.Trim();
            _messageCore.NodeMonitorService[key.ToString()].BackVideoPlaySpeedControlReq(range);
        }
        //拖动播放
        private void button17_Click(object sender, EventArgs e)
        {
            var key = DevKey;
            key.CmdType = CommandType.Playback;
            int time = int.Parse(txtDragDrop.Text.Trim());
            _messageCore.NodeMonitorService[key.ToString()].BackVideoPlayPositionControlReq(time);
        }



        //录像文件查询
        private void btnRecordGet_Click(object sender, EventArgs e)
        {
            _recordSN = 1;
            lvRecord.Items.Clear();
            DateTime startTime = DateTime.Parse(txtStartTime.Text.Trim());
            DateTime stopTime = DateTime.Parse(txtStopTime.Text.Trim());
            //key.CmdType = CommandType.Playback;
            string type = cbxRecordType.SelectedValue.ToString();
            _messageCore.NodeMonitorService[DevKey.ToString()].RecordFileQuery(startTime, stopTime, type);
        }

        //录像文件下载
        private void btnRecordDownload_Click(object sender, EventArgs e)
        {
            var key = DevKey;
            key.CmdType = CommandType.Playback;

            DateTime startTime = DateTime.Parse(txtStartTime.Text.Trim());
            DateTime stopTime = DateTime.Parse(txtStopTime.Text.Trim());

            _messageCore.NodeMonitorService[key.ToString()].VideoDownloadReq(startTime, stopTime);
            _messageCore.NodeMonitorService[key.ToString()].OnStreamReady += Form1_OnStreamReady;
        }

        //开始手动录像
        private void btnStartRecord_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].DeviceControlRecord(true);
        }

        //停止手动录像
        private void btnStopRd_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].DeviceControlRecord(false);
        }
        #endregion

        #region 云台控制
        #region 八方位控制
        //上
        private void btnUP_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.Up, int.Parse(numberSpeed.Value.ToString()));
        }

        //下
        private void btnDown_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.Down, int.Parse(numberSpeed.Value.ToString()));
        }

        //左
        private void btnLeft_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.Left, int.Parse(numberSpeed.Value.ToString()));
        }

        //右
        private void btnRight_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.Right, int.Parse(numberSpeed.Value.ToString()));
        }

        //左上
        private void btnLeftUP_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.UpLeft, int.Parse(numberSpeed.Value.ToString()));
        }

        //右上
        private void btnRightUP_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.UpRight, int.Parse(numberSpeed.Value.ToString()));
        }

        //左下
        private void btnLeftDown_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.DownLeft, int.Parse(numberSpeed.Value.ToString()));
        }

        //右下
        private void btnRightDown_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.DownRight, int.Parse(numberSpeed.Value.ToString()));
        }

        //停止
        private void btnPTZStop_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.Stop, int.Parse(numberSpeed.Value.ToString()));
        }
        #endregion

        #region 预置位控制
        //设置预置位
        private void btnSetPreset_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.SetPreset, int.Parse(numberSpeed.Value.ToString()));
        }

        //查询预置位
        private void btnSearchPreset_Click(object sender, EventArgs e)
        {
            cbxPreset.Items.Clear();
            _messageCore.NodeMonitorService[DevKey.ToString()].DevicePresetQuery();
        }

        //private Dictionary<string, string> presetList = new Dictionary<string, string>();

        void _messageCore_OnPresetQueryReceived(SIPEndPoint remoteEP, PresetInfo preset)
        {
            foreach (var item in preset.PresetList)
            {
                foreach (var items in item.Items)
                {
                    this.Invoke(new Action(() =>
                    {
                        cbxPreset.Items.Add(items.PresetID);

                    }));
                    //presetList.Add(items.PresetID, items.PresetName);
                }
            }
            //reloadPreset();
        }

        private void reloadPreset()
        {
            BindingSource bs = new BindingSource();
            //bs.DataSource = presetList;

            this.Invoke(new Action(() =>
            {
                cbxPreset.DataSource = bs;
                cbxPreset.DisplayMember = "Value";
                cbxPreset.ValueMember = "Key";
            }));
        }

        /// 调用预置位
        private void btnGetPreset_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.GetPreset, int.Parse(numberSpeed.Value.ToString()));
        }

        //删除预置位
        private void btnRemovePreset_Click(object sender, EventArgs e)
        {
            if (cbxPreset.SelectedItem != null)
            {
                _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.RemovePreset, int.Parse(cbxPreset.SelectedItem.ToString()));
                cbxPreset.Items.Remove(cbxPreset.SelectedItem);
                //presetList.Remove(cbxPreset.SelectedText.ToString());
                //reloadPreset();
            }
        }

        #endregion

        #region 变倍/聚焦/光圈
        //变倍+
        private void btnZoom1_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.Zoom1, int.Parse(numberSpeed.Value.ToString()));
        }

        //变倍-
        private void btnZoom2_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.Zoom2, int.Parse(numberSpeed.Value.ToString()));
        }

        //聚焦+
        private void btnFocus1_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.Focus1, int.Parse(numberSpeed.Value.ToString()));
        }

        //聚焦-
        private void btnFocus2_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.Focus2, int.Parse(numberSpeed.Value.ToString()));
        }

        //光圈开
        private void btnIris1_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.Iris1, int.Parse(numberSpeed.Value.ToString()));
        }

        //光圈关
        private void btnIris2_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].PtzContrl(PTZCommand.Iris2, int.Parse(numberSpeed.Value.ToString()));
        }
        #endregion

        #region 拉框放大/缩小
        private DragZoomSet DragZoomValue()
        {
            try
            {
                DragZoomSet zoom = new DragZoomSet();
                //zoom.Length = int.Parse(this.txtLength.Text.Trim());
                //zoom.Width = int.Parse(this.txtWidth.Text.Trim());
                //zoom.MidPointX = int.Parse(this.txtMidPointX.Text.Trim());
                //zoom.MidPointY = int.Parse(this.txtMidPointY.Text.Trim());
                //zoom.LengthX = int.Parse(this.txtLengthX.Text.Trim());
                //zoom.LengthY = int.Parse(this.txtLengthY.Text.Trim());
                return zoom;
            }
            catch
            {
                MessageBox.Show("输入参数错误");
                return null;
            }
        }

        //拉框放大
        private void btnZoomIn_Click(object sender, EventArgs e)
        {
            var zoom = DragZoomValue();
            _messageCore.NodeMonitorService[DevKey.ToString()].DragZoomContrl(zoom, true);
        }

        //拉框缩小
        private void btnZoomOut_Click(object sender, EventArgs e)
        {
            var zoom = DragZoomValue();
            _messageCore.NodeMonitorService[DevKey.ToString()].DragZoomContrl(zoom, false);
        }
        #endregion

        #region 看守位开/关
        //看守位开
        private void btnPositionOpen_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].HomePositionControl(true);
        }

        //看守位关
        private void btnPositionClose_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].HomePositionControl(false);
        }
        #endregion
        #endregion

        #region 处理PS数据

        private byte[] _publicByte = new byte[0];
        public void PsToH264(byte[] buffer)
        {
            _publicByte = copybyte(_publicByte, buffer);
            int i = 0;
            int BANum = 0;
            int startIndex = 0;
            if (buffer == null || buffer.Length < 5)
            {
                return;
            }
            int bytes = _publicByte.Length - 4;
            while (i < bytes)
            {
                if (_publicByte[i] == 0x00 && _publicByte[i + 1] == 0x00 && _publicByte[i + 2] == 0x01 && _publicByte[i + 3] == 0xBA)
                {
                    BANum++;
                    if (BANum == 1)
                    {
                        startIndex = i;
                    }
                    if (BANum == 2)
                    {
                        break;
                    }
                }
                i++;
            }

            if (BANum == 2)
            {
                int esNum = i - startIndex;
                byte[] psByte = new byte[esNum];
                Array.Copy(_publicByte, startIndex, psByte, 0, esNum);

                try
                {
                    //处理psByte
                    doPsByte(psByte);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("===============" + ex.Message + ex.StackTrace.ToString());
                }

                byte[] overByte = new byte[_publicByte.Length - i];
                Array.Copy(_publicByte, i, overByte, 0, overByte.Length);
                _publicByte = overByte;
            }
        }

        public byte[] copybyte(byte[] a, byte[] b)
        {
            byte[] c = new byte[a.Length + b.Length];
            a.CopyTo(c, 0);
            b.CopyTo(c, a.Length);
            return c;
        }

        private void doPsByte(byte[] psDate)
        {
            if (!(psDate[0] == 0 && psDate[1] == 0 && psDate[2] == 1 && psDate[3] == 0xBA))
            {
                Console.WriteLine("出错了！！！！！！！！");
            }
            long scr = 0;
            Stream msStream = new System.IO.MemoryStream(psDate);

            var ph = new PSPacketHeader(msStream);
            scr = ph.GetSCR();
            List<PESPacket> videoPESList = new List<PESPacket>();

            while (msStream.Length - msStream.Position > 4)
            {
                bool findStartCode = msStream.ReadByte() == 0x00 && msStream.ReadByte() == 0x00 && msStream.ReadByte() == 0x01 && msStream.ReadByte() == 0xE0;
                if (findStartCode)
                {
                    msStream.Seek(-4, SeekOrigin.Current);
                    var pesVideo = new PESPacket();
                    pesVideo.SetBytes(msStream);
                    var esdata = pesVideo.PES_Packet_Data;
                    videoPESList.Add(pesVideo);
                }
            }
            msStream.Close();
            HandlES(videoPESList);
        }

        private void HandlES(List<PESPacket> videoPESList)
        {
            try
            {
                var stream = new MemoryStream();
                foreach (var item in videoPESList)
                {
                    stream.Write(item.PES_Packet_Data, 0, item.PES_Packet_Data.Length);
                }
                if (videoPESList.Count == 0)
                {
                    stream.Close();
                    return;
                }
                long tick = videoPESList.FirstOrDefault().GetVideoTimetick();
                var buffer = stream.GetBuffer();
                stream.Close();
                videoPESList.Clear();

                _analyzer.InputData(1, buffer, (uint)buffer.Length, 0, 0, (int)0, 0);
                var packet = _analyzer.GetMediaFramePacket();
                var frame = new MediaFrame()
                {
                    IsAudio = 0,
                    IsKeyFrame = packet.KeyFrame,
                    Height = packet.Height,
                    Width = packet.Width,
                    Channel = 1
                };
                frame.SetData(packet.Buffer);
                playerWin.Play(frame);
            }
            catch (Exception ex)
            {
            }
        }

        private StreamAnalyzer _analyzer;


        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            //RTPHeader header = new RTPHeader();
            //header.Version = 2;
            //header.PaddingFlag = 0;
            //header.HeaderExtensionFlag = 0;
            //header.CSRCCount = 0;
            //header.MarkerBit = 1;
            //header.PayloadType = 8;
            //header.SequenceNumber = 1;
            //header.HeaderExtensionFlag = 0;
            //header.Timestamp = 320;
            //header.SyncSource = 0x0857;
            //header.GetBytes();
            //packet.Header = header;
            //packet.Payload = buffer;

            // string viaStr = via.ToString();

            Console.WriteLine("");

            //Register re = new Register()
            //{
            //    CmdType = CommandType.Catalog,
            //    SN = 1,
            //    SumNum = 3,
            //    DeviceID = "34020000002000000001"
            //};
            //re.DeviceList = new Register.DevList()
            //{
            //    Num = 3
            //};
            //re.DeviceList.Item.Add(new Register.Items()
            //{
            //    DeviceID = "34020000001310000001",
            //    Event = "OFF"
            //});

            //re.DeviceList.Item.Add(
            //new Register.Items()
            //{
            //    DeviceID = "34020000001340000001",
            //    Event = "OFF"
            //});
            //re.DeviceList.Item.Add(new Register.Items()
            //{
            //    DeviceID = "34020000001370000001",
            //    Event = "OFF"
            //});
            //string body = Register.Instance.Save<Register>(re);
            //DateTime begin = new DateTime(2018, 1, 11, 9, 55, 24);
            //DateTime end = new DateTime(2018, 1, 8, 14, 15, 28);
            //uint start = TimeConvert.DateToTimeStamp(begin);
            //uint stop = TimeConvert.DateToTimeStamp(end);
            //int ssrc=24567;
            //string ffffff = "y=0" + ssrc.ToString("D9");
            //int aa = 1474127777;
            //int bb = aa.ToString().Length;
            //string str = "0" + aa.ToString("D9");
            ////t=
            uint time = 1516084912;
            uint end = 1516085076;
            //DateTime start = TimeConvert.TimeStampToDate(time);
           // DateTime stop = TimeConvert.TimeStampToDate(end);
            //int a = 0xfc;
            //byte[] buffer = BitConverter.GetBytes(a);
            //buffer = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((short)a));
            /// test();
            //DeviceConfig config = new DeviceConfig()
            //{
            //    CommandType = CommandType.DeviceControl,
            //    DeviceID = "123",
            //    SN = new Random().Next(1, ushort.MaxValue),
            //    BasicParam = new DeviceParam()
            //    {
            //        Name = "test",
            //        Expiration = 1,
            //        HeartBeatInterval = 1,
            //        HeartBeatCount = 1
            //    }
            //};
            //string xmlBody = GB28181.Sys.XML.DeviceConfig.Instance.Save<DeviceConfig>(config);
        }

        private int test()
        {
            string sdpStr = @"v=0
o=34010000002000000002 0 0 IN IP4 192.168.95.213
s=Play
c=IN IP4 192.168.95.213
t=0 0
m=video 10000 RTP/AVP 96 98 97
a=sendonly
a=rtpmap:96 PS/90000
a=rtpmap:98 H264/90000
a=rtpmap:97 MPEG4/90000";

            string[] sdpLines = sdpStr.Split('\n');

            foreach (var line in sdpLines)
            {
                if (line.Trim().StartsWith("c="))
                {
                    SDPConnectionInformation conn = SDPConnectionInformation.ParseConnectionInformation(line);
                }
            }
            return 0;
        }

        public int f()
        {
            int i = 0;
            try
            {
                ++i;
            }
            finally
            {
                ++i;
            }
            return ++i;
        }


        #region zhangxb 新添加
        /// <summary>
        /// 语音广播通知
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAudioNotify_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].AudioPublishNotify();
        }


        private void btnSubPositionInfo_Click(object sender, EventArgs e)
        {
            int interval = (int)numInterval.Value;
            _messageCore.NodeMonitorService[DevKey.ToString()].MobilePositionQueryRequest(interval, true);
        }

        #endregion

        private void btnSubscribeCancel_Click(object sender, EventArgs e)
        {
            int interval = (int)numInterval.Value;
            _messageCore.NodeMonitorService[DevKey.ToString()].MobilePositionQueryRequest(interval, false);
        }
        #region 移动设备位置数据订阅/通知

        /// <summary>
        /// 移动设备位置数据订阅
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            ListViewItem devItem = new ListViewItem();
            foreach (var item in lvDev.SelectedItems.Cast<ListViewItem>())
            {
                devItem = item;
                break;
            }
            string devID = devItem.ImageKey;
            if (!string.IsNullOrEmpty(devID))
            {
                _messageCore.MobileDataSubscription(devID);
            }
        }

        #endregion

        /// <summary>
        /// 关键帧
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnKeyFrame_Click(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].MakeKeyFrameRequest();
        }

        private void tvCalatog_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag == null)
            {
                return;
            }
            lvDev.Items.Clear();
            if (e.Node.Tag.ToString() == DevCataType.CityCata.ToString())
            {
                foreach (var item in _devList)
                {
                    lvDev.Items.Add(item);
                }
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            _messageCore.NodeMonitorService[DevKey.ToString()].DeviceCatalogSubscribe(false);
        }
    }

    /// <summary>
    /// 心跳
    /// </summary>
    public class Keep
    {
        /// <summary>
        /// 远程终结点
        /// </summary>
        public SIPEndPoint RemoteEP { get; set; }

        /// <summary>
        /// 心跳周期
        /// </summary>
        public KeepAlive Heart { get; set; }
    }
}

using SIPSorcery.GB28181.SIP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SIPSorcery.GB28181.Servers
{
    /// <summary>
    /// 计时器任务
    /// </summary>
    public class TaskTiming
    {
        private SIPRequest _request;
        private SIPTransport _trans;
        private SIPResponse _response;
        private Timer _timeSend;
        //发送请求次数
        private int _sendReqSeq = 1;

        /// <summary>
        /// 超时关闭RTP通道
        /// </summary>
        public event Action OnCloseRTPChannel;

        /// <summary>
        /// 计时器初始化
        /// </summary>
        /// <param name="request">sip响应</param>
        /// <param name="trans">sip传输</param>
        public TaskTiming(SIPRequest request, SIPTransport trans)
        {
            _request = request;
            _trans = trans;
            _timeSend = new Timer();
        }

        /// <summary>
        /// 启动计时器
        /// </summary>
        public void Start()
        {
            _timeSend.Enabled = true;
            _timeSend.Elapsed += timeSend_Elapsed;
            _timeSend.Start();
        }

        /// <summary>
        /// 停止计时器
        /// </summary>
        public void Stop()
        {
            _timeSend.Elapsed -= timeSend_Elapsed;
            _timeSend.Enabled = false;
            _timeSend.Stop();
        }

        /// <summary>
        /// 间隔一定时间触发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timeSend_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_response == null || _request.Header.CallId != _response.Header.CallId)
            {
                _trans.SendRequest(_request);
            }
            else
            {
                this.Stop();
            }
            _sendReqSeq++;
            _timeSend.Interval = 2000;
            if (_sendReqSeq > 5)
            {
                this.Stop();
                if (OnCloseRTPChannel != null)
                {
                    OnCloseRTPChannel();
                }
            }
        }

        /// <summary>
        /// 消息发送请求超时回调
        /// </summary>
        /// <param name="response">sip响应</param>
        internal void MessageSendRequestTimeout(SIPResponse response)
        {
            _response = response;
        }
    }
}

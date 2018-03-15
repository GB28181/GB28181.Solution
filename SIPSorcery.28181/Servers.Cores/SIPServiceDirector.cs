using SIPSorcery.GB28181.Net;
using SIPSorcery.GB28181.Servers.SIPMessage;
using System;
using System.Net.Sockets;
namespace SIPSorcery.GB28181.Servers
{
    public class SIPServiceDirector : ISIPServiceDirector
    {

        private SIPCoreMessageService _sipCoreMessageService;
        public SIPServiceDirector(SIPCoreMessageService sipCoreMessageService)
        {
            _sipCoreMessageService = sipCoreMessageService;
        }
        public ISIPMonitorService GetTargetMonitorService(string gbid)
        {
            if (_sipCoreMessageService == null)
            {
                throw new NullReferenceException("instance not exist!");
            }

            if (_sipCoreMessageService.NodeMonitorService.ContainsKey(gbid))
            {
                return _sipCoreMessageService.NodeMonitorService[gbid];
            }

            return null;

        }

        //make real Request
        public Tuple<string, int, ProtocolType> MakeVideoRequest(string gbid, int[] mediaPort, string receiveIP)
        {
            var target = GetTargetMonitorService(gbid);

            var cSeq = target.RealVideoReq(mediaPort, receiveIP, true);

            var result = target.WaitRequestResult();

            var ipaddress = _sipCoreMessageService.GetReceiveIP(result.Item2.Body);

            var port = _sipCoreMessageService.GetReceivePort(result.Item2.Body, SDPMediaTypesEnum.video);

            return Tuple.Create(ipaddress, port, ProtocolType.Udp);
        }
    }
}

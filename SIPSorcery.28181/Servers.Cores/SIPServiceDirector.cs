using SIPSorcery.GB28181.Net;
using SIPSorcery.GB28181.Servers.SIPMessage;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

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
        async public Task<Tuple<string, int, ProtocolType>> MakeVideoRequest(string gbid, int[] mediaPort, string receiveIP)
        {
            var target = GetTargetMonitorService(gbid);


            var taskResult = await Task.Factory.StartNew(() =>
           {

               var cSeq = target.RealVideoReq(mediaPort, receiveIP, true);

               var result = target.WaitRequestResult();

               return result;
           });

            var ipaddress = _sipCoreMessageService.GetReceiveIP(taskResult.Item2.Body);

            var port = _sipCoreMessageService.GetReceivePort(taskResult.Item2.Body, SDPMediaTypesEnum.video);

            return Tuple.Create(ipaddress, port, ProtocolType.Udp);
        }
    }
}

using SIPSorcery.GB28181.Servers.SIPMessage;
using System;
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
        public Task<Tuple<string, int, int>> CreateVideoRequest(string gbid, int[] mediaPort, string receiveIP)
        {
            var target = GetTargetMonitorService(gbid);
            target.RealVideoReq(mediaPort, receiveIP);

            var result =    Task.Run(() => target.WaitRequestResult());
            // wait ack comeback
            // target.waitOne();




            throw new NotImplementedException();
        }
    }
}

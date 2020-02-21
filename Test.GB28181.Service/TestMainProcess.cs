using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using Xunit;
using GB28181.SIPSorcery.Servers.SIPMessage;
using GB28181.SIPSorcery.Servers;
using GB28181.SIPSorcery.SIP;
using GB28181.SIPSorcery.Sys.Config;
using GB28181.SIPSorcery.Sys.Cache;
using GB28181.SIPSorcery.Sys.Model;
using GB28181Service;

namespace Test.GB28181.Service
{
    public class TestMainProcess
    {
        [Fact]
        public void Run_Default_Return()
        {
            //ISIPRegistrarCore sipRegistrarCore,
            //ISIPTransport sipTransport,
            //ISipAccountStorage sipAccountStorage,
            //IMemoCache< Camera > cameraCache)

            //var ISIPRegistrarCoreMock = new Mock<ISIPRegistrarCore>();
            //var ISIPTransportMock = new Mock<ISIPTransport>();
            //var ISipAccountStorageMock = new Mock<ISipAccountStorage>();
            //SipAccountStorage sas = new SipAccountStorage();
            ////ISipAccountStorageMock.Setup(accouts=> accouts.Accounts.Add(new GB28181.SIPSorcery.SIP.App.SIPAccount { })) ;
            //var IMemoCacheMock = new Mock<IMemoCache<Camera>>();
            var obj = new MainProcess();
            obj.Run();

            Assert.NotNull(obj);
        }
    }
}

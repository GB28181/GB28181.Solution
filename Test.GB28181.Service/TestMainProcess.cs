using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using Xunit;
using SIPSorcery.GB28181.Servers.SIPMessage;
using SIPSorcery.GB28181.Servers;
using SIPSorcery.GB28181.SIP;
using SIPSorcery.GB28181.Sys.Config;
using SIPSorcery.GB28181.Sys.Cache;
using SIPSorcery.GB28181.Sys.Model;
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
            ////ISipAccountStorageMock.Setup(accouts=> accouts.Accounts.Add(new SIPSorcery.GB28181.SIP.App.SIPAccount { })) ;
            //var IMemoCacheMock = new Mock<IMemoCache<Camera>>();
            var obj = new MainProcess();
            obj.Run();

            Assert.NotNull(obj);
        }
    }
}

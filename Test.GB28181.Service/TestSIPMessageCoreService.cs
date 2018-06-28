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
using SIPSorcery.GB28181.Servers.SIPMonitor;

namespace Test.GB28181.Service
{
    public class TestSIPMessageCoreService
    {
        //[Fact]
        //public void Start_Default_Return()
        //{
        //    //ISIPRegistrarCore sipRegistrarCore,
        //    //ISIPTransport sipTransport,
        //    //ISipAccountStorage sipAccountStorage,
        //    //IMemoCache< Camera > cameraCache)

        //    var ISIPRegistrarCoreMock = new Mock<ISIPRegistrarCore>();
        //    var ISIPTransportMock = new Mock<ISIPTransport>();
        //    var ISipAccountStorageMock = new Mock<ISipAccountStorage>();
        //    SipAccountStorage sas = new SipAccountStorage();
        //    //ISipAccountStorageMock.Setup(accouts=> accouts.Accounts.Add(new SIPSorcery.GB28181.SIP.App.SIPAccount { })) ;
        //    var IMemoCacheMock = new Mock<IMemoCache<Camera>>();
        //    var sipmc = new SIPMessageCoreService(
        //        ISIPRegistrarCoreMock.Object,
        //        ISIPTransportMock.Object,
        //        sas,//ISipAccountStorageMock.Object, 
        //        IMemoCacheMock.Object);
        //    sipmc.Start();

        //    Assert.NotNull(sipmc);
        //}

        [Fact]
        public void PtzContrl_Default_Return()
        {
            //var ISIPRegistrarCoreMock = new Mock<ISIPRegistrarCore>();
            //var ISIPTransportMock = new Mock<ISIPTransport>();
            //var ISipAccountStorageMock = new Mock<ISipAccountStorage>();
            //ISipAccountStorageMock.Setup(accouts => accouts.Accounts.Add(new SIPSorcery.GB28181.SIP.App.SIPAccount { }));
            //var IMemoCacheMock = new Mock<IMemoCache<Camera>>();
            //var sipmc = new SIPMessageCoreService(
            //    ISIPRegistrarCoreMock.Object,
            //    ISIPTransportMock.Object,
            //    ISipAccountStorageMock.Object, 
            //    IMemoCacheMock.Object);
            ////sipmc.Start();

            //sipmc.PtzControl(PTZCommand.Right, 1);

            //Assert.NotNull(sipmc);
        }
    }
}

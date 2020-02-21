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
using GB28181.SIPSorcery.Servers.SIPMonitor;

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
        //    //ISipAccountStorageMock.Setup(accouts=> accouts.Accounts.Add(new GB28181.SIPSorcery.SIP.App.SIPAccount { })) ;
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
            //ISipAccountStorageMock.Setup(accouts => accouts.Accounts.Add(new GB28181.SIPSorcery.SIP.App.SIPAccount { }));
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

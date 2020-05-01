using System;
using System.Collections.Generic;
using System.Text;
using SIPSorcery.Sys;
using Xunit;

namespace Testing
{



    public class IPSocketUnitTest
    {

        [Fact]
        public void SampleTest()
        {
            Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
            Assert.True(true, "True was false.");
        }

        [Fact]
        public void ParsePortFromSocketTest()
        {
            Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            int port = IPSocket.ParsePortFromSocket("localhost:5060");
            Console.WriteLine("port=" + port);
            Assert.True(port == 5060, "The port was not parsed correctly.");
        }

        [Fact]
        public void ParseHostFromSocketTest()
        {
            Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            string host = IPSocket.ParseHostFromSocket("localhost:5060");
            Console.WriteLine("host=" + host);
            Assert.True(host == "localhost", "The host was not parsed correctly.");
        }

        [Fact]
        public void Test172IPRangeIsPrivate()
        {
            Console.WriteLine("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            Assert.False(IPSocket.IsPrivateAddress("172.15.1.1"), "Public IP address was mistakenly identified as private.");
            Assert.True(IPSocket.IsPrivateAddress("172.16.1.1"), "Private IP address was not correctly identified.");

            Console.WriteLine("-----------------------------------------");
        }
    }


}

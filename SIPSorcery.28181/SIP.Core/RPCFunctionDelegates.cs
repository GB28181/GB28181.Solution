using SIPSorcery.GB28181.SIP.App;
using System.Collections.Generic;
using System.Net;

namespace SIPSorcery.GB28181.SIP
{
    public delegate void RPCDmsRegisterDelegate(SIPTransaction sipTransaction, SIPAccount sIPAccount);
    public delegate List<SIPAccount> RPCGBServerConfigDelegate();
    public delegate void DeviceAlarmSubscribeDelegate(SIPTransaction sipTransaction);
}

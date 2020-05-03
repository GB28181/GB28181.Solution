using GB28181.SIP.App;
using System.Collections.Generic;
using System.Net;

namespace GB28181.SIP
{
    public delegate void RPCDmsRegisterDelegate(SIPTransaction sipTransaction, SIPAccount sIPAccount);
    public delegate List<SIPAccount> RPCGBServerConfigDelegate();
    public delegate void DeviceAlarmSubscribeDelegate(SIPTransaction sipTransaction);
}

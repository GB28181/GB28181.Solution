using System.Collections.Generic;
using GB28181.App;

namespace GB28181
{
    public delegate void RPCDmsRegisterDelegate(SIPTransaction sipTransaction, SIPAccount sIPAccount);
    public delegate List<SIPAccount> RPCGBServerConfigDelegate();
    public delegate void DeviceAlarmSubscribeDelegate(SIPTransaction sipTransaction);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GB28181.App
{
    public interface ISIPDialogueManager
    {
        void DualTransfer(string username, string callID1, string callID2);
    }
}

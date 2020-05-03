using GB28181.Servers;
using GB28181.Sys;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GB28181.Server.Main
{
    public partial class MainProcess
    {
        private ServiceProvider _serviceProvider = null;

        //for test
        private async Task VideoSessionKeepAlive()
        {
            await Task.Run(() =>
            {
                var mockCaller = _serviceProvider.GetService<ISIPServiceDirector>();
                while (true)
                {
                    for (int i = 0; i < mockCaller.VideoSessionAlive.ToArray().Length; i++)
                    {
                        Dictionary<string, DateTime> dict = mockCaller.VideoSessionAlive[i];
                        foreach (string key in dict.Keys)
                        {
                            TimeSpan ts1 = new TimeSpan(DateTime.Now.Ticks);
                            TimeSpan ts2 = new TimeSpan(Convert.ToDateTime(dict[key]).Ticks);
                            TimeSpan ts = ts1.Subtract(ts2).Duration();
                            if (ts.Seconds > 30)
                            {
                                mockCaller.Stop(key.ToString().Split(',')[0], key.ToString().Split(',')[1]);
                                mockCaller.VideoSessionAlive.RemoveAt(i);
                            }
                        }
                    }
                }
            });
        }

        //for test
        private async Task WaitUserCmd()
        {
            await Task.Run(() =>
             {
                 while (true)
                 {
                     Console.WriteLine("\ninput command : I -Invite, E -Exit");
                     var inputkey = Console.ReadKey();
                     switch (inputkey.Key)
                     {
                         case ConsoleKey.I:
                             {
                                 var mockCaller = _serviceProvider.GetService<ISIPServiceDirector>();
                                 //mockCaller.MakeVideoRequest("42010000001180000184", new int[] { 5060 }, EnvironmentVariables.LocalIp);
                             }
                             break;
                         case ConsoleKey.E:
                             Console.WriteLine("\nexit Process!");
                             break;
                         default:
                             break;
                     }
                     if (inputkey.Key == ConsoleKey.E)
                     {
                         return 0;
                     }
                     else
                     {
                         continue;
                     }
                 }
             });
        }

    }
}

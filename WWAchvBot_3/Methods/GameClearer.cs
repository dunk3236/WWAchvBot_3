using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static WWAchvBot_3.Program;

namespace WWAchvBot_3
{
    public static class GameClearer
    {
        public static void Run(object obj)
        {
            while (true)
            {
                Thread.Sleep(600000);

                var now = DateTime.UtcNow;
                var groupids = Games.Select(x => x.GroupId);

                foreach (var id in groupids)
                {
                    var g = Games.FirstOrDefault(x => x.GroupId == id);
                    if (g == null) return;

                    if (now - g.LastUpdate > TimeSpan.FromMinutes(15))
                    {
                        Bot.Send(Methods.GetString(g.Pinmessage, "CancelledOfInactivity"), g.GroupId);
                        g.Stop();
                    }
                    else if (now - g.LastUpdate > TimeSpan.FromMinutes(10))
                    {
                        if (g.Started)
                        {
                            Bot.Send(Methods.GetString(g.Pinmessage, "InactivityWarning"), g.GroupId);
                        }
                        else
                        {
                            Bot.Send(Methods.GetString(g.Pinmessage, "CancelledOfInactivity"), g.GroupId);
                            g.Stop();
                        }
                    }
                }
            }
        }
    }
}

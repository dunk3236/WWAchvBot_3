using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WWAchvBot_3
{
    public class BotUser
    {
        public long Telegramid;
        public string Name;
        public string Username;
        public long Gamecount;
        public string Subscriptions;
        public string Achievements;
        public string CallbackChoice = "";
        public Language Language;

        public string LinkedName
        {
            get
            {
                return string.IsNullOrEmpty(Username)
                    ? $"<a href=\"tg://user?id={Telegramid}\">{Name}</a>"
                    : $"<a href=\"https://t.me/{Username}\">{Name}</a>";
            }
        }

        public BotUser(long Telegramid, string Name, string Username, long Gamecount = 0, string Subscriptions = "", Language Language = null)
        {
            this.Telegramid = Telegramid;
            this.Name = Name;
            this.Username = Username ?? "";
            this.Gamecount = Gamecount;
            this.Subscriptions = Subscriptions;
            this.Language = Language ?? Language.English;

            UpdateAchv();
        }


        public void UpdateAchv()
        {
            Achievements = string.Join(
                "|",
                JsonConvert.DeserializeObject<List<UserAchv>>(
                    Encoding.UTF8.GetString(
                        new WebClient().DownloadData(
                            $"http://tgwerewolf.com/stats/playerachievements/?pid={Telegramid}&json=true"
                        )
                    )
                ).Select(x => x.Name)
            );
        }

        class UserAchv
        {
            public string Name { get; set; }
            public string Desc { get; set; }
        }
    }
}

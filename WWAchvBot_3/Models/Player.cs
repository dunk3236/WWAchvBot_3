using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWAchvBot_3
{
    class Player
    {
        public long Id;
        public BotUser User;
        public string Role = "Unknown";
        public Player RoleModel = null;
        public bool Love = false;
        public bool Alive = true;

        public Player(BotUser user)
        {
            Id = user.Telegramid;
            User = user;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static WWAchvBot_3.Program;

namespace WWAchvBot_3
{
    public static class Methods
    {
        public static Language GetLanguage(Message msg)
        {
            switch (msg.Chat.Type)
            {
                case ChatType.Supergroup:
                    return Groups.FirstOrDefault(x => x.Id == msg.Chat.Id).Language ?? Language.English;

                case ChatType.Private:
                    return Users.FirstOrDefault(x => x.Telegramid == msg.From.Id).Language ?? Language.English;
            }
            return Language.English;
        }

        public static string GetString(Message msg, string str)
        {
            return GetString(GetLanguage(msg), str);
        }

        public static string GetString(Language lang, string str)
        {
            var res = lang.Strings.FirstOrDefault(x => x.Key == str).Value;
            if (res == null && lang.Name != "english") res = Language.English.Strings.FirstOrDefault(x => x.Key == str).Value;
            return res ?? $"String \"<code>{str}</code>\" missing! Inform @Olgabrezel!";
        }

        public static BotUser GetUser(Message msg, string[] args)
        {
            var c = string.IsNullOrEmpty(args[1]) ? "" : args[1].Split(' ')[0];

            if (msg.ReplyToMessage != null)
            {
                return msg.ReplyToMessage.From.GetOrMakeBotUser();
            }

            if (int.TryParse(c, out int id))
            {
                User u;
                try
                {
                    u = Bot.Api.GetChatMemberAsync(msg.Chat.Id, id).Result.User;
                    if (u != null) return u.GetOrMakeBotUser();

                    var bu = Users.FirstOrDefault(x => x.Telegramid == id);
                    if (bu != null) return bu;
                }
                catch { }
            }

            if (c.StartsWith("@"))
            {
                var bu = Users.FirstOrDefault(x => x.Username.ToLower() == c.Remove(0, 1).ToLower());
                if (bu != null)
                {
                    try
                    {
                        id = (int)bu.Telegramid;
                        var u = Bot.Api.GetChatMemberAsync(msg.Chat.Id, id).Result.User;
                        if (u != null) return u.GetOrMakeBotUser();

                        return bu;
                    }
                    catch { }
                }
            }

            return msg.From.GetOrMakeBotUser();
        }
    }
}

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
    static class Extensions
    {
        public static void Log(this Exception exc, bool InformDev = false)
        {
            var trace = exc.StackTrace;
            var msg = Environment.NewLine + Environment.NewLine;

            do
            {
                msg += exc.Message + Environment.NewLine + Environment.NewLine;
                exc = exc.InnerException;
            }
            while (exc != null);

            msg += trace;

            System.IO.File.AppendAllText(BasePath + "Errors.txt", msg);
            if (InformDev) Bot.Send("<b>An exception was thrown!</b>" + msg, testgroup.Id);
        }

        public static string ToBold(this string str)
        {
            return $"<b>{str.FormatHTML()}</b>";
        }

        public static string FormatHTML(this string str)
        {
            return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        public static BotUser CreateBotUser(this User u)
        {
            string name = string.IsNullOrEmpty(u.LastName) ? u.FirstName : $"{u.FirstName} {u.LastName}";
            var bu = new BotUser(u.Id, name, u.Username, 0, "", Language.English);
            SQL.CreateBotUser(bu);
            Users.Add(bu);
            return bu;
        }

        public static BotUser GetOrMakeBotUser(this User u)
        {
            var bu = Users.FirstOrDefault(x => x.Telegramid == u.Id);
            if (bu == null) return u.CreateBotUser();

            string name = string.IsNullOrEmpty(u.LastName) ? u.FirstName : $"{u.FirstName} {u.LastName}";
            bu.Name = name;
            bu.Username = u.Username;
            bu.UpdateAchv();
            SQL.ChangeBotUser(bu);
            return bu;
        }

        public static bool IsFromAdmin(this Message msg)
        {
            var status = Bot.Api.GetChatMemberAsync(msg.Chat.Id, msg.From.Id).Result.Status;
            return new[] { ChatMemberStatus.Creator, ChatMemberStatus.Administrator }.Contains(status);
        }

        public static string GetLinkedName(this User u)
        {
            string name = string.IsNullOrEmpty(u.LastName) ? u.FirstName.FormatHTML() : $"{u.FirstName} {u.LastName}".FormatHTML();
            if (string.IsNullOrEmpty(u.Username)) return $"<a href=\"tg://user?id={u.Id}\">{name}</a>";
            return $"<a href=\"https://t.me/{u.Username}\">{name}</a>";
        }
    }
}

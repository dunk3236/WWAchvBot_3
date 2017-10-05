using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;
using WWAchvBot_3.Attributes;
using static WWAchvBot_3.Program;

namespace WWAchvBot_3
{
    public partial class Commands
    {
        [Command(Trigger = "startchaos", InGroupOnly = true)]
        public static void Startchaos(Message msg, string[] args)
        {
            Startgame(msg, args);
        }

        [Command(Trigger = "startgame", InGroupOnly = true)]
        public static void Startgame(Message msg, string[] args)
        {
            if (Games.Any(x => x.GroupId == msg.Chat.Id))
            {
                Bot.Reply(Methods.GetString(msg, "AlreadyPlaying"), msg);
                return;
            }

            var pin = Bot.Reply(Methods.GetString(msg, "InitializingGame"), msg);
            Game g = new Game(pin);
            Games.Add(g);
        }


        [Command(Trigger = "ap", InGameOnly = true)]
        public static void Ap(Message msg, string[] args)
        {
            Addplayer(msg, args);
        }

        [Command(Trigger = "addplayer", InGameOnly = true)]
        public static void Addplayer(Message msg, string[] args)
        {
            var g = Games.FirstOrDefault(x => x.GroupId == msg.Chat.Id);

            if (g == null)
            {
                Bot.Reply(Methods.GetString(msg, "MustUseInGame"), msg);
                return;
            }

            if (g.Started)
            {
                Bot.Reply(Methods.GetString(msg, "CantJoinRunning"), msg);
                return;
            }

            var bu = Methods.GetUser(msg, args);
            if (g.Players.Any(x => x.Id == bu.Telegramid))
            {
                Bot.Reply(Methods.GetString(msg, "AlreadyJoined"), msg);
                return;
            }

            g.Players.Add(new Player(bu));
            g.UpdatePin();
            Bot.Reply($"{bu.LinkedName} {Methods.GetString(msg, "AddedGame")}", msg);
        }

        [Command(Trigger = "role", InGameOnly = true)]
        public static void Role(Message msg, string[] args)
        {
            var g = Games.FirstOrDefault(x => x.GroupId == msg.Chat.Id);
            if (g == null || g.Stopped) return;
            if (!g.Started)
            {
                Bot.Reply(Methods.GetString(msg, "MustUseRunningGame"), msg);
                return;
            }

            if (string.IsNullOrEmpty(args[1]))
            {
                Bot.Reply(Methods.GetString(msg, "MustSpecifyRole"), msg);
                return;
            }

            var bu = Methods.GetUser(msg, args);

            if (g == null | bu == null) return;

            var p = g.Players.FirstOrDefault(x => x.Id == bu.Telegramid);

            if (p == null)
            {
                Bot.Reply(Methods.GetString(msg, "NotInGame"), msg);
                return;
            }

            string alias;
            var split = args[1].Split(' ');
            if (split.Count() >= 1 && (split[0].StartsWith("@") | int.TryParse(split[0], out int dummy)))
            {
                alias = split[1].ToLower();
            }
            else
            {
                alias = split[0].ToLower();
            }

            var role = (Methods.GetLanguage(msg).Strings.FirstOrDefault(x => x.Key.StartsWith("Alias") & x.Value.ToLower().Split(',').Select(y => y.Trim()).Contains(alias)).Key ?? "dummy").Remove(0, 5);
            if (string.IsNullOrEmpty(role))
            {
                Bot.Reply(Methods.GetString(msg, "MustSpecifyRole"), msg);
                return;
            }

            p.Role = role;
            g.UpdatePin();
            Bot.Reply($"{p.User.LinkedName}{Methods.GetString(msg, "RoleSetTo")} <i>{Methods.GetString(msg, $"Role{role}")}</i>", msg);
        }

        [Command(Trigger = "lo", InGameOnly = true)]
        public static void Lo(Message msg, string[] args)
        {
            LynchOrder(msg, args);
        }

        [Command(Trigger = "lynchorder", InGameOnly = true)]
        public static void LynchOrder(Message msg, string[] args)
        {
            var g = Games.FirstOrDefault(x => x.GroupId == msg.Chat.Id);
            if (g == null || g.Stopped) return;

            if (!g.Started)
            {
                Bot.Reply(Methods.GetString(msg, "MustUseRunningGame"), msg);
                return;
            }

            string defaultorder = string.Join(Environment.NewLine, g.Players.Where(x => x.Alive).Select(x => x.User.LinkedName)) +
                Environment.NewLine + g.Players.First(x => x.Alive).User.LinkedName;
            string order = (Methods.GetString(msg, "Lynchorder") + ":").ToBold() + Environment.NewLine + g.Lynchorder.Replace("$lynchorder", defaultorder);

            Bot.Reply(order, msg);
        }

        [Command(Trigger = "slo", InGameOnly = true)]
        public static void Slo(Message msg, string[] args)
        {
            SetLynchOrder(msg, args);
        }

        [Command(Trigger = "setlynchorder", InGameOnly = true)]
        public static void SetLynchOrder(Message msg, string[] args)
        {
            if (string.IsNullOrEmpty(args[1]) && (msg.ReplyToMessage == null || msg.ReplyToMessage.From.Id == Bot.Me.Id))
            {
                ResetLynchOrder(msg, args);
                return;
            }

            var g = Games.FirstOrDefault(x => x.GroupId == msg.Chat.Id);
            if (g == null || g.Stopped) return;
            if (!g.Started)
            {
                Bot.Reply(Methods.GetString(msg, "MustUseRunningGame"), msg);
                return;
            }


            var lo = msg.ReplyToMessage == null || msg.ReplyToMessage.From.Id == Bot.Me.Id
                ? args[1]
                : msg.ReplyToMessage.Text;

            g.Lynchorder = lo.Replace("<-->", "↔️").Replace("<->", "↔️").Replace("<>", "↔️").Replace("-->", "➡️").Replace("->", "➡️").Replace(">", "➡️");
            Bot.Reply($"{Methods.GetString(msg, "LynchorderSet")} {msg.From.GetLinkedName()}", msg);
        }


        [Command(Trigger = "rslo", InGameOnly = true)]
        public static void Rslo(Message msg, string[] args)
        {
            ResetLynchOrder(msg, args);
        }

        [Command(Trigger = "resetlynchorder", InGameOnly = true)]
        public static void ResetLynchOrder(Message msg, string[] args)
        {
            var g = Games.FirstOrDefault(x => x.GroupId == msg.Chat.Id);
            if (g == null || g.Stopped) return;

            if (!g.Started)
            {
                Bot.Reply(Methods.GetString(msg, "MustUseRunningGame"), msg);
                return;
            }

            g.Lynchorder = "$lynchorder";
            Bot.Reply($"{Methods.GetString(msg, "LynchorderReset")} {msg.From.GetLinkedName()}", msg);
        }

        [Command(Trigger = "pinglist", InGameOnly = true)]
        public static void Pinglist(Message msg, string[] args)
        {
            var g = Games.FirstOrDefault(x => x.GroupId == msg.Chat.Id);
            if (g == null || g.Started)
            {
                Bot.Reply(Methods.GetString(msg, "MustPingJoinPhase"), msg);
                return;
            }
            var grp = Groups.First(x => x.Id == msg.Chat.Id);
            var diff = DateTime.UtcNow - grp.LastPing;

            if (diff < TimeSpan.FromMinutes(10))
            {
                var wait = TimeSpan.FromMinutes(10) - diff;
                Bot.Reply($"{Methods.GetString(msg, "MustWaitPing")} {wait.ToString("mm\\:ss")}", msg);
                return;
            }

            var groupMarkup = new InlineKeyboardMarkup(
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardUrlButton(Methods.GetString(msg, "SubscribeButton"), $"https://t.me/{Bot.Me.Username}?start=subscribe_{msg.Chat.Id}"),
                    new InlineKeyboardUrlButton(Methods.GetString(msg, "UnsubscribeButton"), $"https://t.me/{Bot.Me.Username}?start=unsubscribe_{msg.Chat.Id}"),
                }
            );

            foreach (var u in Users.Where(x => x.Subscriptions.Contains(msg.Chat.Id.ToString()) & !g.Players.Any(y => y.Id == x.Telegramid)))
            {
                var userMarkup = new InlineKeyboardMarkup(
                    new InlineKeyboardButton[]
                    {
                        new InlineKeyboardCallbackButton(Methods.GetString(u.Language, "UnsubscribeThisButton"), $"unsubscribe|{msg.Chat.Id}"),
                    }
                );

                Bot.Send($"<b>🔔 Ping! 🔔</b>{Environment.NewLine}{Environment.NewLine}{Methods.GetString(u.Language, "PinglistPM")} <a href=\"{grp.Link}\">{grp.Name}</a>", u.Telegramid, replyMarkup: userMarkup);
            }

            grp.LastPing = DateTime.UtcNow;
            Bot.Reply($"<b>🔔 Ping! 🔔</b>{Environment.NewLine}{Environment.NewLine}{Methods.GetString(msg, "PinglistGroup")}", msg, replyMarkup: groupMarkup);
        }

        [Command(Trigger = "dead", InGameOnly = true)]
        public static void Dead(Message msg, string[] args)
        {
            var g = Games.FirstOrDefault(x => x.GroupId == msg.Chat.Id);
            if (g == null || g.Stopped) return;

            var bu = Methods.GetUser(msg, args);
            var p = g.Players.FirstOrDefault(x => x.Id == bu.Telegramid);
            if (p == null)
            {
                Bot.Reply(Methods.GetString(msg, "NotInGame"), msg);
                return;
            }

            if (!g.Started)
            {
                g.Players.Remove(p);
                g.UpdatePin();
                Bot.Reply($"{p.User.LinkedName} {Methods.GetString(msg, "RemovedFromGame")}", msg);
            }
            else
            {

                if (!p.Alive)
                {
                    Bot.Reply(Methods.GetString(msg, "AlreadyDead"), msg);
                    return;
                }

                p.Alive = false;
                g.UpdatePin();
                Bot.Reply($"{p.User.LinkedName} {Methods.GetString(msg, "MarkedAsDead")}", msg);
            }
        }

        [Command(Trigger = "flee", InGameOnly = true)]
        public static void Flee(Message msg, string[] args)
        {
            var g = Games.FirstOrDefault(x => x.GroupId == msg.Chat.Id);
            if (g == null || g.Stopped) return;
            var p = g.Players.FirstOrDefault(x => x.Id == msg.From.Id);
            if (p == null)
            {
                Bot.Reply(Methods.GetString(msg, "NotInGame"), msg);
                return;
            }

            if (!g.Started)
            {
                g.Players.Remove(p);
                g.UpdatePin();
                Bot.Reply($"{p.User.LinkedName} {Methods.GetString(msg, "RemovedFromGame")}", msg);
            }
            else
            {

                if (!p.Alive)
                {
                    Bot.Reply(Methods.GetString(msg, "AlreadyDead"), msg);
                    return;
                }

                p.Alive = false;
                g.UpdatePin();
                Bot.Reply($"{p.User.LinkedName} {Methods.GetString(msg, "MarkedAsDead")}", msg);
            }

        }

        [Command(Trigger = "love", InGameOnly = true)]
        public static void Love(Message msg, string[] args)
        {
            var g = Games.FirstOrDefault(x => x.GroupId == msg.Chat.Id);
            if (g == null || g.Stopped) return;
            if (!g.Started)
            {
                Bot.Reply(Methods.GetString(msg, "MustUseRunningGame"), msg);
                return;
            }

            var bu = Methods.GetUser(msg, args);
            var p = g.Players.FirstOrDefault(x => x.Id == bu.Telegramid);

            var str = "PlayerLoved";
            p.Love = !p.Love;
            if (!p.Love) str = "PlayerUnloved";
            g.UpdatePin();
            Bot.Reply($"{p.User.LinkedName} {Methods.GetString(msg, str)}", msg);
            
        }

        [Command(Trigger = "getpin", InGameOnly = true)]
        public static void GetPin(Message msg, string[] args)
        {
            var g = Games.FirstOrDefault(x => x.GroupId == msg.Chat.Id);
            if (g == null || g.Stopped) return;

            Bot.Reply(Methods.GetString(msg, "PinmessageHere"), g.Pinmessage);
        }

        [Command(Trigger = "la", InGameOnly = true)]
        public static void La(Message msg, string[] args)
        {
            ListAchv(msg, args);
        }

        [Command(Trigger = "listachv", InGameOnly = true)]
        public static void ListAchv(Message msg, string[] args)
        {
            var g = Games.FirstOrDefault(x => x.GroupId == msg.Chat.Id);
            if (g == null || g.Stopped) return;
            if (!g.Started)
            {
                Bot.Reply(Methods.GetString(msg, "MustUseRunningGame"), msg);
                return;
            }

            Bot.Reply(g.GetAchievements(), msg);
        }
    }


    public partial class Callbacks
    {
        [Callback(Trigger = "startgame", RequiresConfirm = true)]
        public static void StartGame(CallbackQuery call, string[] args)
        {
            long chatid = long.Parse(args[1]);
            Game g = Games.FirstOrDefault(x => x.GroupId == chatid);

            if (g == null)
            {
                Bot.Edit(call.Message, Methods.GetString(call.Message, "GameEnded").ToBold());
                Bot.AnswerCallback(call, Methods.GetString(call.Message, "GameEnded"));
                return;
            }

            if (chatid != testgroup.Id && g.Players.Count < 5)
            {
                Bot.AnswerCallback(call, Methods.GetString(call.Message, "NeedFivePlayers"), true);
                return;
            }

            g.Started = true;
            g.UpdatePin();
            Bot.Send($"{call.From.FirstName} {Methods.GetString(call.Message, "StartedGame")}", chatid);
        }

        [Callback(Trigger = "stopgame", RequiresConfirm = true)]
        public static void StopGame(CallbackQuery call, string[] args)
        {
            long chatid = long.Parse(args[1]);
            Game g = Games.FirstOrDefault(x => x.GroupId == chatid);

            if (g == null)
            {
                Bot.Edit(call.Message, Methods.GetString(call.Message, "GameEnded").ToBold());
                Bot.AnswerCallback(call, Methods.GetString(call.Message, "GameEnded"));
                return;
            }

            g.Stopped = true;
            g.UpdatePin();
            if (g.DefaultPin != null) Bot.Pin(g.DefaultPin);
            else Bot.RemovePin(chatid);
            Games.Remove(g);
            Bot.Send($"{call.From.FirstName} {Methods.GetString(call.Message, "StoppedGame")}", chatid);
        }
    }
}

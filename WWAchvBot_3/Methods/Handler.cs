using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static WWAchvBot_3.Program;

namespace WWAchvBot_3
{
    class Handler
    {
        public static void OnMessage(object sender, MessageEventArgs e)
        {
            new Task(() => { MessageHandler(e.Message); }).Start();
        }

        public static void OnCallback(object sender, CallbackQueryEventArgs e)
        {
            new Task(() => { CallbackHandler(e.CallbackQuery); }).Start();
        }

        public static void MessageHandler(Message msg)
        {
            if (!Groups.Any(x => x.Id == msg.Chat.Id) && msg.Chat.Type != ChatType.Private)
            {
                Bot.Reply("Hey there! This bot isn't for every group! If you want to farm achievements with me, join a group in @WWAchievement! <b>Bye.</b>", msg);
                Bot.Api.LeaveChatAsync(msg.Chat.Id);
                Bot.Send($"<b>{msg.From.FirstName} {msg.From.LastName}</b> (@{msg.From.Username}) (<code>{msg.From.Id}</code>) just interacted me with me in <b>{msg.Chat.Title}</b> (<code>{msg.Chat.Id}</code>), which I left because it isn't an allowed group.", testgroup.Id);
                return;
            }
            if (DateTime.UtcNow.AddSeconds(-5) > msg.Date.ToUniversalTime()) return;
            if (Maintenance && msg.Chat.Id != testgroup.Id && !Games.Any(x => x.GroupId == msg.Chat.Id) && !Admins.Contains(msg.From.Id)) return;

            if (msg.Chat.Type == ChatType.Supergroup)
            {
                var grp = Groups.First(x => x.Id == msg.Chat.Id);
                if (msg.Chat.Title.FormatHTML() != grp.Name)
                {
                    grp.Name = msg.Chat.Title.FormatHTML();
                    SQL.ChangeGroup(grp);
                }
            }

            var text = msg.Text;
            if (string.IsNullOrEmpty(text)) return;

            var args = text.Contains(' ')
                ? new[] { text.Split(' ')[0], text.Remove(0, text.IndexOf(' ') + 1) }
                : new[] { text, null };

            if (!args[0].StartsWith("!") & !args[0].StartsWith("/") & args[0].ToLower() != "#ping")
            {
                return;
            }

            var cmd = args[0].StartsWith("/") | args[0].StartsWith("!")
                ? args[0].ToLower().Remove(0, 1).Replace('@' + Bot.Me.Username.ToLower(), "").Replace("@werewolfbot", "")
                : args[0].ToLower().Replace('@' + Bot.Me.Username.ToLower(), "").Replace("@werewolfbot", "");

            var command = Bot.Commands.FirstOrDefault(x => String.Equals(x.Trigger, cmd, StringComparison.CurrentCultureIgnoreCase));
            if (command == null) return;

            if (msg.Chat.Id == TranslationGroup && !Admins.Contains(msg.From.Id))
            {
                Bot.Reply("You may not use commands in here!", msg);
                return;
            }

            if (command.DevOnly & !Devs.Contains(msg.From.Id))
            {
                Bot.Reply(Methods.GetString(msg, "NotDev"), msg);
                return;
            }

            if (command.AdminOnly & !Admins.Contains(msg.From.Id))
            {
                Bot.Reply(Methods.GetString(msg, "NotAdmin"), msg);
                return;
            }

            if (command.InGroupOnly & msg.Chat.Type != ChatType.Supergroup)
            {
                Bot.Reply(Methods.GetString(msg, "MustUseInGroup"), msg);
                return;
            }

            if (command.InGameOnly)
            {
                if (msg.Chat.Type != ChatType.Supergroup)
                {
                    Bot.Reply(Methods.GetString(msg, "MustUseInGroup"), msg);
                    return;
                }

                var g = Games.FirstOrDefault(x => x.GroupId == msg.Chat.Id);

                if (g == null)
                {
                    Bot.Reply(Methods.GetString(msg, "MustUseInGame"), msg);
                    return;
                }
            }

            command.Method.Invoke(msg, args);

        }

        public static void CallbackHandler(CallbackQuery call)
        {
            if (call.Message.Date.ToUniversalTime() < StartTime)
            {
                Bot.AnswerCallback(call, Methods.GetString(call.Message, "OutdatedQuery"));
                Bot.Edit(call.Message, call.Message.Text);
                return;
            }

            var text = call.Data;
            if (string.IsNullOrEmpty(text)) return;

            var args = text.Contains('|')
                ? new[] { text.Split('|')[0], text.Remove(0, text.IndexOf('|') + 1) }
                : new[] { text, null };

            var callback = Bot.Callbacks.FirstOrDefault(x => String.Equals(x.Trigger, args[0], StringComparison.CurrentCultureIgnoreCase));
            if (callback == null) return;

            if (callback.DevOnly & !Devs.Contains(call.From.Id))
            {
                Bot.AnswerCallback(call, "You aren't a bot dev!");
                return;
            }

            if (callback.AdminOnly & !Admins.Contains(call.From.Id))
            {
                Bot.AnswerCallback(call, "You aren't a bot admin!");
                return;
            }

            if (callback.RequiresConfirm)
            {
                var bu = call.From.GetOrMakeBotUser();
                if (bu.CallbackChoice == call.Data)
                {
                    bu.CallbackChoice = "";
                }
                else
                {
                    bu.CallbackChoice = call.Data;
                    Timer t = new Timer(new TimerCallback(delegate { if (bu.CallbackChoice == call.Data) bu.CallbackChoice = ""; }), null, 10000, Timeout.Infinite);
                    Bot.AnswerCallback(call, Methods.GetString(call.Message, "ClickAgainConfirm"), true);
                    return;
                }
            }

            callback.Method.Invoke(call, args);
        }
    }
}

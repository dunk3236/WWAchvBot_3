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
        [Command(Trigger = "start")]
        public static void Start(Message msg, string[] args)
        {
            if (msg.Chat.Type != ChatType.Private) return;
            var bu = msg.From.GetOrMakeBotUser();

            if (!string.IsNullOrEmpty(args[1]))
            {
                var deeplinkArgs = args[1].Split('_');
                AchvGroup group;

                switch (deeplinkArgs[0])
                {
                    case "subscribe":
                        if (deeplinkArgs.Length > 1 && long.TryParse(deeplinkArgs[1], out long groupid) && Groups.Any(x => x.Id == groupid))
                        {
                            group = Groups.First(x => x.Id == groupid);
                            if (bu.Subscriptions.Contains(groupid.ToString()))
                            {
                                Bot.Reply($"{Methods.GetString(msg, "AlreadySubscribing")} {group.Name}", msg);
                                return;
                            }
                            else
                            {
                                bu.Subscriptions += $"{groupid}|";
                                SQL.ChangeBotUser(bu);
                                Bot.Reply($"{Methods.GetString(msg, "SuccessfullySubscribed")} {group.Name}", msg);
                                return;
                            }
                        }
                        break;

                    case "unsubscribe":
                        if (deeplinkArgs.Length > 1 && long.TryParse(deeplinkArgs[1], out groupid) && Groups.Any(x => x.Id == groupid))
                        {
                            group = Groups.First(x => x.Id == groupid);
                            if (!bu.Subscriptions.Contains(groupid.ToString()))
                            {
                                Bot.Reply($"{Methods.GetString(msg, "NotEvenSubscribing")} {group.Name}", msg);
                                return;
                            }
                            else
                            {
                                bu.Subscriptions = bu.Subscriptions.Replace($"{groupid}|", "");
                                SQL.ChangeBotUser(bu);
                                Bot.Reply($"{Methods.GetString(msg, "SuccessfullyUnsubscribed")} {group.Name}", msg);
                                return;
                            }
                        }
                        break;
                }

                Bot.Reply(Methods.GetString(msg, "BotStartText"), msg);
            }
        }

        [Command (Trigger = "listcommands")]
        public static void ListCommands(Message msg, string[] args)
        {
            var bu = Methods.GetUser(msg, args);

            var res = $"<b>User commands:</b>{Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, Bot.Commands.Where(x => !x.AdminOnly & !x.DevOnly).Select(x => x.Trigger))}";

            if (Admins.Contains(bu.Telegramid))
                res += $"{Environment.NewLine}{Environment.NewLine}<b>Bot admin commands:</b>" +
                $"{Environment.NewLine}{string.Join(Environment.NewLine, Bot.Commands.Where(x => x.AdminOnly).Select(x => x.Trigger))}";

            if (Devs.ToList().Contains((int)bu.Telegramid))
                res += $"{Environment.NewLine}{Environment.NewLine}<b>Bot Dev Commands:</b>" + 
                $"{Environment.NewLine}{string.Join(Environment.NewLine, Bot.Commands.Where(x => x.DevOnly).Select(x => x.Trigger))}";

            Bot.Reply(res, msg);
        }
    }



    public partial class Callbacks
    {
        [Callback(Trigger = "subscribe")]
        public static void Subscribe(CallbackQuery call, string[] args)
        {
            var groupid = long.Parse(args[1]);
            var group = Groups.FirstOrDefault(x => x.Id == groupid);
            var bu = call.From.GetOrMakeBotUser();

            if (bu.Subscriptions.Contains(groupid.ToString()))
            {
                Bot.AnswerCallback(call, $"{Methods.GetString(bu.Language, "AlreadySubscribing")} {group.Name}", true);
            }
            else
            {
                bu.Subscriptions += $"{groupid}|";
                SQL.ChangeBotUser(bu);
                Bot.AnswerCallback(call, $"{Methods.GetString(bu.Language, "SuccessfullySubscribed")} {group.Name}");
            }

            var markup = new InlineKeyboardMarkup(
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton(Methods.GetString(bu.Language, "UnsubscribeThisButton"), $"unsubscribe|{groupid}"),
                }
            );
            Bot.Edit(call.Message, $"{Methods.GetString(bu.Language, "SuccessfullySubscribed")} {group.Name}", replyMarkup: markup);
        }

        [Callback(Trigger = "unsubscribe")]
        public static void Unsubscribe(CallbackQuery call, string[] args)
        {
            var groupid = long.Parse(args[1]);
            var group = Groups.FirstOrDefault(x => x.Id == groupid);
            var bu = call.From.GetOrMakeBotUser();

            if (!bu.Subscriptions.Contains(groupid.ToString()))
            {
                Bot.AnswerCallback(call, $"{Methods.GetString(bu.Language, "NotEvenSubscribing")} {group.Name}", true);
            }
            else
            {
                bu.Subscriptions = bu.Subscriptions.Replace($"{groupid}|", "");
                SQL.ChangeBotUser(bu);
                Bot.AnswerCallback(call, $"{Methods.GetString(bu.Language, "SuccessfullyUnsubscribed")} {group.Name}");
            }

            var markup = new InlineKeyboardMarkup(
                new InlineKeyboardButton[]
                {
                    new InlineKeyboardCallbackButton(Methods.GetString(bu.Language, "SubscribeThisButton"), $"subscribe|{groupid}"),
                }
            );
            Bot.Edit(call.Message, $"{Methods.GetString(bu.Language, "SuccessfullyUnsubscribed")} {group.Name}", replyMarkup: markup);
        }
    }
}

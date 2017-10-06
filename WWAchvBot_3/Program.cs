using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace WWAchvBot_3
{
    class Program
    {
        public const string BasePath = "C:\\Olgabrezel\\AchvBot_3\\";
        public static readonly int[] Devs = new[] { 295152997 };
        public static List<long> Admins = new List<long>();
        public static readonly DateTime StartTime = DateTime.UtcNow;

        public static readonly AchvGroup testgroup = new AchvGroup(-1001070844778, "https://t.me/joinchat/EZetZT_Ty2qyC3tUNrVD0w", "Testgroup", null);
        public static List<AchvGroup> Groups = new List<AchvGroup>();
        public static List<BotUser> Users = new List<BotUser>();
        public const long TranslationGroup = -1001142136211;

        public static bool UpdateBot = false;
        public static List<Game> Games = new List<Game>();

        static void Main(string[] args)
        {
            try
            {
                if (!System.IO.File.Exists(BasePath + "Errors.txt"))
                {
                    var fs = System.IO.File.Create(BasePath + "Errors.txt");
                    fs.Close();
                }

                if (!System.IO.File.Exists(BasePath + "Database.sqlite"))
                {
                    SQL.CreateDB();
                }


                Bot.Api.OnMessage += Handler.OnMessage;
                Bot.Api.OnCallbackQuery += Handler.OnCallback;

                #region Load Stuff
                // Load Commands
                foreach (var m in typeof(Commands).GetMethods())
                {
                    var c = new Models.Commands();
                    foreach (var a in m.GetCustomAttributes(true))
                    {
                        if (a is Attributes.Command)
                        {
                            var ca = a as Attributes.Command;
                            c.AdminOnly = ca.AdminOnly;
                            c.DevOnly = ca.DevOnly;
                            c.InGroupOnly = ca.InGroupOnly;
                            c.InGameOnly = ca.InGameOnly;
                            c.Trigger = ca.Trigger;
                            c.Method = (Bot.ChatCommandMethod)m.CreateDelegate(typeof(Bot.ChatCommandMethod));
                            Bot.Commands.Add(c);
                        }
                    }
                }

                // Load Callback Queries
                foreach (var m in typeof(Callbacks).GetMethods())
                {
                    var c = new Models.Callbacks();
                    foreach (var a in m.GetCustomAttributes(true))
                    {
                        if (a is Attributes.Callback)
                        {
                            var ca = a as Attributes.Callback;
                            c.AdminOnly = ca.AdminOnly;
                            c.DevOnly = ca.DevOnly;
                            c.Trigger = ca.Trigger;
                            c.RequiresConfirm = ca.RequiresConfirm;
                            c.Method = (Bot.ChatCallbackMethod)m.CreateDelegate(typeof(Bot.ChatCallbackMethod));
                            Bot.Callbacks.Add(c);
                        }
                    }
                }

                // Load Languages
                Language.ReadAll();

                // Load Groups
                foreach (var grp in SQL.ReadGroups())
                {
                    Groups.Add(grp);
                }

                // Load Users
                Users = SQL.ReadUsers();

                // Load Admins
                Admins = SQL.ReadAdmins();
                #endregion

                Bot.Api.StartReceiving();
                Bot.Send("<b>Started up!</b>", testgroup.Id);
                Thread.Sleep(-1);
            }
            catch (Exception e)
            {
                e.Log(true);
            }
        }

        public static void Restart()
        {
            var dir = System.IO.Directory.EnumerateDirectories($"{BasePath}Running").OrderBy(x => x).LastOrDefault();
            try
            {
                Bot.Api.StopReceiving();
                System.Diagnostics.Process.Start(dir + "\\WWAchvBot_3.exe");
            }
            catch
            {
                Bot.Send("COULDN'T START UP THE NEWEST VERSION! @Olgabrezel", testgroup.Id);
            }

            Environment.Exit(0);
        }

        #region Bot
        public static class Bot
        {
            public static ITelegramBotClient Api = new TelegramBotClient(System.IO.File.ReadAllText(BasePath + "Token.txt"));
            public static Telegram.Bot.Types.User Me = Api.GetMeAsync().Result;

            public static HashSet<Models.Commands> Commands = new HashSet<Models.Commands>();
            public static HashSet<Models.Callbacks> Callbacks = new HashSet<Models.Callbacks>();

            internal delegate void ChatCommandMethod(Message m, string[] args);
            internal delegate void ChatCallbackMethod(CallbackQuery q, string[] args);

            public static Message Reply(string text, Message message, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false, IReplyMarkup replyMarkup = null)
            {
                return Reply(text, message.Chat.Id, message.MessageId, parseMode, disableWebPagePreview, disableNotification, replyMarkup);
            }

            public static Message Reply(string text, long chatid, int messageid, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false, IReplyMarkup replyMarkup = null)
            {
                try
                {
                    return Api.SendTextMessageAsync(chatid, text, parseMode, disableWebPagePreview, disableNotification, messageid, replyMarkup).Result;
                }
                catch
                {
                    //...
                    return null;
                }
            }

            public static Message Send(string text, long chatid, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false, IReplyMarkup replyMarkup = null)
            {
                try
                {
                    return Api.SendTextMessageAsync(chatid, text, parseMode, disableWebPagePreview, disableNotification, 0, replyMarkup).Result;
                }
                catch
                {
                    //...
                    return null;
                }
            }

            public static bool Delete(Message message)
            {
                return Delete(message.Chat.Id, message.MessageId);
            }

            public static bool Delete(long chatid, int messageid)
            {
                try
                {
                    return Api.DeleteMessageAsync(chatid, messageid).Result;
                }
                catch
                {
                    //...
                    return false;
                }
            }

            public static Message Edit(Message msg, string text, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, IReplyMarkup replyMarkup = null)
            {
                return Edit(msg.Chat.Id, msg.MessageId, text, parseMode, disableWebPagePreview, replyMarkup);
            }

            public static Message Edit(long chatid, int messageid, string text, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, IReplyMarkup replyMarkup = null)
            {
                try
                {
                    return Api.EditMessageTextAsync(chatid, messageid, text, parseMode, disableWebPagePreview, replyMarkup).Result;
                }
                catch
                {
                    //...
                    return null;
                }
            }

            public static bool Pin(Message message, bool disableNotification = true)
            {
                return Pin(message.Chat.Id, message.MessageId, disableNotification);
            }

            public static bool Pin(long chatid, int messageid, bool disableNotification = true)
            {
                try
                {
                    return Api.PinChatMessageAsync(chatid, messageid, disableNotification).Result;
                }
                catch
                {
                    //...
                    return false;
                }
            }

            public static bool RemovePin(long chatid)
            {
                try
                {
                    return Api.UnpinChatMessageAsync(chatid).Result;
                }
                catch
                {
                    //...
                    return false;
                }
            }

            public static Message GetPinned(long chatid)
            {
                try
                {
                    var t = Api.GetChatAsync(chatid);
                    t.Wait();
                    return t.Result.PinnedMessage;
                }
                catch (Exception e)
                {
                    //...
                    e.Log(true);
                    return null;
                }
            }

            public static bool AnswerCallback(CallbackQuery callback, string text = null, bool showAlert = false, string url = null)
            {
                try
                {
                    return Api.AnswerCallbackQueryAsync(callback.Id, text, showAlert, url).Result;
                }
                catch
                {
                    //...
                    return false;
                }
            }

        }
        #endregion
    }
}

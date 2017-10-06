using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;
using WWAchvBot_3.Attributes;
using File = System.IO.File;
using static WWAchvBot_3.Program;

namespace WWAchvBot_3
{
    public partial class Commands
    {
        [Command(Trigger = "test", AdminOnly = true)]
        public static void Lol(Message msg, string[] args)
        {
            var pin = Bot.GetPinned(msg.Chat.Id);

            if (pin == null) Bot.Send("No pinmessage!", msg.Chat.Id);

            else Bot.Reply("This is the pinned message.", pin);
        }

#if DEBUG
        [Command(Trigger = "killbot", DevOnly = true)]
        public static void KillBot(Message msg, string[] args)
        {
            Bot.Reply("Killing self... But don't worry, Jehan, I'm still alive :)", msg);
            Games.ForEach(x => x.Stopped = true);
            Games.ForEach(x => x.UpdatePin());
            Environment.Exit(0);
        }
#endif

        [Command(Trigger = "db", DevOnly = true)]
        public static void SQLite(Message msg, string[] args)
        {
            if (string.IsNullOrEmpty(args[1]))
            {
                Bot.Reply("You need to enter a query...", msg);
                return;
            }

            try
            {
                SQL.DB_Command(msg, args);
            }
            catch (SQLiteException e)
            {
                Bot.Reply(e.Message, msg);
                return;
            }
            catch (Exception e)
            {
                e.Log(true);
                return;
            }
        }

        [Command(Trigger = "dbreload", DevOnly = true)]
        public static void ReloadLangs(Message msg, string[] args)
        {
            Language.ReadAll();
            Admins = SQL.ReadAdmins();
            Users = SQL.ReadUsers();
            Groups = SQL.ReadGroups();
            Bot.Reply("Done", msg);
        }

        [Command(Trigger = "sendlang", AdminOnly = true)]
        public static void SendLang(Message msg, string[] args)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            foreach (var lang in Language.All.Select(x => x.Name))
            {
                buttons.Add(new InlineKeyboardButton[] { new InlineKeyboardCallbackButton(lang, $"sendlang|{lang}") });
            }
            buttons.Add(new InlineKeyboardButton[] { new InlineKeyboardCallbackButton("Abort", "sendlang|abort") });
            var markup = new InlineKeyboardMarkup(buttons.ToArray());

            Bot.Reply("Get which language?", msg, replyMarkup: markup);
        }

        [Command(Trigger = "uselang", AdminOnly = true)]
        public static void UseLang(Message msg, string[] args)
        {
            if (msg.ReplyToMessage == null || msg.ReplyToMessage.Type != MessageType.DocumentMessage || !msg.ReplyToMessage.Document.FileName.EndsWith(".json"))
            {
                Bot.Reply("You need to reply to the language file (*.json)", msg);
                return;
            }

            var file = Bot.Api.GetFileAsync(msg.ReplyToMessage.Document.FileId).Result;
            using (var sr = new StreamReader(file.FileStream))
            {
                Language lang;
                string exception = "";

                try
                {
                    lang = JsonConvert.DeserializeObject<Language>(sr.ReadToEnd());
                }
                catch (JsonException JE)
                {
                    Bot.Reply(JE.Message, msg);
                    return;
                }

                if (string.IsNullOrEmpty(lang.Name)) exception += "The language name mustn't be empty!" + Environment.NewLine + Environment.NewLine;
                else if (lang.Name.Length > 30) exception += "The language name is too long (maximum 30 characters)" + Environment.NewLine + Environment.NewLine;

                if (lang.Strings == null || lang.Strings.Count < 1) exception += "No strings found!" + Environment.NewLine + Environment.NewLine;
                else if (lang.Strings.Any(x => x.Key.Length > 50))
                {
                    exception += "Strings with too long keys:" + Environment.NewLine +
                        string.Join(Environment.NewLine, lang.Strings.Keys.Where(x => x.Length > 50)) + Environment.NewLine + Environment.NewLine;
                }

                if (!string.IsNullOrEmpty(exception))
                {
                    Bot.Reply(exception, msg);
                    return;
                }

                string text;

                if (Language.All.Any(x => x.Name == lang.Name) & lang.Name != "english") // Already existing language
                {
                    text = $"<b>New file: {lang.Name}</b>" + Environment.NewLine +
                        $"Number of strings (new file): {lang.Strings.Count}" + Environment.NewLine +
                        $"Number of strings (current file): {Language.All.First(x => x.Name == lang.Name).Strings.Count}" + Environment.NewLine +
                        $"Number of strings (english file): {Language.English.Strings.Count}" + Environment.NewLine +
                        Environment.NewLine +
                        "<b>Missing strings:</b>" + Environment.NewLine +
                        string.Join(Environment.NewLine, Language.English.Strings.Keys.Where(y => !lang.Strings.Keys.Contains(y)));
                }
                else if (lang.Name == "english")
                {
                    text = $"<b>New file: english</b>" + Environment.NewLine +
                        $"Number of strings (new file): {lang.Strings.Count}" + Environment.NewLine +
                        $"Number of strings (current file): {Language.English.Strings.Count}" + Environment.NewLine +
                        Environment.NewLine +
                        "<b>Added strings:</b>" + Environment.NewLine +
                        string.Join(Environment.NewLine, lang.Strings.Keys.Where(x => !Language.English.Strings.Keys.Contains(x))) +
                        Environment.NewLine + Environment.NewLine +
                        "<b>Removed strings:</b>" + Environment.NewLine +
                        string.Join(Environment.NewLine, Language.English.Strings.Keys.Where(y => !lang.Strings.Keys.Contains(y)));
                }
                else
                {
                    text = $"<b>NEW LANGUAGE: {lang.Name.ToUpper()}</b>" + Environment.NewLine +
                        $"Number of strings (new file): {lang.Strings.Count}" + Environment.NewLine +
                        $"Number of strings (english file): {Language.English.Strings.Count}" + Environment.NewLine +
                        Environment.NewLine +
                        "<b>Missing strings:</b>" + Environment.NewLine +
                        string.Join(Environment.NewLine, Language.English.Strings.Keys.Where(y => !lang.Strings.Keys.Contains(y)));
                }

                var markup = new InlineKeyboardMarkup(
                    new InlineKeyboardButton[]
                    {
                        new InlineKeyboardCallbackButton("Yes", "uselang|yes"),
                        new InlineKeyboardCallbackButton("No", $"uselang|no"),
                    }
                );

                Bot.Reply(text, msg);
                Bot.Reply("<b>Do you want to upload?</b>", msg.ReplyToMessage, replyMarkup: markup);
            };
        }

        [Command(Trigger = "grouplang", InGroupOnly = true, AdminOnly = true)]
        public static void Setlang(Message msg, string[] args)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            foreach (var lang in Language.All.Select(x => x.Name))
            {
                buttons.Add(new InlineKeyboardButton[] { new InlineKeyboardCallbackButton(lang, $"setlang|{msg.Chat.Id}|{lang}") });
            }
            buttons.Add(new InlineKeyboardButton[] { new InlineKeyboardCallbackButton("Abort", $"setlang|{msg.Chat.Id}|abort") });
            var markup = new InlineKeyboardMarkup(buttons.ToArray());

            Bot.Reply("Set which language?", msg, replyMarkup: markup);
        }

        [Command(Trigger = "userinfo", AdminOnly = true)]
        public static void UserInfo(Message msg, string[] args)
        {
            var bu = Methods.GetUser(msg, args);
            var status = "User without a single game";
            if (bu.Gamecount > 0) status = "Player";
            if (Admins.Contains(bu.Telegramid)) status = "Bot Admin";
            if (Devs.ToList().Contains((int)bu.Telegramid)) status = "Bot Developer";
            Bot.Reply(
                $"{bu.LinkedName}{Environment.NewLine}" +
                $" - @{bu.Username}{Environment.NewLine}" +
                $" - <code>{bu.Telegramid}</code>{Environment.NewLine}" +
                $" - Gamecount: <b>{bu.Gamecount}</b>{Environment.NewLine}" +
                $" - Achievement count: <b>{bu.Achievements.Count(x => x == '|') + 1}</b>{Environment.NewLine}" +
                $" - Status: {status}"

            , msg);
        }

        [Command(Trigger = "upgrade", DevOnly = true)]
        public static void Upgrade(Message msg, string[] args)
        {
#if RELEASE
            var text = "UPGRADING" + Environment.NewLine + Environment.NewLine;
            var m = Bot.Reply(text + "<b>Pulling Git...</b>", msg);

            Process.Start($"{BasePath}Upgrade\\GitPull.bat").WaitForExit();

            text += "Git pulled." + Environment.NewLine;
            Bot.Edit(m, text + "<b>Restoring nuget packages...</b>");

            Process.Start($"{BasePath}Upgrade\\NugetRestore.bat").WaitForExit();

            text += "Packages restored." + Environment.NewLine;
            Bot.Edit(m, text + "<b>Building release...</b>");

            Process.Start($"{BasePath}Upgrade\\BuildSolution.bat").WaitForExit();

            var txt = File.ReadAllText($"{BasePath}Upgrade\\BuildResults.txt");
            var success = txt.Contains("0 fehlerhaft");

            if (success)
            {
                text += "Release built." + Environment.NewLine;
                Bot.Edit(m, text + "<b>Copying release to bot...</b>");
                var path = BasePath + "Running\\" + DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");

                Process.Start("xcopy.exe", $"/E {BasePath}Source\\WWAchvBot_3\\WWAchvBot_3\\bin\\Release {path}").WaitForExit();

                text += "Release copied to bot. It will be run after all games stopped. Path:" + Environment.NewLine + Environment.NewLine + path + Environment.NewLine + Environment.NewLine + "<b>Operation complete.</b>";
                Bot.Edit(m, text);
                UpdateBot = true;

                if (Games.Count == 0)
                {
                    Restart();
                }
            }
            else
            {
                text += "<b>Failed to build release!</b>" + Environment.NewLine + Environment.NewLine + "Check the output for details:";
                Bot.Edit(m, text);
                Bot.Reply(txt, m);
            }

#else
            Bot.Reply("Can only upgrade when running on release!", msg);
#endif

        }

        [Command(Trigger = "unupdate", DevOnly = true)]
        public static void UnUpdate(Message msg, string[] args)
        {
            if (UpdateBot)
            {
                UpdateBot = false;
                Bot.Reply("The upgrading was cancelled.", msg);
            }
            else
            {
                Bot.Reply("The bot wasn't even about to upgrade!", msg);
            }
        }
    }


    public partial class Callbacks
    {
        [Callback(Trigger = "sendlang", AdminOnly = true)]
        public static void SendLang(CallbackQuery call, string[] args)
        {
            if (args[1] == "abort")
            {
                Bot.AnswerCallback(call, "Aborted.");
                Bot.Edit(call.Message, "Aborted.");
                return;
            }

            Bot.Edit(call.Message, "One moment...");
            var lang = Language.All.FirstOrDefault(x => x.Name == args[1]) ?? Language.English;
            var json = JsonConvert.SerializeObject(lang)
                .Replace("{", Environment.NewLine + Environment.NewLine + "{")
                .Replace("\",", "\"," + Environment.NewLine + Environment.NewLine)
                .Replace("\":\"", "\":" + Environment.NewLine + "\"")
                .Trim();

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(json);
            writer.Flush();
            stream.Position = 0;
            Bot.Api.SendDocumentAsync(call.Message.Chat.Id, new FileToSend(lang.Name + ".json", stream));
            Bot.AnswerCallback(call, "Sending " + lang.Name + ".json");
        }

        [Callback(Trigger = "uselang", AdminOnly = true)]
        public static void UseLang(CallbackQuery call, string[] args)
        {
            if (args[1] == "no")
            {
                Bot.AnswerCallback(call, "Aborted.");
                Bot.Edit(call.Message, "Aborted.");
                return;
            }

            var file = Bot.Api.GetFileAsync(call.Message.ReplyToMessage.Document.FileId).Result;
            using (var sr = new StreamReader(file.FileStream))
            {
                var lang = JsonConvert.DeserializeObject<Language>(sr.ReadToEnd());
                if (Language.All.Any(x => x.Name == lang.Name))
                {
                    SQL.ChangeLanguage(lang);
                }
                else
                {
                    SQL.CreateLanguage(lang);
                }
                Language.ReadAll();
                Bot.AnswerCallback(call, "Uploaded.");
                Bot.Edit(call.Message, call.Message.Text + Environment.NewLine + "<b>Uploaded.</b>");
            }
        }

        [Callback(Trigger = "setlang", AdminOnly = true)]
        public static void Setlang(CallbackQuery call, string[] args)
        {
            var chatid = long.Parse(args[1].Split('|')[0]);
            var grp = Groups.FirstOrDefault(x => x.Id == chatid);

            if (args[1].Split('|')[1] == "abort")
            {
                Bot.AnswerCallback(call, "Aborted.");
                Bot.Edit(call.Message, "Aborted.");
                return;
            }

            var lang = Language.All.FirstOrDefault(x => x.Name == args[1].Split('|')[1]) ?? Language.English;
            grp.Language = lang;
            SQL.ChangeGroup(grp);
            Bot.AnswerCallback(call, "Language set: " + lang.Name);
            Bot.Edit(call.Message, Methods.GetString(lang, "LanguageSet") + " " + lang.Name);

        }
    }
}

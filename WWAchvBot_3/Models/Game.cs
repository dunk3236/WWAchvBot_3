using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;
using static WWAchvBot_3.Program;

namespace WWAchvBot_3
{
    class Game
    {
        public Message DefaultPin;
        public Message Pinmessage;
        public List<Player> Players = new List<Player>();
        public long GroupId;
        public bool Started = false;
        public bool Stopped = false;
        public string Lynchorder = "$lynchorder";

        public List<Player> AlivePlayers;
        public int Spawnablewolves;

        private string JoinText => Methods.GetString(Pinmessage, "GameStarted").ToBold() + Environment.NewLine +
            Environment.NewLine + Methods.GetString(Pinmessage, "GameStartInfo") + Environment.NewLine +
            Environment.NewLine + Methods.GetString(Pinmessage, "Players").ToBold() + $": {Players.Count}".ToBold() + Environment.NewLine +
            string.Join(Environment.NewLine, Players.Select(x => x.User.LinkedName));

        private string RunText
        {
            get
            {
                var txt = Methods.GetString(Pinmessage, "GameRunning").ToBold() + Environment.NewLine +
                Environment.NewLine + Methods.GetString(Pinmessage, "GameRunInfo") + Environment.NewLine +
                Environment.NewLine + Methods.GetString(Pinmessage, "Players").ToBold() + $" ({Players.Count(x => x.Alive)} / {Players.Count}):".ToBold() + Environment.NewLine;

                foreach (var p in Players.Where(x => x.Alive))
                {
                    txt += $"{p.User.LinkedName}: {Methods.GetString(Pinmessage, "Role" + p.Role)}{(p.Love ? " ❤️" : "")}{Environment.NewLine}";
                }
                txt += Environment.NewLine + Environment.NewLine + $"{Methods.GetString(Pinmessage, "DeadPlayers")}:".ToBold() +
                    Environment.NewLine;
                foreach (var p in Players.Where(x => !x.Alive))
                {
                    txt += $"{p.User.LinkedName} ({Methods.GetString(Pinmessage, $"Role{p.Role}")}{(p.Love ? " ❤️" : "")}){Environment.NewLine}";
                }
                return txt;
            }
        }

        private IReplyMarkup JoinMarkup => new InlineKeyboardMarkup(
            new InlineKeyboardButton[]
            {
                new InlineKeyboardCallbackButton(Methods.GetString(Pinmessage, "StartButton"), $"startgame|{GroupId}"),
                new InlineKeyboardCallbackButton(Methods.GetString(Pinmessage, "StopButton"), $"stopgame|{GroupId}"),
            }
        );

        private IReplyMarkup RunMarkup => new InlineKeyboardMarkup(
            new InlineKeyboardButton[]
            {
                new InlineKeyboardCallbackButton(Methods.GetString(Pinmessage, "StopButton"), $"stopgame|{GroupId}"),
            }
        );

        public Game(Message Pin)
        {
            DefaultPin = Bot.GetPinned(Pin.Chat.Id);
            Pinmessage = Pin;
            GroupId = Pin.Chat.Id;
            Bot.Pin(Pinmessage);
            UpdatePin();
        }

        public void UpdatePin()
        {
            if (Stopped)
            {
                Bot.Edit(Pinmessage, Methods.GetString(Pinmessage, "GameEnded").ToBold());
            }
            else if (Started)
            {
                Bot.Edit(Pinmessage, RunText, replyMarkup: RunMarkup);
            }
            else
            {
                Bot.Edit(Pinmessage, JoinText, replyMarkup: JoinMarkup);
            }
        }

        private void CalcAchvInfo()
        {
            AlivePlayers = Players.Where(x => x.Alive).ToList();

            Spawnablewolves = 0;
            Spawnablewolves += AlivePlayers.Count(x => new[] { "AlphaWolf", "Werewolf", "WolfCub", "Traitor", "WildChild" }.Contains(x.Role));
            if (Spawnablewolves > 0)
            {
                Spawnablewolves += AlivePlayers.Count(x => x.Role == "Doppelgänger" || x.Role == "Cursed");
            }
        }

        public string GetAchievements()
        {
            CalcAchvInfo();
            string res = Methods.GetString(Pinmessage, "PossibleAchievements").ToBold() + Environment.NewLine + Environment.NewLine;
            Dictionary<Player, List<string>> possible = new Dictionary<Player, List<string>>();

            foreach (var p in Players)
            {
                possible.Add(p, new List<string>());
                foreach (var achv in Names.Achievements)
                {
                    if (IsAchievable(p, achv)) possible[p].Add(achv);
                }
            }

            foreach (var kvp in possible.Where(x => x.Value.Count >= 1))
            {
                res += kvp.Key.User.LinkedName + Environment.NewLine;
                foreach (var achv in kvp.Value)
                {
                    res += " - " + achv + Environment.NewLine;
                }
                res += Environment.NewLine + Environment.NewLine;
            }

            return res;
        }

        public void Stop()
        {
            Stopped = true;
            UpdatePin();
            if (DefaultPin != null) Bot.Pin(DefaultPin);
            else Bot.RemovePin(Pinmessage.Chat.Id);
            Games.Remove(this);

            if (UpdateBot && Games.Count == 0) Restart();
        }

        public bool IsAchievable(Player player, string achv)
        {
            if (player.User.Achievements.Contains(achv)) return false;

            switch (achv)
            {
                case "Change Sides Works":
                    return player.Role == "Doppelgänger" || player.Role == "WildChild" || player.Role == "Traitor" || (AlivePlayers.Select(e => e.Role).Contains("AlphaWolf") && !new[] { "Werewolf", "AlphaWolf", "WolfCub" }.Contains(player.Role)) || player.Role == "ApprenticeSeer" || (player.Role == "Cursed" && Spawnablewolves >= 1);

                case "Cultist Convention":
                    return !new[] { "AlphaWolf", "WolfCub", "Werewolf", "SerialKiller", "CultistHunter" }.Contains(player.Role) && AlivePlayers.Select(e => e.Role).Count(x => x != "AlphaWolf" && x != "WolfCub" && x != "Werewolf" && x != "SerialKiller" && x != "CultistHunter") >= 10 && AlivePlayers.Select(e => e.Role).Contains("Cultist");

                case "Cultist Fodder":
                    return !new[] { "Werewolf", "WolfCub", "AlphaWolf", "SerialKiller", "CultistHunter" }.Contains(player.Role) && AlivePlayers.Select(e => e.Role).Contains("CultistHunter") && AlivePlayers.Select(e => e.Role).Contains("Cultist");

                case "Double Kill":
                    return (player.Role == "SerialKiller" && AlivePlayers.Select(e => e.Role).Contains("Hunter")) || (player.Role == "Hunter" && AlivePlayers.Select(e => e.Role).Contains("SerialKiller")) || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("Hunter") && AlivePlayers.Select(e => e.Role).Contains("SerialKiller"));

                case "Double Shifter":
                    return false; // TOO HARD YET, GOTTA BE FIXED!

                case "Double Vision":
                    return (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("ApprenticeSeer")) || (player.Role == "ApprenticeSeer" && Players.Select(e => e.Role).Contains("Doppelgänger"));

                case "Even a Stopped Clock is Right Twice a Day":
                    return player.Role == "Fool" || player.Role == "SeerFool" || (player.Role == "Doppelgänger" && (AlivePlayers.Select(e => e.Role).Contains("Fool") || AlivePlayers.Select(e => e.Role).Contains("SeerFool")));

                case "Forbidden Love":
                    return Players.Select(e => e.Role).Contains("Cupid") && ((player.Role == "Villager" && Spawnablewolves >= 1) || (new[] { "AlphaWolf", "Werewolf", "WolfCub", "WildChild", "Traitor" }.Contains(player.Role) && Players.Select(e => e.Role).Contains("Villager")) || (new[] { "Doppelgänger", "Cursed" }.Contains(player.Role) && Spawnablewolves >= 1 && Players.Select(e => e.Role).Contains("Villager"))) && (Players.Count(x => x.Love) < 2 || player.Love);

                case "Hey Man, Nice Shot":
                    return player.Role == "Hunter" || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("Hunter"));

                case "Inconspicuous":
                    return Players.Count >= 20;

                case "I See a Lack of Trust":
                    return (new[] { "Seer", "ApprenticeSeer", "SeerFool" }.Contains(player.Role)) || (player.Role == "Doppelgänger" && (Players.Select(e => e.Role).Contains("Seer") || Players.Select(e => e.Role).Contains("SeerFool") || Players.Select(e => e.Role).Contains("ApprenticeSeer")));

                case "Lone Wolf":
                    return new[] { "Werewolf", "AlphaWolf", "WolfCub" }.Contains(player.Role) && Players.Count(x => new[] { "AlphaWolf", "Werewolf", "WolfCub" }.Contains(x.Role)) == 1 && Players.Count >= 10;

                case "Masochist":
                    return player.Role == "Tanner" || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("Tanner"));

                case "Mason Brother":
                    return AlivePlayers.Select(e => e.Role).Count(x => x == "Mason") >= 2 && new[] { "Mason", "Doppelgänger" }.Contains(player.Role);

                case "OH SHI-":
                    return new[] { "AlphaWolf", "Werewolf", "WolfCub", "SerialKiller" }.Contains(player.Role) && Players.Select(e => e.Role).Contains("Cupid") && (Players.Count(x => x.Love) > 2 || player.Love);

                case "Pack Hunter":
                    return AlivePlayers.Count >= 15 && Spawnablewolves >= 7 && (AlivePlayers.Select(e => e.Role).Contains("AlphaWolf") || new[] { "AlphaWolf", "Werewolf", "WolfCub", "Traitor", "WildChild", "Cursed", "Doppelgänger" }.Contains(player.Role));

                case "Promiscuous":
                    return (player.Role == "Harlot" && AlivePlayers.Select(e => e.Role).Count(x => x != "Werewolf" && x != "WolfCub" && x != "AlphaWolf" && x != "SerialKiller" && x != "Harlot") >= 5) || (player.Role == "Doppelgänger" && AlivePlayers.Any(x => x.Role == "Harlot") && AlivePlayers.Select(e => e.Role).Count(x => x != "Werewolf" && x != "WolfCub" && x != "AlphaWolf" && x != "SerialKiller" && x != "Harlot" && x != "Doppelgänger") >= 5);

                case "Saved by the Bull(et)":
                    return AlivePlayers.Select(e => e.Role).Contains("Gunner") && Spawnablewolves >= 1 && !new[] { "AlphaWolf", "Werewolf", "WolfCub", "Sorcerer", "SerialKiller", "Doppelgänger", "Cultist" }.Contains(player.Role); // this needs checking - which roles can get it and which can't?

                case "Self Loving":
                    return player.Role == "Cupid" && (Players.Count(x => x.Love) < 2 || player.Love);

                case "Serial Samaritan":
                    return (player.Role == "SerialKiller" || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("SerialKiller"))) && Spawnablewolves >= 3;

                case "Should Have Known":
                    return new[] { "Seer", "SeerFool", "ApprenticeSeer" }.Contains(player.Role) && AlivePlayers.Any(x => x.Role == "Beholder");

                case "Should've Said Something":
                    return Players.Select(e => e.Role).Contains("Cupid") && (new[] { "AlphaWolf", "Werewolf", "WolfCub", "WildChild", "Traitor" }.Contains(player.Role) || (new[] { "Doppelgänger", "Cursed" }.Contains(player.Role) && Spawnablewolves >= 1)) && (Players.Count(x => x.Love) > 2 || player.Love);

                case "Smart Gunner":
                    return (player.Role == "Gunner" || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("Gunner"))) && (Spawnablewolves >= 2 || (Spawnablewolves == 1 && AlivePlayers.Select(e => e.Role).Contains("SerialKiller")) || AlivePlayers.Select(e => e.Role).Contains("Cultist"));

                case "So Close":
                    return player.Role == "Tanner" || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("Tanner"));

                case "Speed Dating":
                    return AlivePlayers.Select(e => e.Role).Contains("Cupid") && (Players.Count(x => x.Love) < 2 || player.Love);

                case "Streetwise":
                    return (player.Role == "Detective" || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("Detective"))) && ((Spawnablewolves + AlivePlayers.Select(e => e.Role).Count(x => x == "SerialKiller") >= 4) || AlivePlayers.Select(e => e.Role).Contains("Cultist"));

                case "Sunday Bloody Sunday":
                    return false; //THIS NEEDS WORK

                case "Tanner Overkill":
                    return player.Role == "Tanner" || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("Tanner"));

                case "That's Why You Don't Stay Home":
                    return AlivePlayers.Select(e => e.Role).Contains("Harlot") && (new[] { "AlphaWolf", "Werewolf", "WolfCub", "Cultist", "WildChild", "Traitor" }.Contains(player.Role) || (new[] { "Doppelgänger", "Cursed" }.Contains(player.Role) && Spawnablewolves >= 1) || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("Cultist")));

                case "Wobble Wobble":
                    return Players.Count >= 10 && (player.Role == "Drunk" || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("Drunk")));


                // NEW ACHIEVEMENTS
                case "No Sorcery":
                    return AlivePlayers.Select(e => e.Role).Contains("Sorcerer") && (new[] { "AlphaWolf", "Werewolf", "WolfCub", "WildChild", "Traitor" }.Contains(player.Role) || (new[] { "Cursed", "Doppelgänger" }.Contains(player.Role) && Spawnablewolves >= 1));

                case "Cultist Tracker":
                    return (player.Role == "CultistHunter" || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("CultistHunter"))) && AlivePlayers.Select(e => e.Role).Contains("Cultist");

                case "I'M NOT DRUN-- *BURPPP*":
                    return player.Role == "ClumsyGuy" || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("ClumsyGuy"));

                case "Wuffie-Cult":
                    return (player.Role == "AlphaWolf" && Players.Count >= 8 && AlivePlayers.Count / 2 > AlivePlayers.Count(x => new[] { "AlphaWolf", "WolfCub", "Werewolf" }.Contains(x.Role))) || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("AlphaWolf") && Players.Count >= 9 && AlivePlayers.Count / 2 > AlivePlayers.Count(x => new[] { "AlphaWolf", "WolfCub", "Werewolf" }.Contains(x.Role)));

                case "Did you guard yourself?":
                    return (player.Role == "GuardianAngel" || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("GuardianAngel"))) && Spawnablewolves >= 1;

                case "Spoiled Rich Brat":
                    return player.Role == "Prince" || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("Prince"));

                case "Three Little Wolves and a Big Bad Pig":
                    return Spawnablewolves >= 3 && (player.Role == "Sorcerer" || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("Sorcerer")));

                case "President":
                    return player.Role == "Mayor" || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("Mayor"));

                case "I Helped!":
                    return Spawnablewolves >= 2 && (player.Role == "WolfCub" || (player.Role == "Doppelgänger" && AlivePlayers.Select(e => e.Role).Contains("WolfCub")));

                case "It Was a Busy Night!":
                    return false; // THIS NEEDS WORK!

                case "The First Stone":
                case "In for the Long Haul":
                    return true;




                default:
                    // UNATTAINABLE ONES AND ONES BOT CAN'T KNOW:
                    // AlzheimersPatient
                    // BlackSheep
                    // Dedicated
                    // Developer
                    // Enochlophobia
                    // Explorer
                    // HeresJohnny
                    // IHaveNoIdeaWhatImDoing
                    // Introvert
                    // IveGotYourBack
                    // Linguist
                    // Naughty
                    // Obsessed
                    // OHAIDER
                    // SpyVsSpy
                    // Survivalist
                    // Veteran
                    // WelcomeToHell
                    // WelcomeToTheAsylum
                    return false;
            }
        }
    }
}

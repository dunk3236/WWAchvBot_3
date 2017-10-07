using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using static WWAchvBot_3.Program;

namespace WWAchvBot_3
{
    class SQL
    {
        static readonly string connectionstring = $"Data Source={BasePath}Database.sqlite";

        #region Initialize
        public static void CreateDB()
        {
            SQLiteConnection.CreateFile(BasePath + "Database.sqlite");
            List<string> queries = new List<string>()
            {
                "create table languages (name varchar(30) primary key)",
                "insert into languages values ('english')",
                "create table english (key varchar(50) primary key not null default '', value text)",
                "create table admins (id integer primary key)",
                "insert into admins values (" + string.Join("),(", Devs) + ")",
                "create table users (id integer primary key autoincrement, telegramid integer unique, name text, username text, gamecount integer not null default 0, subscribing text not null default '', language varchar(30) not null default 'english')",
                "create table groups (groupid integer primary key, link varchar(100), name varchar(100) not null default 'No groupname found', language varchar(30) not null default 'english')",
                "insert into groups values (" + testgroup.Id + ", " + testgroup.Link + ", " + testgroup.Name + ", 'english')",
            };
            foreach (var q in queries) RunNoQuery(q);
        }
        #endregion

        #region General
        public static void RunNoQuery(string query)
        {
            SQLiteConnection conn = new SQLiteConnection(connectionstring);
            conn.Open();
            new SQLiteCommand(query, conn).ExecuteNonQuery();
            conn.Close();
        }
        #endregion

        #region Command
        public static void DB_Command(Message msg, string[] args)
        {
            SQLiteConnection conn = new SQLiteConnection(connectionstring);
            string raw = "";

            var queries = args[1].Split(';');
            var reply = "";
            foreach (var sql in queries)
            {
                conn.Open();

                using (var comm = conn.CreateCommand())
                {
                    comm.CommandText = sql;
                    var reader = comm.ExecuteReader();
                    var result = "";
                    if (reader.HasRows)
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                            raw += reader.GetName(i) + (i == reader.FieldCount - 1 ? "" : " - ");
                        result += raw + Environment.NewLine + Environment.NewLine;
                        raw = "";
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                                raw += (reader.IsDBNull(i) ? "<i>NULL</i>" : reader[i]) + (i == reader.FieldCount - 1 ? "" : " - ");
                            result += raw + Environment.NewLine;
                            raw = "";
                        }
                    }
                    if (reader.RecordsAffected > 0) result += $"\n<i>{reader.RecordsAffected} record(s) affected.</i>";
                    else if (string.IsNullOrEmpty(result)) result = sql.ToLower().StartsWith("select") || sql.ToLower().StartsWith("update") || sql.ToLower().StartsWith("pragma") || sql.ToLower().StartsWith("delete") ? "<i>Nothing found.</i>" : "<i>Done.</i>";
                    reply += result + "\n\n";
                    conn.Close();
                }
            }
            Bot.Reply(reply, msg);
        }
        #endregion

        #region Language
        public static Dictionary<string, string> ReadLanguage(string name)
        {
            SQLiteConnection conn = new SQLiteConnection(connectionstring);
            var query = $"select key, value from {name}";
            conn.Open();

            var comm = new SQLiteCommand(query, conn);
            var reader = comm.ExecuteReader();

            var lang = new Dictionary<string, string>();

            while (reader.Read())
            {
                lang.Add((string)reader[0], (string)reader[1]);
            }

            conn.Close();
            return lang;
        }

        public static List<string> ReadLangList()
        {
            SQLiteConnection conn = new SQLiteConnection(connectionstring);
            var query = "select * from languages";
            conn.Open();
            var reader = new SQLiteCommand(query, conn).ExecuteReader();

            List<string> langs = new List<string>();

            while (reader.Read())
            {
                langs.Add((string)reader[0]);
            }
            conn.Close();
            return langs;
        }

        public static void CreateLanguage (Language lang)
        {
            var queries = new List<string>()
            {
                $"insert into languages values ('{lang.Name}')",
                $"create table {lang.Name} (key varchar(50) primary key not null default '', value text)",
            };
            foreach (var str in lang.Strings)
            {
                queries.Add($"insert into {lang.Name} values ('{str.Key.Replace("'", "''")}', '{str.Value.Replace("'", "''")}')");
            }

            foreach (var q in queries) RunNoQuery(q);
        }

        public static void ChangeLanguage(Language lang)
        {
            var queries = new List<string>();
            var current = Language.All.First(x => x.Name == lang.Name);

            foreach (var str in lang.Strings.Where(x => !current.Strings.ContainsKey(x.Key)))
            {
                queries.Add($"insert into {lang.Name} values ('{str.Key.Replace("'", "''")}', '{str.Value.Replace("'", "''")}')");
            }
            foreach (var str in current.Strings.Where(x => !lang.Strings.ContainsKey(x.Key)))
            {
                queries.Add($"delete from {lang.Name} where key = '{str.Key.Replace("'", "''")}'");
            }
            foreach (var str in lang.Strings.Where(x => current.Strings.ContainsKey(x.Key)))
            {
                queries.Add($"update {lang.Name} set value = '{str.Value.Replace("'", "''")}' where key = '{str.Key.Replace("'", "''")}'");
            }

            foreach (var q in queries) RunNoQuery(q);
        }
        #endregion

        #region Groups
        public static List<AchvGroup> ReadGroups()
        {
            SQLiteConnection conn = new SQLiteConnection(connectionstring);
            var query = "select * from groups";
            conn.Open();
            var comm = new SQLiteCommand(query, conn);
            var reader = comm.ExecuteReader();

            List<AchvGroup> groups = new List<AchvGroup>();

            while (reader.Read())
            {
                groups.Add(new AchvGroup((long)reader[0], (string)reader[1], (string)reader[2], Language.All.FirstOrDefault(x => x.Name == (string)reader[3]) ?? Language.English));
            }

            conn.Close();
            return groups;
        }

        public static void ChangeGroup(AchvGroup grp)
        {
            var query = $"update groups set language = '{grp.Language.Name}', name = '{grp.Name.FormatHTML().Replace("'", "''")}' where groupid = {grp.Id}";
            RunNoQuery(query);
        }
        #endregion

        #region Users
        public static List<BotUser> ReadUsers()
        {
            SQLiteConnection conn = new SQLiteConnection(connectionstring);
            var query = "select * from users";
            conn.Open();
            var reader = new SQLiteCommand(query, conn).ExecuteReader();
            var users = new List<BotUser>();

            while (reader.Read())
            {
                users.Add(new BotUser((long)reader[1], (string)reader[2], reader[3] is DBNull ? null : (string)reader[3], (long)reader[4], (string)reader[5], Language.All.FirstOrDefault(x => x.Name == (string)reader[6]) ?? Language.English));
            }
            conn.Close();
            return users;
        }

        public static List<long> ReadAdmins()
        {
            SQLiteConnection conn = new SQLiteConnection(connectionstring);
            var query = "select id from admins";
            conn.Open();
            var reader = new SQLiteCommand(query, conn).ExecuteReader();
            var admins = new List<long>();

            while (reader.Read())
            {
                admins.Add((long)reader[0]);
            }
            conn.Close();
            return admins;
        }

        public static void CreateBotUser(BotUser bu)
        {
            string query;

            if (string.IsNullOrEmpty(bu.Username))
            {
                query = $"insert into users (telegramid, name) values ({bu.Telegramid}, '{bu.Name.Replace("'", "''")}')";
            }
            else
            {
                query = $"insert into users (telegramid, name, username) values ({bu.Telegramid}, '{bu.Name.Replace("'", "''")}', '{bu.Username}')";
            }
            RunNoQuery(query);
        }

        public static void ChangeBotUser(BotUser bu)
        {
            string query = $"update users set name = '{bu.Name.Replace("'", "''")}', username = '{bu.Username}', language = '{bu.Language.Name}', subscribing = '{bu.Subscriptions}' where telegramid = {bu.Telegramid}";
            RunNoQuery(query);
        }
#endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWAchvBot_3
{
    public class Language
    {
        public static List<Language> All = new List<Language>();
        public static Language English => All.First(x => x.Name == "english");

        public static void ReadAll()
        {
            var temp = new List<Language>();
            foreach (var lang in SQL.ReadLangList())
            {
                temp.Add(new Language(lang));
                temp.First(x => x.Name == lang).Strings = SQL.ReadLanguage(lang);
                All = temp;
            }
        }


        public string Name;
        public Dictionary<string, string> Strings;

        public Language(string Name)
        {
            this.Name = Name;
        }

        public string GetValue(string key)
        {
            var s = Strings.FirstOrDefault(x => x.Key == key).Value;
            return s ?? "String missing! Inform @Olgabrezel please!";
        }
    }
}

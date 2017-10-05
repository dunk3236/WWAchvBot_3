using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWAchvBot_3
{
    class AchvGroup
    {
        public long Id;
        public string Link;
        public string Name;
        public Language Language;
        public DateTime LastPing = DateTime.MinValue;

        public AchvGroup(long Id, string Link, string Name, Language Language)
        {
            this.Id = Id;
            this.Link = Link;
            this.Name = Name;
            this.Language = Language;
        }
    }
}

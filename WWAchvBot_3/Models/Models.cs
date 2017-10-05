using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WWAchvBot_3.Program;

namespace WWAchvBot_3.Models
{
    class Commands
    {
        public string Trigger { get; set; }
        public bool InGroupOnly { get; set; }
        public bool InGameOnly { get; set; }
        public bool AdminOnly { get; set; }
        public bool DevOnly { get; set; }
        public Bot.ChatCommandMethod Method { get; set; }
    }

    class Callbacks
    {
        public string Trigger { get; set; }
        public bool AdminOnly { get; set; }
        public bool DevOnly { get; set; }
        public bool RequiresConfirm { get; set; }
        public Bot.ChatCallbackMethod Method { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWAchvBot_3.Attributes
{
    public class Command : Attribute
    {
        public string Trigger { get; set; }
        public bool InGroupOnly { get; set; } = false;
        public bool InGameOnly { get; set; } = false;
        public bool AdminOnly { get; set; } = false;
        public bool DevOnly { get; set; } = false;
    }

    public class Callback : Attribute
    {
        public string Trigger { get; set; }
        public bool AdminOnly { get; set; } = false;
        public bool DevOnly { get; set; } = false;
        public bool RequiresConfirm { get; set; } = false;
    }
}

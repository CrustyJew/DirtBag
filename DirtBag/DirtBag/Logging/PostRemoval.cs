using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Logging {
    class PostRemoval {
        public int RemovalID { get; set; }
        public DateTime TimeStamp { get; set; }
        public string ModName { get; set; }
        public string Reason { get; set; }
        public int PostID { get; set; }

    }
}

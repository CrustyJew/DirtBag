using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Models {
    public class BannedEntity {
        public string SubName { get; set; }
        public string EntityString { get; set; }
        public string BannedBy { get; set; }
        public string BanReason { get; set; }
        public DateTime? BanDate { get; set; }
        public string ThingID { get; set; }

    }
}

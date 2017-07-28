using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dirtbag.Models {
    public class BannedEntity {
        public int ID { get; set; }
        public string SubName { get; set; }
        public string EntityString { get; set; }
        public EntityType Type { get; set; }

        public string BannedBy { get; set; }
        public string BanReason { get; set; }

        private DateTime? _banDate;
        public DateTime? BanDate {
            get { return _banDate; }
            set {
                if(value.HasValue) {
                    _banDate = DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
                }
                else _banDate = null;
            }
        }
        public string ThingID { get; set; }


        public enum EntityType {
            Channel = 1, User = 2
        }
    }
}

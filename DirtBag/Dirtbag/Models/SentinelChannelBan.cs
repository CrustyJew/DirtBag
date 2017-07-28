using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dirtbag.Models
{
    public class SentinelChannelBan
    {
        public string MediaChannelID { get; set; }
        public string MediaAuthor { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public VideoProvider MediaPlatform { get; set; }

        private DateTime _banDate;
        public DateTime BlacklistDateUTC {
            get { return _banDate; }
            set {
                    _banDate = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }
        }
        public string BlacklistBy { get; set; }
        public string MediaChannelUrl { get; set; }
        public bool GlobalBan { get; set; }
    }
}

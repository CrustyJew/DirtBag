using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DirtbagWebservice.Models
{
    public class SentinelChannelBan
    {
        public string MediaChannelID { get; set; }
        public string MediaAuthor { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public VideoProvider MediaPlatform { get; set; }
        public DateTime BlacklistDateUTC { get; set; }
        public string BlacklistBy { get; set; }
        public string MediaChannelUrl { get; set; }
        public bool GlobalBan { get; set; }
    }
}

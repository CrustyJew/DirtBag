using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dirtbag.Models {
    public class AnalysisRequest {
        public string ThingID { get; set; }
        public AuthorInfo Author { get; set; }
        [JsonConverter(typeof(UnixTimeStampConverter))]
        public DateTime? EntryTime { get; set; }
        public string MediaID { get; set; }
        public string MediaChannelID { get; set; }
        public string MediaChannelName { get; set; }
        public VideoProvider MediaPlatform { get; set; }
        public string PermaLink { get; set; }

    }
}

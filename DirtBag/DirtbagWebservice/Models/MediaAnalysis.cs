using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DirtbagWebservice.Models
{
    public class MediaAnalysis : AnalysisDetails
    {
        public string MediaID { get; set; }
        public string MediaChannelID { get; set; }
        public string MediaChannelName { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public VideoProvider MediaPlatform { get; set; }

    }
}

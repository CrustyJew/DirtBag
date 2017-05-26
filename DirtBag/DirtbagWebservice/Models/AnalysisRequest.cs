using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtbagWebservice.Models {
    public class AnalysisRequest {
        public string ThingID { get; set; }
        public AuthorInfo Author { get; set; }
        public DateTime EntryTime { get; set; }
        public string MediaID { get; set; }
        public string MediaChannelID { get; set; }
        public string MediaChannelName { get; set; }
        public VideoProvider MediaPlatform { get; set; }

    }
}

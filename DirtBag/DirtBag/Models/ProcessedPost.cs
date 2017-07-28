using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dirtbag.Models {
    public class ProcessedItem {
        public string SubName { get; set; }
        public string ThingID { get; set; }
        public string Author { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public AnalyzableTypes ThingType { get; set; }
        public string PermaLink { get; set; }
        public string MediaID { get; set; }
        public string MediaChannelID { get; set; }
        public string MediaChannelName { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public VideoProvider MediaPlatform { get; set; }
        public string Action { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Modules.Modules SeenByModules { get; set; }
        public AnalysisDetails AnalysisDetails { get; set; }

        public ProcessedItem() {
            AnalysisDetails = new AnalysisDetails();
        }

        public ProcessedItem( string subName, string thingID, string author, string action, string link, string mediaID, string mediaChannelID, string mediaChannelName, VideoProvider mediaProvider, AnalysisDetails details, Modules.Modules seenByModules) {
            SubName = subName;
            ThingID = thingID;
            Action = action;
            Author = author;
            if (thingID.StartsWith("t1_"))
            {
                ThingType = AnalyzableTypes.Comment;
            }
            else if (thingID.StartsWith("t3_"))
            {
                ThingType = AnalyzableTypes.Post;
            }
            MediaID = mediaID;
            MediaPlatform = mediaProvider;
            MediaChannelID = mediaChannelID;
            MediaChannelName = mediaChannelName;
            PermaLink = link;
            AnalysisDetails = details;
            SeenByModules = seenByModules;
        }
    }
}

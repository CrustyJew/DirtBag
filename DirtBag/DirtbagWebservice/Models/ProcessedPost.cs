using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtbagWebservice.Models {
    public class ProcessedItem {
        public string SubName { get; set; }
        public string ThingID { get; set; }
        public AnalyzableTypes ThingType { get; set; }
        public string PermaLink { get; set; }
        public string MediaID { get; set; }
        public VideoProvider MediaPlatform { get; set; }
        public string Action { get; set; }
        public Modules.Modules SeenByModules { get; set; }
        public AnalysisDetails AnalysisDetails { get; set; }

        public ProcessedItem() {
            AnalysisDetails = new AnalysisDetails();
        }

        public ProcessedItem( string subName, string thingID, string action, string link, string mediaid, VideoProvider mediaProvider) {
            SubName = subName;
            ThingID = thingID;
            Action = action;
            if (thingID.StartsWith("t1_"))
            {
                ThingType = AnalyzableTypes.Comment;
            }
            else if (thingID.StartsWith("t3_"))
            {
                ThingType = AnalyzableTypes.Post;
            }
            MediaID = mediaid;
            MediaPlatform = mediaProvider;
            PermaLink = link;
            AnalysisDetails = new AnalysisDetails();
        }
    }
}

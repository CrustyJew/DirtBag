using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Models {
    public class ProcessedPost {
        [JsonProperty]
        public string SubName { get; set; }
        [JsonProperty]
        public string PostID { get; set; }
        [JsonProperty]
        public string Action { get; set; }
        [JsonProperty]
        public Modules.Modules SeenByModules { get; set; }
        [JsonProperty]
        public Modules.PostAnalysisResults AnalysisResults { get; set; }

        public ProcessedPost() {

        }

        public ProcessedPost( string subName, string postID, string action ) {
            SubName = subName;
            PostID = postID;
            Action = action;
            AnalysisResults = new Modules.PostAnalysisResults();
        }
    }
}

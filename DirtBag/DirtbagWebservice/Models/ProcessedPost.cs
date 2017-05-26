using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtbagWebservice.Models {
    public class ProcessedItem {
        [JsonProperty]
        public string SubName { get; set; }
        [JsonProperty]
        public string ThingID { get; set; }

        public AnalyzableTypes ThingType { get; set; }
        [JsonProperty]
        public string Action { get; set; }
        [JsonProperty]
        public Modules.Modules SeenByModules { get; set; }
        [JsonProperty]
        public AnalysisDetails AnalysisDetails { get; set; }

        public ProcessedItem() {
            AnalysisDetails = new AnalysisDetails();
        }

        public ProcessedItem( string subName, string thingID, string action, AnalyzableTypes type ) {
            SubName = subName;
            ThingID = thingID;
            Action = action;
            ThingType = type;
            AnalysisDetails = new AnalysisDetails();
        }
    }
}

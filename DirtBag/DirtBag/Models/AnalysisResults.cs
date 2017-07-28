using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dirtbag.Models {
    public class AnalysisResults {
        [JsonConverter( typeof( StringEnumConverter ) )]
        public Action RequiredAction { get; set; }
        public AnalysisDetails AnalysisDetails { get; set; }
        

        public AnalysisResults() {
            AnalysisDetails = new Models.AnalysisDetails();
            RequiredAction = Action.None;
        }

        public enum Action {
            Remove, Report, None
        }
    }
}

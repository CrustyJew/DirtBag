using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Models {
    public class AnalysisResults {
        [JsonConverter( typeof( StringEnumConverter ) )]
        public Action RequiredAction { get; set; }
        public AnalysisDetails AnalysisDetails { get; set; }
        

        public enum Action {
            Remove, Report, Nothing
        }
    }
}

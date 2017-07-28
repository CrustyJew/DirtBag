using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dirtbag.Models
{
    public class AnalysisResponse
    {
        public string SubName { get; set; }
        public string ThingID { get; set; }
        public string Author { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public AnalyzableTypes ThingType { get; set; }
        public string PermaLink { get; set; }
        public string Action { get; set; }
        public double HighScore { get {
                return this.Analysis.Max(( a ) => { return a.TotalScore; });
            }
        }
        [JsonConverter(typeof(StringEnumConverter))]
        public Modules.Modules SeenByModules { get; set; }

        public List<MediaAnalysis> Analysis { get; set; }

        public AnalysisResponse() {
            Analysis = new List<MediaAnalysis>();
        }
    }
}

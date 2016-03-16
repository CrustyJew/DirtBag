using Newtonsoft.Json;

namespace DirtBag.Modules {
	public class AnalysisScore {
        [JsonProperty]
		public double Score { get; set; }
        [JsonProperty]
		public string Reason { get; set; }
        [JsonProperty]
		public string ReportReason { get; set; }
        [JsonProperty]
        public Flair RemovalFlair { get; set; }
        [JsonProperty]
		public string ModuleName { get; set; }
		public AnalysisScore() {
			Score = 0;
			Reason = "";
			ModuleName = "";
		}
		public AnalysisScore(double score, string reason, string reportReason, string moduleName ) {
			Score = score;
			Reason = reason;
			ReportReason = reportReason;
			ModuleName = moduleName;
		}

        public AnalysisScore( double score, string reason, string reportReason, string moduleName, Flair removalFlair )
            :this(score,reason,reportReason,moduleName) {

            RemovalFlair = removalFlair;
        }
		

	}

}

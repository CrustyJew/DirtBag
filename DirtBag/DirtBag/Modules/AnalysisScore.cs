namespace DirtBag.Modules {
	class AnalysisScore {
		public double Score { get; set; }
		public string Reason { get; set; }
		public string ReportReason { get; set; }
		public string ModuleName { get; set; }
		public AnalysisScore() {
			Score = 0;
			Reason = "";
			ModuleName = "";
		}
		public AnalysisScore(double score, string reason,string reportReason, string moduleName ) {
			Score = score;
			Reason = reason;
			ReportReason = reportReason;
			ModuleName = moduleName;
		}
		
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Modules {
	class AnalysisScore {
		public int Score { get; set; }
		public string Reason { get; set; }
		public string ReportReason { get; set; }
		public string ModuleName { get; set; }
		public AnalysisScore() {
			Score = 0;
			Reason = "";
			ModuleName = "";
		}
		public AnalysisScore(int score, string reason,string reportReason, string moduleName ) {
			Score = score;
			Reason = reason;
			ReportReason = reportReason;
			ModuleName = moduleName;
		}
		
	}
}

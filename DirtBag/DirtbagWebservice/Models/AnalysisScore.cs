using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBagWebservice.Models {
    public class AnalysisScore {
        public double Score { get; set; }
        public string Reason { get; set; }
        public string ReportReason { get; set; }
        public Flair RemovalFlair { get; set; }
        public Modules.Modules Module { get; set; }
        public AnalysisScore() {
            Score = 0;
            Reason = "";
        }
        public AnalysisScore( double score, string reason, string reportReason, Modules.Modules module ) {
            Score = score;
            Reason = reason;
            ReportReason = reportReason;
            Module = module;
        }

        public AnalysisScore( double score, string reason, string reportReason, Modules.Modules module, Flair removalFlair )
            : this( score, reason, reportReason, module ) {

            RemovalFlair = removalFlair;
        }


    }
}

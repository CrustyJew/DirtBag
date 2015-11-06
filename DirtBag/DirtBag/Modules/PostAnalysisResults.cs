using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Modules {
	public class PostAnalysisResults {
		public double TotalScore {
			get {
				return Scores.Count > 0 ? Scores.Select( s => s.Score ).Aggregate( ( s, t ) => s + t ) : 0;
			}
		}
		public string ReportReason {
			get {
				string reason = string.Join( ", ", Scores.Select( s => s.ReportReason ) );
				if (reason.Length > 100 ) {
					reason = reason.Substring( 0, 99 ); // just chop it off.. sorry no better way at the moment
				}
				return reason;
			}
		}
		public RedditSharp.Things.Post Post { get; set; }

		public List<AnalysisScore> Scores { get; set; }

		public PostAnalysisResults() {
			Scores = new List<AnalysisScore>();
		}
		public PostAnalysisResults(RedditSharp.Things.Post post ) :this() {
			Post = post;
		}

	}

	
}

using System.Collections.Generic;
using System.Linq;
using RedditSharp.Things;

namespace DirtBag.Modules {
	class PostAnalysisResults {
		public double TotalScore {
			get {
				return Scores.Count > 0 ? Scores.Select( s => s.Score ).Aggregate( ( s, t ) => s + t ) : 0;
			}
		}
		public string ReportReason {
			get {
				var reason = string.Join( ", ", Scores.Select( s => s.ReportReason ) );
				if (reason.Length > 100 ) {
					reason = reason.Substring( 0, 99 ); // just chop it off.. sorry no better way at the moment
				}
				return reason;
			}
		}
        public bool HasFlair {
            get {
                return Scores.Where( f => f.RemovalFlair != null ).Count() > 0;
            }
        }
        public string FlairText {
            get {
                return string.Join( " / ", Scores.Where( f => f.RemovalFlair != null ).Select( f => f.RemovalFlair.Text ).Distinct() );
            }
        }

        public string FlairClass {
            get {
                Flair highestPrio = null;
                foreach(Flair f in Scores.Where( f => f.RemovalFlair != null ).Select(s=>s.RemovalFlair) ) {
                    if ( highestPrio == null || f.Priority < highestPrio.Priority ) highestPrio = f;
                }
                return highestPrio.Class;
            }
        }

        public Post Post { get; set; }

		public List<AnalysisScore> Scores { get; set; }

		public PostAnalysisResults() {
			Scores = new List<AnalysisScore>();
		}
		public PostAnalysisResults(Post post ) :this() {
			Post = post;
		}

	}

	
}

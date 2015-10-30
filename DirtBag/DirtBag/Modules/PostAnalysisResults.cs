using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Modules {
	class PostAnalysisResults {
		public int TotalScore {
			get {
				return Scores.Select( s => s.Score ).Aggregate( ( s, t ) => s + t );
			}
		}
		public RedditSharp.Things.Post Post { get; set; }
		public int MyProperty { get; set; }

		public List<AnalysisScore> Scores { get; set; }

		public PostAnalysisResults() {
			Scores = new List<AnalysisScore>();
		}

	}

	
}

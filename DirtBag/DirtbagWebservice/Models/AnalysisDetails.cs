using Newtonsoft.Json;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtbagWebservice.Models {
    public class AnalysisDetails {

        public double TotalScore
        {
            get
            {
                return Scores.Count > 0 ? Scores.Select( s => s.Score ).Aggregate( ( s, t ) => s + t ) : 0;
            }
        }

        public string ReportReason
        {
            get
            {
                var reason = string.Join( ", ", Scores.Select( s => s.ReportReason ) );
                if ( reason.Length > 100 ) {
                    reason = reason.Substring( 0, 99 ); // just chop it off.. sorry no better way at the moment
                }
                return reason;
            }
        }

        public bool HasFlair
        {
            get
            {
                return Scores.Where( f => f.RemovalFlair != null ).Count() > 0;
            }
        }

        public string FlairText
        {
            get
            {
                return string.Join( " / ", Scores.Where( f => f.RemovalFlair != null ).Select( f => f.RemovalFlair.Text ).Distinct() );
            }
        }


        public string FlairClass
        {
            get
            {
                Flair highestPrio = null;
                foreach ( Flair f in Scores.Where( f => f.RemovalFlair != null ).Select( s => s.RemovalFlair ) ) {
                    if ( highestPrio == null || f.Priority < highestPrio.Priority ) highestPrio = f;
                }
                return highestPrio?.Class;
            }
        }

        [JsonIgnore]
        public Modules.Modules AnalyzingModule { get; set; }

        [JsonProperty]
        public List<AnalysisScore> Scores { get; set; }

        public AnalysisDetails() {
            Scores = new List<AnalysisScore>();
        }

        public AnalysisDetails( string thingID, Modules.Modules module ) : this() {
            ThingID = thingID;
            string lthingID = thingID.ToLower();
            if ( lthingID.StartsWith( "t1_" ) ) {
                ThingType = AnalyzableTypes.Comment;
            }
            else if ( lthingID.StartsWith( "t3_" ) ) {
                ThingType = AnalyzableTypes.Post;
            }
            AnalyzingModule = module;
        }

        public string ThingID { get; set; }
        public Models.AnalyzableTypes ThingType { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Helpers {
    class YouTubeHelpers {
        public static string ExtractVideoID(string url ) {
            string id = null;
            string lowerUrl = url.ToLower();
            if ( lowerUrl.Contains( "youtube" ) ) {
                //it's a YouTube link
                if ( url.Contains( "v=" ) ) {
                    id = url.Substring( url.IndexOf( "v=" ) + 2 ).Split( '&' )[0];
                }
            }
            else if ( lowerUrl.Contains( "youtu.be" ) ) {
                id = url.Substring( url.IndexOf( ".be/" ) + 4 ).Split( '&' )[0];
            }
            return id;
        }
    }
}

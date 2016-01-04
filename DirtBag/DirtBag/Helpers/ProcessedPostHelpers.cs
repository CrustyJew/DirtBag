using Ionic.Zlib;
using Newtonsoft.Json;

namespace DirtBag.Helpers {
    class ProcessedPostHelpers {
        public static byte[] SerializeAndCompressResults( Logging.ProcessedPost post ) {
            if ( post.AnalysisResults == null ) return null;

            string jsonAnalysisResults = JsonConvert.SerializeObject( post.AnalysisResults );
            return ZlibStream.CompressString( jsonAnalysisResults );
        }

        public static Modules.PostAnalysisResults InflateAndDeserializeResults(byte[] compressed ) {
            if ( compressed == null ) return new Modules.PostAnalysisResults();

            string json = ZlibStream.UncompressString( compressed );
            return JsonConvert.DeserializeObject<Modules.PostAnalysisResults>( json );
        }
    }
}

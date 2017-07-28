using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dirtbag
{
    public class UnixTimeStampConverter : JsonConverter {
        public override bool CanConvert( Type objectType ) {
            return objectType == typeof(double) || objectType == typeof(DateTime);
        }

        public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer ) {
            var token = JToken.Load(reader);
            if(reader.TokenType ==  JsonToken.Date) return token.Value<DateTime>();
            return UnixTimeStampToDateTime(token.Value<double?>());
        }

        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer ) {
            writer.WriteValue(value);
        }
        private DateTime? UnixTimeStampToDateTime( double? unixTimeStamp ) {
            // Unix timestamp is seconds past epoch
            if(!unixTimeStamp.HasValue) return null;
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp.Value);
            return dtDateTime;
        }
    }
}

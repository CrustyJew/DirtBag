using System;
using Newtonsoft.Json;

namespace DirtBag {
    [Flags]
    public enum PostType {
        None = 0x0,
        New = 0x1,
        Rising = 0x02,
        Hot = 0x04,
        All = New | Rising | Hot
    }

    internal class PostTypeConverter : JsonConverter {
        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer ) {
            serializer.Serialize( writer, value );
        }

        public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer ) {
			var data = existingValue.ToString();//reader.ReadAsString();//.Load( reader ).ToString();

            PostType result;

            var valid = Enum.TryParse( data, true, out result );

            if ( !valid )
                result = PostType.None;

            return result;
        }

        public override bool CanConvert( Type objectType ) {
            return objectType == typeof( PostType );
        }
    }
}

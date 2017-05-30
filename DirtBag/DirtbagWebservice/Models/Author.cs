using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtbagWebservice.Models {
    public class AuthorInfo {
        public string Name { get; set; }
        [JsonConverter(typeof(UnixTimeStampConverter))]
        public DateTime? Created { get; set; }
        public int? CommentKarma { get; set; }
        public int? LinkKarma { get; set; }

    }
}

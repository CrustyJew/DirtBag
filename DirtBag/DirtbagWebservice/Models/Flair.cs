using Newtonsoft.Json;

namespace DirtBagWebservice.Models {
    public class Flair {
        [JsonProperty]
        public string Text { get; set; }
        [JsonProperty]
        public string Class { get; set; }
        [JsonProperty]
        public int Priority { get; set; }
        public Flair() {

        }
        public Flair(string text, string cssClass, int priority ) {
            Text = text;
            Class = cssClass;
            Priority = priority;
        }
    }

    
}

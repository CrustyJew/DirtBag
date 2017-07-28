using Newtonsoft.Json;

namespace Dirtbag.Models {
    public class Flair {
        public string Text { get; set; }
        public string Class { get; set; }
        public int Priority { get; set; }
        public bool Enabled { get; set; }
        public Flair() {
            Enabled = false;
        }
        public Flair(string text, string cssClass, int priority ) {
            Text = text;
            Class = cssClass;
            Priority = priority;
            Enabled = true;
        }
    }

    
}

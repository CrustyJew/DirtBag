using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag {
    public class Flair {
        public string Text { get; set; }
        public string Class { get; set; }
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

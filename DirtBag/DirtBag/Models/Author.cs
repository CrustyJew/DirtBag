using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Models {
    public class AuthorInfo {
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public int CommentKarma { get; set; }
        public int LinkKarma { get; set; }

    }
}

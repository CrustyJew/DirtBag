using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dirtbag.Models
{
    public class UserPostInfo
    {
        public string ThingID { get; set; }
        public string Username { get; set; }
        public string MediaAuthor { get; set; }
        public string MediaChannelID { get; set; }
        public VideoProvider MediaPlatform { get; set; }
        public string MediaUrl { get; set; }
    }
}

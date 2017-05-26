using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DirtbagWebservice.Models.DAL
{
    public class SpamDetectorModule
    {
        public string Subreddit { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
        public bool Enabled { get; set; }
        public double Weight { get; set; }

        public SpamDetectorModule(string subreddit, Models.YouTubeSpamDetectorModule module) {
            Subreddit = subreddit;
            Name = module.Name;
            Value = module.Value;
            Enabled = module.Enabled;
            Weight = module.Weight;
        }
    }
}

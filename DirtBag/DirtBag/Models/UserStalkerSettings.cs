using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Models {
    public class UserStalkerSettings : IModuleSettings {
        public bool Enabled { get; set; }

        public int EveryXRuns { get; set; }

        public PostType PostTypes { get; set; }

        public double ScoreMultiplier { get; set; }

        public void SetDefaultSettings() {
            Enabled = false;
            EveryXRuns = 1;
            PostTypes = PostType.New;
            ScoreMultiplier = 1;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBagWebservice.Models {
    public class HighTechBanHammerSettings : IModuleSettings {
        public bool Enabled { get; set; }
        public double ScoreMultiplier { get; set; }

        public HighTechBanHammerSettings() {
            SetDefaultSettings();
        }

        public void SetDefaultSettings() {
            Enabled = true;
            ScoreMultiplier = 99;
        }
    }
}

using System;

namespace Dirtbag.Modules {
    [Flags]
    public enum Modules {
        None = 0x00,
        LicensingSmasher = 0x01,
        YouTubeSpamDetector = 0x02,
        UserStalker = 0x04,
        SelfPromotionCombustor = 0x08,
        HighTechBanHammer = 0x10
    }
}

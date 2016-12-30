using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.BLL {
    public class AnalyzePostBLL {
        private BLL.SubredditSettingsBLL subSetsBLL;
        
        public AnalyzePostBLL(BLL.SubredditSettingsBLL settingsBLL ) {
            subSetsBLL = settingsBLL;
        }

        public async Task<DirtBag.Models.AnalysisResults> AnalyzePost(Models.AnalysisRequest request ) {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Dirtbag.Models
{
    public class ReprocessRequest: AnalysisRequest
    {
        public string Subreddit { get; set; }
    }
}

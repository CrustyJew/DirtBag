using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dirtbag.Models
{
    public class RabbitAnalysisRequestMessage : AnalysisRequest
    {
        public string Subreddit { get; set; }
    }
}

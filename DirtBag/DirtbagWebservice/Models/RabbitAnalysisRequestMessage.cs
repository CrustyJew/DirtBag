using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DirtBagWebservice.Models
{
    public class RabbitAnalysisRequestMessage : AnalysisRequest
    {
        public string Subreddit { get; set; }
    }
}

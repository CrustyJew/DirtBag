using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DirtbagWebservice.Models
{
    public class RabbitAnalysisRequestMessage : AnalysisRequest
    {
        public string Subreddit { get; set; }
    }
}

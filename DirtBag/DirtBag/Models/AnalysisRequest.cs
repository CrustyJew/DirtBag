﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Models {
    public class AnalysisRequest {
        public string ThingID { get; set; }
        public AuthorInfo Author { get; set; }
        public DateTime EntryTime { get; set; }
        public string VideoLink { get; set; }

    }
}
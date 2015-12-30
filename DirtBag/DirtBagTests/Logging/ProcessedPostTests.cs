using Microsoft.VisualStudio.TestTools.UnitTesting;
using DirtBag.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Logging.Tests {
    [TestClass()]
    public class ProcessedPostTests {
        [TestMethod()]
        public void SaveProcessedPostTest() {
            ProcessedPost p = new ProcessedPost( "videos", "3ynqhs", "remove" );
            p.AnalysisResults = new Modules.PostAnalysisResults();
            p.AnalysisResults.Scores.Add( new Modules.AnalysisScore( 1, "thisasds totally notasdfadt. nope.. noasdft all", "testing1, or so I'm told", "TestModuleName", new Flair( "flurrr", "red", 1 ) ) );
            p.AnalysisResults.Scores.Add( new Modules.AnalysisScore( 1, "this is totally not a test. nope.. not at all", "testing2, or so I'm told", "TestModuleName", new Flair( "flurrr", "red", 1 ) ) );
            p.AnalysisResults.Scores.Add( new Modules.AnalysisScore( 1, "this asdfy not a test. nope.. not fsda", "testing3, or so I'm told", "TestModuleName", new Flair( "flurrr", "red", 1 ) ) );
            p.AnalysisResults.Scores.Add( new Modules.AnalysisScore( 1, "this is totally not a test. nope.. not at all", "testing4, or so I'm told", "TestModuleName", new Flair( "flurrr", "red", 1 ) ) );
            p.AnalysisResults.Scores.Add( new Modules.AnalysisScore( 1, "thasdfff is totallyvxzst. nope.. not fsfall", "testing5, or so I'm told", "TestModuleName", new Flair( "flurrr", "red", 1 ) ) );

            Logging.ProcessedPost.SaveProcessedPost( p );
            Assert.Fail();
        }
    }
}
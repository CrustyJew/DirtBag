using Microsoft.VisualStudio.TestTools.UnitTesting;
using DirtBag.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Helpers.Tests {
    [TestClass()]
    public class YouTubeHelpersTests {
        [TestMethod()]
        public void ExtractVideoIdTest() {
            var s = YouTubeHelpers.ExtractVideoId( "https://www.youtube.com/watch?v=jVYsjpaaI0Y0" );
            Assert.AreEqual( "jVYsjpaaI0Y", s );
        }

        [TestMethod()]
        public void ExtractVideoIdTest1() {
            var s = YouTubeHelpers.ExtractVideoId( "https%3A%2F%2Fwww.youtube.com%2Fattribution_link%3Fa%3D0-ZqjYHsVIo%26u%3D%252Fwatch%253Fv%253DWjxDUNol8yg%2526feature%253Dshare" );
            Assert.AreEqual( "WjxDUNol8yg", s );
        }
    }
}
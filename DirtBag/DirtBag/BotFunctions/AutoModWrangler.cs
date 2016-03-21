using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.BotFunctions {
    public class AutoModWrangler {
        public RedditSharp.Things.Subreddit SubReddit { get; set; }

        private const string AUTOMOD_WIKI_PAGE = "config/automoderator";
        private const string WIKI_SECTION_START_IDENTIFIER = "#***DIRTBAG BOT SECTION***";
        private const string WIKI_SECTION_END_IDENTIFIER = "#***END DIRTBAG BOT SECTION***";

        private string wikiContent = "";
        private int startBotSection;
        private int botSectionLength;

        public AutoModWrangler() {
        }

        public AutoModWrangler(RedditSharp.Things.Subreddit subReddit ) {
            SubReddit = subReddit;
        }

        public bool AddToBanList( IEnumerable<DirtBag.Models.BannedEntity>) {
            

            return true;
        }

        private bool SaveBanList(IEnumerable<string> entities ) {
            string newWikiContent = wikiContent.Substring( 0, startBotSection - 1 );
        }

        private string GetAutomodConfig() {

            RedditSharp.WikiPage automodWiki;
            try {
                automodWiki = SubReddit.Wiki.GetPage( AUTOMOD_WIKI_PAGE );
                wikiContent = automodWiki.MarkdownContent;
            }
            catch ( WebException ex ) {
                if ( ( ex.Response as HttpWebResponse ).StatusCode == HttpStatusCode.NotFound ) {
                    wikiContent = WIKI_SECTION_START_IDENTIFIER + GetDefaultBotConfigSection() + WIKI_SECTION_END_IDENTIFIER;
                }
                else {
                    throw;
                }
            }

            string botConfigSection = "";
            if ( !wikiContent.Contains( WIKI_SECTION_START_IDENTIFIER ) ) return "";
            if ( !wikiContent.Contains( WIKI_SECTION_END_IDENTIFIER ) ) return "";
            startBotSection = wikiContent.IndexOf( WIKI_SECTION_START_IDENTIFIER ) + WIKI_SECTION_START_IDENTIFIER.Length;
            botSectionLength = wikiContent.IndexOf( WIKI_SECTION_END_IDENTIFIER, startBotSection ) - startBotSection;
            botConfigSection = wikiContent.Substring( startBotSection, botSectionLength );
            return botConfigSection;
        }

        private string GetDefaultBotConfigSection() {
            string config = @"
---
author+media_author_url: [{0}]
action: remove
action_reason: ""Dirtbag Banned Author/Channel : {{match}}""
priority: 9001
---
";
            return config;
        }
    }
}

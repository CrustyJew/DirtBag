using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.BotFunctions {
    public class AutoModWrangler {
        public RedditSharp.Things.Subreddit SubReddit { get; set; }

        private const string AUTOMOD_WIKI_PAGE = "config/automoderator";
        private const string WIKI_SECTION_START_IDENTIFIER = "#***DIRTBAG BOT SECTION***";
        private const string WIKI_SECTION_END_IDENTIFIER = "#***END DIRTBAG BOT SECTION***";
        private const string MATCH_PARAMETER = "author+media_author_url: ";
        public AutoModWrangler() {
        }

        public AutoModWrangler(RedditSharp.Things.Subreddit subReddit ) {
            SubReddit = subReddit;
        }

        public List<string> GetBannedAuthorsAndChannels() {
            string config = GetAutomodConfig();
            int ulistStart = config.IndexOf( MATCH_PARAMETER ) + MATCH_PARAMETER.Length;
            int ulistLen = config.IndexOf( "\r\n", ulistStart ) - ulistStart;
            string ulistRaw = config.Substring( ulistStart, ulistLen );

            //remove square brackets
            ulistRaw = ulistRaw.Substring( 1 );
            ulistRaw = ulistRaw.Substring( 0, ulistRaw.Length - 1 );

            return ulistRaw.Split( ',' ).ToList();
        }

        public string GetAutomodConfig() {

            RedditSharp.WikiPage automodWiki = SubReddit.Wiki.GetPage( AUTOMOD_WIKI_PAGE );

            string botConfigSection = "";
            if ( !automodWiki.MarkdownContent.Contains( WIKI_SECTION_START_IDENTIFIER ) ) return "";
            if ( !automodWiki.MarkdownContent.Contains( WIKI_SECTION_END_IDENTIFIER ) ) return "";
            int startPos = automodWiki.MarkdownContent.IndexOf( WIKI_SECTION_START_IDENTIFIER ) + WIKI_SECTION_START_IDENTIFIER.Length;
            int configLen = automodWiki.MarkdownContent.IndexOf( WIKI_SECTION_END_IDENTIFIER, startPos ) - startPos;
            botConfigSection = automodWiki.MarkdownContent.Substring( startPos, configLen );
            return botConfigSection;
        }

        private string GetDefaultBotConfigSection() {
            string config = @"
---
author+media_author_url: []
action: remove
action_reason: ""Dirtbag Banned Author/Channel : {{match}}""
priority: 9001
---
";
            return config;
        }
    }
}

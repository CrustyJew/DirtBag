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

        public AutoModWrangler( RedditSharp.Things.Subreddit subReddit ) {
            SubReddit = subReddit;
        }

        public async Task<bool> AddToBanList( IEnumerable<DirtBag.Models.BannedEntity> entities ) {
            Logging.BannedEntities bannedEnts = new Logging.BannedEntities();
            await bannedEnts.LogNewBannedEntities( entities );
            string reason = $"Banned {string.Join( ",", entities.Select( e => e.EntityString + ":" + e.BanReason ) )} by {string.Join( ",", entities.Select( e => e.BannedBy ).Distinct() )}";
            if ( reason.Length > 255 ) reason = $"Banned {string.Join( ",", entities.Select( e => e.EntityString ) )} for {string.Join( ",", entities.Select( e => e.BanReason ).Distinct() )} by {string.Join( ",", entities.Select( e => e.BannedBy ).Distinct() )}";
            if ( reason.Length > 255 ) reason = "Banned lots of things and the summary is too long for the description.. RIP";

            bool done = false;
            int count = 1;
            while ( !done && count < 5 ) {
                try {
                    done = await SaveAutoModConfig( reason );
                }
                catch ( WebException ex ) {
                    if ( ( ex.Response as HttpWebResponse ).StatusCode == HttpStatusCode.Forbidden ) {
                        throw;
                    }
                    else count++;
                }

            }

            return true;
        }


        private async Task<bool> SaveAutoModConfig( string reason ) {

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
            string updatedWiki = wikiContent;
            string botConfigSection = "";
            bool noStart = false;
            bool noEnd = false;
            if ( !wikiContent.Contains( WIKI_SECTION_START_IDENTIFIER ) ) noStart = true;
            if ( !wikiContent.Contains( WIKI_SECTION_END_IDENTIFIER ) ) noEnd = true;

            if ( noStart && noEnd ) { wikiContent += $@"
{WIKI_SECTION_START_IDENTIFIER + GetDefaultBotConfigSection() + WIKI_SECTION_END_IDENTIFIER}
"; }
            else if ( noStart || noEnd ) throw new Exception( "Wiki contains a start or an end section, but not both" );

            startBotSection = wikiContent.IndexOf( WIKI_SECTION_START_IDENTIFIER ) + WIKI_SECTION_START_IDENTIFIER.Length;
            botSectionLength = wikiContent.IndexOf( WIKI_SECTION_END_IDENTIFIER, startBotSection ) - startBotSection;
            if ( botSectionLength < 0 ) { throw new Exception( "End section identifier is before the start identifier" ); }
            botConfigSection = wikiContent.Substring( startBotSection, botSectionLength );

            Logging.BannedEntities bannedEnts = new Logging.BannedEntities();
            var ents = await bannedEnts.GetBannedEntities( SubReddit.Name );
            string entsString = string.Join( ", ", ents.Select( e => "\"" + e.EntityString + "\"" ) );
            updatedWiki = updatedWiki.Remove( startBotSection, botSectionLength );
            updatedWiki = updatedWiki.Insert( startBotSection, String.Format( GetDefaultBotConfigSection(), entsString ) );

            SubReddit.Wiki.EditPage( AUTOMOD_WIKI_PAGE, updatedWiki, reason: reason );
            return true;


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

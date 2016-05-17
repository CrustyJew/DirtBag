using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.BotFunctions {
    public class AutoModWrangler {
        public RedditSharp.Things.Subreddit Subreddit { get; set; }

        private const string AUTOMOD_WIKI_PAGE = "config/automoderator";
        private const string WIKI_SECTION_START_IDENTIFIER = "#***DIRTBAG BOT SECTION***";
        private const string WIKI_SECTION_END_IDENTIFIER = "#***END DIRTBAG BOT SECTION***";

        private string wikiContent = "";
        private int startBotSection;
        private int botSectionLength;
        private static MemoryCache cache = MemoryCache.Default;
        private const string CACHE_PREFIX = "BannedEntities:";

        public AutoModWrangler() {
        }

        public AutoModWrangler( RedditSharp.Things.Subreddit subreddit ) {
            Subreddit = subreddit;
        }

        public async Task<bool> AddToBanList( IEnumerable<DirtBag.Models.BannedEntity> entities ) {
            DAL.BannedEntities bannedEntDAL = new DAL.BannedEntities();
            await bannedEntDAL.LogNewBannedEntities( entities );

            var newList = await bannedEntDAL.GetBannedEntities( Subreddit.Name );
            cache.Set( CACHE_PREFIX + Subreddit, newList, DateTimeOffset.Now.AddMinutes( 30 ) );

            var userEntities = entities.Where( e => e.Type == Models.BannedEntity.EntityType.User );
            string reason = $"Banned {string.Join( ",", userEntities.Select( e => e.EntityString + ":" + e.BanReason ) )} by {string.Join( ",", userEntities.Select( e => e.BannedBy ).Distinct() )}";
            if ( reason.Length > 255 ) reason = $"Banned {string.Join( ",", userEntities.Select( e => e.EntityString ) )} for {string.Join( ",", userEntities.Select( e => e.BanReason ).Distinct() )} by {string.Join( ",", userEntities.Select( e => e.BannedBy ).Distinct() )}";
            if ( reason.Length > 255 ) reason = "Banned lots of things and the summary is too long for the description.. RIP";

            bool done = false;
            //only needs to update config if there was a user added
            done = userEntities.Count() <= 0;
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
                    await Task.Delay( 100 );
                }

            }

            return true;
        }

        public async Task<bool> RemoveFromBanList(int id, string modName ) {
            DAL.BannedEntities bannedEntDAL = new DAL.BannedEntities();
            string entName = await bannedEntDAL.RemoveBannedEntity( id, Subreddit.Name, modName );

            var newList = await bannedEntDAL.GetBannedEntities( Subreddit.Name );
            cache.Set( CACHE_PREFIX + Subreddit, newList, DateTimeOffset.Now.AddMinutes( 30 ) );

            bool done = false;
            done = string.IsNullOrWhiteSpace( entName );
            int count = 1;
            while ( !done && count < 5 ) {
                try {
                    done = await SaveAutoModConfig( $"{modName} unbanned {entName}" );
                }
                catch ( WebException ex ) {
                    if ( ( ex.Response as HttpWebResponse ).StatusCode == HttpStatusCode.Forbidden ) {
                        throw;
                    }
                    else count++;
                    await Task.Delay( 100 );
                }

            }
            return true;
        }

        public async Task<bool> UpdateBanReason(int id, string subredditName, string modName, string banReason ) {
            DAL.BannedEntities bannedEntDAL = new DAL.BannedEntities();
            var toReturn = await bannedEntDAL.UpdateBanReason( id, subredditName, modName, banReason );

            var newList = await bannedEntDAL.GetBannedEntities( Subreddit.Name );
            cache.Set( CACHE_PREFIX + Subreddit, newList, DateTimeOffset.Now.AddMinutes( 30 ) );

            return toReturn;
        }

        public async Task<IEnumerable<Models.BannedEntity>> GetBannedList() {
            DAL.BannedEntities bannedEntDAL = new DAL.BannedEntities();
            var cacheVal = cache[CACHE_PREFIX + Subreddit];
            if ( cacheVal == null ) {
                var newList = await bannedEntDAL.GetBannedEntities( Subreddit.Name );
                cache.Set( CACHE_PREFIX + Subreddit, newList, DateTimeOffset.Now.AddMinutes( 30 ) );
                return newList;
            }
            return (IEnumerable<Models.BannedEntity>) cacheVal;
        }

        public async Task<IEnumerable<Models.BannedEntity>> GetBannedList( Models.BannedEntity.EntityType type ) {
            var list = await GetBannedList();
            return list.Where( i => i.Type == type );
        }

        public async Task<bool> SaveAutoModConfig( string editReason ) {

            RedditSharp.WikiPage automodWiki;
            try {
                automodWiki = Subreddit.Wiki.GetPage( AUTOMOD_WIKI_PAGE );
                wikiContent = WebUtility.HtmlDecode( automodWiki.MarkdownContent );
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
            bool noStart = false;
            bool noEnd = false;
            if ( !wikiContent.Contains( WIKI_SECTION_START_IDENTIFIER ) ) noStart = true;
            if ( !wikiContent.Contains( WIKI_SECTION_END_IDENTIFIER ) ) noEnd = true;

            if ( noStart && noEnd ) {
                wikiContent += $@"
{WIKI_SECTION_START_IDENTIFIER + GetDefaultBotConfigSection() + WIKI_SECTION_END_IDENTIFIER}
";
            }

            else if ( noStart || noEnd ) throw new Exception( "Wiki contains a start or an end section, but not both" );

            string updatedWiki = wikiContent;

            startBotSection = wikiContent.IndexOf( WIKI_SECTION_START_IDENTIFIER ) + WIKI_SECTION_START_IDENTIFIER.Length;
            botSectionLength = wikiContent.IndexOf( WIKI_SECTION_END_IDENTIFIER, startBotSection ) - startBotSection;
            if ( botSectionLength < 0 ) { throw new Exception( "End section identifier is before the start identifier" ); }
            botConfigSection = wikiContent.Substring( startBotSection, botSectionLength );


            var ents = await GetBannedList( Models.BannedEntity.EntityType.User );
            string entsString = string.Join( ", ", ents.Select( e => "\"" + e.EntityString + "\"" ) );
            updatedWiki = updatedWiki.Remove( startBotSection, botSectionLength );
            updatedWiki = updatedWiki.Insert( startBotSection, String.Format( GetDefaultBotConfigSection(), entsString ) );

            Subreddit.Wiki.EditPage( AUTOMOD_WIKI_PAGE, updatedWiki, reason: editReason );
            return true;


        }

        private string GetDefaultBotConfigSection() {
            string config = @"
---
author:
    name: [{0}]
action: remove
action_reason: ""Dirtbag Banned Author/Channel : {{{{match}}}}""
priority: 9001
---
";
            return config;
        }
    }
}

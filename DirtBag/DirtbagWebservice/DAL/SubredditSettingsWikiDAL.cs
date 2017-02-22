using DirtBagWebservice.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RedditSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DirtBagWebservice.DAL {
    public class SubredditSettingsWikiDAL : ISubredditSettingsDAL {
        private static string WikiPageName = "dirtbag";
        private Reddit client;
        public SubredditSettingsWikiDAL( Reddit redditClient ) {
            client = redditClient;
        }
        public async Task<Models.SubredditSettings> GetSubredditSettingsAsync( string subreddit ) {
            var wiki = (await client.GetSubredditAsync( subreddit ) ).Wiki;
            
            WikiPage settingsPage;
            try {
                settingsPage = await wiki.GetPageAsync( WikiPageName );
            }
            catch ( WebException ex ) {
                if ( ( ex.Response as HttpWebResponse ).StatusCode == HttpStatusCode.NotFound ) {
                    //Page doesn't exist, create it with defaults.
                    var settings = await CreateWikiPageAsync( wiki );
                    return settings;
                }
                else if ( ( ex.Response as HttpWebResponse ).StatusCode == HttpStatusCode.Unauthorized ) {
                    throw new Exception( "Bot needs wiki permissions yo!" );
                }
                else { //todo retry handling?
                    throw;
                }
            }
            if ( string.IsNullOrEmpty( settingsPage.MarkdownContent ) ) {
                var settings = await CreateWikiPageAsync( wiki );
                return settings;
            }


            Models.SubredditSettings sets;
            try {
                sets = JsonConvert.DeserializeObject<Models.SubredditSettings>( settingsPage.MarkdownContent );
            }
            catch {
                throw new Exception( "Wikipage is corrupted. Fix it, clear wiki page, or delete the page to recreate with defaults." );
            }
            sets.LastModified = settingsPage.RevisionDate.Value;
            var addedDefaults = false;
            /***Module Defaults***/
            if ( sets.LicensingSmasher == null ) {
                sets.LicensingSmasher = new LicensingSmasherSettings();
                addedDefaults = true;
            }
            if ( sets.YouTubeSpamDetector == null ) {
                sets.YouTubeSpamDetector = new YouTubeSpamDetectorSettings();
                addedDefaults = true;
            }
            if ( sets.SelfPromotionCombustor == null ) {
                sets.SelfPromotionCombustor = new SelfPromotionCombustorSettings();
                addedDefaults = true;
            }
            /***End Module Defaults ***/
            if ( addedDefaults ) {
                await wiki.EditPageAsync( WikiPageName, JsonConvert.SerializeObject( this, Formatting.Indented, new StringEnumConverter() ).Replace( "\r\n  ", "\r\n\r\n    " ), reason: "Add module default" );
                
                sets.LastModified = DateTime.UtcNow.AddMinutes( 1 );
            }
            Console.WriteLine( "Settings in wiki changed or read for first time : Revision Date = {0}", sets.LastModified );
            return sets;
        }

        private async Task<Models.SubredditSettings> CreateWikiPageAsync( Wiki wiki ) {
            Models.SubredditSettings settings = new Models.SubredditSettings();
            settings.Version = Program.VersionNumber;
            settings.LastModified = DateTime.UtcNow;
            settings.ReportScoreThreshold = -1;
            settings.RemoveScoreThreshold = -1;
            /*** Module Settings ***/
            settings.LicensingSmasher = new LicensingSmasherSettings();
            settings.YouTubeSpamDetector = new YouTubeSpamDetectorSettings();
            settings.SelfPromotionCombustor = new SelfPromotionCombustorSettings();
            /*** End Module Settings ***/
            await wiki.EditPageAsync( WikiPageName, JsonConvert.SerializeObject( settings, Formatting.Indented, new StringEnumConverter() ).Replace( "\r\n  ", "\r\n\r\n    " ) );
            await wiki.SetPageSettingsAsync( WikiPageName, false, WikiPageSettings.WikiPagePermissionLevel.Mods);
            return settings;
        }
    }
}

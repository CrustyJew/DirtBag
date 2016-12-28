using DirtBag.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RedditSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.DAL {
    public class SubredditSettingsWikiDAL : ISubredditSettingsDAL {
        private static string WikiPageName = "dirtbag";
        private Reddit client;
        public SubredditSettingsWikiDAL( Reddit redditClient ) {
            client = redditClient;
        }
        public async Task<Models.SubredditSettings> GetSubredditSettingsAsync( string subreddit ) {
            var wiki = client.GetSubreddit( subreddit ).Wiki;
            WikiPage settingsPage;
            try {
                settingsPage = wiki.GetPage( WikiPageName );
            }
            catch ( WebException ex ) {
                if ( ( ex.Response as HttpWebResponse ).StatusCode == HttpStatusCode.NotFound ) {
                    //Page doesn't exist, create it with defaults.
                    var settings = await Task.Factory.StartNew( () => CreateWikiPage( wiki ) );
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
                var settings = await Task.Factory.StartNew( () => CreateWikiPage( wiki ) );
                return settings;
            }


            Models.SubredditSettings sets;
            try {
                sets = await Task.Factory.StartNew( () => {
                    return JsonConvert.DeserializeObject<Models.SubredditSettings>( settingsPage.MarkdownContent );
                });
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
            /*if ( sets.UserStalker == null ) {
                UserStalker = new UserStalkerSettings();
                addedDefaults = true;
            }*/
            if ( sets.SelfPromotionCombustor == null ) {
                sets.SelfPromotionCombustor = new SelfPromotionCombustorSettings();
                addedDefaults = true;
            }
            if ( sets.HighTechBanHammer == null ) {
                sets.HighTechBanHammer = new HighTechBanHammerSettings();
                addedDefaults = true;
            }
            /***End Module Defaults ***/
            if ( addedDefaults ) {
                await Task.Factory.StartNew( () => {
                    wiki.EditPage( WikiPageName, JsonConvert.SerializeObject( this, Formatting.Indented, new StringEnumConverter() ).Replace( "\r\n  ", "\r\n\r\n    " ), reason: "Add module default" );
                } );
                sets.LastModified = DateTime.UtcNow.AddMinutes( 1 );
            }
            Console.WriteLine( "Settings in wiki changed or read for first time : Revision Date = {0}", sets.LastModified );
            return sets;
        }

        private Models.SubredditSettings CreateWikiPage( Wiki wiki ) {
            Models.SubredditSettings settings = new Models.SubredditSettings();
            settings.Version = Program.VersionNumber;
            settings.RunEveryXMinutes = 10;
            settings.LastModified = DateTime.UtcNow;
            settings.ReportScoreThreshold = -1;
            settings.RemoveScoreThreshold = -1;
            /*** Module Settings ***/
            settings.LicensingSmasher = new LicensingSmasherSettings();
            settings.YouTubeSpamDetector = new YouTubeSpamDetectorSettings();
            settings.UserStalker = new UserStalkerSettings();
            settings.SelfPromotionCombustor = new SelfPromotionCombustorSettings();
            /*** End Module Settings ***/
            wiki.EditPage( WikiPageName, JsonConvert.SerializeObject( settings, Formatting.Indented, new StringEnumConverter() ).Replace( "\r\n  ", "\r\n\r\n    " ) );
            wiki.SetPageSettings( WikiPageName, new WikiPageSettings { Listed = false, PermLevel = 2 } );

            return settings;
        }
    }
}

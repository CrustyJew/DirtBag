using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using DirtBag.Modules;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RedditSharp;
using System.Net;

namespace DirtBag {
    [Serializable]
    public class BotSettings {
        [JsonIgnore]
        public string Subreddit { get; set; }
        [JsonProperty]
        public double Version { get; set; }
		[JsonProperty]
		[Range(1,9000)]
		public int RunEveryXMinutes { get; set; }
		[JsonProperty]
		public double ReportScoreThreshold { get; set; }
		[JsonProperty]
		public double RemoveScoreThreshold { get; set; }
		[JsonIgnore]
        public DateTime LastModified { get; set; }
        /*** MODULE SETTINGS ***/
        [JsonProperty]
        public LicensingSmasherSettings LicensingSmasher { get; set; }
        [JsonProperty]
        public YouTubeSpamDetectorSettings YouTubeSpamDetector { get; set; }
        [JsonProperty]
        public UserStalkerSettings UserStalker { get; set; }
        /*** END MODULE SETTINGS ***/

        public event EventHandler OnSettingsModified;

        private static string WikiPageName = "dirtbag";
        private const int FortyFiveMinutes = 2700000;

        public void Initialize( Reddit r ) {
            if ( string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["WikiPageName"] ) ) throw new Exception( "Provide setting 'WikiPageName' in AppConfig to store bot settings." );
            WikiPageName = System.Configuration.ConfigurationManager.AppSettings["WikiPageName"];
            LastModified = new DateTime( 1900, 1, 1, 1, 1, 1 );
            var state = new SettingsTimerState
            {
                RedditRef = r,
                SettingsRef = this
            };
            state.TimerRef = new Timer( SettingsTimer, state, 0, FortyFiveMinutes );
        }
        public void ReadSettings( Reddit r ) {
            var wiki = r.GetSubreddit( Subreddit ).Wiki;
            WikiPage settingsPage;
            try {
                settingsPage = wiki.GetPage( WikiPageName );
            }
            catch(WebException ex) {
                if ( ( ex.Response as HttpWebResponse ).StatusCode == HttpStatusCode.NotFound ) {
                    //Page doesn't exist, create it with defaults.
                    CreateWikiPage( wiki );
                    OnSettingsModified?.Invoke( this, EventArgs.Empty );
                    return;
                }
                else if ( ( ex.Response as HttpWebResponse ).StatusCode == HttpStatusCode.Unauthorized){
                    throw new Exception( "Bot needs wiki permissions yo!" );
                }
                else { //todo retry handling?
                    throw;
                }
            }
            if ( string.IsNullOrEmpty( settingsPage.MarkdownContent ) ) {
                CreateWikiPage( wiki );
                OnSettingsModified?.Invoke( this, EventArgs.Empty );
                return;
            }

            if ( settingsPage.RevisionDate != null && settingsPage.RevisionDate.Value > LastModified ) {
                BotSettings sets;
                try {
                    sets = JsonConvert.DeserializeObject<BotSettings>( settingsPage.MarkdownContent );
                }
                catch {
                    throw new Exception( "Wikipage is corrupted. Fix it, clear wiki page, or delete the page to recreate with defaults." );
                }
                Version = sets.Version;
                LastModified = settingsPage.RevisionDate.Value;
				RemoveScoreThreshold = sets.RemoveScoreThreshold;
				ReportScoreThreshold = sets.ReportScoreThreshold;
				RunEveryXMinutes = sets.RunEveryXMinutes;


                var addedDefaults = false;
                /***Module Defaults***/
                if ( sets.LicensingSmasher == null ) {
                    LicensingSmasher = new LicensingSmasherSettings();
                    addedDefaults = true;
                }
				else {
					LicensingSmasher = sets.LicensingSmasher;
				}
                if (sets.YouTubeSpamDetector == null ) {
                    YouTubeSpamDetector = new YouTubeSpamDetectorSettings();
                    addedDefaults = true;
                }
                else {
                    YouTubeSpamDetector = sets.YouTubeSpamDetector;
                }
                if(sets.UserStalker == null ) {
                    UserStalker = new UserStalkerSettings();
                    addedDefaults = true;
                }
                else {
                    UserStalker = sets.UserStalker;
                }
                /***End Module Defaults ***/
                if ( addedDefaults ) {
                    wiki.EditPage( WikiPageName, JsonConvert.SerializeObject( this, Formatting.Indented, new StringEnumConverter()).Replace( "\r\n  ", "\r\n\r\n    " ), reason: "Add module default" );
                    LastModified = DateTime.UtcNow.AddMinutes( 1 );
                }
                Console.WriteLine("Settings in wiki changed or read for first time : Revision Date = {0}", LastModified);
                OnSettingsModified?.Invoke( this, EventArgs.Empty );
            }
            else {
                Console.WriteLine( "No updates to settings detected in wiki" );
            }


        }

        private void CreateWikiPage( Wiki wiki ) {
            Version = Program.VersionNumber;
			RunEveryXMinutes = 10;
            LastModified = DateTime.UtcNow;
			ReportScoreThreshold = -1;
			RemoveScoreThreshold = -1;
			/*** Module Settings ***/
			LicensingSmasher = new LicensingSmasherSettings();
            YouTubeSpamDetector = new YouTubeSpamDetectorSettings();
            UserStalker = new UserStalkerSettings();
            /*** End Module Settings ***/
            wiki.EditPage( WikiPageName, JsonConvert.SerializeObject( this, Formatting.Indented, new StringEnumConverter()).Replace("\r\n  ","\r\n\r\n    ") );
            wiki.SetPageSettings( WikiPageName, new WikiPageSettings { Listed = false, PermLevel = 2 } );
        }
        private static void SettingsTimer( object s ) {
            var state = (SettingsTimerState) s;
            Console.WriteLine( "Checking settings from wiki" );
            state.SettingsRef.ReadSettings( state.RedditRef );
        }
        private class SettingsTimerState {
            public Timer TimerRef { get; set; }
            public Reddit RedditRef { get; set; }
            public BotSettings SettingsRef { get; set; }
        }

    }
}

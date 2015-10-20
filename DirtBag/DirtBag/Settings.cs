using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
namespace DirtBag {
    [Serializable]
    public class BotSettings {
        [JsonIgnore]
        public string Subreddit { get; set; }
        [JsonProperty]
        public double Version { get; set; }
        [JsonProperty]
        public List<string> Modules { get; set; }
        [JsonIgnore]
        public DateTime LastModified { get; set; }
        /*** MODULE SETTINGS ***/
        [JsonProperty]
        public Modules.LicensingSmasherSettings LicensingSmasher { get; set; }

        /*** END MODULE SETTINGS ***/

        public event EventHandler OnSettingsModified;

        private const string WIKIPAGE_NAME = "codenamedirtbag";
        private const int FORTY_FIVE_MINUTES = 2700000;

        public void Initialize( RedditSharp.Reddit r ) {
            LastModified = new DateTime( 1900, 1, 1, 1, 1, 1 );
            SettingsTimerState state = new SettingsTimerState();
            state.RedditRef = r;
            state.SettingsRef = this;
            state.TimerRef = new Timer( SettingsTimer, state, 0, FORTY_FIVE_MINUTES );
        }
        public void ReadSettings( RedditSharp.Reddit r ) {
            RedditSharp.Wiki wiki = r.GetSubreddit( Subreddit ).Wiki;
            RedditSharp.WikiPage settingsPage;
            try {
                settingsPage = wiki.GetPage( WIKIPAGE_NAME );
            }
            catch {
                //Page doesn't exist, create it with defaults.
                CreateWikiPage( wiki );
                return;
            }
            if ( string.IsNullOrEmpty( settingsPage.MarkdownContent ) ) {
                CreateWikiPage( wiki );
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
                this.Modules = sets.Modules;
                this.Version = sets.Version;
                this.LastModified = settingsPage.RevisionDate.Value;

                bool addedDefaults = false;
                /***Module Defaults***/
                if ( this.LicensingSmasher == null ) {
                    LicensingSmasher = new Modules.LicensingSmasherSettings();
                    addedDefaults = true;
                }
                /***End Module Defaults ***/
                if ( addedDefaults ) {
                    wiki.EditPage( WIKIPAGE_NAME, JsonConvert.SerializeObject( this ), reason: "Add module default" );
                    this.LastModified = DateTime.UtcNow.AddMinutes( 1 );
                }
                System.Diagnostics.Debug.WriteLine( string.Format( "Settings in wiki changed or read for first time : Revision Date = {0}", LastModified ) );
                if ( OnSettingsModified != null ) {
                    OnSettingsModified( this, EventArgs.Empty );
                }
            }
            else {
                System.Diagnostics.Debug.WriteLine( "No updates to settings detected in wiki" );
            }


        }

        private void CreateWikiPage( RedditSharp.Wiki wiki ) {
            Version = Program.VersionNumber;
            Modules = new List<string>();
            LastModified = DateTime.UtcNow;
            /*** Module Settings ***/
            LicensingSmasher = new Modules.LicensingSmasherSettings();
            /*** End Module Settings ***/
            wiki.EditPage( WIKIPAGE_NAME, JsonConvert.SerializeObject( this ) );
        }
        private void SettingsTimer( object s ) {
            SettingsTimerState state = (SettingsTimerState) s;
            System.Diagnostics.Debug.WriteLine( "Checking settings from wiki" );
            state.SettingsRef.ReadSettings( state.RedditRef );
        }
        private class SettingsTimerState {
            public Timer TimerRef { get; set; }
            public RedditSharp.Reddit RedditRef { get; set; }
            public BotSettings SettingsRef { get; set; }
        }

    }
}

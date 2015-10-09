using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using RedditSharp;
namespace DirtBag {
	class RedditAuth {

		public string AccessToken { get; private set; }

		private string uname;
		private string pass;
		private string clientID;
		private string clientSecret;

		private TimerState timerState;

		private const int FIFTYFIVE_MINUTES = 3300000;
		
		public RedditAuth() {
			uname = ConfigurationManager.AppSettings["BotUsername"];
			pass = ConfigurationManager.AppSettings["BotPassword"];
			clientID = ConfigurationManager.AppSettings["ClientID"];
			clientSecret = ConfigurationManager.AppSettings["ClientSecret"];
			if ( string.IsNullOrEmpty( uname ) ) throw new Exception( "Missing 'BotUsername' in config" );
			if ( string.IsNullOrEmpty( pass ) ) throw new Exception( "Missing 'BotPassword' in config" );
			if ( string.IsNullOrEmpty( clientID ) ) throw new Exception( "Missing 'ClientID' in config" );
			if ( string.IsNullOrEmpty( clientSecret ) ) throw new Exception( "Missing 'ClientSecret' in config" );
			timerState = new TimerState();
		}
		public void GetNewToken() {
			try {
				AuthProvider ap = new AuthProvider( clientID, clientSecret, "/" );
				AccessToken = ap.GetOAuthToken( uname, pass );
			}
			catch { //TODO error handling
				timerState.TimerRunning = false;
				throw;
			}
        }

		public void Login() {
			timerState.TimerRunning = true;
			timerState.TimerRef = new System.Threading.Timer( new System.Threading.TimerCallback( RefreshTokenTimer ), timerState, 0, FIFTYFIVE_MINUTES );
          
		}

		private void RefreshTokenTimer(object s ) {
			TimerState state = (TimerState) s;
			if ( !state.TimerRunning ) {
				state.TimerRef.Dispose();
				System.Diagnostics.Debug.WriteLine( "Timer stopping" );
			}
			else {
				GetNewToken();
			}
		}

		private class TimerState {
			public bool TimerRunning;
			public System.Threading.Timer TimerRef;
		}

		
	}
}

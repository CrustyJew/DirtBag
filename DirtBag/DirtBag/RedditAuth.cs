using System;
using System.Configuration;
using System.Threading;
using RedditSharp;

namespace DirtBag {
    class RedditAuth {

        public string AccessToken { get; private set; }

        private readonly string _uname;
        private readonly string _pass;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;
        private readonly TimerState _timerState;
        private readonly IWebAgent _webAgent;

        private const int FiftyfiveMinutes = 3300000;

        public RedditAuth(IWebAgent agent) {
            _uname = ConfigurationManager.AppSettings["BotUsername"];
            _pass = ConfigurationManager.AppSettings["BotPassword"];
            _clientId = ConfigurationManager.AppSettings["ClientID"];
            _clientSecret = ConfigurationManager.AppSettings["ClientSecret"];
            _redirectUri = ConfigurationManager.AppSettings["RedirectURI"];
            if ( string.IsNullOrEmpty( _uname ) ) throw new Exception( "Missing 'BotUsername' in config" );
            if ( string.IsNullOrEmpty( _pass ) ) throw new Exception( "Missing 'BotPassword' in config" );
            if ( string.IsNullOrEmpty( _clientId ) ) throw new Exception( "Missing 'ClientID' in config" );
            if ( string.IsNullOrEmpty( _clientSecret ) ) throw new Exception( "Missing 'ClientSecret' in config" );
            if ( string.IsNullOrEmpty( _redirectUri ) ) throw new Exception( "Missing 'RedirectURI' in config" );
            _webAgent = agent;
            _timerState = new TimerState();
        }
        public void GetNewToken() {
            try {
                var ap = new AuthProvider( _clientId, _clientSecret, _redirectUri, _webAgent );
                AccessToken = ap.GetOAuthToken( _uname, _pass );
                _webAgent.AccessToken = AccessToken;
            }
            catch { //TODO error handling
                _timerState.TimerRunning = false;
                throw;
            }
        }

        public void Login() {
            _timerState.TimerRunning = true;
            _timerState.TimerRef = new Timer( RefreshTokenTimer, _timerState, FiftyfiveMinutes, FiftyfiveMinutes );
            GetNewToken();
        }

        private void RefreshTokenTimer( object s ) {
            var state = (TimerState) s;
            if ( !state.TimerRunning ) {
                state.TimerRef.Dispose();
                Console.WriteLine( "Timer stopping" );
            }
            else {
                GetNewToken();
            }
        }

        private class TimerState {
            public bool TimerRunning;
            public Timer TimerRef;
        }


    }
}

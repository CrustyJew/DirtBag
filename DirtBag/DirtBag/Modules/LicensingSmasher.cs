using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Google.Apis.YouTube.v3;
using System.Text.RegularExpressions;

namespace DirtBag.Modules {
	class LicensingSmasher {
		public bool IsRunning { get; set; }
		public RedditSharp.Reddit RedditClient { get; set; }
		public string Subreddit { get; set; }

		private static Regex VideoID = new Regex(@"?: youtube\.com/(?:(?:watch|attribution_link)\?(?:.*(?:&|%3F|&amp;))?v(?:=|%3D)|embed/)|youtu\.be/)([a-zA-Z0-9-_]{11}");
		private void NewPostLicensingTimer( object s ) {
			LicensingTimerState state = (LicensingTimerState) s;
			RedditSharp.Things.Subreddit sub = state.RedditRef.GetSubreddit( state.Smasher.Subreddit );
			List<RedditSharp.Things.Post> newPosts = sub.New.Take(25).ToList();

			foreach(RedditSharp.Things.Post post in newPosts ) {
				if(post.Url.Host.Contains("youtube") || post.Url.Host.Contains( "youtu.bu" )){
					//it's a YouTube vid
					Google.Apis.YouTube.v3.YouTubeService yt = new YouTubeService();
					var req = yt.Videos.List( "snippet" );
					req.Id = VideoID.Match( post.Url.ToString() ).Value;
					var response = req.Execute();
					//response.Items.First().
				}
			}
		}
		private class LicensingTimerState {
			public LicensingSmasher Smasher { get; set; }
			public Timer TimerRef { get; set; }
			public RedditSharp.Reddit RedditRef { get; set; }
		}
	}
}

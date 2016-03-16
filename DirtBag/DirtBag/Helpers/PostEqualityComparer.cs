using System.Collections.Generic;
using RedditSharp.Things;

namespace DirtBag.Helpers {
	class PostIdEqualityComparer : IEqualityComparer<Post> {
		public bool Equals( Post x, Post y ) {
			return x.Id == y.Id;
		}

		public int GetHashCode( Post obj ) {
			return obj.Id.GetHashCode();
		}
	}
}

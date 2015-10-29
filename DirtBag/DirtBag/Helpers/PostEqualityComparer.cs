using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedditSharp.Things;

namespace DirtBag.Helpers {
	class PostIdEqualityComparer : IEqualityComparer<RedditSharp.Things.Post> {
		public bool Equals( Post x, Post y ) {
			return x.Id == y.Id;
		}

		public int GetHashCode( Post obj ) {
			return obj.Id.GetHashCode();
		}
	}
}

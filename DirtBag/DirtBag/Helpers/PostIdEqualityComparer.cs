using System.Collections.Generic;
using RedditSharp.Things;

namespace Dirtbag.Helpers
{
    public class PostIdEqualityComparer : IEqualityComparer<Post> {
        public bool Equals( Post x, Post y ) {
            return x.Id == y.Id;
        }

        public int GetHashCode( Post obj ) {
            return obj.Id.GetHashCode();
        }
    }
}

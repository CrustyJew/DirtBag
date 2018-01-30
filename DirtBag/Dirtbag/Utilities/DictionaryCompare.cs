using System.Collections.Generic;
using System.Linq;
namespace Dirtbag {
    //http://stackoverflow.com/a/3928856/2452505
    public static partial class Utilities
    {
        public static bool ListKVPEqual<TKey, TValue>(
    this List<KeyValuePair<TKey, TValue>> first, List<KeyValuePair<TKey, TValue>> second ) {
            return first.ListKVPEqual( second, null );
        }

        public static bool ListKVPEqual<TKey, TValue>(
            this List<KeyValuePair<TKey, TValue>> first, List<KeyValuePair<TKey, TValue>> second,
            IEqualityComparer<TValue> valueComparer ) {
            if ( first == second ) return true;
            if ( ( first == null ) || ( second == null ) ) return false;
            if ( first.Count != second.Count ) return false;

            valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;

            foreach ( var kvp in first ) {
                int secondCount = second.Where(kv => kv.Key.Equals(kvp.Key) && valueComparer.Equals(kv.Value, kvp.Value)).Count();
                int origCount = first.Where(kv => kv.Key.Equals(kvp.Key) && valueComparer.Equals(kv.Value,kvp.Value)).Count();
                if(secondCount != origCount) return false;
            }
            return true;
        }
    }
}

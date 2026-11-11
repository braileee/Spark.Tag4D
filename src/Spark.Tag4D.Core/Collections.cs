using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Spark.Tag4D.Core
{
    public static class CollectionExtensions
    {
        /// <summary>Appends every item of <paramref name="items"/> to the collection.</summary>
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                collection.Add(item);
            }
        }
    }
}

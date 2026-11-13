using Spark.Tag4D.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Tag4D.Settings.Services
{
    /// <summary>The properties that were added to / removed from the Properties sheet by one save.</summary>
    public sealed class ModelSyncChange
    {
        public List<PropertySetItem> Added { get; } = new List<PropertySetItem>();

        public List<PropertySetItem> Removed { get; } = new List<PropertySetItem>();

        public int AddedCount => Added.Sum(set => set.PropertyItems.Count);

        public int RemovedCount => Removed.Sum(set => set.PropertyItems.Count);

        public bool IsEmpty => AddedCount == 0 && RemovedCount == 0;

        /// <summary>Compares two versions of the Properties sheet by property name (case-insensitive).</summary>
        public static ModelSyncChange Between(IEnumerable<PropertySetItem> before, IEnumerable<PropertySetItem> after)
        {
            var change = new ModelSyncChange();
            Collect(after, before, change.Added);
            Collect(before, after, change.Removed);
            return change;
        }

        private static void Collect(IEnumerable<PropertySetItem> from, IEnumerable<PropertySetItem> other, List<PropertySetItem> result)
        {
            var known = new HashSet<string>(
                other.SelectMany(set => set.PropertyItems).Select(item => item.PropertyName),
                StringComparer.OrdinalIgnoreCase);

            foreach (PropertySetItem set in from)
            {
                var items = set.PropertyItems
                    .Where(item => !string.IsNullOrWhiteSpace(item.PropertyName) && !known.Contains(item.PropertyName))
                    .Select(item => new PropertyItem { PropertyName = item.PropertyName, DataType = item.DataType })
                    .ToList();

                if (items.Count > 0)
                {
                    result.Add(new PropertySetItem { PropertySetName = set.PropertySetName, PropertyItems = items });
                }
            }
        }
    }

    /// <summary>
    /// Mirrors changes of the Properties sheet in the open model (host application). Implemented by the host add-in;
    /// both methods may complete later (host API calls run on the host's own schedule).
    /// </summary>
    public interface IModelSynchronizer
    {
        /// <summary>Short name of the model, for messages (for example "Revit project").</summary>
        string ModelName { get; }

        /// <summary>Creates the added and deletes the removed properties. <paramref name="completed"/> gets true on success.</summary>
        void Apply(ModelSyncChange change, Action<bool> completed);

        /// <summary>Undoes <see cref="Apply"/>: deletes what was added and creates what was removed.</summary>
        void Rollback(ModelSyncChange change, Action<bool> completed);
    }
}

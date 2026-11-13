using Autodesk.Revit.DB;
using Spark.Tag4D.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Revit.App.Tag
{
    public class TagOptions
    {
        public List<PropertyField> ChangedFields { get; set; } = new List<PropertyField>();
        public List<Element> Elements { get; set; } = new List<Element>();
        public List<ExpressionRule> Expressions { get; set; } = new List<ExpressionRule>();
        public ObservableCollection<PropertyField> Fields { get; internal set; } = new ObservableCollection<PropertyField>();
        public List<ComposedPropertyItem> ComposedItems { get; set; } = new List<ComposedPropertyItem>();
        public List<PropertyValueMatch> PropertyValueExactMatches { get; set; } = new List<PropertyValueMatch>();
        public bool UpdateAllValues { get; internal set; }

        /// <summary>Called with true while the handler writes values (so the pane does not treat them as user edits), false when done.</summary>
        public Action<bool> Syncing { get; set; }
    }
}

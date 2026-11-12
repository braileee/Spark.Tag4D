using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Revit.Core
{
    public static class DocumentExtensions
    {
        public static CategorySet GetUsedBoundModelCategories(this Document doc, List<BuiltInCategory> excludedCategories = null)
        {
            var categorySet = new CategorySet();

            // Collect all elements that belong to a category
            var collector = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .Where(e => e.Category != null && 
                e.Category.CategoryType == CategoryType.Model && 
                e.Category.AllowsBoundParameters);

            foreach (var e in collector)
            {
                if(excludedCategories != null)
                {
#if REVIT2024 || REVIT2025 || REVIT2026
                    BuiltInCategory builtInCategory = (BuiltInCategory)e.Category.Id.Value;
#else
                    BuiltInCategory builtInCategory = (BuiltInCategory)e.Category.Id.IntegerValue;
#endif

                    if (excludedCategories.Contains(builtInCategory))
                    {
                        continue;
                    }
                }

                categorySet.Insert(e.Category);
            }

            return categorySet;
        }
    }
}

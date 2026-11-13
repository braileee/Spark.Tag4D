using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Spark.Tag4D.Revit.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Tag4D.Revit.App.Review
{
    /// <summary>Finds the model elements that have an empty value in one of the chosen parameters and isolates them in a 3D view.</summary>
    public class ReviewEventHandler : RevitEventWrapper<ReviewOptions>
    {
        public const string ViewName = "Tag 4D - Review";

        public override void Execute(UIApplication app, ReviewOptions options)
        {
            try
            {
                UIDocument uiDocument = app.ActiveUIDocument;
                if (uiDocument == null)
                {
                    options.OnResult?.Invoke("Open a Revit project first.");
                    return;
                }

                Document document = uiDocument.Document;

                if (options.Operation == ReviewOperation.Reset)
                {
                    Reset(document, options);
                    return;
                }

                Show(uiDocument, document, options);
            }
            catch (Exception exception)
            {
                options.OnResult?.Invoke($"Failed: {exception.Message}");
            }
        }

        private static void Show(UIDocument uiDocument, Document document, ReviewOptions options)
        {
            List<ElementId> ids = FindElementsWithMissingValues(document, options.PropertyNames);

            if (ids.Count == 0)
            {
                options.OnResult?.Invoke("No elements with missing values for the selected parameters.");
                return;
            }

            View3D view = GetOrCreateView(document);

            using (Transaction transaction = new Transaction(document, "Isolate elements with missing values"))
            {
                transaction.Start();
                view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                view.IsolateElementsTemporary(ids);
                transaction.Commit();
            }

            uiDocument.ActiveView = view;
            uiDocument.Selection.SetElementIds(ids);

            options.OnResult?.Invoke($"{ids.Count} elements with missing values are isolated in the view '{ViewName}'.");
        }

        private static void Reset(Document document, ReviewOptions options)
        {
            View3D view = FindView(document);
            if (view == null)
            {
                options.OnResult?.Invoke("Nothing to reset.");
                return;
            }

            using (Transaction transaction = new Transaction(document, "Reset isolation"))
            {
                transaction.Start();
                view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                transaction.Commit();
            }

            options.OnResult?.Invoke("The isolation was removed.");
        }

        /// <summary>One checked parameter, resolved once per project instead of once per element.</summary>
        private sealed class Target
        {
            public string Name;
            public Guid? Guid;
        }

        private static List<ElementId> FindElementsWithMissingValues(Document document, List<string> names)
        {
            // Which checked parameters are bound to which categories (the project bindings are the only place that says so).
            var byCategory = new Dictionary<ElementId, List<Target>>();
            var wanted = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

            DefinitionBindingMapIterator iterator = document.ParameterBindings.ForwardIterator();
            iterator.Reset();
            while (iterator.MoveNext())
            {
                Definition definition = iterator.Key;
                if (definition == null || !wanted.Contains(definition.Name) || !(iterator.Current is ElementBinding binding))
                {
                    continue;
                }

                var target = new Target { Name = definition.Name, Guid = (definition as ExternalDefinition)?.GUID };

                foreach (Category category in binding.Categories)
                {
                    if (category.CategoryType != CategoryType.Model)
                    {
                        continue;
                    }

                    if (!byCategory.TryGetValue(category.Id, out List<Target> targets))
                    {
                        byCategory[category.Id] = targets = new List<Target>();
                    }

                    targets.Add(target);
                }
            }

            var result = new List<ElementId>();
            if (byCategory.Count == 0)
            {
                return result;
            }

            // Only the bound categories are read, and only the parameters bound to the category of each element.
            var elements = new FilteredElementCollector(document)
                .WherePasses(new ElementMulticategoryFilter(byCategory.Keys.ToList()))
                .WhereElementIsNotElementType();

            foreach (Element element in elements)
            {
                Category category = element.Category;
                if (category == null || !byCategory.TryGetValue(category.Id, out List<Target> targets))
                {
                    continue;
                }

                foreach (Target target in targets)
                {
                    Parameter parameter = target.Guid.HasValue ? element.get_Parameter(target.Guid.Value) : element.LookupParameter(target.Name);

                    // An element that does not have the parameter at all is not "missing a value".
                    if (parameter != null && IsMissing(parameter))
                    {
                        result.Add(element.Id);
                        break;
                    }
                }
            }

            return result;
        }

        private static bool IsMissing(Parameter parameter)
        {
            if (!parameter.HasValue)
            {
                return true;
            }

            return parameter.StorageType == StorageType.String && string.IsNullOrWhiteSpace(parameter.AsString());
        }

        private static View3D FindView(Document document)
        {
            return new FilteredElementCollector(document)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .FirstOrDefault(view => !view.IsTemplate && view.Name == ViewName);
        }

        private static View3D GetOrCreateView(Document document)
        {
            View3D view = FindView(document);
            if (view != null)
            {
                return view;
            }

            ViewFamilyType type = new FilteredElementCollector(document)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .First(item => item.ViewFamily == ViewFamily.ThreeDimensional);

            using (Transaction transaction = new Transaction(document, "Create the missing values view"))
            {
                transaction.Start();
                view = View3D.CreateIsometric(document, type.Id);
                view.Name = ViewName;
                transaction.Commit();
            }

            return view;
        }
    }
}

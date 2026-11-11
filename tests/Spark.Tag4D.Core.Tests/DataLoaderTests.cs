using System;
using System.IO;
using System.Linq;
using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Xunit;

namespace Spark.Tag4D.Core.Tests
{
    public class DataLoaderTests : IDisposable
    {
        private readonly string _workbookPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xlsx");
        private readonly SettingsRoot _settings;

        public DataLoaderTests()
        {
            File.Copy(WorkbookTests.SamplePath(), _workbookPath);
            _settings = SettingsLoader.Read(Path.Combine(AppContext.BaseDirectory, "Files", "Tag4DFiles", "Settings.json"));
        }

        public void Dispose() => File.Delete(_workbookPath);

        [Fact]
        public void Shipped_settings_file_is_readable_despite_trailing_commas()
        {
            Assert.Equal("Properties", _settings.ExcelSettings.PropertiesSheet.SheetName);
            Assert.Equal(new[] { "LocationRoot Match", "SpaceID Match" }, _settings.ExcelSettings.PropertyMatchSheet.SheetNames.ToArray());
            Assert.Equal("4DGroup Exact Match", _settings.ExcelSettings.PropertyExactMatchSheet.SheetNames.Single());
        }

        [Fact]
        public void Loads_property_sets_grouped_by_set_name()
        {
            var sets = ExcelDataLoader.LoadPropertySetItems(_workbookPath, _settings.ExcelSettings.PropertiesSheet);

            Assert.NotEmpty(sets);
            Assert.Contains(sets, s => s.PropertySetName == "PropertySet_3L_Identity"
                                       && s.PropertyItems.Any(p => p.PropertyName == "LocationRoot" && p.DataType == "string"));
        }

        [Fact]
        public void Property_names_are_sorted()
        {
            var names = ExcelDataLoader.GetPropertyNames(_workbookPath, _settings.ExcelSettings.PropertiesSheet);
            Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal).ToList(), names);
            Assert.Contains("LocationRoot", names);
        }

        [Fact]
        public void Loads_picklists_layers_matches_expressions_and_composed()
        {
            var excel = _settings.ExcelSettings;

            var picklists = ExcelDataLoader.LoadPicklistGroups(_workbookPath, excel.PicklistSheet);
            Assert.Contains(picklists, g => g.GroupName == "LocationRoot" && g.Values.Count > 0);

            var layers = ExcelDataLoader.ReadLayerMappings(_workbookPath, excel.LayersMappingSheet);
            Assert.Contains(layers, l => l.LayerName == "TA_-_---_-----" && l.PropertiesWithValues["ParentTypeGroup"] == "DRN");

            var matches = ExcelDataLoader.LoadPropertiesValueMatches(_workbookPath, excel.PropertyMatchSheet);
            Assert.Contains(matches, m => m.PropertyNameTarget == "SpaceID" && m.PropertyNameAndValuesSource["LocationRoot"] == "PS-N");

            var exact = ExcelDataLoader.LoadPropertiesExactValueMatches(_workbookPath, excel.PropertyExactMatchSheet);
            Assert.Contains(exact, m => m.PropertyNameTarget == "4DGroup" && m.PropertyValueTarget == "EAR_EARTHWORKS");

            var rules = ExcelDataLoader.LoadExpressions(_workbookPath, excel.ExpressionSheet);
            Assert.Contains(rules, r => r.SourcePropertyName == "TypeID" && r.Function == ".Split('.')[0]" && r.TargetPropertyName == "ParentTypeGroup");

            var composed = ExcelDataLoader.LoadComposed(_workbookPath, excel.ComposedSheet);
            Assert.Contains(composed, c => c.ComposedPropertyName == "WBSID"
                                           && c.Formula == "<LocationRoot>.<SubLocation>.<OrientationID>");
        }

        [Fact]
        public void Saved_data_can_be_loaded_again()
        {
            var excel = _settings.ExcelSettings;

            var rules = new[]
            {
                new ExpressionRule { SourcePropertyName = "A", Function = ".Trim()", TargetPropertyName = "B" }
            };
            ExcelDataLoader.SaveRules(_workbookPath, excel.ExpressionSheet, rules);
            var loadedRules = ExcelDataLoader.LoadExpressions(_workbookPath, excel.ExpressionSheet);
            Assert.Single(loadedRules);
            Assert.Equal(".Trim()", loadedRules[0].Function);

            var picklists = new System.Collections.Generic.List<PicklistGroup>
            {
                new PicklistGroup { GroupName = "G1", Values = { "b", "a" } },
                new PicklistGroup { GroupName = "G2", Values = { "x" } }
            };
            ExcelDataLoader.SavePicklists(_workbookPath, excel.PicklistSheet.SheetName, picklists);
            var loadedPicklists = ExcelDataLoader.LoadPicklistGroups(_workbookPath, excel.PicklistSheet);
            Assert.Equal(new[] { "G1", "G2" }, loadedPicklists.Select(g => g.GroupName).ToArray());
            Assert.Equal(new[] { "a", "b" }, loadedPicklists[0].Values.ToArray());

            // Other sheets are untouched.
            Assert.NotEmpty(ExcelDataLoader.ReadLayerMappings(_workbookPath, excel.LayersMappingSheet));
        }

        [Fact]
        public void Composed_formulas_resolve_known_placeholders_only()
        {
            var item = new ComposedPropertyItem { ComposedPropertyName = "X", Formula = "<A>.<b>.<Missing>" };
            var values = new System.Collections.Generic.Dictionary<string, string?> { ["A"] = "1", ["B"] = "2" };

            Assert.Equal("1.2.<Missing>", ComposedItemEvaluator.Resolve(item, values));
            Assert.Equal(new[] { "A", "b", "Missing" }, ComposedItemEvaluator.GetPropertyNames(item.Formula).ToArray());
        }

        [Fact]
        public void TagData_loads_everything_in_one_call()
        {
            TagData data = TagData.Load(_settings, _workbookPath);

            Assert.NotEmpty(data.PropertySets);
            Assert.NotEmpty(data.Picklists);
            Assert.NotEmpty(data.LayerMappings);
            Assert.NotEmpty(data.Matches);
            Assert.NotEmpty(data.ExactMatches);
            Assert.NotEmpty(data.Expressions);
            Assert.NotEmpty(data.Composed);
            Assert.True(data.PicklistsByProperty.ContainsKey("LocationRoot"));
        }

        [Fact]
        public void TagData_reports_a_missing_workbook_with_a_friendly_error()
        {
            var exception = Assert.Throws<TagDataException>(() => TagData.Load(_settings, Path.Combine(Path.GetTempPath(), "nope.xlsx")));
            Assert.Contains("Excel", exception.Message);
        }
    }
}

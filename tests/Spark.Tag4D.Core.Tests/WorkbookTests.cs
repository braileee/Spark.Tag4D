using System;
using System.IO;
using System.Linq;
using Spark.Tag4D.Core.Spreadsheets;
using Xunit;

namespace Spark.Tag4D.Core.Tests
{
    public class WorkbookTests
    {
        [Fact]
        public void Created_workbook_round_trips_values_and_types()
        {
            Workbook workbook = Workbook.Create();
            Worksheet sheet = workbook.AddSheet("Data");
            sheet.SetValue(0, 0, "Name");
            sheet.SetValue(0, 2, "Count");
            sheet.SetValue(1, 0, "  padded  ");
            sheet.SetValue(1, 2, 42);
            sheet.SetValue(2, 1, true);
            sheet.SetValue(3, 0, "A & <B>");

            Workbook reopened = RoundTrip(workbook);
            Worksheet loaded = reopened.FindSheet("data")!;

            Assert.Equal("Name", loaded.GetText(0, 0));
            Assert.Equal("Count", loaded.GetText(0, 2));
            Assert.Equal("  padded  ", loaded.GetText(1, 0));
            Assert.Equal(42d, loaded.GetValue(1, 2));
            Assert.Equal("42", loaded.GetText(1, 2));
            Assert.Equal(true, loaded.GetValue(2, 1));
            Assert.Equal("A & <B>", loaded.GetText(3, 0));
            Assert.Equal(string.Empty, loaded.GetText(10, 10));
            Assert.Equal(4, loaded.RowCount);
            Assert.Equal(3, loaded.GetColumnCount(0));
        }

        [Fact]
        public void Out_of_order_writes_keep_rows_and_cells_sorted()
        {
            Workbook workbook = Workbook.Create();
            Worksheet sheet = workbook.AddSheet("S");
            sheet.SetValue(5, 3, "late");
            sheet.SetValue(1, 0, "early");
            sheet.SetValue(5, 0, "first-in-row");

            Worksheet loaded = RoundTrip(workbook).Sheets.Single();
            var xml = loaded.Document.ToString();

            Assert.True(xml.IndexOf("r=\"A2\"", StringComparison.Ordinal) < xml.IndexOf("r=\"A6\"", StringComparison.Ordinal));
            Assert.True(xml.IndexOf("r=\"A6\"", StringComparison.Ordinal) < xml.IndexOf("r=\"D6\"", StringComparison.Ordinal));
            Assert.Equal("late", loaded.GetText(5, 3));
        }

        [Fact]
        public void Sheets_can_be_added_removed_and_cleared()
        {
            Workbook workbook = Workbook.Create();
            workbook.AddSheet("One").SetValue(0, 0, "x");
            workbook.AddSheet("Two").SetValue(0, 0, "y");
            workbook.AddSheet("Three").SetValue(0, 0, "z");

            Assert.True(workbook.RemoveSheet("Two"));
            Assert.False(workbook.RemoveSheet("Two"));

            Workbook reopened = RoundTrip(workbook);
            Assert.Equal(new[] { "One", "Three" }, reopened.Sheets.Select(s => s.Name).ToArray());
            Assert.Equal("z", reopened.FindSheet("Three")!.GetText(0, 0));

            reopened.FindSheet("One")!.Clear();
            Assert.Equal(0, reopened.FindSheet("One")!.RowCount);
        }

        [Theory]
        [InlineData("")]
        [InlineData("a/b")]
        [InlineData("[x]")]
        [InlineData("this name is definitely longer than thirty-one characters")]
        public void Invalid_sheet_names_are_rejected(string name)
        {
            Assert.Throws<ArgumentException>(() => Workbook.Create().AddSheet(name));
        }

        [Fact]
        public void Duplicate_sheet_names_are_rejected()
        {
            Workbook workbook = Workbook.Create();
            workbook.AddSheet("Dup");
            Assert.Throws<ArgumentException>(() => workbook.AddSheet("dup"));
        }

        [Fact]
        public void Report_formatting_produces_a_valid_sheet()
        {
            Workbook workbook = Workbook.Create();
            Worksheet sheet = workbook.AddSheet("Report");
            sheet.SetRow(0, new object[] { "File", "Value" });
            sheet.SetRow(1, new object[] { "a.ifc", "long long value" });
            sheet.SetRowBold(0, 2);
            sheet.FreezeRows(1);
            sheet.SetAutoFilter(0, 0, 1, 1);
            sheet.AutoSizeColumns(2);

            Worksheet loaded = RoundTrip(workbook).Sheets.Single();
            string xml = loaded.Document.ToString();

            // Schema order matters to Excel: sheetViews < cols < sheetData < autoFilter.
            int views = xml.IndexOf("<sheetViews", StringComparison.Ordinal);
            int cols = xml.IndexOf("<cols", StringComparison.Ordinal);
            int data = xml.IndexOf("<sheetData", StringComparison.Ordinal);
            int filter = xml.IndexOf("<autoFilter", StringComparison.Ordinal);
            Assert.True(views >= 0 && views < cols && cols < data && data < filter, xml);
            Assert.Contains("ref=\"A1:B2\"", xml);
            Assert.Contains("s=\"1\"", xml);
        }

        [Fact]
        public void Existing_workbook_formatting_survives_edits()
        {
            string path = TempCopyOfSample();
            try
            {
                Workbook workbook = Workbook.Open(path);
                string[] sheetNames = workbook.Sheets.Select(s => s.Name).ToArray();
                Assert.Contains("Properties", sheetNames);

                workbook.FindSheet("Expressions")!.SetValue(5, 0, "NewSource");
                workbook.Save(path);

                Workbook reopened = Workbook.Open(path);
                Assert.Equal(sheetNames, reopened.Sheets.Select(s => s.Name).ToArray());
                Assert.Equal("NewSource", reopened.FindSheet("Expressions")!.GetText(5, 0));
                Assert.Equal("Property Set Name", reopened.FindSheet("Properties")!.GetText(0, 0));

                // Untouched parts (tables, styles, theme) must still be in the package.
                using var zip = System.IO.Compression.ZipFile.OpenRead(path);
                Assert.Contains(zip.Entries, e => e.FullName.StartsWith("xl/tables/", StringComparison.Ordinal));
                Assert.Contains(zip.Entries, e => e.FullName == "xl/styles.xml");
                Assert.Contains(zip.Entries, e => e.FullName == "xl/theme/theme1.xml");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Removing_a_sheet_from_an_existing_workbook_keeps_the_package_consistent()
        {
            string path = TempCopyOfSample();
            try
            {
                Workbook workbook = Workbook.Open(path);
                Assert.True(workbook.RemoveSheet("SpaceID Match"));
                workbook.Save(path);

                Workbook reopened = Workbook.Open(path);
                Assert.Null(reopened.FindSheet("SpaceID Match"));
                Assert.NotNull(reopened.FindSheet("Composed"));

                using var zip = System.IO.Compression.ZipFile.OpenRead(path);
                string contentTypes = new StreamReader(zip.GetEntry("[Content_Types].xml")!.Open()).ReadToEnd();
                foreach (string name in zip.Entries.Select(e => e.FullName))
                {
                    // every override must point to a part that exists
                    Assert.True(!contentTypes.Contains("PartName=\"/" + name + "\"") || zip.GetEntry(name) != null);
                }

                foreach (var match in System.Text.RegularExpressions.Regex.Matches(contentTypes, "PartName=\"/([^\"]+)\"").Cast<System.Text.RegularExpressions.Match>())
                {
                    Assert.NotNull(zip.GetEntry(match.Groups[1].Value));
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Shared_strings_and_numbers_are_read_from_real_files()
        {
            Workbook workbook = Workbook.Open(SamplePath());
            Worksheet layers = workbook.FindSheet("Layers Mapping")!;

            Assert.Equal("LayerName", layers.GetText(0, 0));
            Assert.Equal("TA_-_---_-----", layers.GetText(1, 0));
            Assert.True(layers.RowCount > 1000);
        }

        internal static string SamplePath() =>
            Path.Combine(AppContext.BaseDirectory, "Files", "Tag4DFiles", "IFC parameters.xlsx");

        private static string TempCopyOfSample()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xlsx");
            File.Copy(SamplePath(), path);
            return path;
        }

        private static Workbook RoundTrip(Workbook workbook)
        {
            using var stream = new MemoryStream();
            workbook.Save(stream);
            stream.Position = 0;
            return Workbook.Load(stream);
        }
    }
}

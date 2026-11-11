using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Spark.Tag4D.Core.Spreadsheets
{
    /// <summary>
    /// A single worksheet. Rows and columns are zero-based. Missing rows/cells read as empty.
    /// </summary>
    public sealed class Worksheet
    {
        // Order of child elements required by the worksheet schema (CT_Worksheet).
        private static readonly string[] ChildOrder =
        {
            "sheetPr", "dimension", "sheetViews", "sheetFormatPr", "cols", "sheetData", "sheetCalcPr",
            "sheetProtection", "protectedRanges", "scenarios", "autoFilter", "sortState", "dataConsolidate",
            "customSheetViews", "mergeCells", "phoneticPr", "conditionalFormatting", "dataValidations",
            "hyperlinks", "printOptions", "pageMargins", "pageSetup", "headerFooter", "rowBreaks", "colBreaks",
            "customProperties", "cellWatches", "ignoredErrors", "smartTags", "drawing", "legacyDrawing",
            "legacyDrawingHF", "picture", "oleObjects", "controls", "webPublishItems", "tableParts", "extLst"
        };

        private static readonly XNamespace Main = Workbook.Main;

        private readonly Workbook _workbook;
        private XDocument? _document;
        private XElement? _sheetData;
        private SortedDictionary<int, XElement>? _rows;
        private Dictionary<long, XElement>? _cells;

        internal Worksheet(Workbook workbook, string name, string partName)
        {
            _workbook = workbook;
            Name = name;
            PartName = partName;
        }

        /// <summary>Sheet (tab) name.</summary>
        public string Name { get; }

        internal string PartName { get; }

        internal bool IsDirty { get; private set; }

        internal XDocument Document
        {
            get
            {
                EnsureLoaded();
                return _document!;
            }
        }

        /// <summary>Number of rows up to and including the last row that exists in the sheet.</summary>
        public int RowCount
        {
            get
            {
                EnsureLoaded();
                return _rows!.Count == 0 ? 0 : _rows.Keys.Max() + 1;
            }
        }

        /// <summary>Number of columns up to and including the last cell of the given row.</summary>
        public int GetColumnCount(int row)
        {
            EnsureLoaded();
            if (!_rows!.TryGetValue(row, out XElement? rowElement))
            {
                return 0;
            }

            int last = -1;
            foreach (XElement cell in rowElement.Elements(Main + "c"))
            {
                last = Math.Max(last, ColumnOf(cell));
            }

            return last + 1;
        }

        /// <summary>True when the row is missing or all of its cells are blank.</summary>
        public bool IsRowEmpty(int row)
        {
            int columns = GetColumnCount(row);
            for (int column = 0; column < columns; column++)
            {
                if (!string.IsNullOrWhiteSpace(GetText(row, column)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Returns the cell value as text; empty string when the cell does not exist.</summary>
        public string GetText(int row, int column)
        {
            object? value = GetValue(row, column);

            switch (value)
            {
                case null:
                    return string.Empty;
                case string text:
                    return text;
                case bool flag:
                    return flag ? "TRUE" : "FALSE";
                case double number:
                    return FormatNumber(number);
                default:
                    return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
        }

        /// <summary>Returns the typed cell value (string, double, bool) or null.</summary>
        public object? GetValue(int row, int column)
        {
            EnsureLoaded();
            if (!_cells!.TryGetValue(Key(row, column), out XElement? cell))
            {
                return null;
            }

            string? type = (string?)cell.Attribute("t");

            if (type == "inlineStr")
            {
                XElement? inline = cell.Element(Main + "is");
                return inline == null ? string.Empty : Workbook.ReadRichText(inline);
            }

            string? raw = cell.Element(Main + "v")?.Value;
            if (raw == null)
            {
                return null;
            }

            switch (type)
            {
                case "s":
                    return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)
                        ? _workbook.GetSharedString(index)
                        : string.Empty;
                case "b":
                    return raw == "1" || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
                case "str":
                case "e":
                case "d":
                    return raw;
                default:
                    return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
                        ? number
                        : (object)raw;
            }
        }

        /// <summary>
        /// Writes a value. Strings are stored inline, numbers/booleans natively; null or empty text clears the cell.
        /// Existing cell formatting is preserved.
        /// </summary>
        public void SetValue(int row, int column, object? value)
        {
            if (row < 0) throw new ArgumentOutOfRangeException(nameof(row));
            if (column < 0) throw new ArgumentOutOfRangeException(nameof(column));

            EnsureLoaded();

            bool isBlank = value == null || (value is string blank && blank.Length == 0);
            if (isBlank && !_cells!.ContainsKey(Key(row, column)))
            {
                return;
            }

            XElement cell = GetOrCreateCell(row, column);
            cell.RemoveNodes();
            cell.SetAttributeValue("t", null);

            switch (value)
            {
                case null:
                    break;
                case string text:
                    if (text.Length > 0)
                    {
                        cell.SetAttributeValue("t", "inlineStr");
                        var textElement = new XElement(Main + "t", text);
                        if (text.Length != text.Trim().Length || text.Contains("\n"))
                        {
                            textElement.SetAttributeValue(XNamespace.Xml + "space", "preserve");
                        }

                        cell.Add(new XElement(Main + "is", textElement));
                    }

                    break;
                case bool flag:
                    cell.SetAttributeValue("t", "b");
                    cell.Add(new XElement(Main + "v", flag ? "1" : "0"));
                    break;
                case double number:
                    cell.Add(new XElement(Main + "v", number.ToString("R", CultureInfo.InvariantCulture)));
                    break;
                case float single:
                    cell.Add(new XElement(Main + "v", ((double)single).ToString("R", CultureInfo.InvariantCulture)));
                    break;
                case int or long or short or byte or decimal:
                    cell.Add(new XElement(Main + "v", Convert.ToString(value, CultureInfo.InvariantCulture)));
                    break;
                case DateTime date:
                    cell.SetAttributeValue("t", "inlineStr");
                    cell.Add(new XElement(Main + "is", new XElement(Main + "t", date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))));
                    break;
                default:
                    cell.SetAttributeValue("t", "inlineStr");
                    cell.Add(new XElement(Main + "is", new XElement(Main + "t", Convert.ToString(value, CultureInfo.InvariantCulture))));
                    break;
            }

            IsDirty = true;
        }

        /// <summary>Writes a complete row of values starting at column 0.</summary>
        public void SetRow(int row, IEnumerable<object?> values)
        {
            int column = 0;
            foreach (object? value in values)
            {
                SetValue(row, column++, value);
            }
        }

        /// <summary>Removes every row, keeping the sheet itself (and its formatting parts).</summary>
        public void Clear()
        {
            EnsureLoaded();
            _sheetData!.RemoveNodes();
            _rows!.Clear();
            _cells!.Clear();
            IsDirty = true;
        }

        /// <summary>Applies a bold font to the first <paramref name="columnCount"/> cells of a row.</summary>
        public void SetRowBold(int row, int columnCount)
        {
            EnsureLoaded();
            int style = _workbook.GetBoldStyleIndex();

            for (int column = 0; column < columnCount; column++)
            {
                GetOrCreateCell(row, column).SetAttributeValue("s", style);
            }

            IsDirty = true;
        }

        /// <summary>Freezes the first <paramref name="rowCount"/> rows so they stay visible while scrolling.</summary>
        public void FreezeRows(int rowCount)
        {
            EnsureLoaded();
            XElement views = GetOrCreateChild("sheetViews");
            XElement view = views.Element(Main + "sheetView") ?? new XElement(Main + "sheetView", new XAttribute("workbookViewId", 0));
            if (view.Parent == null)
            {
                views.Add(view);
            }

            view.Elements(Main + "pane").Remove();
            view.AddFirst(new XElement(Main + "pane",
                new XAttribute("ySplit", rowCount),
                new XAttribute("topLeftCell", $"A{rowCount + 1}"),
                new XAttribute("activePane", "bottomLeft"),
                new XAttribute("state", "frozen")));
            IsDirty = true;
        }

        /// <summary>Adds an auto-filter over the given zero-based inclusive range.</summary>
        public void SetAutoFilter(int firstRow, int firstColumn, int lastRow, int lastColumn)
        {
            EnsureLoaded();
            GetChild("autoFilter")?.Remove();

            string reference = $"{ColumnName(firstColumn)}{firstRow + 1}:{ColumnName(lastColumn)}{lastRow + 1}";
            GetOrCreateChild("autoFilter").SetAttributeValue("ref", reference);
            IsDirty = true;
        }

        /// <summary>Sizes the first <paramref name="columnCount"/> columns to fit their content.</summary>
        public void AutoSizeColumns(int columnCount, int maxWidth = 80)
        {
            EnsureLoaded();
            GetChild("cols")?.Remove();

            var cols = new XElement(Main + "cols");
            for (int column = 0; column < columnCount; column++)
            {
                int longest = 0;
                foreach (KeyValuePair<int, XElement> row in _rows!)
                {
                    if (_cells!.ContainsKey(Key(row.Key, column)))
                    {
                        string text = GetText(row.Key, column);
                        longest = Math.Max(longest, text.Length);
                    }
                }

                double width = Math.Min(Math.Max(longest + 2, 8), maxWidth);
                cols.Add(new XElement(Main + "col",
                    new XAttribute("min", column + 1),
                    new XAttribute("max", column + 1),
                    new XAttribute("width", width.ToString("0.##", CultureInfo.InvariantCulture)),
                    new XAttribute("customWidth", 1)));
            }

            if (cols.HasElements)
            {
                InsertChild(cols);
            }

            IsDirty = true;
        }

        internal void MarkClean() => IsDirty = false;

        /// <summary>Converts a zero-based column index to its letters (0 = A).</summary>
        public static string ColumnName(int column)
        {
            var builder = new StringBuilder();
            for (int value = column + 1; value > 0; value = (value - 1) / 26)
            {
                builder.Insert(0, (char)('A' + (value - 1) % 26));
            }

            return builder.ToString();
        }

        private static string FormatNumber(double number)
        {
            if (Math.Abs(number) < 1e15 && Math.Abs(number - Math.Round(number)) < double.Epsilon)
            {
                return ((long)number).ToString(CultureInfo.InvariantCulture);
            }

            return number.ToString("R", CultureInfo.InvariantCulture);
        }

        private static long Key(int row, int column) => ((long)row << 20) | (uint)column;

        private static int ColumnOf(XElement cell)
        {
            string? reference = (string?)cell.Attribute("r");
            return reference == null ? -1 : ParseReference(reference).Column;
        }

        private static (int Row, int Column) ParseReference(string reference)
        {
            int column = 0;
            int index = 0;

            while (index < reference.Length && char.IsLetter(reference[index]))
            {
                column = (column * 26) + (char.ToUpperInvariant(reference[index]) - 'A' + 1);
                index++;
            }

            int row = 0;
            while (index < reference.Length && char.IsDigit(reference[index]))
            {
                row = (row * 10) + (reference[index] - '0');
                index++;
            }

            return (row - 1, column - 1);
        }

        private void EnsureLoaded()
        {
            if (_document != null)
            {
                return;
            }

            _document = _workbook.LoadPartXml(PartName);
            _sheetData = _document.Root!.Element(Main + "sheetData");
            if (_sheetData == null)
            {
                _sheetData = new XElement(Main + "sheetData");
                InsertChild(_sheetData);
            }

            _rows = new SortedDictionary<int, XElement>();
            _cells = new Dictionary<long, XElement>();

            int nextRow = 0;
            foreach (XElement rowElement in _sheetData.Elements(Main + "row"))
            {
                int rowIndex = (int?)rowElement.Attribute("r") - 1 ?? nextRow;
                nextRow = rowIndex + 1;
                _rows[rowIndex] = rowElement;

                int nextColumn = 0;
                foreach (XElement cell in rowElement.Elements(Main + "c"))
                {
                    string? reference = (string?)cell.Attribute("r");
                    int columnIndex = reference == null ? nextColumn : ParseReference(reference).Column;
                    nextColumn = columnIndex + 1;
                    _cells[Key(rowIndex, columnIndex)] = cell;
                }
            }
        }

        private XElement GetOrCreateRow(int row)
        {
            if (_rows!.TryGetValue(row, out XElement? existing))
            {
                return existing;
            }

            var created = new XElement(Main + "row", new XAttribute("r", row + 1));
            int? nextKey = _rows.Keys.Where(k => k > row).Cast<int?>().FirstOrDefault();

            if (nextKey == null)
            {
                _sheetData!.Add(created);
            }
            else
            {
                _rows[nextKey.Value].AddBeforeSelf(created);
            }

            _rows[row] = created;
            RemoveDimension();
            return created;
        }

        private XElement GetOrCreateCell(int row, int column)
        {
            if (_cells!.TryGetValue(Key(row, column), out XElement? existing))
            {
                return existing;
            }

            XElement rowElement = GetOrCreateRow(row);
            var created = new XElement(Main + "c", new XAttribute("r", $"{ColumnName(column)}{row + 1}"));

            XElement? successor = rowElement.Elements(Main + "c").FirstOrDefault(c => ColumnOf(c) > column);
            if (successor == null)
            {
                rowElement.Add(created);
            }
            else
            {
                successor.AddBeforeSelf(created);
            }

            _cells[Key(row, column)] = created;
            return created;
        }

        private XElement? GetChild(string name) => _document!.Root!.Element(Main + name);

        private XElement GetOrCreateChild(string name)
        {
            XElement? child = GetChild(name);
            if (child != null)
            {
                return child;
            }

            child = new XElement(Main + name);
            InsertChild(child);
            return child;
        }

        private void InsertChild(XElement child)
        {
            int order = Array.IndexOf(ChildOrder, child.Name.LocalName);
            XElement root = _document!.Root!;
            XElement? successor = root.Elements().FirstOrDefault(e => Array.IndexOf(ChildOrder, e.Name.LocalName) > order);

            if (successor == null)
            {
                root.Add(child);
            }
            else
            {
                successor.AddBeforeSelf(child);
            }
        }

        private void RemoveDimension() => GetChild("dimension")?.Remove();
    }
}

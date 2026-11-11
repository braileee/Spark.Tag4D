using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Spark.Tag4D.Core.Spreadsheets
{
    /// <summary>
    /// Minimal, dependency-free reader/writer for Office Open XML workbooks (.xlsx).
    /// <para>
    /// The package is kept as a set of raw parts; only the parts that are actually touched
    /// (workbook manifest and modified worksheets) are re-serialised. Everything else
    /// (styles, tables, themes, column widths, ...) is preserved byte-for-byte, so editing an
    /// existing workbook does not destroy its formatting.
    /// </para>
    /// </summary>
    public sealed class Workbook
    {
        internal const string ContentTypeWorksheet = "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml";

        internal static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        internal static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PackageRel = "http://schemas.openxmlformats.org/package/2006/relationships";
        private static readonly XNamespace ContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";

        private const string WorkbookPart = "xl/workbook.xml";
        private const string WorkbookRelsPart = "xl/_rels/workbook.xml.rels";
        private const string ContentTypesPart = "[Content_Types].xml";
        private const string StylesPart = "xl/styles.xml";
        private const string SharedStringsPart = "xl/sharedStrings.xml";
        private const string CalcChainPart = "xl/calcChain.xml";

        private readonly Dictionary<string, byte[]> _parts = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _partOrder = new List<string>();
        private readonly List<Worksheet> _sheets = new List<Worksheet>();

        private XDocument _workbookXml = null!;
        private XDocument _workbookRels = null!;
        private XDocument _contentTypes = null!;
        private XDocument? _styles;
        private IReadOnlyList<string>? _sharedStrings;
        private int _boldStyleIndex = -1;

        private Workbook()
        {
        }

        /// <summary>Worksheets in tab order.</summary>
        public IReadOnlyList<Worksheet> Sheets => _sheets;

        /// <summary>Creates an empty workbook that contains no sheets.</summary>
        public static Workbook Create()
        {
            using var stream = new MemoryStream();
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                WriteEntry(zip, ContentTypesPart,
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                    "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                    "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                    "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                    "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                    "</Types>");
                WriteEntry(zip, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                    "</Relationships>");
                WriteEntry(zip, WorkbookPart,
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                    "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets/></workbook>");
                WriteEntry(zip, WorkbookRelsPart,
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                    "</Relationships>");
                WriteEntry(zip, StylesPart,
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                    "<fonts count=\"1\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
                    "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
                    "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                    "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                    "<cellXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/></cellXfs>" +
                    "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
                    "</styleSheet>");
            }

            stream.Position = 0;
            return Load(stream);
        }

        /// <summary>Opens a workbook from disk. The file is not kept locked.</summary>
        public static Workbook Open(string path)
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return Load(file);
        }

        /// <summary>Loads a workbook from a stream.</summary>
        public static Workbook Load(Stream stream)
        {
            var workbook = new Workbook();

            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    using Stream entryStream = entry.Open();
                    using var buffer = new MemoryStream();
                    entryStream.CopyTo(buffer);
                    workbook.SetPart(entry.FullName, buffer.ToArray());
                }
            }

            workbook.Initialize();
            return workbook;
        }

        /// <summary>Finds a sheet by name (case-insensitive).</summary>
        public Worksheet? FindSheet(string name)
        {
            return _sheets.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Appends a new empty sheet.</summary>
        public Worksheet AddSheet(string name)
        {
            ValidateSheetName(name);

            if (FindSheet(name) != null)
            {
                throw new ArgumentException($"A sheet named '{name}' already exists.", nameof(name));
            }

            int sheetNumber = 1;
            while (_parts.ContainsKey($"xl/worksheets/sheet{sheetNumber}.xml"))
            {
                sheetNumber++;
            }

            string partName = $"xl/worksheets/sheet{sheetNumber}.xml";
            var document = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement(Main + "worksheet", new XElement(Main + "sheetData")));

            SetPart(partName, Serialize(document));

            string relationshipId = NextId(_workbookRels.Root!.Elements(PackageRel + "Relationship").Select(e => (string?)e.Attribute("Id")), "rId");
            _workbookRels.Root.Add(new XElement(PackageRel + "Relationship",
                new XAttribute("Id", relationshipId),
                new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                new XAttribute("Target", $"worksheets/sheet{sheetNumber}.xml")));

            _contentTypes.Root!.Add(new XElement(ContentTypes + "Override",
                new XAttribute("PartName", "/" + partName),
                new XAttribute("ContentType", ContentTypeWorksheet)));

            XElement sheets = _workbookXml.Root!.Element(Main + "sheets")!;
            int sheetId = sheets.Elements(Main + "sheet")
                .Select(e => (int?)e.Attribute("sheetId") ?? 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            sheets.Add(new XElement(Main + "sheet",
                new XAttribute("name", name),
                new XAttribute("sheetId", sheetId),
                new XAttribute(Rel + "id", relationshipId)));

            var sheet = new Worksheet(this, name, partName);
            _sheets.Add(sheet);
            return sheet;
        }

        /// <summary>Removes a sheet by name. Returns false when the sheet does not exist.</summary>
        public bool RemoveSheet(string name)
        {
            Worksheet? sheet = FindSheet(name);
            if (sheet == null)
            {
                return false;
            }

            int index = _sheets.IndexOf(sheet);
            XElement sheets = _workbookXml.Root!.Element(Main + "sheets")!;
            XElement sheetElement = sheets.Elements(Main + "sheet").ElementAt(index);
            string? relationshipId = (string?)sheetElement.Attribute(Rel + "id");
            sheetElement.Remove();

            XElement? relationship = _workbookRels.Root!.Elements(PackageRel + "Relationship")
                .FirstOrDefault(e => (string?)e.Attribute("Id") == relationshipId);
            relationship?.Remove();

            RemovePart(sheet.PartName);
            RemoveSheetDependencies(sheet.PartName);
            FixDefinedNamesAfterRemoval(index);
            DropCalcChain();

            _sheets.RemoveAt(index);
            return true;
        }

        /// <summary>Saves the workbook to disk, replacing the target file atomically.</summary>
        public void Save(string path)
        {
            string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = path + ".tmp";
            using (var file = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Save(file);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(temporaryPath, path);
        }

        /// <summary>Writes the workbook to a stream.</summary>
        public void Save(Stream stream)
        {
            if (_sheets.Any(s => s.IsDirty))
            {
                DropCalcChain();
            }

            foreach (Worksheet sheet in _sheets.Where(s => s.IsDirty))
            {
                SetPart(sheet.PartName, Serialize(sheet.Document));
                sheet.MarkClean();
            }

            if (_styles != null)
            {
                SetPart(StylesPart, Serialize(_styles));
            }

            SetPart(WorkbookPart, Serialize(_workbookXml));
            SetPart(WorkbookRelsPart, Serialize(_workbookRels));
            SetPart(ContentTypesPart, Serialize(_contentTypes));

            using var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

            // [Content_Types].xml must be first for strict consumers.
            foreach (string name in _partOrder.OrderBy(n => n == ContentTypesPart ? 0 : 1))
            {
                if (!_parts.TryGetValue(name, out byte[]? data))
                {
                    continue;
                }

                ZipArchiveEntry entry = zip.CreateEntry(name, CompressionLevel.Optimal);
                using Stream entryStream = entry.Open();
                entryStream.Write(data, 0, data.Length);
            }
        }

        internal XDocument LoadPartXml(string partName)
        {
            return ParseXml(_parts[partName]);
        }

        internal string GetSharedString(int index)
        {
            _sharedStrings ??= ReadSharedStrings();
            return index >= 0 && index < _sharedStrings.Count ? _sharedStrings[index] : string.Empty;
        }

        /// <summary>Returns the style index of a bold cell format, adding it to the workbook on first use.</summary>
        internal int GetBoldStyleIndex()
        {
            if (_boldStyleIndex >= 0)
            {
                return _boldStyleIndex;
            }

            _styles ??= _parts.ContainsKey(StylesPart) ? LoadPartXml(StylesPart) : null;
            if (_styles == null)
            {
                return 0;
            }

            XElement root = _styles.Root!;
            XElement fonts = GetOrAddStyleCollection(root, "fonts", before: new[] { "fills" });
            fonts.Add(new XElement(Main + "font", new XElement(Main + "b"), new XElement(Main + "sz", new XAttribute("val", 11)), new XElement(Main + "name", new XAttribute("val", "Calibri"))));
            int fontId = fonts.Elements(Main + "font").Count() - 1;
            fonts.SetAttributeValue("count", fontId + 1);

            XElement cellXfs = GetOrAddStyleCollection(root, "cellXfs", before: new[] { "cellStyles", "dxfs" });
            cellXfs.Add(new XElement(Main + "xf",
                new XAttribute("numFmtId", 0),
                new XAttribute("fontId", fontId),
                new XAttribute("fillId", 0),
                new XAttribute("borderId", 0),
                new XAttribute("xfId", 0),
                new XAttribute("applyFont", 1)));
            _boldStyleIndex = cellXfs.Elements(Main + "xf").Count() - 1;
            cellXfs.SetAttributeValue("count", _boldStyleIndex + 1);

            return _boldStyleIndex;
        }

        private static XElement GetOrAddStyleCollection(XElement root, string name, string[] before)
        {
            XElement? collection = root.Element(Main + name);
            if (collection != null)
            {
                return collection;
            }

            collection = new XElement(Main + name, new XAttribute("count", 0));
            XElement? successor = root.Elements().FirstOrDefault(e => before.Contains(e.Name.LocalName));
            if (successor != null)
            {
                successor.AddBeforeSelf(collection);
            }
            else
            {
                root.Add(collection);
            }

            return collection;
        }

        private void Initialize()
        {
            _contentTypes = LoadPartXml(ContentTypesPart);
            _workbookXml = LoadPartXml(WorkbookPart);
            _workbookRels = LoadPartXml(WorkbookRelsPart);

            XElement? sheets = _workbookXml.Root!.Element(Main + "sheets");
            if (sheets == null)
            {
                sheets = new XElement(Main + "sheets");
                _workbookXml.Root.Add(sheets);
            }

            Dictionary<string, string> targets = _workbookRels.Root!
                .Elements(PackageRel + "Relationship")
                .Where(e => e.Attribute("Id") != null && e.Attribute("Target") != null)
                .ToDictionary(e => (string)e.Attribute("Id")!, e => (string)e.Attribute("Target")!);

            foreach (XElement sheetElement in sheets.Elements(Main + "sheet"))
            {
                string name = (string?)sheetElement.Attribute("name") ?? string.Empty;
                string? relationshipId = (string?)sheetElement.Attribute(Rel + "id");

                if (relationshipId == null || !targets.TryGetValue(relationshipId, out string? target))
                {
                    continue;
                }

                string partName = ResolveTarget("xl/", target);
                if (_parts.ContainsKey(partName))
                {
                    _sheets.Add(new Worksheet(this, name, partName));
                }
            }
        }

        private IReadOnlyList<string> ReadSharedStrings()
        {
            if (!_parts.ContainsKey(SharedStringsPart))
            {
                return new List<string>();
            }

            XDocument document = LoadPartXml(SharedStringsPart);
            var result = new List<string>();

            foreach (XElement item in document.Root!.Elements(Main + "si"))
            {
                result.Add(ReadRichText(item));
            }

            return result;
        }

        internal static string ReadRichText(XElement element)
        {
            var builder = new StringBuilder();

            foreach (XElement child in element.Elements())
            {
                if (child.Name == Main + "t")
                {
                    builder.Append(child.Value);
                }
                else if (child.Name == Main + "r")
                {
                    builder.Append(child.Element(Main + "t")?.Value);
                }
            }

            return builder.ToString();
        }

        private void RemoveSheetDependencies(string sheetPartName)
        {
            string directory = sheetPartName.Substring(0, sheetPartName.LastIndexOf('/') + 1);
            string relsName = directory + "_rels/" + sheetPartName.Substring(directory.Length) + ".rels";

            if (!_parts.ContainsKey(relsName))
            {
                return;
            }

            XDocument rels = LoadPartXml(relsName);
            foreach (XElement relationship in rels.Root!.Elements(PackageRel + "Relationship"))
            {
                if ((string?)relationship.Attribute("TargetMode") == "External")
                {
                    continue;
                }

                string? target = (string?)relationship.Attribute("Target");
                if (target != null)
                {
                    RemovePart(ResolveTarget(directory, target));
                }
            }

            RemovePart(relsName);
        }

        private void RemovePart(string partName)
        {
            if (!_parts.Remove(partName))
            {
                return;
            }

            _partOrder.RemoveAll(p => string.Equals(p, partName, StringComparison.OrdinalIgnoreCase));

            XElement? contentType = _contentTypes.Root!.Elements(ContentTypes + "Override")
                .FirstOrDefault(e => string.Equals((string?)e.Attribute("PartName"), "/" + partName, StringComparison.OrdinalIgnoreCase));
            contentType?.Remove();
        }

        private void FixDefinedNamesAfterRemoval(int removedIndex)
        {
            XElement? definedNames = _workbookXml.Root!.Element(Main + "definedNames");
            if (definedNames == null)
            {
                return;
            }

            foreach (XElement name in definedNames.Elements(Main + "definedName").ToList())
            {
                int? localSheetId = (int?)name.Attribute("localSheetId");
                if (localSheetId == null)
                {
                    continue;
                }

                if (localSheetId == removedIndex)
                {
                    name.Remove();
                }
                else if (localSheetId > removedIndex)
                {
                    name.SetAttributeValue("localSheetId", localSheetId - 1);
                }
            }

            if (!definedNames.HasElements)
            {
                definedNames.Remove();
            }

            XElement? workbookView = _workbookXml.Root.Element(Main + "bookViews")?.Element(Main + "workbookView");
            workbookView?.SetAttributeValue("activeTab", 0);
            workbookView?.SetAttributeValue("firstSheet", 0);
        }

        private void DropCalcChain()
        {
            if (!_parts.ContainsKey(CalcChainPart))
            {
                return;
            }

            RemovePart(CalcChainPart);

            XElement? relationship = _workbookRels.Root!.Elements(PackageRel + "Relationship")
                .FirstOrDefault(e => ((string?)e.Attribute("Target"))?.EndsWith("calcChain.xml", StringComparison.OrdinalIgnoreCase) == true);
            relationship?.Remove();
        }

        private void SetPart(string name, byte[] data)
        {
            if (!_parts.ContainsKey(name))
            {
                _partOrder.Add(name);
            }

            _parts[name] = data;
        }

        private static void WriteEntry(ZipArchive zip, string name, string content)
        {
            ZipArchiveEntry entry = zip.CreateEntry(name);
            using Stream stream = entry.Open();
            byte[] bytes = new UTF8Encoding(false).GetBytes(content);
            stream.Write(bytes, 0, bytes.Length);
        }

        internal static XDocument ParseXml(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        }

        internal static byte[] Serialize(XDocument document)
        {
            using var stream = new MemoryStream();
            var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), OmitXmlDeclaration = false };
            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                document.Save(writer);
            }

            return stream.ToArray();
        }

        private static string ResolveTarget(string baseDirectory, string target)
        {
            if (target.StartsWith("/", StringComparison.Ordinal))
            {
                return target.TrimStart('/');
            }

            var segments = new List<string>(baseDirectory.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
            foreach (string segment in target.Split('/'))
            {
                if (segment == "..")
                {
                    if (segments.Count > 0)
                    {
                        segments.RemoveAt(segments.Count - 1);
                    }
                }
                else if (segment != "." && segment.Length > 0)
                {
                    segments.Add(segment);
                }
            }

            return string.Join("/", segments);
        }

        private static string NextId(IEnumerable<string?> existing, string prefix)
        {
            var used = new HashSet<string?>(existing);
            int number = 1;
            while (used.Contains(prefix + number.ToString(CultureInfo.InvariantCulture)))
            {
                number++;
            }

            return prefix + number.ToString(CultureInfo.InvariantCulture);
        }

        private static void ValidateSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 31 || name.IndexOfAny(new[] { '[', ']', ':', '*', '?', '/', '\\' }) >= 0)
            {
                throw new ArgumentException(
                    $"'{name}' is not a valid sheet name (1-31 characters, none of []:*?/\\).", nameof(name));
            }
        }
    }
}

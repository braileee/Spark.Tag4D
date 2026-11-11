using System.Text;
using WixSharp;
using File = WixSharp.File;

namespace Spark.Tag4D.Setup;

/// <summary>
/// One installer for every supported Revit release. The add-in is installed as an Autodesk "application plug-in
/// bundle" in %ProgramData%, which Revit discovers per release through PackageContents.xml, so there is nothing
/// version specific to do at install time.
/// <para>
/// Expected payload: one sub-directory per Revit release (<c>2023</c>, <c>2024</c>, ...) holding that build.
/// </para>
/// </summary>
internal sealed class RevitInstaller : InstallerBuilder
{
    private const string BundleName = "Spark.Tag4D.bundle";
    private const string AddInFile = "Spark.Tag4D.Revit.addin";

    public RevitInstaller(SetupOptions options)
        : base(options)
    {
    }

    protected override Guid UpgradeCode => new("0812103B-3D68-4BE2-9A5C-B6ECBCEB605E");

    protected override string ProductName => "Spark.Tag4D for Revit";

    protected override string OutputName => "Spark.Tag4D-Revit";

    protected override string ValidatorFolder => @"Spark.Tag4D\Revit";

    protected override Dir[] CreateAddInDirectories()
    {
        string[] releases = Directory.GetDirectories(Options.PayloadDirectory)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => name.Length == 4 && name.All(char.IsDigit))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        if (releases.Length == 0)
        {
            throw new InvalidOperationException($"No Revit release folders (2023, 2024, ...) found in {Options.PayloadDirectory}.");
        }

        foreach (string release in releases)
        {
            if (!System.IO.File.Exists(Path.Combine(Options.PayloadDirectory, release, AddInFile)))
            {
                throw new FileNotFoundException($"Revit {release} payload is missing {AddInFile}.");
            }
        }

        string packageContents = Path.Combine(Path.GetTempPath(), $"Spark.Tag4D.PackageContents.{Guid.NewGuid():N}.xml");
        System.IO.File.WriteAllText(packageContents, CreatePackageContents(releases), new UTF8Encoding(false));

        var contents = releases
            .Select(release => new Dir(release, BuildTree(Path.Combine(Options.PayloadDirectory, release)).ToArray()))
            .Cast<WixEntity>()
            .ToArray();

        return new[]
        {
            new Dir(
                $@"%CommonAppDataFolder%\Autodesk\ApplicationPlugins\{BundleName}",
                new File(packageContents) { TargetFileName = "PackageContents.xml" },
                new Dir("Contents", contents)),
        };
    }

    private string CreatePackageContents(IEnumerable<string> releases)
    {
        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xml.AppendLine($"<ApplicationPackage SchemaVersion=\"1.0\" AutodeskProduct=\"Revit\" ProductType=\"Application\" Name=\"Spark.Tag4D\" AppVersion=\"{Options.Version}\" Description=\"IFC property tagging for Revit\" Author=\"{Manufacturer}\" AppNameSpace=\"Spark.Tag4D\" UpgradeCode=\"{{{UpgradeCode}}}\" ProductCode=\"{{{UpgradeCode}}}\">");
        xml.AppendLine($"  <CompanyDetails Name=\"{Manufacturer}\" />");

        foreach (string release in releases)
        {
            xml.AppendLine($"  <Components Description=\"Revit {release}\">");
            xml.AppendLine($"    <RuntimeRequirements OS=\"Win64\" Platform=\"Revit\" SeriesMin=\"R{release}\" SeriesMax=\"R{release}\" />");
            xml.AppendLine($"    <ComponentEntry AppName=\"Spark.Tag4D\" Version=\"{Options.Version}\" ModuleName=\"./Contents/{release}/{AddInFile}\" AppDescription=\"IFC property tagging for Revit\" LoadOnRevitStartup=\"True\" />");
            xml.AppendLine("  </Components>");
        }

        xml.AppendLine("</ApplicationPackage>");
        return xml.ToString();
    }
}

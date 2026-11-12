using System.Text;
using WixSharp;
using File = WixSharp.File;

namespace Spark.Tag4D.Setup;

/// <summary>
/// One installer for every supported MicroStation release (CE, 2025, 2026 - all run the same .NET Framework add-in).
/// <para>
/// The add-in is installed once, below Program Files. For each release a <c>Spark.Tag4D.cfg</c> is dropped into that
/// release's <c>Organization</c> configuration folder; MicroStation includes every <c>*.cfg</c> found there on start-up,
/// which is what loads the add-in automatically (<c>MS_DGNAPPS</c>) and registers its ribbon tab (<c>MS_RIBBONXML</c>).
/// </para>
/// </summary>
internal sealed class MicroStationInstaller : InstallerBuilder
{
    private const string AddInFolder = @"Spark.Tag4D\MicroStation\AddIn";

    /// <summary>Folder names Bentley uses below Program Files / ProgramData for the supported releases.</summary>
    private static readonly string[] ProductFolders = { "MicroStation CE", "MicroStation 2025", "MicroStation 2026" };

    public MicroStationInstaller(SetupOptions options)
        : base(options)
    {
    }

    protected override Guid UpgradeCode => new("47584796-F349-483F-A40B-413997935AD2");

    protected override string ProductName => "Spark.Tag4D for MicroStation";

    protected override string OutputName => "Spark.Tag4D-MicroStation";

    protected override string ValidatorFolder => @"Spark.Tag4D\MicroStation";

    protected override Dir[] CreateAddInDirectories()
    {
        WixEntity[] entries = BuildTree(Options.PayloadDirectory).ToArray();

        if (entries.Length == 0)
        {
            throw new InvalidOperationException($"No installable files found in {Options.PayloadDirectory}.");
        }

        foreach (string required in new[] { "Spark.Tag4D.Microstation.dll", "Spark.Tag4D.ribbon.xml" })
        {
            if (!System.IO.File.Exists(Path.Combine(Options.PayloadDirectory, required)))
            {
                throw new FileNotFoundException($"The MicroStation payload is missing {required}.");
            }
        }

        string configuration = Path.Combine(Path.GetTempPath(), $"Spark.Tag4D.cfg.{Guid.NewGuid():N}");
        System.IO.File.WriteAllText(configuration, CreateConfiguration(), new UTF8Encoding(false));

        var directories = new List<Dir>
        {
            new Dir($@"%ProgramFiles64Folder%\{AddInFolder}", entries),
        };

        directories.AddRange(ProductFolders.Select(product => new Dir(
            $@"%CommonAppDataFolder%\Bentley\{product}\Configuration\Organization",
            new File(configuration) { TargetFileName = "Spark.Tag4D.cfg" })));

        return directories.ToArray();
    }

    private static string CreateConfiguration()
    {
        // MicroStation configuration files use forward slashes and need the trailing slash on directories.
        string addIn = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), AddInFolder)
            .Replace('\\', '/') + "/";

        return string.Join(
            Environment.NewLine,
            "# Spark.Tag4D - written by the Spark.Tag4D installer.",
            "# Loads the add-in on every MicroStation start and adds the Tag4D ribbon tab.",
            $"MS_ADDIN_DEPENDENCYPATH > {addIn}",
            $"MS_ADDINPATH            > {addIn}",
            "MS_DGNAPPS              > Spark.Tag4D.Microstation",
            $"MS_RIBBONXML            > {addIn}Spark.Tag4D.ribbon.xml",
            $"MS_ICONPATH             > {addIn}Icons/",
            string.Empty);
    }
}

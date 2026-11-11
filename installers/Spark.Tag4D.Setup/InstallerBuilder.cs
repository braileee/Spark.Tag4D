using WixSharp;
using File = WixSharp.File;

namespace Spark.Tag4D.Setup;

/// <summary>
/// Describes one MSI. The two products differ only in where the add-in goes; both ship the IFC validator.
/// </summary>
internal abstract class InstallerBuilder
{
    protected const string Manufacturer = "Spark";

    protected InstallerBuilder(SetupOptions options)
    {
        Options = options;
    }

    protected SetupOptions Options { get; }

    /// <summary>Stable upgrade code so newer versions replace older ones.</summary>
    protected abstract Guid UpgradeCode { get; }

    protected abstract string ProductName { get; }

    protected abstract string OutputName { get; }

    /// <summary>Folder (under Program Files) that receives the validator.</summary>
    protected abstract string ValidatorFolder { get; }

    /// <summary>Installation directories for the add-in itself.</summary>
    protected abstract Dir[] CreateAddInDirectories();

    public static InstallerBuilder Create(SetupOptions options) => options.Product switch
    {
        "revit" => new RevitInstaller(options),
        "microstation" => new MicroStationInstaller(options),
        _ => throw new ArgumentOutOfRangeException(nameof(options)),
    };

    public string Build()
    {
        var directories = new List<Dir>(CreateAddInDirectories())
        {
            CreateValidatorDirectory(),
        };

        var project = new Project(ProductName, directories.Cast<WixEntity>().ToArray())
        {
            GUID = UpgradeCode,
            Version = Options.Version,
            Platform = Platform.x64,
            Scope = InstallScope.perMachine,
            OutDir = Options.OutputDirectory,
            OutFileName = $"{OutputName}-{Options.Version}",
            SourceBaseDir = Options.PayloadDirectory,
            ValidateBackgroundImage = false,
            MajorUpgradeStrategy = new MajorUpgradeStrategy
            {
                UpgradeVersions = VersionRange.OlderThanThis,
                NewerProductInstalledErrorMessage = "A newer version of Spark.Tag4D is already installed.",
            },
        };

        project.ControlPanelInfo.Manufacturer = Manufacturer;
        project.ControlPanelInfo.Name = ProductName;

        Directory.CreateDirectory(Options.OutputDirectory);
        return Compiler.BuildMsi(project);
    }

    private Dir CreateValidatorDirectory()
    {
        string executable = Path.Combine(Options.ValidatorDirectory, "Spark.Tag4D.IfcValidator.exe");
        if (!System.IO.File.Exists(executable))
        {
            throw new FileNotFoundException("The IFC validator executable was not found in the validator directory.", executable);
        }

        // The executable carries the Start-menu shortcut; everything else in the folder is plain payload.
        var entries = new List<WixEntity>
        {
            new File(executable, new FileShortcut("Spark.Tag4D IFC Validator", @"%ProgramMenu%\Spark.Tag4D")),
        };

        entries.AddRange(BuildTree(Options.ValidatorDirectory, skip: executable));

        return new Dir($@"%ProgramFiles64Folder%\{ValidatorFolder}\IfcValidator", entries.ToArray());
    }

    /// <summary>Mirrors a directory on disk as WixSharp entities (files first, then sub-directories).</summary>
    protected static IEnumerable<WixEntity> BuildTree(string directory, string? skip = null)
    {
        foreach (string file in Directory.GetFiles(directory))
        {
            if (string.Equals(file, skip, StringComparison.OrdinalIgnoreCase) || Path.GetExtension(file) == ".pdb")
            {
                continue;
            }

            yield return new File(file);
        }

        foreach (string subDirectory in Directory.GetDirectories(directory))
        {
            WixEntity[] children = BuildTree(subDirectory).ToArray();
            if (children.Length > 0)
            {
                yield return new Dir(Path.GetFileName(subDirectory), children);
            }
        }
    }
}

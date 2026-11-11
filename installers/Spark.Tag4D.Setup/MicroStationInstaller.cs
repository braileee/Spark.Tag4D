using WixSharp;
using File = WixSharp.File;

namespace Spark.Tag4D.Setup;

/// <summary>
/// MicroStation add-in. MicroStation loads managed add-ins from its <c>Mdlapps</c> folder, so the staged add-in
/// output is installed there.
/// </summary>
internal sealed class MicroStationInstaller : InstallerBuilder
{
    private const string MicroStationVersion = "2025";

    public MicroStationInstaller(SetupOptions options)
        : base(options)
    {
    }

    protected override Guid UpgradeCode => new("47584796-F349-483F-A40B-413997935AD2");

    protected override string ProductName => $"Spark.Tag4D for MicroStation {MicroStationVersion}";

    protected override string OutputName => $"Spark.Tag4D-MicroStation-{MicroStationVersion}";

    protected override string ValidatorFolder => @"Spark.Tag4D\MicroStation";

    protected override Dir[] CreateAddInDirectories()
    {
        WixEntity[] entries = BuildTree(Options.PayloadDirectory).ToArray();

        if (entries.Length == 0)
        {
            throw new InvalidOperationException($"No installable files found in {Options.PayloadDirectory}.");
        }

        return new[]
        {
            new Dir($@"%ProgramFiles64Folder%\Bentley\MicroStation {MicroStationVersion}\MicroStation\Mdlapps", entries),
        };
    }
}

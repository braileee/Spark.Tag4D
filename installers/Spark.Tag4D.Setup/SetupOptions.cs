namespace Spark.Tag4D.Setup;

/// <summary>Command line of the installer builder.</summary>
internal sealed record SetupOptions(string Product, Version Version, string PayloadDirectory, string ValidatorDirectory, string OutputDirectory)
{
    public const string Usage =
        "Usage: Spark.Tag4D.Setup --product <revit|microstation> --version <x.y.z> --payload <dir> --validator <dir> [--output <dir>]";

    public static SetupOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < args.Length; index++)
        {
            string name = args[index];
            if (!name.StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length)
            {
                throw new ArgumentException($"Unexpected argument '{name}'. {Usage}");
            }

            values[name[2..]] = args[++index];
        }

        string Required(string key) =>
            values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException($"Missing --{key}. {Usage}");

        string product = Required("product").ToLowerInvariant();
        if (product is not ("revit" or "microstation"))
        {
            throw new ArgumentException($"Unknown product '{product}'. {Usage}");
        }

        if (!Version.TryParse(Required("version"), out Version? version))
        {
            throw new ArgumentException($"'{values["version"]}' is not a valid version (expected x.y.z). {Usage}");
        }

        string payload = Path.GetFullPath(Required("payload"));
        string validator = Path.GetFullPath(Required("validator"));

        foreach (string directory in new[] { payload, validator })
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directory}");
            }
        }

        string output = Path.GetFullPath(values.TryGetValue("output", out string? configured) ? configured : "artifacts/installers");
        return new SetupOptions(product, version, payload, validator, output);
    }
}

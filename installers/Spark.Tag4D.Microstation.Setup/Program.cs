using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using WixSharp;
using WixToolset.Dtf.WindowsInstaller;
using Asm = System.Reflection.Assembly;

[assembly: InternalsVisibleTo("Spark.Tag4D.Microstation.Setup.aot")]

namespace Spark.Tag4D.Microstation.Setup
{
    public class Program
    {
        public static string StartupAppDllPath =>
            Asm.GetAssembly(typeof(Program))!.Location;

        public static string FileExtensions { get; } = ".dll|.rfa|.json|.png|.xlsx";

        public static Guid ProductGuid { get; } =
            new Guid("47584796-F349-483F-A40B-413997935AD2");

        // ✅ FIX: Version must be a method/property evaluated lazily,
        //    not used in other static field initializers (ordering bug)
        public static string Version
        {
            get
            {
#if INSTALLERMS2025
                return "2025";
#else
                return string.Empty;
#endif
            }
        }

        // ✅ FIX: These now use => (expression body) so Version is
        //    evaluated at call time, not at static init time
        public static string ProductName =>
            $"Microstation IFC manager app for {Version}";

        public static string InstallerTitle =>
            $"{ProductName} Installer";

        public static string InstallerName =>
            $"PSU-Microstation-IFC-Manager-{Version}-Win-Installer";

        private static void Main()
        {
            string filepath = BuildMsi();
            Console.WriteLine($"MSI created: {filepath}");
        }

        private static string BuildMsi()
        {
            // ✅ FIX: was `asm` (undefined) — correct alias is `Asm`
            var config = Asm.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "Configuration")?.Value;

            Console.WriteLine($"Configuration: {config}");
            Console.WriteLine($"Version: {Version}");

            string targetFolderPath =
                $@"%ProgramFiles64Folder%\Bentley\MicroStation {Version}\MicroStation\Mdlapps";

            string sourceFolderPath = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    @"..\..\..\..\Spark.Tag4D.Microstation\Output\Spark.Tag4D.Microstation"
                )
            );

            Console.WriteLine($"Source: {sourceFolderPath}");
            Console.WriteLine($"Target: {targetFolderPath}");

            Dir[] dirs = GetInstallInventory(sourceFolderPath, targetFolderPath);

            var project = new Project
            {
                Name = ProductName,
                Dirs = dirs,
                Version = GetVersion(),
                GUID = ProductGuid,
                ValidateBackgroundImage = false,
                OutDir = "Build"
            };

            project.OutFileName = $"{InstallerName}-{project.Version}";

            project.MajorUpgradeStrategy = new MajorUpgradeStrategy
            {
                UpgradeVersions = VersionRange.ThisAndOlder,
                PreventDowngradingVersions = VersionRange.NewerThanThis,
                NewerProductInstalledErrorMessage = "Newer version already installed",
            };

            WixSharp.MSBuild.EmitAutoGenFiles = true;

            return Compiler.BuildMsi(project);
        }

        private static Dir[] GetInstallInventory(string sourceFolderPath, string targetFolderPath)
        {
            var allowedExtensions = FileExtensions
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            WixEntity[] tree = BuildDirTree(sourceFolderPath, sourceFolderPath, allowedExtensions);

            if (tree.Length == 0)
                throw new InvalidOperationException(
                    $"No installable files found in: {sourceFolderPath}");

            return [new Dir(targetFolderPath, tree)];
        }

        private static WixEntity[] BuildDirTree(
            string rootPath,
            string currentPath,
            HashSet<string> allowedExtensions)
        {
            var entities = new List<WixEntity>();

            // Only files in THIS folder (not recursive — subdirs handled below)
            var files = Directory
                .GetFiles(currentPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => allowedExtensions.Contains(Path.GetExtension(f)))
                .Select(f => new WixSharp.File(f));

            entities.AddRange(files);

            // Each subfolder becomes a nested Dir with its own children
            foreach (string subDir in Directory.GetDirectories(currentPath))
            {
                WixEntity[] children = BuildDirTree(rootPath, subDir, allowedExtensions);

                if (children.Length > 0)
                {
                    string folderName = Path.GetFileName(subDir);
                    entities.Add(new Dir(folderName, children));

                    Console.WriteLine(
                        $"[{Path.GetRelativePath(rootPath, subDir)}] {children.Length} item(s)");
                }
            }

            return entities.ToArray();
        }

        private static Version GetVersion()
        {
            Asm assembly = Asm.LoadFrom(StartupAppDllPath);
            return assembly.GetName().Version!;
        }

        private static void OnStart(SetupEventArgs e)
        {
            var processes = Process.GetProcesses().ToList();

            if (processes.Any(p => p.ProcessName.Equals(
                    "microstation", StringComparison.OrdinalIgnoreCase)))
            {
                e.Result = ActionResult.Failure;

                if (!e.IsUISupressed)
                {
                    // ✅ FIX: was MessageBox.Show(e.Session.GetMainWindow(), ...)
                    //    which is the wrong overload; use WinForms directly
                    System.Windows.Forms.MessageBox.Show(
                        "Setup aborted because MicroStation is running.");
                }
            }
        }

        private static void LogMessage(Session session, string message)
        {
            session?.Log(message);
        }
    }
}
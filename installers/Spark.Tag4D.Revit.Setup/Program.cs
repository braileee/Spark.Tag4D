using System.Diagnostics;
using System.Runtime.CompilerServices;
using WixSharp;
using WixToolset.Dtf.WindowsInstaller;
using Asm = System.Reflection.Assembly;

[assembly: InternalsVisibleTo(assemblyName: "Spark.Tag4D.Revit.Setup.aot")] // assembly name + '.aot suffix

namespace Spark.Tag4D.Revit.Setup
{
    public class Program
    {
        public static string StartupAppDllPath
        {
            get
            {
                return Asm.GetAssembly(typeof(Program))!.Location;
            }
        }

        public static string FileExtensions { get; } = ".dll|.rfa|.json|.png|.xlsx";

        /// <summary>Revit bundle folder name.</summary>
        public static string BundleName { get; } = "Spark.Tag4D.Revit";

        public static string BundleAddinFile { get; } = "Spark.Tag4D.Revit.addin";

        /// <summary>Set product guid.</summary>
        public static Guid ProductGuid { get; } = new Guid("0812103B-3D68-4BE2-9A5C-B6ECBCEB605E");

        /// <summary>Set product name.</summary>
        public static string ProductName { get; } = $"Revit IFC manager app for Revit {RevitVersion}";

        /// <summary>Set installer title.</summary>
        public static string InstallerTitle { get; } = $"{ProductName} Installer";

        /// <summary>Set installer output name template.</summary>
        public static string InstallerName { get; } = $"PSU-Revit-IFC-Manager-{RevitVersion}-Win-Installer";

        public static string RevitVersion
        {
            get
            {

#if INSTALLERREVIT2023
                return "2023";
#elif INSTALLERREVIT2024
            return "2024";
#elif INSTALLERREVIT2025
            return "2025";
#elif INSTALLERREVIT2026
            return "2026";
#endif
            }
        }

        /// <summary>Produces MSI with digital signature.</summary>
        private static void Main()
        {

            string filepath = BuildMsi();
        }

        /// <summary>Produces msi.</summary>
        /// <returns>Path to MSI file</returns>
        private static string BuildMsi()
        {
            Environment.SetEnvironmentVariable("AppDataFolder", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

            string commonFolderPath = $"%AppDataFolder%\\Autodesk\\Revit\\Addins\\";

            string pluginsFolderPath = Path.Combine(commonFolderPath, RevitVersion);

            string bundleFolderPath = Path.Combine(pluginsFolderPath, BundleName);

            Dir[] dirs = GetInstallInvetory(pluginsFolderPath, bundleFolderPath);

            Project project = new Project()
            {
                Name = ProductName,
                Dirs = dirs,
                Version = GetVersion(),
                GUID = ProductGuid,
                ValidateBackgroundImage = false,
                SourceBaseDir = bundleFolderPath,
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

        /// <summary>Gets install directories with files to be installed.</summary>
        /// <returns>Array of install directories</returns>
        private static Dir[] GetInstallInvetory(string applicationPluginsFolder, string bundleFolderPath)
        {
            Files contentFiles = Files.FromBuildDir(bundleFolderPath, FileExtensions);
            Dir contentDir = new(bundleFolderPath, contentFiles);
            Dir addinFolder = new Dir(applicationPluginsFolder, new WixSharp.File(Path.Combine(applicationPluginsFolder, BundleAddinFile)));

            return [contentDir, addinFolder];
        }

        /// <summary>During generating installer it will retrieve version from assembly.</summary>
        /// <returns>Version</returns>
        private static Version GetVersion()
        {
            Asm assembly = Asm.LoadFrom(StartupAppDllPath);

            return assembly.GetName().Version!;
        }

        /// <summary>Gets install directory path.</summary>
        /// <param name="session">Session object</param>
        /// <returns>Path to install directory</returns>

        /// <summary>Prevents install if AutoCAD not present in os.</summary>
        /// <param name="e">Args</param>
        private static void OnStart(SetupEventArgs e)
        {
            List<Process> processes = Process.GetProcesses().ToList();

            if (processes.Any(p => p.ProcessName == "revit"))
            {
                e.Result = ActionResult.Failure;

                if (!e.IsUISupressed)
                {
                    MessageBox.Show(e.Session.GetMainWindow(), "Setup had to be aborted because Revit is running.");
                }
            }
        }

        /// <summary>Logs messages during OnStart event.</summary>
        /// <param name="session">Install session</param>
        /// <param name="message">Message</param>
        private static void LogMessage(Session session, string message)
        {
            if (session != null)
            {
                session.Log(message);
            }
        }
    }
}

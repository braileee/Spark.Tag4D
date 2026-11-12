# Spark.Tag4D

IFC property tagging for **Autodesk Revit** and **Bentley MicroStation**, plus a standalone **IFC validator**.
Rules (property sets, picklists, matches, expressions, composed properties, layer mappings) live in one Excel workbook
and are edited from the built-in settings window.

## Repository layout

| Path | Purpose |
| --- | --- |
| `src/Spark.Tag4D.Core` | UI-free core (`netstandard2.0`): XLSX engine, expression engine, settings, MVVM base, domain models |
| `src/Spark.Tag4D.UI` | Shared WPF design system (`Themes/Tag4D.xaml`) and file dialogs |
| `src/Spark.Tag4D.Settings` | Settings editor window shared by Revit and MicroStation |
| `src/Spark.Tag4D.Revit.Core` / `.Revit.App` / `.Revit` | Revit helpers, commands + tagging pane, add-in entry point |
| `src/Spark.Tag4D.Microstation` | MicroStation add-in |
| `src/Spark.Tag4D.IfcValidator` | Standalone validator (the only third-party runtime dependency is **Xbim**) |
| `installers/Spark.Tag4D.Setup` | Builds the two MSI installers (WixSharp, build-time only) |
| `tests/Spark.Tag4D.Core.Tests` | Unit tests for the core |

## Building

Open `Spark.Tag4D.slnx` and pick a solution configuration. Names are `<Debug|Release> <target>`:

| Target | Builds |
| --- | --- |
| `R2023` `R2024` `R2025` `R2026` | the Revit add-in for that release (and its shared libraries) |
| `MSCE` `MS2025` `MS2026` | the MicroStation add-in for CE / 2025 / 2026 |
| `IfcValidator` | the standalone IFC validator |
| `Installer` | the installer builder |
| *(none)* | everything (Revit 2025, MicroStation 2025) |

`src/Spark.Tag4D.Revit` and `src/Spark.Tag4D.Microstation` ship launch profiles for every supported release
(`Revit 2023` … `Revit 2026`, `MicroStation CE/2025/2026`) plus one that follows the active configuration
(`Revit (matches configuration)`, `MicroStation (matches configuration)`). A **Debug** build installs itself into the
matching Revit / MicroStation, so F5 with a matching profile loads the add-in.

```bash
dotnet test tests/Spark.Tag4D.Core.Tests                                   # core tests (any OS)
dotnet build src/Spark.Tag4D.Revit -c "Release R2025"                      # or: -c Release -p:RevitVersion=2025
dotnet build src/Spark.Tag4D.Microstation -c "Debug MS2026"                # or: -p:MicroStationVersion=2026 (CE | 2025 | 2026)
dotnet publish src/Spark.Tag4D.IfcValidator -c Release -r win-x64 --self-contained
```

* Revit 2023/2024 build for .NET Framework 4.8, 2025+ for .NET 8 (`build/Revit.props`). The Revit API comes from NuGet.
  Debug builds copy the add-in to `%APPDATA%\Autodesk\Revit\Addins\<year>`; disable with `-p:DeployToRevit=false`.
* MicroStation CE (10.17), 2025 and 2026 all run managed add-ins on .NET Framework, so one `net48` build serves all three
  (`build/MicroStation.props`). The Bentley API is referenced from the NuGet package `Speckle.MicroStation.API`
  (a third-party re-publication of the MicroStation CE 10.16 assemblies, compile-time only - nothing Bentley is copied
  to the output). Debug builds copy the add-in to `%LOCALAPPDATA%\Spark.Tag4D\MicroStation\<version>` and write
  `Spark.Tag4D.cfg` into the `Organization` configuration folder of that MicroStation (disable with
  `-p:DeployToMicroStation=false`).
* Build output goes to `artifacts/` (see `Directory.Build.props`); package versions are centralised in `Directory.Packages.props`.

### How MicroStation loads the add-in

`Spark.Tag4D.cfg` (in `%ProgramData%\Bentley\<MicroStation>\Configuration\Organization`, which MicroStation includes on
start-up) sets `MS_ADDINPATH`, `MS_ADDIN_DEPENDENCYPATH` and `MS_DGNAPPS`, so the add-in is loaded automatically.
`MS_RIBBONXML` registers `Spark.Tag4D.ribbon.xml`, which adds a **Tag4D** tab (Settings, Load Parameters, Tag Elements)
to the Drawing, Modeling and Visualization workflows. The buttons run the `Tag4D ...` key-ins from `commands.xml`.
The key-ins also work from the key-in browser if the ribbon is unavailable.

## Branching and releases

* `develop` – integration branch; every push/PR is built and tested.
* `main` – released code. **Push a tag `vX.Y.Z` on a commit that is on `main`** and the `Build` workflow produces
  exactly two installers and attaches them to a GitHub release:
  * `Spark.Tag4D-Revit-X.Y.Z.msi` – one MSI for Revit 2023–2026 (Autodesk application-plugin bundle in `%ProgramData%`)
  * `Spark.Tag4D-MicroStation-X.Y.Z.msi` – MicroStation CE, 2025 and 2026 (add-in below Program Files, `Spark.Tag4D.cfg` per release for autoload)

  Both installers contain the IFC validator (`%ProgramFiles%\Spark.Tag4D\<product>\IfcValidator`, Start-menu shortcut included).
* Run the workflow manually with *build_installers* ticked to get the MSIs as workflow artifacts without a release.

## Design notes

* **No third-party runtime libraries** except Xbim in the validator. Excel files are read and written by the in-house
  `Spark.Tag4D.Core.Spreadsheets` (`.xlsx` only; formatting, tables and styles of existing workbooks are preserved),
  expressions by `Spark.Tag4D.Core.Expressions` (a small safe language, no dynamic compilation), MVVM by `Core.Mvvm`,
  UI styling by `Spark.Tag4D.UI`. JSON uses `System.Text.Json`.
* **Expression language** (Expressions sheet, *Function* column): `==VALUE` / `!=VALUE`, or a member chain on the source
  value such as `.Split('.')[0]`, `.Substring(0, 3).ToUpper()`, `.Replace('-', '_')`, `.PadLeft(3, '0')`.
* User preferences (last used files) are stored in `%APPDATA%\Spark.Tag4D\user-settings.json`.

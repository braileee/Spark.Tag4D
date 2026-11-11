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

```bash
dotnet test tests/Spark.Tag4D.Core.Tests                       # core tests (any OS)
dotnet build src/Spark.Tag4D.Revit -c Release -p:RevitVersion=2025   # 2023, 2024, 2025 or 2026
dotnet publish src/Spark.Tag4D.IfcValidator -c Release -r win-x64 --self-contained
```

* Revit 2023/2024 build for .NET Framework 4.8, 2025+ for .NET 8 (`build/Revit.props`). The Revit API comes from NuGet.
  Add `-p:DeployToRevit=true` to copy a build into `%APPDATA%\Autodesk\Revit\Addins\<year>` while developing.
* MicroStation's assemblies are not on NuGet. Point the build at an installation with
  `-p:MicroStationDir="C:\Program Files\Bentley\MicroStation 2025\MicroStation"`.
* Build output goes to `artifacts/` (see `Directory.Build.props`); package versions are centralised in `Directory.Packages.props`.

## Branching and releases

* `develop` – integration branch; every push/PR is built and tested.
* `main` – released code. **Push a tag `vX.Y.Z` on a commit that is on `main`** and the `Build` workflow produces
  exactly two installers and attaches them to a GitHub release:
  * `Spark.Tag4D-Revit-X.Y.Z.msi` – one MSI for Revit 2023–2026 (Autodesk application-plugin bundle in `%ProgramData%`)
  * `Spark.Tag4D-MicroStation-2025-X.Y.Z.msi` – MicroStation 2025 (`Mdlapps`)

  Both installers contain the IFC validator (`%ProgramFiles%\Spark.Tag4D\<product>\IfcValidator`, Start-menu shortcut included).
* Repository secret `MICROSTATION_ASSEMBLIES_URL` must point to a zip that mirrors the MicroStation program folder; it is
  required for tag builds and optional otherwise (the MicroStation job is then skipped).
* Run the workflow manually with *build_installers* ticked to get the MSIs as workflow artifacts without a release.

## Design notes

* **No third-party runtime libraries** except Xbim in the validator. Excel files are read and written by the in-house
  `Spark.Tag4D.Core.Spreadsheets` (`.xlsx` only; formatting, tables and styles of existing workbooks are preserved),
  expressions by `Spark.Tag4D.Core.Expressions` (a small safe language, no dynamic compilation), MVVM by `Core.Mvvm`,
  UI styling by `Spark.Tag4D.UI`. JSON uses `System.Text.Json`.
* **Expression language** (Expressions sheet, *Function* column): `==VALUE` / `!=VALUE`, or a member chain on the source
  value such as `.Split('.')[0]`, `.Substring(0, 3).ToUpper()`, `.Replace('-', '_')`, `.PadLeft(3, '0')`.
* User preferences (last used files) are stored in `%APPDATA%\Spark.Tag4D\user-settings.json`.

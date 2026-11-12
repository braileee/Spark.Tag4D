# Spark.Tag4D

IFC property tagging for Autodesk Revit and Bentley MicroStation, plus a standalone IFC validator. All rules live in one
Excel workbook edited through the shared Settings window. See `README.md` for the full layout and release process.

## Build and test

Solution: `Spark.Tag4D.slnx` (needs SDK 9+; CI uses 9.0.x). Solution configurations are `<Debug|Release> <target>`:
`R2023`..`R2026` (Revit), `MSCE` / `MS2025` / `MS2026` (MicroStation), `IfcValidator`, `Installer`; plain `Debug`/`Release`
build everything.

```bash
dotnet build Spark.Tag4D.slnx -c "Debug R2025" -p:DeployToRevit=false
dotnet build src/Spark.Tag4D.Revit -c "Release R2024"          # or -c Release -p:RevitVersion=2024
dotnet build src/Spark.Tag4D.Microstation -c "Debug MS2026"    # or -p:MicroStationVersion=CE|2025|2026
dotnet test tests/Spark.Tag4D.Core.Tests
```

- Revit/MicroStation version comes from the configuration name (`build/Revit.props`, `build/MicroStation.props`);
  `BuildFlavor` (Debug/Release) is derived in `Directory.Build.props`. Output goes to `artifacts/`.
- **Debug builds deploy automatically** to the matching Revit add-ins folder / MicroStation (`Spark.Tag4D.cfg` in the
  version's `Organization` folder). Pass `-p:DeployToRevit=false -p:DeployToMicroStation=false` for plain builds.
- Package versions are central (`Directory.Packages.props`). The Bentley API is the NuGet package
  `Speckle.MicroStation.API` (compile-time only).
- The WPF temp project of the multi-targeted libraries (`Spark.Tag4D.UI`, `.Settings`) occasionally fails with
  CS2012 / MSB3061 "file in use" on `artifacts/obj/.../debug_`. It is a known race; rerun the build.

## Architecture

- `Core` (netstandard2.0, nullable on): XLSX engine, expressions, settings, MVVM base (`ObservableObject`,
  `RelayCommand`), `TagData.Load()` reads settings + workbook in one go.
- `UI`: `Themes/Tag4D.xaml` design system, `FileDialogs`, `Behaviors/ComboBoxBehavior` (mouse-wheel lock).
- `Settings`: tabs are `SettingsPageViewModel`s (`Title`, `SheetInfo`, `IsDirty`, `Save()`); `SettingsViewModel` owns the
  shared Save/Cancel and `SaveAll()`; `SettingsView` asks about unsaved changes on close.
- `Revit.App`: `ParametersCommand` (modeless Parameters window; load/delete parameters, IFC mapping file) and the Tag
  Elements dockable pane. Revit API work runs through `RevitEventWrapper<T>` external events.
- `Microstation`: net48 add-in loaded via `MS_DGNAPPS`; keyins in `commands.xml`; ribbon in
  `Ribbon/Spark.Tag4D.ribbon.xml` (registered with `MS_RIBBONXML`); icons in `Icons/`.

## Conventions and gotchas

- Revit.App, Revit and Settings projects use `Nullable=disable`; Core and the MicroStation csproj differ - check the csproj.
- Revit 2023/2024 build for net48, 2025+ for net8; version-specific API uses `#if REVIT2024 || ...`.
- A WPF root element (`Window`/`Page`) does not pick up an implicit style from its own resources: set
  `Background`/`Foreground`/`FontFamily` explicitly on it (otherwise the Revit pane turns dark).
- `pack://application:,,,/Spark.Tag4D.UI;...` URIs resolve by assembly name, so the add-ins register an
  `AssemblyResolve` handler for sibling `Spark.Tag4D.*` assemblies (`Tag4DApplication`, MicroStation `App`).
- The installers need WiX 4 (`wix` tool) and are built by CI only.
- MicroStation autoload/ribbon and the Revit UI have been checked by offscreen rendering and builds, not end-to-end in
  the host applications; verify there after UI or add-in loading changes.
- Branching: work on `develop`, release by tagging `vX.Y.Z` on `main`. Commit messages end with the Claude co-author line.

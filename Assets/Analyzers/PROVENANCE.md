# Vendored analyzer provenance

- `Microsoft.Unity.Analyzers.dll` v1.27.0, from NuGet
  (https://www.nuget.org/packages/Microsoft.Unity.Analyzers/1.27.0).
- License: MIT — © Microsoft Corporation
  (https://github.com/microsoft/Microsoft.Unity.Analyzers/blob/main/LICENSE.md).
- The DLL lives at `Assets/Microsoft.Unity.Analyzers.dll` (repo root of Assets).
  Applied project-wide via **`Assets/csc.rsp`** (`-analyzer:` + `-ruleset:` flags) —
  empirically, on Unity 6000.5 the `RoslynAnalyzer` label alone did not attach the
  analyzer to asmdef assemblies; the label is kept (harmless, and other tooling reads
  it) but csc.rsp is the load-bearing wiring. All plugin platforms disabled in the
  .meta — analyzers are compiler plugins, not runtime code.
- Update procedure: download the new .nupkg, replace the DLL, keep this file current.

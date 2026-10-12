# Warning suppressions in the shared folder

[← Conventions](conventions.md)

Convention 12 suppresses no trim, AOT or single-file warning, and convention 17 makes every warning an
error. This inventory, in the shape of Broiler.VM's rule N36, lists every warning suppression in a
source or project file below `Broiler.ECMAScript/` by file: a `#pragma warning disable`, a
`SuppressMessage` or `UnconditionalSuppressMessage` attribute, a non-empty `NoWarn`, a
`WarningsNotAsErrors` or a `SuppressTrimAnalysisWarnings` element. Rule SL5 scans the folder and holds
this section to what it finds: each suppression is listed here, and when the scan finds none the
section says None.

The shared folder's `Directory.Build.props` sets `NoWarn` **empty**, which resets the suppressions the
packaging file adds for the engine; that is not a suppression and is not listed. The witnesses of the
rules, which exist to break them, are not scanned.

## Inventory

None.

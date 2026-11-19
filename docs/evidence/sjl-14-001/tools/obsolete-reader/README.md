# Part F's metadata reader

Bundle `sjl-14-001`'s part F ([`decision-rule.md`](../../decision-rule.md)): "a metadata reader, committed to the bundle,
listing the [Obsolete] attribute, its diagnostic identifier and its message on every public type of
Broiler.JavaScript.Ast and Broiler.JavaScript.Parser". Drafted on 2026-11-19 on window W2's branch, before any run of part
F. It reads metadata only, from an assembly or from a package's `lib/net10.0/` assemblies, and also lists each
assembly's type forwarders; `Program.cs`'s header gives the columns. Every mark of window W2 carries the diagnostic
identifier `BJS0002` (`Broiler.JS/Broiler.JavaScript.Ast/ObsoleteMarks.cs`); the reader prints the identifier it finds
and checks none.

    dotnet run --project docs/evidence/sjl-14-001/tools/obsolete-reader -c Release -- \
        <artifacts>/Broiler.JavaScript.Ast.<version>.nupkg <artifacts>/Broiler.JavaScript.Parser.<version>.nupkg

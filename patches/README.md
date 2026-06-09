# Out-of-tree patches

Changes that belong in a submodule repository but could not be pushed from the
environment where they were prepared (the session was scoped to `MaiRat/Broiler.JS`
only). Each patch is a `git diff` taken at the submodule root and applies cleanly
with `git apply` from inside the submodule checkout.

## `broiler-unicode-problem10-compound-units.patch`

Fixes **Problem 10** of [#723](https://github.com/MaiRat/Broiler.JS/issues/723):
`Intl.NumberFormat` with `style: "unit"`, `unit: "kilometer-per-hour"` returned the
literal identifier (`"987 kilometer-per-hour"`) instead of the locale's precomputed
form.

The curated CLDR unit set in `Broiler.Unicode` omitted the ECMA-402 sanctioned
compound units. The patch adds `kilometer-per-hour`, `mile-per-hour` and
`mile-per-gallon` to `CldrUnitsParser.CuratedUnits` (the CLDR category prefix is
stripped, so `speed-kilometer-per-hour` becomes `kilometer-per-hour`), re-trims the
15 per-locale `units.json` slices, and regenerates `CldrUnitData.g.cs`. The
precomputed locale forms are then used, verified against the test262 fixtures:

| locale | short | long | narrow |
| --- | --- | --- | --- |
| en-US | `-987 km/h` | `-987 kilometers per hour` | `-987km/h` |
| de-DE | `-987 km/h` | `-987 Kilometer pro Stunde` | `-987 km/h` |
| ja-JP | `-987 km/h` | `時速 -987 キロメートル` | `-987km/h` |

Fixes `intl402/NumberFormat/prototype/format/unit-{en-US,de-DE,ja-JP}.js`.

### Applying it

```bash
# from the Broiler.JS checkout
cd Broiler.Unicode
git checkout -b claude/awesome-ramanujan-01qa92
git apply ../patches/broiler-unicode-problem10-compound-units.patch
git commit -am "CLDR units: include ECMA-402 compound units (kilometer-per-hour, etc.)"
git push -u origin claude/awesome-ramanujan-01qa92
# open a PR on MaiRat/Broiler.Unicode, then bump the submodule pointer in Broiler.JS
```

Equivalent to editing `CuratedUnits` by hand and running
`dotnet run --project src/UnicodeCldr.LocaleData.DataTool -- update 48.2.0`
(the patch just bakes in the regenerated data so no network/regeneration is needed).

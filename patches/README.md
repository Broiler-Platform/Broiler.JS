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

## `broiler-unicode-issue887-emoji-17.patch`

Fixes the **Unicode 17.0 emoji** cluster (Problems 134–137) of
[#887](https://github.com/MaiRat/Broiler.JS/issues/887) — and the resurfacing of the
same four tests as **Problems 121–124** of
[#889](https://github.com/MaiRat/Broiler.JS/issues/889): `\p{Basic_Emoji}`,
`\p{RGI_Emoji_Modifier_Sequence}`, `\p{RGI_Emoji_ZWJ_Sequence}` and `\p{RGI_Emoji}`
(properties of strings, `v` flag) missed sequences added in Unicode/Emoji 17.0 — for
example `\p{Basic_Emoji}` did not match 🛘 (U+1F6D8 LANDSLIDE).

The bundled emoji tables in `Broiler.Unicode` were generated from Emoji 16.0. The patch
adds the Emoji 17.0 source data under `data/unicode/17.0/`
(`emoji-test.txt`, `emoji-sequences.txt`, `emoji-zwj-sequences.txt`) and regenerates
`src/UnicodeEmoji.StringProperties/Generated/EmojiTrieData.g.cs`
(`UnicodeEmojiVersion` → `"17.0"`, 3790 → 3953 sequences). `EmojiStringProperties`
and the `JSRegExp` v-flag property-of-strings path then match the full Emoji 17.0 set.

Fixes test262 `built-ins/RegExp/property-escapes/generated/strings/{Basic_Emoji,
RGI_Emoji_Modifier_Sequence,RGI_Emoji_ZWJ_Sequence}.js` and
`built-ins/RegExp/unicodeSets/generated/rgi-emoji-17.0.js`.

### Applying it

```bash
# from the Broiler.JS checkout
cd Broiler.Unicode
git checkout -b claude/brave-shannon-r7q7om
git apply ../patches/broiler-unicode-issue887-emoji-17.patch
git add -A   # the patch introduces new files under data/unicode/17.0/
git commit -m "emoji: regenerate string-property tables from Unicode/Emoji 17.0"
git push -u origin claude/brave-shannon-r7q7om
# open a PR on MaiRat/Broiler.Unicode, then bump the submodule pointer in Broiler.JS
```

Equivalent to running
`dotnet run --project src/UnicodeEmoji.StringProperties.DataTool -- update 17.0`
(the patch bakes in the downloaded data and regenerated tables so no network access is
needed; the official `unicode.org` host is not on the environment's egress allowlist, so
the data files were mirrored from `unicode-org/unicodetools`).

# Broiler.VM's arm of the differential check: pinned copies of its code at its SJL-9 rule commit

Broiler.VM's JSD-0082, decision D22, makes the gate of each stage of milestone SJL-9.3 a **differential check against
Broiler.VM's code at its rule commit, beside the library, test-only and JIT only**. This folder is that code: copies of
Broiler.VM's source at its SJL-9 rule commit, **`4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f`** (Broiler-Platform/Broiler.VM#171),
compiled into `Broiler.ECMAScript.Intl.Tests` and into nothing else.

**How Broiler.VM's code is referenced.** By a pinned copy in this test project, not by the packed profile: the members the
check compares are internal to `Broiler.VM.Profile.JavaScript`, which no package of this repository may reference
(rule SL1 holds the libraries; nothing here is a library), and Broiler.VM's packages reach Broiler.JS through no feed. A
copy is never packed: the test project sets `IsPackable` false, and the library's own project references nothing here.
It runs under the JIT only; the Native AOT probe compiles none of it.

**The rules for a copy.**
- Each file is `git show 4cf9ca44:<path>` with only the changes its header lists: the namespace
  (`Broiler.ECMAScript.Intl.Tests.Differential.Vm`), the generated SPDX and assurance header and the per-member
  assurance annotations removed, and, where named, members and types removed whole that the check does not compare and
  that would pull in code of another milestone's library (a formatter, the collator), with the `using` lines only they
  needed. No line that decides an answer is edited.
- The realm's copies (`JsRealm.*.cs`, from stage SJL-9.3a) run over the stand-ins in `../VmEngine.cs`: the smallest
  engine object model the copied members name (script values, objects and properties, built-in functions, the thrown
  error, `JsEngine`'s reads and the realm members they call). The stand-ins are not copies and decide no answer: they
  carry the harness's Strings, Booleans and option bags to the copied code and its answers back, and refuse any value
  the population never gives.
- A copy is never edited afterwards. Another revision of Broiler.VM's code is another copy, from a named commit. A second copy
  of a file at the same commit, keeping members the first removed, is a file of its own, named for what it keeps, and the
  two hold no member in common (`JsRealm.NumberFormat.DigitOptions.cs` beside `JsRealm.NumberFormat.cs`, from stage
  SJL-9.3b).
- Each stage adds the copies its sections compare, with a row below; `VmCopiesTests` holds every file of this folder to
  its row's SHA-256, and every row to a file.

| File | Source at `4cf9ca44` | Git blob | Bytes | SHA-256 of the copy |
|---|---|---|---|---|
| `JsBigInt.cs` | `src/Broiler.VM.Profile.JavaScript/JsBigInt.cs` | `8898b1970980797a0b10fe6581af4b14f5804678` | 8154 | `dd3cb8550ef7d18cd252a2aa76861a09a634e97e8f550e7552a38cb34a8c0812` |
| `JsDateData.cs` | `src/Broiler.VM.Profile.JavaScript/JsDateData.cs` | `fe1b2d5a850dbdd7b170518d36e3fcab25a020ce` | 4952 | `1c677234e25933e1f6820165e8ec2c0a24a871e181b2e920169bab3641093ce8` |
| `JsDecimal.cs` | `src/Broiler.VM.Profile.JavaScript/JsDecimal.cs` | `de0c9461fc759cb63a07024cc7cbb211a9725c83` | 18599 | `c1e644466df6eac49ff2a29eb662ae23e9464c672493ea24a0bcc01fbc3343ed` |
| `JsIntlData.cs` | `src/Broiler.VM.Profile.JavaScript.Format/JsIntlData.cs` | `fd0dd0f90140eeeb9a5b2f31a8f600d12dd98f84` | 8366 | `4af5985aa04684feaf0e832ff56f17e234c568bb7199fb88c68bf3792eb9452d` |
| `JsIntlTables.cs` | `src/Broiler.VM.Profile.JavaScript/JsIntlTables.cs` | `46f1856060283e74727ccfc0b87f9872cfccb0e6` | 16340 | `382a523d87268ee174bfa7c987db3cd76af8b3730530f6b764ae87c4dc6ffd18` |
| `JsLocaleInfo.cs` | `src/Broiler.VM.Profile.JavaScript/JsLocaleInfo.cs` | `5cc13ac82844c7ce0a301ae21ed1a6d9f1cc444c` | 2225 | `6f57a8bfb9717c6f101211bdbe19b64bea94a0ddaed15fc0f6d6ac13e8b9d678` |
| `JsLocaleTag.cs` | `src/Broiler.VM.Profile.JavaScript/JsLocaleTag.cs` | `7201ae19b2714d2916e7bd5d9b77c4cf2bfa9a59` | 25717 | `b42b6735e730d5e08d0c74aaaaf5f97e3d02b8b5ef92e15c3739af75153a11d2` |
| `JsNumberData.cs` | `src/Broiler.VM.Profile.JavaScript/JsNumberData.cs` | `a0373603e82f719389f0386e089384f5a6a48708` | 11776 | `5ce292f128c1e363e5d15f13e37eb9812250a1b84fafe9373e1723a110291ff6` |
| `JsNumberFormat.cs` | `src/Broiler.VM.Profile.JavaScript/JsNumberFormat.cs` | `c65c7914ced68db36cf939d9ea2880847c62bbe2` | 3936 | `0a65ec6e3fff21e3bb5f2d24ced264df33e797c2749fcd6ddbbe8ec31bc0d1c5` |
| `JsNumberFormatter.cs` | `src/Broiler.VM.Profile.JavaScript/JsNumberFormatter.cs` | `e31736af3c25d85e91b2652ea0f849de0f34ed44` | 7321 | `26fc3f9311e7b515d9fcdc51ec210c06ef990d97116bb62fe69957306f59bb71` |
| `JsPluralRule.cs` | `src/Broiler.VM.Profile.JavaScript/JsPluralRule.cs` | `5ba6931e402f6bca873448b019f36905033fd61c` | 8063 | `e3d83d18cbaa6fdc37658d830bd24bc1a2143db3360073164637592a3c67ee6f` |
| `JsRealm.DateTimeFormat.cs` | `src/Broiler.VM.Profile.JavaScript/JsRealm.DateTimeFormat.cs` | `bff1811cd34a91f50a9840a2bfc4de81bfb31161` | 4988 | `d22c98a67ef3b08375e7a698caabfe877d4c989fee538d3ac3c31df3128843b4` |
| `JsRealm.Intl.cs` | `src/Broiler.VM.Profile.JavaScript/JsRealm.Intl.cs` | `b75e44c777594b41d36a06ddaa366708b83c4159` | 13150 | `5abafd99db6b8f8fe72df71220933652a273b4626ffc77064efd370c3b69c145` |
| `JsRealm.Locale.cs` | `src/Broiler.VM.Profile.JavaScript/JsRealm.Locale.cs` | `d70618ca673e4d30b47b1e3424d7a734253b68ef` | 23796 | `9bd5f8ad8a81f71559d6a12010e97318ce2e3a1ecab57da9ce351db751f2d302` |
| `JsRealm.NumberFormat.DigitOptions.cs` | `src/Broiler.VM.Profile.JavaScript/JsRealm.NumberFormat.cs` | `7ae56e7fcc1a01aa976edb86b137b30bb30fd309` | 17813 | `74def44fad28f8e14c56a851410da0495c4eccc3cd114f7554ede9c9e62e8a2a` |
| `JsRealm.NumberFormat.cs` | `src/Broiler.VM.Profile.JavaScript/JsRealm.NumberFormat.cs` | `7ae56e7fcc1a01aa976edb86b137b30bb30fd309` | 2446 | `2282bad9905b42d8eb1d41213cbd9d6ab4245385efae99b2562d2761b3e99360` |
| `JsRealm.PluralRules.cs` | `src/Broiler.VM.Profile.JavaScript/JsRealm.PluralRules.cs` | `a6358f49750fd30674c07b8f73e7ff34c2b2b06b` | 8409 | `0fac45951c3d2bd583d2d3a8f5a19da96b60b65e85182298852574a1008fbd0d` |

**What the copies are compared on.** The check's sections are in `../`: D0, the table decoders, from stage SJL-9.3a's
skeleton (the plan's task 5.0), over the six decoder copies; and L1 to L4, locale identifiers, negotiation, Intl.Locale
and `Intl.supportedValuesOf`, from stage SJL-9.3a's task 5.a (`../LocaleSection.cs`, its population in
`../LocalePopulation.cs`), over `JsLocaleTag.cs` and the four `JsRealm` copies; and M1 to M3, P1 and P2, the Intl
mathematical value, the digit options, FormatNumericToString, plural rules and plural ranges, from stage SJL-9.3b's task 5.b
(`../DecimalSection.cs`, its population in `../DecimalPopulation.cs`), over `JsDecimal.cs`, `JsNumberFormat.cs`,
`JsBigInt.cs`, `JsNumberFormatter.cs`, `JsRealm.NumberFormat.DigitOptions.cs`, `JsRealm.PluralRules.cs` and the plural
copies of stage SJL-9.3a (`JsPluralRule.cs`, `JsNumberData.cs`). Each section's population, its counts
and every difference's class are in its report (`dotnet Broiler.ECMAScript.Intl.Tests.dll differential`, `../Program.cs`).

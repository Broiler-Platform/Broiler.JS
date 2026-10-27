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
  assurance annotations removed, and, where named, members removed whole that the check does not compare and that would
  pull in code of another milestone's library. No line that decides an answer is edited.
- A copy is never edited afterwards. Another revision of Broiler.VM's code is another copy, from a named commit.
- Each stage adds the copies its sections compare, with a row below; `VmCopiesTests` holds every file of this folder to
  its row's SHA-256, and every row to a file.

| File | Source at `4cf9ca44` | Git blob | Bytes | SHA-256 of the copy |
|---|---|---|---|---|
| `JsDateData.cs` | `src/Broiler.VM.Profile.JavaScript/JsDateData.cs` | `fe1b2d5a850dbdd7b170518d36e3fcab25a020ce` | 4952 | `1c677234e25933e1f6820165e8ec2c0a24a871e181b2e920169bab3641093ce8` |
| `JsIntlData.cs` | `src/Broiler.VM.Profile.JavaScript.Format/JsIntlData.cs` | `fd0dd0f90140eeeb9a5b2f31a8f600d12dd98f84` | 8366 | `4af5985aa04684feaf0e832ff56f17e234c568bb7199fb88c68bf3792eb9452d` |
| `JsIntlTables.cs` | `src/Broiler.VM.Profile.JavaScript/JsIntlTables.cs` | `46f1856060283e74727ccfc0b87f9872cfccb0e6` | 16340 | `382a523d87268ee174bfa7c987db3cd76af8b3730530f6b764ae87c4dc6ffd18` |
| `JsLocaleInfo.cs` | `src/Broiler.VM.Profile.JavaScript/JsLocaleInfo.cs` | `5cc13ac82844c7ce0a301ae21ed1a6d9f1cc444c` | 2225 | `6f57a8bfb9717c6f101211bdbe19b64bea94a0ddaed15fc0f6d6ac13e8b9d678` |
| `JsNumberData.cs` | `src/Broiler.VM.Profile.JavaScript/JsNumberData.cs` | `a0373603e82f719389f0386e089384f5a6a48708` | 11776 | `5ce292f128c1e363e5d15f13e37eb9812250a1b84fafe9373e1723a110291ff6` |
| `JsPluralRule.cs` | `src/Broiler.VM.Profile.JavaScript/JsPluralRule.cs` | `5ba6931e402f6bca873448b019f36905033fd61c` | 8063 | `e3d83d18cbaa6fdc37658d830bd24bc1a2143db3360073164637592a3c67ee6f` |

**What the copies are compared on.** The check's sections are in `../`: D0, the table decoders, from stage SJL-9.3a's
skeleton (the plan's task 5.0). Each section's population, its counts and every difference's class are in its report
(`dotnet Broiler.ECMAScript.Intl.Tests.dll differential`, `../Program.cs`).

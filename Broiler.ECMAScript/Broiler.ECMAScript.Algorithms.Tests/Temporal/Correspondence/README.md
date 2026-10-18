# Broiler.VM's answers to the calendar, duration and difference rows of correspondence `sjl-10-001`

## The calendar rows (part B)

These ten files are Broiler.VM's answers to the probes of the calendar rows C1 to C18 of SJL-10.1's
correspondence (Broiler.VM's `docs/evidence/sjl-10-001/correspondence.md`, section 6). Each is the extracted answer
`<name>.vm.txt` of `docs/evidence/sjl-10-001/correspondence-runs/syntax/`, byte for byte as Broiler.VM's commit
`2edf1b171b7b6c41d93947935cfe0cd1f4c6c2b0` ("SJL-10.1: the syntax half's probes and both engines' answers") holds
it, and as its commit `f39bd3c` still holds it. Each line is one probe and Broiler.VM's answer, `<what> => <answer>`;
the probe that wrote it is `probes/<name>.js` beside the answer there, with its `prelude.js`.

Milestone SJL-10.3 (part B) copied them on 2026-10-18 so that the library's calendar tests,
[`CalendarCorrespondenceTests`](../CalendarCorrespondenceTests.cs), replay every line the library can answer at
library level and hold the library to Broiler.VM's answer, which the correspondence records as the specification's
on every calendar row. Lines that only an engine can answer (a Temporal object lent as a calendar, row C13) are
named there and counted.

| File | Rows | Bytes | SHA-256 | Git blob |
|---|---|---|---|---|
| `c01-identifiers.vm.txt` | C1-C3 | 5,884 | `088c9ebfc0b535371e30f9a2aa080ae0ed1386c3ae2ca60e76b40ad2d90c4d63` | `3227d2cbb454324160f105200126868ebf2cb29d` |
| `c02-month-codes.vm.txt` | C4, C5, C7 | 36,663 | `7cf61ae7cc257698e1c8a0ede90b04cf4304c884a33933d7f27c6719475778f9` | `4733b7bfe8f9af75abddde83e9bab509f40ca662` |
| `c03-eras.vm.txt` | C8, C9 | 52,866 | `e13dc8f75a92bd3ced663647ff23de614f24dea9440818ba278a1088d5fa6bb0` | `751e6dcc427d99cfbaf95b1d6d831d27d116875c` |
| `c04-reference-years.vm.txt` | C10 | 80,272 | `2606ebb9d09042b72b6cb6db406d392692c0e65dd132e7be9bf6674f68358ba8` | `d641b4fabb5199e5de53061a4774a88bed078aed` |
| `c05-formula-edges.vm.txt` | C14 | 38,171 | `e663dbf8648e57821b2a7d9fc8e3377a7363d02d8badca07126dd98c5e1548ac` | `0bc4c7bda3555015b926e0ec368af357dfb3afaf` |
| `c06-table-edges.vm.txt` | C18 | 1,904 | `6e463f7121eedf7a045917735aa41bbe2b9eb996866f039112fdaa9197085cc9` | `35b2917d54a77ed99b98b16776975b450951bac0` |
| `c07-slot-and-narrowing.vm.txt` | C11, C13 | 4,738 | `690313068af54f2264c140729238fa3e8dc98218e8fdaaa24b0a9ca5e9c56a69` | `c25025c9d34f6ac3f5321d573bd39f76c8568183` |
| `c08-saturation.vm.txt` | C12 | 1,521 | `4ea726e208deb5b40865b077c4a8656a39fbd00df8b2a13dc0e8e482f03d512f` | `b0d1089b3725095f3a7dd5d701afd01a634d811c` |
| `c09-conversions.vm.txt` | C15-C17 | 11,319 | `6ffb13baf0f71575fca596160cc1b64bf45c1c2ed27ab0550ede76fb457dce32` | `67d730d5bb763cf28b31fa4e92f0e029708c9a15` |
| `c10-monthcode-digits.vm.txt` | C6 | 4,410 | `812f921f38c32a0ffad8c6172d1a3adc4fd5547c988752edeb6b40352620d5d7` | `7dcfbb165a43c12d8716040ad48a880796f68b26` |

The tests check each copy's SHA-256 against this table before they read it. `c01-identifiers.vm.txt` holds one NUL
character, the probe's `chinese\u0000` written raw in an annotation, as Broiler.VM's copy does. A copy is never
edited: another answer is another copy from another named commit.

## The duration and difference rows (part C)

These twenty files are Broiler.VM's answers to the probes of the duration rows D1 to D11 and difference rows F1 to
F8 of the same correspondence (its sections 3 and 4), with rows R7 and B5 where their probes add a duration. Each is the
extracted answer `<name>.vm.txt` of `docs/evidence/sjl-10-001/correspondence-runs/core/`, byte for byte as Broiler.VM's
commit `c141a0cf7dd0bdf759be2aaf597036290bc07b78` ("SJL-10.1: the core half of the correspondence from code, with its
runs") holds it, and as its commit `f39bd3c` still holds it; the probe that wrote it is `probes/<name>.js` beside it.

Milestone SJL-10.3 (part C) copied them on 2026-10-18 for
[`DurationCorrespondenceTests`](../DurationCorrespondenceTests.cs), which computes every line through the runner's
Temporal adapter over the library and holds it to Broiler.VM's answer, a refusal by its constructor alone. Row D7's
probe is in `d6-round-plain-relative`; row D12 is code no input reaches and has no probe (its library part is
`DurationApiTests`'s).

| File | Rows | Bytes | SHA-256 | Git blob |
|---|---|---|---|---|
| `d1-sign-and-shape.vm.txt` | D1 | 592 | `5578f6d455af9cc7724c4b6eecdecba645697b7922d413dc8f8d1bb8f12ec7e9` | `7be5b3c623f08b355a0610bf8bb0c477fc08f4b7` |
| `d2-largest-unit-and-balance.vm.txt` | D2 | 1,254 | `67faa656eb9df7b16e4d8ee2ae40b2d8b8d8b4539ef78541a6c5cb59051da33c` | `fcc1e2b1825c3ce14d7350e263a236a9fb5ee3da` |
| `d3-add-calendar-units.vm.txt` | D3 | 1,458 | `e386f476e1e032a9d56e5064ae96e1b8e6941af15276f0554b29f81ea3984b2a` | `da9d26c9c6828c0c0c61038b356d05373ab0eb25` |
| `d4-compare.vm.txt` | D4 | 1,288 | `b97ebe856b7a0b49a94c34bfe99fac1c38273f8a0f3b953d096d6b57fd577c2a` | `7e993f3ff50fc00921a59f14d4758ab8384d214b` |
| `d5-round-time.vm.txt` | D5 | 4,837 | `bbd8251488b6a40eb6292aa234976a1de1b9db36ed3745ea3209d008af361c9a` | `04214ba339472e365e44e3daf2ba1d03dc5467a4` |
| `d6-round-plain-relative.vm.txt` | D6, D7 | 2,298 | `213c3ffd3b3ac209b3d100dd53ea5b2d9edec0ad821946300b7553fa2537f22a` | `2132da16358f8e27267802f2a9222eaff7baef03` |
| `d8-shifted-window.vm.txt` | D8 | 599 | `5c490096c3c974030dc698ac8b651e9a2241704364fbc65dac82195129038cf0` | `9063ac8f401226f6cfaa837a4317c6acbe96b586` |
| `d9-round-zoned-relative.vm.txt` | D9 | 1,761 | `61a0319f895587b109136baa867f54c5c9d5676b151e64adbb63737eabfaec89` | `865bbc30dfe6c4ae0d33936ece17f90bb9ce9c83` |
| `d10-total.vm.txt` | D10 | 1,878 | `8f20b0b5de4b225f037b6413b1d0a3d781c2436984523b21f4c468e814819dcd` | `66a3bfd0329708854c14eec16fa74427acd00b8c` |
| `d11-non-iso-relative.vm.txt` | D11 | 976 | `865f38efaabf020594de990749050eab4d5c8f410c29216971c9284951671542` | `2177fa477c925e00a6198a6cde2f1dce61d8e0a3` |
| `f1-settings-and-defaults.vm.txt` | F1 | 2,765 | `1b579d2baf0886db1eb972969755f0bffb9b86b29ee7fd05e5deaaf8e77c42e4` | `ae7930b5fdcb305194a02a4d83f3ef99220ef920` |
| `f2-date-differences.vm.txt` | F2 | 884 | `c7f8d64560fa115f68fc7e75ae38fea346264399645c780fb8e45fdcbebb5338` | `185ea2b19c8bfb62b4c9ca989141821cbe9a4c72` |
| `f3-date-rounding.vm.txt` | F3 | 1,644 | `22b993fb4eb3b8f0a1afd8fadcd29d8c4414d228960cda7c6b71d99dc74ad8b3` | `838305ffecf24eb051c1e06c72fa0afc0489586c` |
| `f4-non-iso-rounding.vm.txt` | F4 | 1,838 | `3c3f729ab5f99a110931dc616aaf2dc094650ac14c31a9907c87ec070bfb909c` | `8a91d926bc9f30b36fc679d8f9b29f120e872c95` |
| `f5-datetime-differences.vm.txt` | F5 | 1,370 | `f70fc434f59e592b092ff789ae87b6ef2a6591a81780ca15f64a75818a11f671` | `404342349e2524a4c65e3be0365738b60d979067` |
| `f6-time-and-instant.vm.txt` | F6 | 1,129 | `626eacb13cd50ada7c794304c7454b9e90a6f319370cc48fc0e1ff9ad9aadf2b` | `51395ce1d1b9182de8276d345765ebee1e4e3fdc` |
| `f7-zoned-differences.vm.txt` | F7 | 2,099 | `409aa4b9b02f6baf8307c7a881a5489292269419ce9723ce343f0b194187d063` | `729e8223d9d26ba6891d5f1d46b0c0d943b1fd71` |
| `f8-yearmonth-differences.vm.txt` | F8 | 910 | `14f8eb93963bf12c2614bcfe5783038f37e3bb4ec4f630c6d8b0da690c637bea` | `0fb1bf3ea0bdfd21cc11897d76fbababfd510081` |
| `r7-narrowing-in-addition.vm.txt` | R7 | 2,744 | `23b4b3080ce92266a519cb2f183046ba509fb095ebd81eb92cd2e794fd1107ec` | `4abf0c44b5b9e30fa2d4ee718a907c3a48ebd500` |
| `b5-limits-in-addition.vm.txt` | B5 | 2,041 | `3a790c0bc97e3f72340440c24103d82334067034627ad2482f16a3f2da1cbff2` | `c6b8506129cf3b4d8d0727ab7009a4ad46a8e09e` |

The same rule holds: the tests check each copy's SHA-256 against this table, and a copy is never edited.

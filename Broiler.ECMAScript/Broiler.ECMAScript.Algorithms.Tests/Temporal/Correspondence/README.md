# Broiler.VM's answers to the calendar rows of correspondence `sjl-10-001`

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

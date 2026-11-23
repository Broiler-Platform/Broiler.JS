# Part K's deletion search

Bundle `sjl-14-001`'s part K ([`decision-rule.md`](../../decision-rule.md)): "the deletion search script, committed to
the bundle with the after commit's change, as bundle sjl-11-001's part E runs its E-1 search". Committed with window W2's
change on 2026-11-19, before any run of part K. [`search.py`](search.py)'s docstring says what it searches, member by
member as [`dispositions.md`](../../dispositions.md) lists SJL-14.5's deletion list, and how its control runs. It lists
every hit with its file and line and judges nothing: the part's reading says which hits are facade members, the
compiler's own `ContainsLegacyOctalLiteral` that stays until SJL-15.5, other types' members of the same name, or text.

# Independent Thunderclap review — MARVEL-846

Reviewed author `4dcd8afb07af9837134c9206033f39ac590bbd11` against
`c354b6b9`, including the actual 10005 row and all inventory changes. Verdict:
no correctness finding in this bounded nonpublic card packet. This review adds
only independent tests and this record; it changes no engine or authored row.

Authority: generated `datasets/cards/cards.json` 10005 prints cost 3, SUPERPOWER,
and “Hero Action: Choose up to 3 different enemies. Deal 3 damage to each of
them.” It has no attack label or attack trait. Current RR 1.8 choose-game-element,
target, event, labeled-ability and damage clauses support the distinct optional
group, ordinary damage and simultaneous placement. The vendored MarvelCDB FAQ
queried 10005 and has no entry; the local ruling search found no Thunderclap
entry. No external acquisition was needed for this review.

Independent raw JSON extraction confirms 708 predecessor objects are unchanged
against `c354b6b9`, with concatenated UTF-8 SHA-256
`2945f4c3bcbd3d9bd486862086ab9988c9f7c504b04f14f9f6df4d7f7e17ce13`.
The book has 709 rows and 24 Hulk faces; 10015 remains absent. Inventory diffs
update whole-book counts and bound the existing starter slice to its original
eight rows. Existing earlier pack slices and 90001 negative controls remain.

`HulkThunderclapCardReviewTests` adds four cases:

- Seat 1 Hulk spends actual Limitless Strength and chooses exactly two enemies,
  one engaged with each player. Seat 1 owns the choice and both discarded cards;
  the third minion and villain remain undamaged.
- Underpayment preserves cards, generator state/word count and agenda work.
  A paid retry matches an unprobed control's complete causal events and state.
- A live event damage bonus modifies both recipients.
- An attack damage bonus does not modify either ordinary-damage recipient or
  produce an Attack event; actual M.O.D.O.K. Retaliate does not fire.

The author's actual-row cases independently remain relevant: zero/one/three
selection, Tough, Guard, Stun, prevention and would-defeat suspension, maximum,
duplicate and stale rejection, retry and query purity. Those tests keep actual
10005 while clearly labelling the synthetic interrupt fixture. I inspected the
author's four valid killed data mutants and their logs; the invalid form-spelling
probe is excluded from mutation evidence.

Windows evidence is `C:/Users/mggar/Source/thunderclap-review-evidence/`:
`authority.json` retains the independent preservation facts; `baseline.log` and
`baseline.trx` show 17 passing card/review/citation cases. `maximum1.log` records
an independently killed valid data mutant (maximum 3 to 1): the new two-enemy
choice fails `InvalidTargets` at answer validation. The authored file is restored
byte-for-byte, then `restored-build.log` records a successful nonincremental
Release rebuild. `filter.txt` preserves the affected-class regression filter;
`restored.log` and `restored.trx` retain **300 passed, zero failed**. `commit.log` retains
normal hook evidence. Early `review.log` documents corrected review-fixture
errors (discard-area API and a mistaken payment-card ID), not product defects.

This review does not establish complete Hulk admission, a public setup/game,
native UI acceptance, save/replay or Linux CI. Those remain integration scope.

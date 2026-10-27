# Hulk Basic reprints — MARVEL-848

Base: reviewed H1 `43a2e812d7417a12a9f8bc0a17fcc16996ff551b`.
This packet appends exactly five research faces, in order: `10020` Energy,
`10021` Genius, `10022` Strength, `10023` Avengers Mansion and `10024`
Helicarrier. The authored book has 693 faces, including eight of Hulk's 33
printed faces. All 688 predecessor raw JSON objects remain byte-identical.
No engine, setup, starter recipe or public admission changes are included.

## Authority and implementation

Compared every field of the corrected `datasets/cards/cards.json` records for
10020–10024 against 01088–01092. They are identical apart from `card_id` and
`pack`; the existing execution bodies are reused under the actual Hulk IDs.
Each resource has two printed icons (`YY`, `BB`, `RR` respectively), no ability,
and a printed maximum of one per deck. Both supports have a printed maximum of
one per player. Mansion exhausts, chooses any player and draws one card for that
player. Helicarrier exhausts and reduces the chosen player's next successfully
played card this phase by one.

RR 1.8 `rr:resource.1` governs generating the printed resources by discarding a
physical hand card. `rr:max-maximum.2–.3` governs deck quantities and per-player
control limits. No timing ambiguity or new ruling interpretation is introduced.
The existing Core interpretation of the identical support text supplies the
choice, resource-cost modifier and phase expiry mechanisms.

## Behavioral evidence

`HulkBasicReprintTests` exercises the actual authored rows:

- Each resource pays a two-cost attack with exactly one physical hand card;
  both printed icons, one discarded copy, event discard and four damage are
  observed. Printed maximum deck quantities remain one.
- A non-first Mansion controller chooses either player's exact next deck card,
  in either controller form. Only that hand grows by one; the source exhausts,
  cannot activate twice without readying and is unavailable to the other seat.
- A non-first Helicarrier controller chooses either player. Repeated pricing
  retains the discount, another player's card keeps its full price, rejected
  payment retains the digest and discount, and a valid retry pays the reduced
  price. Successful play consumes the reduction; an unused reduction expires
  at the player-phase boundary.
- Both supports obey title-based per-player limits across Core and Hulk copies,
  while another player may control their own copy. Departure restores eligibility.

The exact authored inventory admits only these five additional faces. Sixteen
existing full-book count assertions change from 688 to 693; their scoped face
inventories and the Strange `Skip(682).Take(3)` checks remain intact. The genuine
unknown-card controls remain unauthored. No public product acceptance is claimed.

## Verification and fixture corrections

The corrected focused Release run passes 48 tests: this class and the complete
AbilityData family. Logs are external under `%TEMP%/hulk-basic-focused.log`.
The independent raw-object comparison is `%TEMP%/hulk-basic-raw-prefix.log`.

Initial fixture runs are not acceptance evidence. Hero Actions initially lacked
hero form. A direct `runner.Chose` call consumed a choice continuation without
advancing its agenda frame; the corrected helper uses `Sequence.Work`,
`Sequence.Answer` and `Sequence.Finish`. The bare payment scene also needs a
nonempty player deck so ordinary deck reset does not recycle the payment before
the discard assertion. The corrected rejection/retry test passes with the real
choice lifecycle. No engine defect was established. The obsolete failing probe
is preserved externally as `%TEMP%/hulk-basic-rejected-retry-probe.cs`; it is an
invalid lifecycle fixture, not evidence of a supported transaction failure.

Four final data mutants run against a passing baseline: Mansion draws two,
Mansion draws for its controller, Helicarrier discounts by two, and Helicarrier
omits its exhaust cost. Each fails its corresponding behavioral assertion;
the restored book is rebuilt nonincrementally before the combined affected run.
Earlier carrier mutant runs executed against a failing fixture baseline and are
excluded from acceptance evidence. Final logs overwrite those exploratory runs
as `%TEMP%/hulk-basic-mutant-*.log`; the summary, restored build, exact filter
and affected results use the `hulk-basic-` prefix in the same external directory.

The combined affected selection includes all sixteen mechanically changed
classes, H1 identity/Leap/reviewer tests, these reprints, AbilityData and rule
citations. The combined affected selection passes 173 tests (zero failures/skips), with a
nonincremental Release build reporting zero warnings/errors. The packet handoff
records the normal-hook commit.
Broader managed, native, public setup and complete-game coverage remain at the
integration boundary.

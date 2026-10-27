# Designated nemesis minion — MARVEL-832

Delivery branch: `fix/designated-nemesis-minion`, based on `cfec0321`.
Plane issue: `59ce1699-16c5-4123-a3a3-94a60ea0c30c`.

`yourAsideMinion` selects the printed nemesis role rather than the first minion
remaining set aside. `ICardFacts.IsNemesisMinion` supplies that role from the
catalog. Its complete printed product/set membership determines whether the set
has one minion face or multiple faces; physical copy counts, current locations
and set-aside ordering cannot change that classification.

RR 1.8 `rr:nemesis-encounter-set.3` identifies the set's single minion;
`.3.1` requires printed parenthetical designation for multiple minions.
The generated catalog records both Taskmaster (08026) and Hydra Mercenary
(08028) with `Nemesis: Natasha Romanoff`. That association alone cannot assign
the minion role: Taskmaster prints `(Black Widow's nemesis minion.)`, while
Hydra Mercenary does not. The catalog reads that parenthetical grammar without
matching titles or identity names. Multiple explicit designations remain roles;
a queried multi-minion set without any designation raises
`RulesNotImplementedException`. Unrelated research sets are not rejected during
catalog loading. The new fact's default grants no nemesis role.

The catalog implementation uses existing generated text and attributes; no
extractor input, generated card data, setup, digest, RNG, public admission,
ability-book row, reveal runner or agenda behavior changes.

## Current Shadow of the Past uniqueness clarification

The March 2026 ruling `ruling:a956304c470dac6c` has RR scope `1.7-1.8`.
Its answer is repeated in `ruling:95f5bab9a8a768fd`. When the set-aside nemesis
cannot enter because of uniqueness, discard that minion, deal the revealing
player one facedown encounter card, and discard Shadow without surge. This
agrees with the current uniqueness replacement in `rr:unique-icon.4.2` and
specifically resolves Shadow's printed conditional surge sentence.

`datasets/rules-graph.json` pins the current ruling hash and SHA-256 of 01190's
UTF-8 `text_plain`, with a card-scoped supersession of the pre-1.5
`ruling:ead76dd0b3c966ee` Taskmaster/Black Widow answer. This does not modify a
general Rules Reference clause or change the absent-nemesis surge branch.
The historical Hall of Heroes record and the contradictory August 31, 2020
MarvelCDB FAQ for 01190 remain unchanged as research records. No refresh or
ruling-inventory/adjudication change is needed for these already vendored
records.

## Evidence

Logs live outside the repository at
`C:/Users/mggar/Source/evidence/designated-nemesis/`.

- `focused.log`: 37 focused tests passed, including all five Core singleton
  minions; actual Black Widow/Taskmaster setup with the nemesis present/absent
  and reordered; absent designated minion with only one Hydra Mercenary
  remaining set aside; existing Standard, nested reveal, reprint and citation
  classes. The full encounter reveal procedure proves discard, exactly one
  deal, side scheme placement, rest shuffled into the encounter deck, and an
  empty agenda. The deal trigger distinguishes uniqueness from printed surge.
- `first-minion.log`: compiled selector bypass mutant fails the actual
  reordered and absent-nemesis regressions.
- `singleton-refusal.log`: compiled singleton-role mutant fails Core controls.
- `guess-empty.log`: compiled unsupported-empty mutant fails explicit-refusal
  controls.
- `restored-build.log`, `restored-content.log`, `rules.log`, `cards-check.log`:
  final non-incremental restoration (zero warnings/errors), all 56 affected
  content tests and 14 ruling/graph/citation tests passing, and offline card
  extraction verification matching both generated files.
- `commit.log`: normal pre-commit gates, executed serially after restoration.

These are focused engine/content checks. No new product is admitted and no
native UI or complete managed/platform certification is claimed. Independent
review and integration remain with the delivery lead.

## Independent review — 2026-10-26

Reviewed source `365c52233aae21e32be8a52c763bbca60c63d36f` in isolated branch
`review/designated-nemesis`, together with the two added independent test classes.
Verdict: **accept the designated-minion selector and current Shadow uniqueness
interpretation within this engine/content scope**. No production correction was
needed. Integration and broader required gates remain with the delivery lead;
this review does not admit a pack or establish native/product acceptance.

`NemesisMinionIndependentTests` adds singleton physical-quantity controls,
same-key sets in different products, and lazy refusal of an ambiguous research
set while unrelated facts remain usable. `ShadowDesignationIndependentTests`
uses actual two-player setup, with Black Widow in the second seat, for four
complete encounter reveals: Taskmaster/Rhino crossed with designated minion
present/absent. The absent cases leave only one Hydra Mercenary set aside.
Assertions distinguish successful entry under the correct player's control,
uniqueness discard and its single replacement deal, printed absent-minion surge,
the untouched first player's nemesis set, remaining-set shuffle and empty agenda.
The actual two physical Hydra Mercenaries are both explicitly non-designated.

Authority review compared the vendored RR 1.8 nemesis and uniqueness clauses,
both identical March 2026 answers (`a956304c470dac6c` and `95f5bab9a8a768fd`,
scope `1.7-1.8`), and the pre-1.5 Taskmaster answer. The card-scoped relationship
pins the current answer hash and 01190 text SHA, supersedes only that historical
interaction, and leaves the August 31, 2020 MarvelCDB FAQ intact. The graph's
references resolve to the applicable clauses.

External evidence: `C:/Users/mggar/Source/evidence/designated-nemesis-review/`.

- `content.log`: initial 35 focused Content cases passed, including seven added
  review cases.
- `first-minion-mutant.log`: removing the role predicate from `Aside` compiled
  and failed all four new Shadow cases. Hydra Mercenary entered while the true
  designated Taskmaster was shuffled, or the absent-case secondary entered.
- `restored-build.log`: forced non-incremental Release build, zero warnings and
  errors, after restoring the production source.
- `restored-content.log` and `restored-content.trx`: 54 cases passed using
  `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release
  --no-build --filter 'FullyQualifiedName~NemesisMinion|FullyQualifiedName~Shadow|
  FullyQualifiedName~StandardSetCardsTests|FullyQualifiedName~CardCatalogTests|
  FullyQualifiedName~RuleCitationTests'` (filter shown wrapped).
- `rules.log`: all 14 cases passed using
  `dotnet test tests/Marvel.Rules.Tests/Marvel.Rules.Tests.csproj -c Release
  --filter 'FullyQualifiedName~CardRulingRelationshipTests|
  FullyQualifiedName~RulesGraphTests|FullyQualifiedName~RuleCitationTests'`
  (filter shown wrapped).
- `commit.log`: normal pre-commit gates on the restored source and review tests.

All commands used the Windows PTY execution path; all test sessions were
harvested. No full-suite, Linux, native or package certification is claimed.

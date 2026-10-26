# Exact printed-set search prerequisite — MARVEL-812

Base `52033c88`, 2026-10-26. This packet implements the shared `cardsIn.set`
selector. It authors no production ability row and changes no dataset, setup,
public admission, wire contract, RNG stream or digest format.

Printed authority is `datasets/cards/cards.json` 09006 Mystical Studies:
“Search your deck and discard pile for a Doctor Strange card and add it to your
hand. Shuffle your deck.” The catalog carries `set: "doctor_strange"` separately
from pack and Hero classification. `ICardFacts.EncounterSet` already exposes that
printed field; the selector reuses it with exact ordinal equality. The DSL name,
nonempty-string validation and readable set description are engine choices.

`PrintedSetSearchFixture` supplies that test-local Studies shape with its real
printed one-resource event cost and alter-ego restriction, on directly prepared
nonpublic two-player boards. `PrintedSetSearchTests` proves physical own-deck or
own-discard choice by seat 1; exclusion of another Hero card, same-pack Brother
Voodoo, foreign deck/discard, play-area cards and the typed Invocation areas;
rejected foreign-answer preservation of digest/RNG/pending work; exact case and
Invocation-set mismatch; top-card bound before set/kind/class criteria; legal
no-match payment and shuffle; unchanged omitted-set search; malformed empty,
whitespace, integer, list and map inputs; and exact set equality in the guard used
to describe a search. No card-ID whitelist or Hero-class substitute implements
the filter. Existing projection consumers use the same typed selector evaluator.

Authority clauses are current RR 1.8 `rr:search.1` (choice among matches), `.2`
(cards remain in searched areas), and `.3` (shuffle the entire searched deck).
The preceding research record is
`doctor-strange-search-evidence.md` in the MARVEL-810 search packet, revision
`5e07f642`; it preserves the compiled unsupported-set probe. Neither record
establishes Doctor Strange product admission.

Verification against this packet:

- Compiled mutant removing only runtime set filtering: **6 failed, 8 passed**,
  including foreign-set candidate leakage and wrong-case/no-match behavior.
- Exact source restoration followed by nonincremental Content project Release
  build: **0 warnings, 0 errors**.
- Whole relevant search, description, lowering, admission, ability-data/book and
  rule-citation classes: **219 passed, 0 failed, 0 skipped**.
- Normal commit hooks run architecture, complexity and file-length policies.

Logs are outside the repository under
`C:/Users/mggar/Source/printed-set-{mutant,restored-build,final-tests,commit}.log`.
The final run supersedes earlier fixture-entry failures. Broader whole-game,
platform and integrated-candidate certification belongs to wave integration.

## Independent prerequisite review

Reviewed implementation `4261d57f` against base `52033c88`, 2026-10-26,
in the isolated `review/printed-set-search` worktree. Printed 09006 and current
RR 1.8 SEARCH govern the card-shaped fixture; `set`, ordinal matching and
description spelling remain explicit engine choices. No runtime correctness
finding remains in this bounded prerequisite review. This permits dependent
card authoring to use this selector; it establishes no whole-pack acceptance,
production 09006 row, public admission, native interaction or platform result.

The consumer audit traced singular and plural selection, removal membership,
projected membership traces and `AbilityProjectedStateQueries` to
`AbilitySelectorEvaluation.CardsIn`, directly or through
`AbilityRuntimeQueries.Every`. Those consumers retain the full typed selector.
Area-dependency, resolving-player and concealed-pile analyses read the named
areas rather than duplicate printed criteria. The description equivalence guard
compares `Set` alongside every existing area criterion. `SearchRange` applies
the physical top bound before the criteria; ordinary own deck/discard areas do
not expand to foreign or Invocation areas.

`PrintedSetSearchReviewTests` adds 13 independent cases: live singular/plural
and projected queries with combined kind/trait/title/class/set criteria;
exact-case rejection; projected departure without live mutation; pre-search
labels independent of zero/one/two hidden matches, without digest/RNG/agenda
changes; omitted, wrong-case and trailing-space guard mismatch with a matching
guard control; whitespace rejection at the lowering path; boolean/null rejection
by the value parser; and strict rejection of the unsupported `cardSet` alias.
The stale `cardSet` prose in the DSL reference is corrected to `set`.
The implementation tests separately exercise physical choice in both own areas,
same-pack/different-set and other-Hero exclusions, foreign answer atomicity,
Invocation exclusion, top-bound ordering, paid no-match shuffle and omitted-set
legacy behavior.

Measured independent verification:

- Focused implementation plus independent cases: **27 passed**, none failed or
  skipped.
- Compiled removal of runtime set filtering: **7 failed, 20 passed**, including
  the independent live/projected query case.
- Compiled omission of `Set` from description guard equality: **4 failed,
  23 passed**; the independent omitted, wrong-case and trailing-space guards all
  reject the misleading search label.
- Both mutations restored exactly. A nonincremental Release build of
  `tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj` follows each
  restoration, with **0 warnings and 0 errors**.
- Final restored-source Release run with `--no-build` and filter
  `FullyQualifiedName~PrintedSetSearch|FullyQualifiedName~EveryWordTheDatasetUsesIsWrittenDownInTheDslDocument`:
  **28 passed, 0 failed, 0 skipped**, including the corrected DSL vocabulary
  reference. This bounded result supplements, rather than repeats, the
  implementation packet's 219-class-filter test evidence.

Independent logs are outside the repository at
`C:/Users/mggar/Source/printed-set-review-{tests,filter-mutant,equivalence-mutant,restored-build,final-tests,commit}.log`.
An attempted broader substring-filter run is **incomplete**, not passing
evidence. It exposed the missing `cardSet` vocabulary after the prose edit;
the reference now documents both the standalone condition and the distinct
`cardsIn.set` filter. That run was cancelled after its broad filter selected
work beyond the bounded prerequisite review. Its process completed before the
final focused verification; no global process termination was used. Normal
commit hooks retain architecture, complexity and file-length policies.

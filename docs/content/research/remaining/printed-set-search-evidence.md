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

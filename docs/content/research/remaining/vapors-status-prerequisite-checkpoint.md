# Vapors status replacement prerequisite — MARVEL-809

Base `05cb51c2`; worktree `C:/Users/mggar/Source/marvel-vapors-status`, branch
`feat/vapors-status-replacement`. This is an engine prerequisite candidate awaiting
independent review. It does not admit Doctor Strange, a public setup or an ability
book row. No printed dataset, ruling graph or UI changes are included.

The typed `replaceStatus` leaf takes one bound physical status card and a fixed
`tough`, `stunned` or `confused` destination. `statusCards` selects physical statuses
on live characters in object-id order. Existing `chooseCard` and `choose` carry
both decisions. `StatusReplacement` validates the live card and destination before
removing or creating either status; ordinary caps, Steady, Stalwart and explicit
cannot restrictions apply. Replacement emits `Replace_Status`, never `Give_Status`.
These vocabulary spellings and append-only physical allocation are engine choices.
Vulnerable checks whether the replacement makes the character stunned/confused;
Steady needs two of the corresponding cards.

Authority: printed `09035` in `datasets/cards/cards.json`; current RR1.8
status-cards, steady, stalwart, cannot and vulnerable; vendored rulings
`dd639f2e28801fbc`, `17ea4e93b0ab8a25`, `66a4e0d2b3a71959`,
`0416488a6eb376e3`, and `8883bc6b0af1c9b8`. The scoped 2024 no-status
ruling governs the independent self-discard instruction; the older prohibition
does not suppress that instruction. No provenance relationships were authored.

The blocked-status failure was a candidate admission defect: a nested `choose`
has no direct target-legality node, so the containing physical-card selector kept
a Stalwart Tough even when all destination options were illegal. The bounded
three-kind status recognizer now requires one legal replacement option when
filtering that card. An empty independent replacement sentence leaves the later
self-discard available. Existing empty-scheme behavior remains covered.

Verification on the candidate sources, 2026-10-26:

- Focused Content: 26/26, covering Vapors through both Spell Mastery and Master,
  non-first Strange selecting another player's exact physical status, empty and
  blocked boards, same-kind and duplicate option filtering, blocked card filtering,
  malformed DSL, and existing Invocation empty-choice regressions.
- Rules: 13 cases after the added Steady same-kind control. Includes ordinary,
  Steady, Stalwart, all three explicit cannot restrictions, departed physical
  selection, Vulnerable's ordinary/Steady thresholds and no defeat events.
- Invalid choices at both prompts, and a stale physical-choice answer at the type
  prompt, preserve digest, RNG state/word consumption and outstanding agenda.
  Successful answers complete the original continuation and empty its agenda.
- Four compiled mutants are killed: bypass status cap, permit same-kind replacement
  on Steady, omit blocked-candidate option filtering, and emit `Give_Status`.
  The same-kind mutant initially survived the ordinary cap, exposing the missing
  Steady control; the new test kills it. Logs are outside the repository at
  `C:/Users/mggar/Source/vapors-mutant-{limits,same-kind,filter,given-verb}.log`.
- Restored sources received a nonincremental Content build. The subsequent whole
  Content project run terminated without a result; its log ends at test discovery
  and its process/session is no longer recoverable. The chained whole Rules run
  did not start. No whole-project green result is claimed. Logs:
  `vapors-restored-build.log` and `vapors-restored-content.log` in that same directory.
- Whole architecture: 129/129 (`vapors-finish-architecture.log`), before the final
  Steady theory-data addition. Normal commit hooks run without bypass.

The initial normal commit attempt failed the complexity hook in completion
observation, leaf admission and query dispatch. The candidate separates committed
field-change counting from other event observations, status completion prediction
from other leaf outcomes, and villain/scheme relationship queries from query-family
dispatch. These are responsibility extractions with unchanged gameplay semantics;
no debt baseline or hook exemption is added.

Final decomposition checks on 2026-10-26:

- Nonincremental Content test-project Release build: zero warnings/errors
  (`vapors-decomposition-build.log`).
- Expanded affected Content classes: 1,078/1,078, including Vapors, lowering,
  actual completion, active-counter admission, Wrecking Crew query/continuation
  regressions, Invocation, exhaustion, resolution status, and heal/status cases
  (`vapors-decomposition-content.log`).
- Rules status replacement: 13/13 (`vapors-decomposition-rules.log`).
- Final whole architecture: 129/129 (`vapors-decomposition-architecture.log`).

The normal commit executes the architecture-policy, complexity and file-length
hooks. Its output is recorded in `vapors-decomposition-commit.log`; these hooks
are not bypassed.

Compatibility decision for this bounded prerequisite: retain current digest and
protocol versions. No digest field shape/order, enum value, RNG operation or
serialized event subtype changes. `StateFieldCatalog.IsModifiable` admits three
new modifier names but does not add serialized `StateFields` entries. Replacement
uses existing `CardsMoved`, `CardsCreated`, and `CardAttached` records and the
existing string-valued verb. `GameEvent` polymorphism and `EngineResponse.Events`
therefore need no new discriminator. Supported production abilities do not invoke
this test-local operation, so existing supported games/traces keep their behavior.
Session replay compares complete semantic JSON; no historic trace is rewritten.

Consumer inspection: View has generic create/move/attach presentation and derives
the cause text from the verb; there is no bespoke replacement sentence. Event
activity projection retains the verb. This is not a claim of player comprehension,
socket round trips, strict protocol certification, save/restart during these two
choices, or backward compatibility of a future publicly admitted Strange book.
No Mister Sinister runtime/response integration is claimed: the evidence here is
the absence of `Give_Status` events, per ruling `0416488a6eb376e3`.

The packet does not prove arbitrary projected status mutation, repeated frames or
nested power suspension. Preserve their existing explicit unsupported refusals;
review and test each newly required context before admitting it. Independent
review, causal event comparison and any protocol/public/native integration needed
for pack delivery remain separate acceptance work.

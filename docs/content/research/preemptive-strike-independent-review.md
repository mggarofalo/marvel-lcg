# Independent review of individual boost cancellation

Reviewed engine prerequisite `59715a540a563bf5cf9f0a84f0be5b225a990869` in isolated
worktree `C:/Users/mggar/Source/marvel-preemptive-review`, branch
`review/preemptive-boost`, on 2026-10-26. Verdict: **changes required**.
No authored ability-book row or public admission for 05014 is included.

## Independent actual-row review, 2026-10-26

Reviewed `2851eb04` in isolated worktree
`C:/Users/mggar/Source/marvel-lcg-preemptive-actual-review`, branch
`review/preemptive-actual-controls`. Verdict: **approved for the actual 05014
ability row and the scoped engine behavior**. This does not establish public
pack admission, native interaction readiness or full managed/platform acceptance.

Raw authority was independently rechecked: joined printed 05014 in
`datasets/cards/cards.json`, plus vendored rulings `1ea208998735ef4b`,
`33d8c914aa86d19f` and `937d6ff4f970a1da` in `datasets/rulings/rulings.json`.
The actual row expresses the Hero Interrupt, villain attack faceup condition and
Defense label. Its dependent cancellation captures the number actually cancelled
before reflecting that quantity to the triggering villain. Earlier independently
reviewed engine controls and the actual full-book individual-card positives
remain consistent with these authorities.

`PreemptiveStrikeActualRowReviewTests` extracts the actual 05014 JSON row from
`AuthoredCards.Text`, without copying its trigger or effect, and appends one
explicitly synthetic generic faceup observer to an isolated catalog. This allows
inspection of otherwise absent interrupt prompts; it is not a public factory or
whole-pack journey. Six cases establish:

- A two-icon villain attack offers the actual event alongside the generic
  observer. Either the attacked hero or the other hero can pay, cancel both
  icons, deal two damage, discard the event and become the completed attack's
  defender. The paying hero takes the unmodified two attack damage; the other
  hero takes none.
- A villain scheme does not offer the event, both standalone and synthetically
  suspended under an outer attack. The latter isolates scheduler occurrence
  binding rather than claiming a printed simultaneous activation.
- A minion attack with a preassigned boost card does not offer the event.
- An alter-ego payer cannot use it during another player's hero-form attack.
  This has the same affordable event, observer and two-icon contribution as the
  corresponding eligible hero control.

Independent Windows Release evidence under `C:/Users/mggar/Source/`:

- `preemptive-actual-restored-focused.log`: **21 pass**, the six actual-row
  cases, actual full-book individual cancellation cases, attack-faceup controls
  and rule-citation checks. `preemptive-actual-restored-build.log` records the
  forced `--no-incremental` restored Release build with zero warnings/errors.
- Data mutants alter only the actual 05014 row, load through the same test
  catalog and fail for distinguishing observations: generic faceup trigger
  (`preemptive-actual-mutant-generic.log`, two scheme failures); enemy actor
  (`preemptive-actual-mutant-actor.log`, minion offer failure); omitted hero form
  (`preemptive-actual-mutant-form.log`, alter-ego offer failure); omitted Defense
  label (`preemptive-actual-mutant-label.log`, both payer cases fail on damage or
  defender identity). Each mutant exits 1; the restored same focused set passes.
- Dataset bytes are restored in `finally`; no production or ability-book edits
  are included. Normal architecture, complexity and file-length commit hooks
  apply to this tests/evidence packet.

## Independent final review of attack-only faceup repair, 2026-10-26

Reviewed `fb2e770a5a1f8405bfd5f990b2f12f23e3b241a3` in isolated worktree
`C:/Users/mggar/Source/marvel-attack-boost-review`, branch
`review/attack-boost-qualifier`. Verdict: **approved for the scoped engine
prerequisite and synthetic interpretation**. The scheme-offer finding is resolved.
This does not author or admit 05014.

The raw joined 05014 text requires a hero interrupt while the villain attacks.
The synthetic trigger now explicitly combines hero form, villain actor and the
attack-only faceup alias. The alias derives directly from the concrete boost
operation's `Attacking` value and shares the generic faceup occurrence, actor,
physical boost-card target and cancellation receipt. Neither attack initiation
nor an enclosing `world.Attack` qualifies it. The existing standalone scheme,
synthetically suspended-attack scheme, minion and villain controls distinguish
those boundaries. The nested scheme case is a scheduler isolation probe;
printed Boost-requested activations remain sequential under `rr:activation.8`.
Vendored rulings `1ea208998735ef4b`, `33d8c914aa86d19f` and
`937d6ff4f970a1da` remain consistent with the unchanged cancellation lifecycle.

The independent `AttackBoostFaceupIndependentReviewTests` observer has cost-free
generic and attack-only faceup interrupts, plus an attack-initiation forced
interrupt. Each faceup ability is offered once in the same occurrence; resolving
one leaves exactly the other. The complete attack draws exactly three cards,
including the single initiation draw, resolves one boost and completes once.
The observer remains ready, so exhaustion cannot conceal duplicate offers.

Independent Windows Release evidence outside the repository:

- `C:/Users/mggar/Source/attack-boost-independent-content.log`: **23 pass**,
  attack qualifier, independent cost-free observer, individual cancellation,
  earlier cancellation review controls and rule-citation checks.
- `C:/Users/mggar/Source/attack-boost-independent-rules.log`: **53 pass**,
  all Rules Boost cases and rule-citation checks.

The candidate's compiled scheme-alias mutant evidence remains recorded below.
This review adds tests and evidence only; no production, ability-book or version
edits. Full managed, native and cross-platform acceptance are outside this
focused review; ordinary integration gates still apply.

## Attack-only faceup repair candidate, 2026-10-26

The engine adds `WhenAttackBoostCardTurnedFaceup` to the same physical faceup
occurrence only when `BoostFaceupOperation.Attacking` is true. The generic
`WhenBoostCardTurnedFaceup` remains available during attacks and schemes. The
alias does not reuse attack-initiation timing or inspect `world.Attack`, so a
suspended outer attack cannot qualify a scheme boost. Synthetic 05014 triggers
combine the attack alias with villain actor and hero form; no authored ability
row or public content admission changes.

The existing `VillainSchemeFaceupDoesNotOfferTheAttackOnlyInterrupt` regression
now lives in `AttackBoostFaceupQualifierTests`, with standalone and nested cases.
A costed generic faceup observer keeps the exact scheme window open. The nested
case first reaches a real villain attack faceup window, then synthetically
suspends it with a scheme while `world.Attack` remains present. This isolates the
scheduler binding; printed Boost-requested activations ordinarily wait for the
current activation to finish. Both cases retain the event in hand and offer
only the generic observer. A minion attack carries the attack alias but does not
offer the villain-only event. The villain positive control executes the generic
interrupt and then the cancellation in the same occurrence, with one completed
attack and the expected numeric cancellation/damage.

Focused Windows Release evidence outside the repository:

- `C:/Users/mggar/Source/attack-boost-qualifier-content.log`: **35 pass**,
  cancellation, live-count review controls, qualifier cases, existing boost,
  activation, Green Goblin and citation cases.
- `C:/Users/mggar/Source/attack-boost-qualifier-rules.log`: **53 pass**,
  Rules boost and citation cases.
- `C:/Users/mggar/Source/attack-boost-qualifier-mutant.log`: compiled mutant
  restoring the attack alias on scheme faceup occurrences; both standalone and
  suspended-attack scheme controls fail, exit 1.
- Source restored in `finally`, followed by forced `--no-incremental` Release
  build, zero warnings/errors in `attack-boost-qualifier-restored-build.log`;
  restored focused Content set **35 pass** in
  `attack-boost-qualifier-restored-content.log`.

This candidate awaits independent review and integration gates. These focused
results do not claim full managed, native or cross-platform acceptance.

## Independent final review of live-count repair, 2026-10-26

Reviewed `40bbdc74f6ca74629b6a684adb1f729225d3dfe3` in
`C:/Users/mggar/Source/marvel-preemptive-live-icons`. The live-count finding is
resolved. **Changes remain required before authoring 05014**: its synthetic
interpretation is offered when a villain's scheme boost card turns faceup.
Raw joined 05014 requires "while the villain attacks". A Defense label does not
enforce this restriction: `AttackAbilityDefense.CanUseDefenseAbility` expressly
permits abilities outside attacks under `rr:defend-defense.4.8`.

`VillainSchemeFaceupDoesNotOfferTheAttackOnlyInterrupt` queues a villain scheme
with a hero-form paying identity, an affordable event and a two-icon boost card.
It reaches that exact scheme faceup window and fails because the event is offered.
This is a synthetic admission regression, not public content coverage. The actual
interpretation needs an engine-owned predicate that binds the attack requirement
to this faceup activation, including when other attack context is suspended.

Independent Windows Release evidence outside the repository:

- `preemptive-independent-final-content.log`: **31 pass**, individual
  cancellation, independent review controls, existing boost/activation/Goblin
  cases and rule citations, before the added scheme control.
- `preemptive-independent-final-rules.log`: **55 pass**, all Rules Boost cases,
  Wrecking attack routing and rule citations.
- `preemptive-independent-final-review-controls.log`: **8 pass, 1 fail**;
  only the new scheme restriction assertion fails. The stale-payment control
  now repeats `Remaining`, `Waiting` and `Describe` queries after taking its
  baseline and before rejection. Card digest, full RNG state/consumption,
  activation/attack, pending frames and events remain unchanged; subsequent
  completion matches the unprobed control. No separate information-signal
  inspection is claimed.

The raw 05014 text and all three cited rulings were rechecked. Live admission,
dependent quantity projection, preparation and actual cancellation consistently
read the modified quantity. Actual cancelled icons remain separate, so a second
event cannot reuse them and later star-added icons survive. Exact activation,
world, incarnation and outstanding-occurrence guards remain. The adapted Rules
tests preserve event order, numeric contribution, damage and discard routing
assertions while driving the queued attack and explicit defender choice. Their
encounter reserve prevents unrelated deck exhaustion.

This verdict approves the scoped live-count repair, not the missing attack-only
interpretation or full managed/cross-platform acceptance. Review changes own
tests and this evidence record only; no production or ability-book edits.

## Live-count repair candidate, 2026-10-26

The repair on `fix/preemptive-live-icons`, based on review commit `2c69a87d`,
makes `Remaining` read the card's current modified numeric icons and amplify,
less the actual quantity cancelled by earlier effects. Discovery, dependent
quantity projection, preparation and commitment share that value. The final
Boost contribution uses the same calculation after the star resolves, so later
star-added icons contribute without changing damage already dealt for cancellation.
Exact world, activation, incarnation and outstanding occurrence checks remain.

The two earlier-interrupt regressions now pass for +1 and -1 changes. Added
zero and over-reduced controls keep the faceup window open, establish zero live
icons, withdraw the cancellation offer, retain the event/payment in hand and
complete the attack with no boost contribution. Direct Rules component callers
now run scheduled faceup work and explicit defender decisions; their order,
numeric contribution, damage accounting and discard-routing assertions remain.
An encounter reserve keeps the two-card fixtures from exhausting their deck.

Focused Windows Release evidence outside the repository:

- `C:/Users/mggar/Source/preemptive-live-icons-final-content.log`: **35 pass**,
  covering cancellation, independent-review controls, existing boost/activation,
  Green Goblin, admission and citation tests.
- `C:/Users/mggar/Source/preemptive-live-icons-rules-focused.log`: **53 pass**,
  the full Rules `FullyQualifiedName~Boost` selection plus rule citations.
- `C:/Users/mggar/Source/preemptive-live-icons-mutant-frozen.log`: a compiled
  mutant freezes the initial total again; all four +1/-1/zero/over-reduced
  controls fail at their live-count assertions, with exit 1.
- Production source was restored in `finally`, followed by an explicit
  `--no-incremental` Release build: zero warnings/errors in
  `preemptive-live-icons-restored-build.log`. The restored 26-case focused set
  passes in `preemptive-live-icons-restored-focused.log`; the broader sets above
  subsequently pass against restored production source.

This is a repair candidate awaiting independent review and integration gates;
these focused results do not establish full managed or cross-platform acceptance.

## Confirmed finding

`BoostIconCancellation.Remaining` captures the count before any faceup interrupt
and does not reflect an earlier interrupt changing that card's boost icons.
The new synthetic `EarlierFaceupInterruptChangesTheIconsCancelledAndDamage`
regression completes a costed, exhausting faceup interrupt first and proves the
boost card's modified count is respectively 3 or 1. Preemptive Strike then deals
2 damage in both cases, rather than 3 or 1. The +1 case also leaves one boost
icon contributing to the attack instead of cancelling every pre-star icon.

Authority is the raw joined 05014 text in `datasets/cards/cards.json` (cancel all
icons; damage for each cancelled this way) and vendored ruling
`937d6ff4f970a1da`: Crest can modify the counted icons before Preemptive Strike,
which deals additional damage for the added/cancelled icon. This control tests
the common faceup-interrupt prerequisite, without claiming to implement Crest's
separate count-triggered timing or admitting that card.

Rulings `1ea208998735ef4b` and `33d8c914aa86d19f` were also reviewed: constants
and amplify precede faceup interrupts, star effects follow them, star-added icons
survive cancellation, and already cancelled icons cannot be cancelled again.
The repair must update live admission/projection and cancellation consistently,
while retaining actual cancelled quantities separately from later star changes.

## Evidence

All logs are outside the repository, under `C:/Users/mggar/Source/`.

- `preemptive-review-new-tests-final.log`: all six new review cases compile;
  **4 pass, 2 fail** for the confirmed +1/-1 quantity finding. Passing controls
  cover a second event unable to cancel the same icons, a Boost-requested
  deferred attack keeping its own boost card and icons, a boost card entering
  play through its star and remaining there, and stale hand payment rejection.
- The stale-payment control compares card digest, complete RNG state and
  consumption, current occurrence, pending frames, activation/attack and event
  trace before rejection, then compares the completed run with an unprobed
  control. It does not claim a dedicated hidden-information-signal inspection.
- `preemptive-review-baseline.log`: focused original cases, citation checks and
  the initial five review cases: **26 pass, 2 fail**. The later sixth zone-change
  control is recorded separately above.
- `preemptive-review-mutant-activation.log`: compiled bug-restoring mutant removes
  the exact activation-ID equality in `AttackBoost.MatchesActivation`.
  `CompletionFromAnotherActivationCannotContributeToTheSameEnemy` kills it:
  an unexpected `BoostResolved` contributes 2 icons to the same enemy under a
  different activation ID. The mutant run exits 1 for the intended assertion.
- Production source was restored in a `finally` block. A subsequent explicit
  `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release
  --no-restore --no-incremental` passes (`preemptive-review-restored-build.log`).
  The same completion control and original focused/citation set then pass:
  **23 passed**, `preemptive-review-restored-focused.log`.

The review does not establish full managed, native or cross-platform acceptance.
The existing star-addition, constants/amplify, zero-icon, multiplayer defense and
stale completion controls were read and run in the focused set. No production,
agenda, runner, ability-book or version change is part of this review packet.

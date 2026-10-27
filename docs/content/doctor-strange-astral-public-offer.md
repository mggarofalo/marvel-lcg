# Astral Projection offered scheme admission

The public starter-game sweep on candidate `d887173f` reached a rejected
09003 answer in Absorbing Man seed 5 (round 12, decision 96) and Rhino Expert
seed 6 (round 2, decision 12). The latter prompt advertised schemes 53 and 71
for Astral Projection while its ordinary thwart offer included only 71.
The public policy selected advertised main scheme 53 and supplied a complete
two-resource payment; action admission rejected that chosen scheme before costs.
Original trace: `C:/Users/mggar/Source/evidence/strange-public-game-astral-seed5.log`.

`AbilitySchemeChoice.Admit` correctly searches for at least one usable scheme
when discovering the action and checks the exact chosen scheme on execution.
`AbilityCostSelection.Ask` describes the broader in-play scheme relation.
The affordance description now filters that cost request using the existing
action admission function for each candidate. It changes no payment, private
inspection, selected incarnation, simultaneous ordering or continuation behavior.
There is no card-specific client or policy rule.

Authority is the generated 09003 printed text, RR 1.8 `rr:target.2`,
`rr:target.3.3`, `rr:cost.6`, `rr:crisis-icon.1` and
`rr:confuse-confused.5.1`. The choose-scheme cost cannot make an otherwise
unaffected scheme a valid target. Confusion still permits attempting the
thwart without a valid thwart target and still requires the scheme cost.
The vendored MarvelCDB FAQ queries 09003 and contains no specific ruling.

`AstralOfferedSchemeTests` uses the production ability on nonpublic legal-state
boards. Four cases distinguish an empty main scheme from a threatened side
scheme and a crisis-blocked threatened main scheme from its threatened side
scheme, both normally and while confused. They assert the exact offered set,
mandatory cardinality and cost explanation. Forcing the excluded main scheme
raises before either hand card is spent. Repeated action/description queries
preserve card digest, RNG state/consumption and agenda; subsequent complete
resolution matches an unprobed control, including private encounter inspection.
Confusion pays both cards, removes its status and suppresses all effects.

Windows validation uses PTY execution throughout:

- Focused new class: 4 passed.
- Compiled mutant adds an impossible seat predicate to bypass filtering:
  nonincremental Content build, then `--no-build` focused test: 2 normal cases
  fail their target-set assertions; 2 confusion controls pass.
- Restored source, nonincremental Content build, then affected whole classes:
  178 passed. Filter includes Astral, DeckArrowCost, MinionEntryArrowCost,
  HammerThrowRecoveryPreflight, ShrinkPaymentPreflight, TargetReference,
  TargetAcquired, AbilityCostLowering, SafeHouseCost, RecipientCostSuspension,
  EventRecipientCommitment and RuleCitationTests.

Logs: `C:/Users/mggar/Source/evidence/astral-offer-focused.log`,
`astral-offer-mutant-build.log`, `astral-offer-mutant.log`,
`astral-offer-restored-build.log`, `astral-offer-restored-affected.log`;
TRX files are in `C:/Users/mggar/Source/evidence/astral-offer/`.
The final helper extraction also passes all 178 cases in
`astral-offer-final-affected.log`; normal architecture, complexity and file-length
commit hooks pass. An initial inline implementation exceeded Describe's
complexity budget; cohesive cost-target admission now has its own private method.

## Exact public seed retest

Source `9eb7d9e5` plus public-game proof `a038531d` (picked locally as
`799b31fa`) ran the exact two failing scenarios with unchanged policy. The test
compares every accepted decision with an independent public host, including
event JSON, RNG consumption, state digest and result, then verifies the complete
durable accepted trace through session replay.

Rhino Expert seed 6 now passes: PlayersLose, round 3, 27 decisions,
169 RNG words; final state SHA-256
`551FD33F1573F50F60892671AB54204260CCB6FFF0AE06A24AAC76C4E85713AE`, trace
SHA-256 `FE88375F96CE59E14A94C9B088A51D779691CF7203B8829661FA51EB04BEDFAA1`.

Absorbing Man seed 5 reaches a separate unsupported boundary when building
the turn after decision 93 (round-11 defender):
`A labelled-power continuation reads the board after an unprojected defeat window.`
The stack is `AbilityPowerSuspension` → `AbilityEntryAdmission` →
`AbilitySchemeChoice` → `AdmittedCostTargets`. Per-candidate checking reaches
a side-scheme path that action discovery's first admissible candidate does not
visit. `AbilityProjectedDefeatWindows.ValidateContinuation` refuses a deferred
defeat window followed by a board-dependent continuation it cannot project.
The engine must implement and prove that continuation prerequisite; treating
unsupported as an absent legal target or catching the exception would hide it.
This packet preserves the boundary and does not claim a complete Absorbing Man
game or replay.

A temporary diagnostic run identifies candidate **Hydra Patrol (04154)**,
physical object 89 with 2 threat. Astral's initial removal defeats it; its
printed When Defeated searches for and puts a Hydra minion into play for each
player, then shuffles the encounter deck. The rejected continuation node is
Astral's `removeThreat` receipt tail. Diagnostic log:
`C:/Users/mggar/Source/evidence/astral-offer-public-boundary-diagnostic.log`.
The instrumentation is restored exactly and a nonincremental Acceptance build
passes afterward (`astral-offer-diagnostic-restored-build.log`). It changes
neither the source commit nor the recorded unsupported outcome.

Combined result: 1 passed, 1 failed. Full log:
`C:/Users/mggar/Source/evidence/astral-offer-public-seeds-5-6.log`; TRX:
`C:/Users/mggar/Source/evidence/astral-offer/public-seeds-5-6.trx`. The new failure's
accepted prefix is archived as
`C:/Users/mggar/Source/evidence/astral-offer/absorbing-man-5-new-boundary.session.json`.
The original rejected-answer prefixes remain in
`C:/Users/mggar/Source/evidence/strange-public-games/`.
Independent review, broad integration, platform and native acceptance remain separate.

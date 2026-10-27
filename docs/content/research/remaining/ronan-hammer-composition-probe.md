# Bring the Hammer Down composition diagnostic

Parent: MARVEL-567. Engine baseline: `bae78b4ff74c7a2b508d0e93d39ea1197d5826f1`.
This packet adds synthetic tests only; it does not author or admit 90004.

## Authority and question

The joined `datasets/cards/cards.json` 90004 record says: “If this activation
defeats a character, deal the first player 1 facedown encounter card.” Its printed
boost value is 3. Physical quantities and the pinned publisher PDF are recorded in
[the source pin](ronan-source-pin.md). The vendored MarvelCDB FAQ has no entry
whose code is 90004. Current RR is 1.8; `rr:activation.7` makes the initiating
effect resolve after the activation fully resolves, and `rr:boost-boost-icon.3`
excludes boost-ability damage from damage dealt by the activation. A positive
damage total therefore cannot stand in for the printed defeat condition.

The tested candidate uses existing `chooseCard`, `afterActivation`, `wasDefeated`
and `dealEncounterCards` syntax. The choice is an explicit synthetic instrument
to retain the ally before departure, not an extra choice proposed for the actual
card. Only 90004's boost has a local row. Its actual printed boost icons are
processed by the ordinary attack procedure. No production book, setup, cards or
engine changes are included.

## Executed observations

`RonanHammerProbeTests` drives `Sequence.Work` / `Sequence.Answer` through the
normal defender, boost, damage and completion procedure. Both players have legal
decks; spare zero-icon encounter cards avoid exhaustion ending the game.

| Probe | Observed outcome |
|---|---|
| Rhino attacks, Mockingbird defends, 90004 boosts | 5 attack damage; ally defeated and discarded; delayed marker executes once; `wasDefeated(chosen)` does not deal an encounter card |
| Same attack, synthetic +10 ally health | Same 5 attack damage; ally survives; same delayed marker and same false predicate |
| Lethal control with unconditional delayed dealing | Exactly one facedown card reaches first player 1, distinct from attacked player 0; a repeated work call does not deal another |
| Scheme control | No attack completion or ally damage; delayed marker still executes; no encounter card dealt |

Every probe asserts an unfinished game, no current activation and an empty
agenda. The missing conditional outcome is therefore not abandonment from game
loss or pending work. The unconditional control separates predicate failure from
delayed scheduling or first-player dealing failure.

The tests preserve the current diagnostic outcome, including zero dealt cards in
the lethal conditional case. They **do not certify 90004 behavior**: that case
must deal one under its printed text. They are intended to be replaced or updated
when an actual implementation supplies the qualifying causal evidence.

## Bounded conclusion and remaining work

The tested existing composition cannot distinguish lethal from nonlethal attack
damage. Inspection localizes the absent evidence: `EnemyActivation` and
`CompletedEnemyAttack` retain damage/completion facts but no character-defeat
receipt; `RecordImmediateActivationResult` publishes damage/threat/made, while
`wasDefeated` consults the captured triggering occurrence. `afterActivation`
restores its boost-registration occurrence, rather than the later damage/defeat
occurrence. A retained activation-qualified defeat fact, exposed to the delayed
condition, is the concrete prerequisite suggested by this execution evidence.
The probe does not establish that every conceivable alternative composition is
impossible, nor separately establish how departed-selector resolution contributes
to the candidate predicate's false result.

The actual reveal instruction (Ronan present/absent, engaged player in either
form), boost-ability defeats, nested activation attribution, prevented defeat,
multiple defeats and final production-row authoring remain unverified here.
The receipt implementation must distinguish those causal boundaries; this packet
does not invent accepted syntax for it or authorize public Ronan admission.

## Verification

On Windows, from the isolated `marvel-lcg-ronan-hammer` worktree:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~RonanHammerProbeTests|FullyQualifiedName~RuleCitationTests' --logger 'trx;LogFileName=ronan-hammer-probe.trx' --results-directory C:/Users/mggar/Source/evidence/ronan-hammer-probe
```

Result: 6 passed, 0 failed (4 diagnostic cases and 2 citation checks). Full output:
`C:/Users/mggar/Source/evidence/ronan-hammer-probe-run.log`; TRX:
`C:/Users/mggar/Source/evidence/ronan-hammer-probe/ronan-hammer-probe.trx`.

Earlier attempts with the narrower `FullyQualifiedName~RonanHammerProbeTests`
filter exposed fixture issues: missing `Marvel.Rules.Timing` import (CS0246,
CS0103), reading marker counters with the wrong prefix (`c_` is required), and
using `hit_points` instead of the engine's `health` modifier. Those were corrected
before the passing run. The final run rebuilt the test source. No engine mutant,
whole-pack test, integration lane or public admission journey is claimed.

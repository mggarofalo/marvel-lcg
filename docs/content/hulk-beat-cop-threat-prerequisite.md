# Hulk Beat Cop threat destination prerequisite

MARVEL-850. Base: `4dcd8afb07af9837134c9206033f39ac590bbd11` (709 reviewed rows).
This packet adds an engine prerequisite; Beat Cop 10029 remains outside the
committed ability book and public product admission.

## Authority and supported behavior

Printed `datasets/cards/cards.json` 10029: "Exhaust Beat Cop → move 1 threat
from a scheme to here." Current RR 1.8 `rr:move.1`, `.2`, `.6`, `.7` govern:
endpoints differ, both must be valid, threat leaving a scheme is removed, and
threat entering a scheme is placed. A support is not a scheme. The support
therefore stores moved threat without `WhenThreatWouldBePlaced`,
`WhenThreatPlaced`, scheme completion, or destination forced-response credit.

Engine choice: support destinations are bounded to the exact source support
in the resolving controller's play area. Physical ownership may differ. Other
non-scheme destinations and support-source removal are outside this capability.
The same eligibility function serves ability admission and execution. No card
ID enables the capability. Existing scheme-to-scheme movement retains its
ordinary placement/prevention/response path.

Removal uses existing permission and side-scheme defeat behavior. The engine's
existing removal-first ordering waits for source defeat before support storage.
Destination membership, incarnation and current controller are checked again
then; a departed, reentered or transferred source cannot receive the tokens.
Only the amount actually removed is stored, including a source scheme's
last threat. `ThreatMoveReceipt.Placed` reports destination tokens received for
either schemes or support storage; destination response credit remains false
for support storage. Token changes emit `FieldSet` with `Move_Threat`.

## Evidence

All logs below are outside the repository under
`C:/Users/mggar/AppData/Local/Temp/`.

- `beat-cop-before.log`: exact exported actual 10029 draft first-action probe,
  injected only into a test-local book, fails in both forms because no matching
  action is offered. No production ability row was added.
- `beat-cop-coverage.log`: 57 focused cases pass after the change, including
  complete `ThreatMovePairChoiceTests`, `ThreatMovePairReviewTests`,
  `ThreatMovementTests`, `ThreatMovementReviewTests`, `ThreatMovementDslTests`,
  `SupportThreatMovementTests` and the actual draft probe.
- Actual 10029 probe checks both forms and player seats, cross-owner controller
  use, query state/RNG-word/agenda stability, exhaustion, exact support storage,
  untouched other support and no thwart event.
- Generic support probes use printed Core support Avengers Mansion (01091)
  with explicitly synthetic abilities. They distinguish clamped removal from
  requested amount, ordinary removal prohibition from thwart prohibition,
  invalid support destinations and controller transfer, departure/reentry before execution and during
  source defeat, and absent scheme-placement windows/events/response credit.
- `beat-cop-mutant.log`: compiled destination filtering mutant restores the
  scheme-only condition. All four actual draft cases fail at missing action
  admission. Mutation restored before subsequent checks.
- `beat-cop-restored-build.log`: non-incremental restored Release build.
- `beat-cop-restored-tests.log`: focused restored-source rerun.
- `beat-cop-architecture.log`: architecture suite.

No Agenda, AbilityRunner, ability-book, setup, or public-admission changes.
Independent review and later actual Beat Cop row delivery remain separate work.

## Independent engine review

Reviewed source `61d0973e5a26f72ded65a2b25b9dacfa2ff8fca3` against base
`4dcd8afb07af9837134c9206033f39ac590bbd11` in an isolated review worktree.
No correctness finding remains in this bounded support-destination prerequisite.
The review followed `rr:move.1`, `.2`, `.6`, `.7` and the live-source/incarnation
contract; it does not admit Hulk or the production Beat Cop ability row.

Independent evidence under
`C:/Users/mggar/AppData/Local/Temp/beat-cop-independent-review/`:

- `focused.log`: 58 cases pass across complete `ThreatMovementTests`,
  `ThreatMovementReviewTests`, `ThreatMovementDslTests`, `ThreatMovePairChoiceTests`,
  `ThreatMovePairReviewTests`, `SupportThreatMovementTests`,
  `SupportThreatMovementReviewTests` and `BeatCopThreatDestinationTests`.
- The new controller-transfer pair suspends on the source scheme's actual
  defeat choice. An unchanged controller receives two tokens after defeat;
  transferring the same physical incarnation to another controller receives
  none. Both retain the exact removal receipt and finish pending work.
- The four actual 10029 first-action cases additionally reject an invalid answer
  without changing card state, RNG word count, pending agenda or events, then
  accept the valid retry. This covers both forms and seats with cross-owner use.
- `controller-mutant.log`: deleting the live controller restriction compiles;
  the transfer case fails with `Placed` equal to 2 instead of 0, while the
  unchanged-controller control passes. The production source was restored.
- `restored-build.log`: forced non-incremental Release build succeeds;
  `restored-focused.log`: all 58 cases pass against restored sources.

The actual-row probe is deliberately a test-local injection while 10029 is
absent from the production ability book. When the production row is delivered,
remove that injection or explicitly replace the production row in this synthetic
fixture; adding a duplicate row is not an integration strategy. Existing strict
duplicate-row rejection remains required.
- `commit-hooks.log`: normal pre-commit architecture policy (66 cases),
  cyclomatic complexity and file-length checks pass. No hook bypass is used.

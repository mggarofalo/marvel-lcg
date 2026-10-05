# Effective card lifecycle validation

Scope: MARVEL-384, MARVEL-385 and MARVEL-444. Reference main:
`3246b4a5261fbcf3f33db01c91a6e58b28631b12`. These are local observations,
not final PR acceptance or independent product usability approval.

## Distinguishing behavior

`EffectiveCardLifecycleTests` proves assignment is explicit: placing a facedown
player card in an engaged area does not assign an identity. A synthetic Scout
with 2/3/4 base values demonstrates reusable minion statistics. Departure to
four destinations detaches damage, exhaustion, counters and profile; in-play
transfer retains the copy. Printed face, physical id and owner remain stable.

`TemporaryIdentityRegressionTests` exercises authored Core data: inactive held
resources/text, restored player identity, unique/max restrictions, Upgraded
Drones during elimination transfer, 01140's response after departure, stale
incarnation/facing and permanent removal. `ActiveSourceCopyTests` suspends an
effect, changes its live source's counter from 3 to 5, discards it and observes
5 through last-known state, with and without another suspension after departure.
The physical destination holds zero.

`ComposableCardSelectionTests` distinguishes granted/lost current traits,
control versus ownership, public-area scope (including hosted upgrades), ordering and purity. BP Claws
lacks TECH and is still selected. Repeated-trace regressions distinguish
replacement villain ordering and engagement into another player's area.
An unordered last-card lookup after projected re-engagement explicitly raises:
the membership trace does not represent move chronology. No migrated Core card
uses that unsupported composition.

Authorities are vendored `rr:leaves-play.1`, `rr:leaves-play.2.3`,
`rr:damage.step.7`, `rr:damage.step.8`, `rr:response.1`,
`rr:removed-from-the-game.2`, printed 01140 and the vendored 01185 ruling.
Tests cite readable clauses. Wire choices are documented in
[digest v3](state-digest-v3.md).

## Comparison with main

The [manual audit](engine-card-lifecycle-evidence/corpus-audit.cs.txt) runs each
Core transcript with default bindings and catalog-declared exceptions. It reads
the internal final world only in the isolated audit process; hidden snapshots
are not published. Both checkouts yield 684 results, no unexpected errors and
identical RNG word consumption. The final candidate rerun exactly matches the
candidate snapshots used to update 412 pinned hashes.

Excluding only the version and new profile field, 648 states match main.
Another 35 differ only by clearing exhaustion or threat on departure. One
differs in discard order: the Ultron III overkill transcript's forced response
puts the held Photonic Blast above the already discarded Tactical Strike.
This is the explicit authored-response timing correction, not selector
equivalence. RNG, physical ids and zone spellings are unchanged.

## Fixed selector workload

The [workload](engine-card-lifecycle-evidence/selector-workload.cs.txt) compiles
the book and each selector once, then calls the actual internal evaluator. The
fixed two-player board has 37 cards, four BP upgrades, six facedown minions and
two TECH discard sets. Each query gets 1,000 warmups and five samples of 20,000
calls. Median time includes evaluation objects and selection allocations.
Sequential processes used .NET 10.0.400, macOS arm64. No live cache was added.

| Query | Main µs/call | Candidate µs/call | Ordered ids on both |
|---|---:|---:|---|
| BP upgrades | 2.777 | 2.874 | 33, 34, 35, 36 |
| Global facedown Drones | 0.360 | 4.801 | 10, 11, 12, 26, 27, 28 |
| Engaged Drones | 2.081 | 2.295 | 12, 11, 10 |
| Identities with TECH upgrade in discard | 1.708 | 2.352 | 0, 16 |
| Top TECH in chosen discard | 0.770 | 2.324 | 31 |

Raw [main](engine-card-lifecycle-evidence/selector-base.json) and
[candidate](engine-card-lifecycle-evidence/selector-current.json) samples assert
unchanged digest, area count and RNG. Legacy global Drone selection used object
order; engaged selection used insertion order. The authored expressions retain
that distinction. Generic global filters cost more than the specialized scan;
these numbers establish cost on this board, not a scaling or latency guarantee.

To reproduce, build `tools/Marvel.Behavior.Run` in Release in each checkout.
Copy the workload to `Program.cs` in a temporary net8.0 console project outside
the repository; enable implicit usings and nullable, and set assembly name
`Marvel.Content.Tests` for existing internal test access. Reference Marvel.Content,
Marvel.Cards, Marvel.Core and Marvel.Rules DLLs directly from that checkout's
`tools/Marvel.Behavior.Run/bin/Release/net8.0`. Run with `CHECKOUT base` or
`CHECKOUT current`. The audit similarly references Marvel.Behavior.Run and
Gherkin and takes the checkout path. Direct references preserve repository build
configuration.

## Executed mutations

The [experiments](engine-card-lifecycle-evidence/mutation-experiments.py.txt)
use an isolated detached worktree, prove the unmutated sets pass, change one
expression, rebuild the selected test project and restore source between cases.
Copied timestamps are refreshed to invalidate old mutant DLLs. Root source and
the user's game are not mutated. [Results](engine-card-lifecycle-evidence/mutation-results.json)
name the exact tests and retained logs.

| Mutant | Wrong decision | Distinguishing observation |
|---|---|---|
| M01 | Keep departed state | Destination has no profile/damage/tokens |
| M02 | Resume from stale snapshot | Effect reads 5, not 3 |
| M03 | Expose held resources | Blank minion generates none |
| M04 | Ignore defeat facing | Faceup copy yields no facedown response |
| M05 | Ignore incarnation | Old occurrence cannot recover reentered copy |
| M06 | Select removed card | Permanent removal yields no response |
| M07 | Reverse object order | Ascending ids survive insertion differences |
| M08 | Sort only outer wrapper | Nested order selects replacement Rhino over Titania |
| M09 | Freeze ordered membership | Re-engagement rejects unsupported lethal action |
| M10 | Read real engagement | Trace-local player regression rejects action |
| M11 | Assume unknown chronology | Unordered projected last raises before wrong offer |
| M12 | Admit unknown response profile | Located compiler error names missing profile |
| M13 | Admit zero-health profile | Compiler rejects nonpositive HP |
| M14 | Drop nested area reads | Discard-area admission must reject the last-card lookup |
| M15 | Exclude hosted upgrades | Inspired belongs to its host controller's player area |

All 15 compile and are exercised; each is killed by its intended check. M08
initially survived because the competing minion lacked BRUTE. The strengthened
test uses Titania 01162, asserts that prerequisite and catches the mutant.
No survivor is called equivalent; no setup/build failure counts as a kill.

## Gates and identity

Local Release build has zero warnings/errors. The unit lane passes 2,828 checks
before the final two resource/removal regressions; both added regressions pass
in the current focused suite. Integration's server 186, Godot 357 and
architecture 26 checks pass. All 80 full-game acceptance cases pass. Both
dependency walls watched every forbidden build fail. The complete native matrix
passes all 11 scales from 50% through 150%, reduced motion and two-player 100%/150%
variants. The rendered single/two-player 100%/150% matrix passes and produces 48
checkpoints. Final all-project counts are supplied by the normal commit hooks.

Restartable native/rendered smoke clients use supported desktop profiles and do
not advance the user's existing window. Development identity is `0.1.0-dev.0`,
commit `local`, replay v3, protocol 19, save 4. It is not a clean commit artifact.
Final PR-head adversarial review and exhaustive exact-SHA Windows/Linux CI are
still required for landing.

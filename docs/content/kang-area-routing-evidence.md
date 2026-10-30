# Kang local activation routing prerequisite

MARVEL-858, under MARVEL-568. Source baseline:
`430f0658a0c066d34c559899eb2b01a3805f4623`. This record accompanies the
routing source commit; the delivery record holds its exact commit hash.

This is an implemented prerequisite, pending independent review. It does not
implement Kang I defeat, the delayed stage-II split, setup or public admission.
The [implementation handoff](research/remaining/kang-implementation-handoff.md)
remains the authority and outstanding-scope reference.

## Authority and representation

`pack:mc11:game-areas` says cards in one game area cannot affect another game
area, players sharing an area may interact normally, and play continues in turn
order using the shared encounter deck. `pack:mc11:areas` puts each stage-3 scheme
and its matching Kang II in the player's new game area, with stage 2B outside
those areas. Printed facts come from `datasets/cards/cards.json`.

The prepared capability fixtures use real Immortus, Iron Lad, Chronopolis,
Inexorable Fate and Master of Time faces. They place scenario-owned local zones
within existing player play areas, move those play areas using `World.Join`, and
detach the shared villain play area. No new topology, state fields, card
allocation format or RNG consumption is introduced.

The activation planner selects the reachable villain separately for each player.
An absent local villain does not skip minion ordering or subsequent players.
Ordinary scheme activations use an engine-owned main-scheme destination resolved
within the enemy's game area, retaining the existing play-area narrowing where
applicable. Thus a joined player's minion can use the group's scheme, and central
2B cannot become a local enemy's threat recipient. Ambiguous destinations raise
explicitly rather than choosing by physical card order. Wrecking Crew's explicit
active-villain and corresponding-side-scheme relations retain their precedence.

## Executable evidence

`KangAreaRoutingTests` contains 11 nonpublic capability checks. These exercise
both first-player orders; interpreter and bare engine ports; boost-host ordering;
local and cross-area reach; central exclusion; ordinary Rhino behavior; players
sharing an area; grouped minions; absence of a local scheme; and ambiguity
rejection. The no-villain minion-order fixture deliberately retains its scheme
to isolate the planner continuation; it is a synthetic capability state, not a
claim that Kang's defeat text has resolved. Tests drain the real agenda and
check unfinished outcome, pending activation and RNG where relevant.

Final source verification, 2026-10-27, Windows:

```powershell
dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~KangAreaRoutingTests|FullyQualifiedName~Wrecking|FullyQualifiedName~RedSkull'
dotnet test tests/Marvel.Rules.Tests/Marvel.Rules.Tests.csproj -c Release --filter 'FullyQualifiedName~Wrecking|FullyQualifiedName~EncounterTopologyTests|FullyQualifiedName~Activation'
```

Results: restored build passed; 961 content tests passed; 70 Rules tests passed.
This is focused Windows evidence, not full managed, native or Linux certification.

Each following mutant compiled and failed the targeted checks. Sources were
restored serially, followed by the forced rebuild above and passing final checks.
The first five ran against the nine-check fixture before the grouped-minion
extension; the last ran against the final 11 checks.

| Mutant | Distinguishing failures |
|---|---:|
| Select the global villain for every player | 5 |
| Select the first global main scheme | 7 |
| Return early when a local villain is absent | 1 |
| Select the first of multiple local villains | 1 |
| Select the first of multiple eligible main schemes | 1 |
| Omit game-area filtering of main-scheme destinations | 3 |

Raw logs are outside the repository in `C:/Users/mggar/Source/`:
`kang-routing-final-build.log`, `kang-routing-final-content.log`,
`kang-routing-final-rules.log`, and `kang-mutant-*.log`. Normal pre-commit hooks
are required for the source commit. Independent review remains outstanding.

## Next bounded implementation

Implement Kang's own phase-bound orchestration before claiming a played split:

- Integrate the actual stage-I When Defeated timing so no generic final-villain
  victory is committed, and capture the exact phase-end pending operation.
- At that phase end resolve stage 2A: count/discard side schemes and place central
  acceleration tokens, choose distinct stage-3 schemes through the seeded stream
  in player order, and remove unused stages.
- Resolve each actual 3A and its corresponding II entry, including flat HP,
  toughness, printed starting threat and encounter dealing, before flipping 2A.
- Prove stage 1 completion loss, stage-3 policies, central acceleration
  replacement, local villain-phase threat arithmetic and legal target offers.
- Implement II defeat and phase-end group reunion, uniqueness reconciliation and
  stage-4 advancement in later bounded work; prove pending-state and replay
  reconstruction before public admission.

No production ability book, setup dataset, admission catalog, View, UI, Server,
Session, Agenda or digest source changes belong to this prerequisite.

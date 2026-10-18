# Architecture invariants and evidence

The engine decides legality and state transitions. Content supplies admitted
facts and programs; presentation consumes authorized results. Enforcement protects
these responsibilities without claiming that syntax can recognize every misplaced
rule or that passing tests establish product acceptance.

## Gate inventory

`Architecture` below means
`dotnet test tests/Marvel.Architecture.Tests/Marvel.Architecture.Tests.csproj -c Release`.
These tests run in both **Platform gates** jobs, which the required
**Native integration** checks depend on along with every native journey shard. Unit and content
behavior runs once in required **Managed correctness** on routine PRs. Explicit
certification and releases repeat managed behavior on Windows and Linux.

| Invariant and owner | Enforcement and controls | Local evidence | Remaining review |
|---|---|---|---|
| Presentation dependencies and transitive access; project composition | `Directory.Build.targets`, assembly policy and source-member policy. Presentation-wall positive projects consume engine legality; forbidden projects must fail with `MARVELPRESENTATION`, `MARVELPRESENTATIONCONFIG` or `MARVELINFRASTRUCTURE`. | Architecture; `pwsh tools/presentation-wall.ps1` | An allowed API can still be used to duplicate a rule; inspect the decision it owns. |
| Godot and runtime floor; build composition | Godot-wall valid projects and deliberately invalid dependency, opt-out and framework projects. | `pwsh tools/godot-wall.ps1` | New dependencies need an ownership decision, not an expanded allowlist by default. |
| Typed scheduler operations; Rules | `AgendaProcedureBoundaryTests` compile valid owned payloads and reject mismatched operations and forged subclasses/copy constructors. | Architecture | Payload types alone do not prove completion order. Timing behavior must distinguish suspension from completion. |
| Invocation completion and result publication; Rules/Cards | `CardEntryExecutionContractTests` distinguishes empty event output from unfinished owned work. Action/resource isolation tests reject foreign and completed invocations. Payment and damage replacement continuation tests retain accepted choices and wait for child results before proceeding. | Rules and Content tests | Every new entry port must admit its owner before mutation. A child procedure can extend its work after its parent suspends; pending results must remain explicit throughout that chain. |
| One seeded stream and no ambient gameplay inputs; Core/Rules | `EngineSourcePolicyTests` compile valid deterministic controls and invalid aliases, static imports, target-typed RNG creation and restoration. Assembly metadata checks independently reject ambient dependencies. Core stream/digest authority tests remain pinned. | Architecture; Core tests | Deterministic iteration and concurrent mutation need behavioral evidence. Host transport concurrency is outside the gameplay restriction. |
| Immutable shared content and game-owned execution; Content/Cards/Rules | `EngineStaticStatePolicy` checks actual symbols and nested stored types, including readonly interfaces and mutable builders. `SharedEngineDataTests` attempt vocabulary mutation and verify all shared JSON options are sealed. | Architecture; `CompiledProgramInterleavingTests` | No checker proves arbitrary method bodies pure. Review ownership, captured delegates and future caches. |
| Admission before mutation; Rules/Server | `GameDecisionAdmissionTests`, `DecisionAssessmentTests`, authorization/revision and client recovery tests reject forged or stale answers and then exercise a valid answer. | Rules and Server tests | Every new entry path must use the owning validation boundary. Transport rejection and semantic illegality are distinct. |
| Detached exported state; View/Session/Transport | `ProjectionSnapshotIsolationTests`, `PrintedFactSnapshotTests`, `JournalCaptureIsolationTests`, `EmbeddedTransportIsolationTests` mutate nested source and exported values independently. | View, Session and Server tests | Review every new collection-bearing contract; readonly interfaces can hide mutable storage. |
| Query purity; Rules/Cards/View | `DecisionQueryPurityTests` checks full RNG state, agenda, prompt, card state and information signals before comparing future outcomes with an unprobed Core game. Content query-isolation and interleaving tests cover interpreter queries. | View and Content tests | This is a finite set of query paths, not proof of all possible effects. A card digest excludes RNG and execution state. |
| Replay at each accepted decision; Session | Journal prompt/event comparisons, `SessionReplay.Verify`, strict save admission and interleaved games. `NestedInterruptReplayTests` saves a Core Get Behind Me! attack at its nested defense prompt and compares subsequent events, prompts, full RNG state and card state with uninterrupted execution. Counterexamples alter unchosen offers, ordering, cost constraints and semantic events. | Session and Server tests | New external inputs must be admitted and captured; final-state equality alone is insufficient. |
| Visibility across alternate routes; View/Server | Restricted/cooperative scope tests, hidden recipient/window receipts, unauthorized seat and embedded/socket isolation cases. | View and Server tests | New explanations, counts, errors, history and accessibility text are information-bearing outputs. |
| Layout debt shrinks; source ownership | Exact current-source diagnostics plus `SourceDebtRatchetTests` compare against the branch base. Controls accept line movement/removal and reject new owners, overload debt and larger measurements. | Architecture; architecture-policy hook | A justified exception changes the policy visibly with focused review; editing the current baseline cannot authorize growth. |
| Tests contain behavior and avoid exact duplication; test ownership | `TestQualityPolicyTests` emits compiled valid/invalid fixtures and scans tracked/unignored tests. Different theory inputs and delegated assertions remain valid. | Architecture; architecture-policy hook | Assertions can be weak or implementation-shaped despite passing this narrow gate. Use behavioral review and targeted compiled mutants. |
| Native membership and observable completion; test tooling | `NativeManifestTests` independently enumerates the 125-case baseline union and verifies profiles, scale, motion, players, script identity, fixtures and completion bounds. Seventeen added contracts bring the exhaustive profile to 142 cases and representative profile to 132; seven additional inspection-scale cases remain separately selectable. Shared Windows/Linux executor enforces process exit and required markers. | `pwsh tools/native-cases.ps1 -List`; focused `-Case`; native matrix | A completion marker is only as strong as its assertions. Distinguish legal prepared scenes, full journeys, synthetic shapes and untested interactions. |

The repository ruleset was inspected during MARVEL-538 preparation: the default
branch requires Managed correctness, Native integration on Windows and Linux,
and Windows desktop community package, with strict up-to-date checks. Hooks are
early feedback, not a substitute for those server-side requirements. Recheck the
actual ruleset when changing job names or protection; a workflow file alone does
not establish that a check is required.

## Extending content without moving rules into clients

1. Identify the vendored rule, ruling and printed card text. Keep a rules-derived
   answer separate from an engine choice. Check timing rulings before encoding a
   new timing assertion.
2. Admit facts through the owning dataset/catalog gate. Generated data remains
   reproducible offline; authored setup has behavioral validation. Research data
   does not make an expansion playable.
3. Choose an existing engine mechanism where it fits. A bespoke engine module is
   appropriate when the DSL would hide the mechanic. The scheduler and transport
   must not dispatch by individual card identities.
4. Make mutable state game- or invocation-owned and reconstructible from admitted
   inputs and decisions. Any future collection, campaign-history or custom-content
   input needs explicit validation and replay capture before it can affect play.
   Do not reach out to a live collection or previous game's process state.
5. Expose visibility-safe semantic offers, costs, constraints and consequences.
   Specialized UI may help compose an intention; it must call engine legality
   rather than derive it from text, counters or geometry.
6. Prove the relevant decision boundaries, including rejection, suspension,
   completion, interleaving and replay. Kill a plausible compiled mutant at a new
   decision point, or document why it is equivalent. Add native evidence for a
   new interaction contract, not automatically for every card.

Collection searches, geometric card state and previous-game conditions are
design stress tests for ownership, not asserted card requirements or permission
to expand the Core runtime. Do not build speculative frameworks to satisfy them.

## Targeted mutation evidence

Four individually compiled mutations were exercised while implementing MARVEL-538:
retaining a nested target group in `PromptSnapshot`, retaining `AreaReordered.Order`,
retaining provider keyword storage, and recording a search information signal
from `WorldProjection.For`. The corresponding isolation/query-purity tests failed;
each mutation was removed and the clean tests passed. These observations establish
sensitivity to those mistakes, not a mutation score or exhaustive purity proof.

Four individually compiled payment mutations were also exercised for MARVEL-539:
resetting the accepted instruction cursor, publishing event payment markers before
resource generation completes, advancing a paid choice while its payment is
suspended, and accepting the wrong taken-damage amount. Each caused its focused
behavioral test to fail; exact production source was restored and the clean focused
suite passed. Damage-cost placement and defeat settlement have separate owned
receipts so simultaneous physical costs can finish before defeat interrupts, while
the post-arrow effect still waits for settlement.

Contributor identity checks were tested with three further compiled mutations:
exhausting a selected ally's returned copy, accepting a returned hand contributor
after an earlier generator suspends, and postponing an already-generated hand
resource's discard. The tests observed the incorrect new-copy mutation, missing
rejection, and incorrect area during the pending generator respectively. Restored
source passed all four contributor cases. A separate compiled mutation removed
attachment-host preflight: the rejected upgrade had already moved and paid, so
the unchanged-hand assertion failed. Restoring preflight passed the CardPlay and
standalone-response focus.

Required physical costs have three additional compiled negative controls:
skipping a missing selected copy, ignoring an already-exhausted target, and
discarding incarnation identity for damage and hand-discard costs. Each mutation
allowed an unpaid effect or changed the wrong copy and failed its focused test.
After exact source restoration, all eight required-cost cases passed. Multi-card
costs validate every accepted copy before changing the first one.

A suspended healing-cost regression distinguishes healing the required damage
from an identity already healed by intervening work. Removing the amount guard
produced a compiled failing mutant while the unchanged-target control passed;
restored tests include the actual Core Rechannel ability. An unpaid heal cannot
run the post-arrow draw.

Full final-source platform validation and the effect-lifecycle/native additions
are recorded with the implementation PR. A local build using existing dependency
binaries is useful during parallel work but cannot certify the final integrated
engine. Product-owner acceptance is a separate obligation in `tests/AGENTS.md`.

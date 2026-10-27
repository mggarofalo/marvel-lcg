# Kree Fanatic public complete-game proof

Canonical delivery: MARVEL-567. Production source: `d1871dd20dbec6930c566f6231a452885e0fa532`.
This packet adds acceptance evidence only; the five actual ability rows and engine remain unchanged.

The four fixed games use normal `DatasetGameFactory` admission, explicit public
`kree_fanatic` modular selection, published starters and shared `CoreGamePolicy`.
The policy sees engine-offered decisions, costs and targets. No prepared states,
deck ordering, seed search or victory requirement is used. Each accepted step is
compared with an independent normal `EngineHost` session: prompt, exact ordered
events, cumulative RNG words, state fingerprint and terminal result. Strict durable
JSON plus `SessionReplay.Verify` reconstructs each completed trace, including its
complete final MT19937 state. Policy queries preserve digest, RNG and pending work.

## Partial result and blocker

| Scenario | Heroes in seat order | Seed | Result | Round | Decisions | RNG words |
|---|---|---:|---|---:|---:|---:|
| Rhino Standard | Spider-Man | 1 | PlayersLose | 4 | 33 | 130 |
| Rhino Expert | Spider-Man | 2 | PlayersLose | 3 | 29 | 140 |
| Rhino Standard | Captain Marvel, Spider-Man | 3 | Blocked at host rejection | 5 | 102 accepted | 174 |
| Rhino Expert | Captain Marvel, Spider-Man | 4 | PlayersLose | 3 | 46 | 256 |

The unchanged four-case proof is **red: 3 passed, 1 failed**. These are partial
evidence, not complete pack acceptance. At seed 3 decision 103 the pending
`WhenThreatWouldBePlaced` optional interrupt offers Great Responsibility from
anchor 75 to seat 1. `DecisionAssessment` accepts decline and direct resolution
continues, but the public host rejects `invalid_decision`: “the decision is not
available to that seat.” `EngineHostResolution.AvailableTo` consults
`Game.PromptFor(actor)`, unlike direct resolution. The accepted prefix preserves
the original prompt and preceding elimination sequence for independent diagnosis.
The test records eliminated-seat/source-location details when reached; it does
not alter host authorization or remove the failing journey.

The single-case diagnostic confirms seat 1 is eliminated, its authorized prompt
is null, and anchor 75 is face `01061` in `UpgradesArea`. Decision 102 chooses the
next engaged minion; its events include Judge, Jury, Executioner's forced response
to friendly defeat before the pending threat interrupt. This locates the defect
in an ordinary reached continuation rather than an arranged encounter.

The governing elimination authority must be checked before repair:
`rr:player-elimination.step.5` removes the player's game elements and participation;
`rr:player-elimination.5` finishes an ability already resolving. A stale new
optional offer and an existing required continuation need distinct treatment.

## Characteristic replay checkpoints

Indexes are one based and include mulligans and optional declines. They identify
the accepted answer whose resolution emits the event, not an arranged snapshot.

- Standard solo seed 1: decision 10 reveals Ronan into seat 0's engaged enemies;
  decision 27 resolves You Dare Oppose Me; decision 33 resolves a three-icon
  Bring the Hammer Down attack boost.
- Expert solo seed 2: decision 18 attaches The Accused to seat 0's identity.
- Expert multiplayer seed 4: decision 20 resolves You Dare Oppose Me; decision 22
  resolves Ronan's boost entry engaged with seat 1 during a scheme activation;
  decisions 31 and 35 move Ronan's engagement to seats 0 and 1 respectively.
- Standard multiplayer seed 3: replay the original archive through 102 accepted
  decisions before diagnosing decision 103. Do not replace its heroes or seed.

Judge, Jury, Executioner is not naturally exercised by the three completed games.
Existing actual-row tests supply its entry/threat, friendly defeat and boost
evidence; the targeted Ronan classes also cover characteristic branches that a
natural game need not draw. Public-game movement observations alone do not claim
complete printed-card branch coverage.

## Artifacts and remaining work

`C:/Users/mggar/Source/evidence/ronan-public-games-preserved.log` records the
complete 3-pass/1-fail run (1.18 minutes). Archives are under
`C:/Users/mggar/AppData/Local/Temp/marvel-ronan-public-games/`:
`rhino-1.session.json`, `rhino_expert-2.session.json`,
`rhino-3.session.json` (102 accepted decisions), `rhino_expert-4.session.json`.
`ronan-seed3-last-accepted-step.json` in the external evidence directory preserves
the preceding step for diagnosis. Full decision logs include state hashes; each
completed-game summary includes final digest and accepted-trace SHA-256.

`ronan-seed3-diagnostic.log` reruns the unchanged seed-3 case alone and confirms
the same rejection (1 failed, 46.5 seconds). The targeted actual-row run in
`ronan-public-game-actual-rows.log` is **37 passed, 1 failed** across
`RonanActualCardTests`, `RonanHammerActualCardTests`, `RonanJudgeActualCardTests`,
`RonanRemainingCardTests`, `RonanPhaseBeginningTests` and
`RonanSelectedEngagementTests`. Its sole failure is the older
`AuthoredFacesRemainExcludedUntilTheSeparateAdmissionPacket` assertion, which
expects Ronan faces to remain absent after this source revision admits them.
The runtime behavior checks pass; the admission assertion needs reconciliation
in the admission packet, outside this packet's owned files.

The next action is independent engine diagnosis/repair, then rerun these exact
four cases unchanged. Fresh-host characteristic review, native evidence and
combined/platform gates remain separate delivery work. No owner approval gate
is added for ordinary modular content.

## Facedown timing repair

`AbilityWindowAdmission` requires faceup placement for ordinary in-play printed
window abilities. Explicit event-play origins remain available, including a
facedown event in its owner's hand. Authority is
`rr:in-play-and-out-of-play.10`: "Facedown cards attached to in-play cards are out
of play." In the original seed-3 trace, card 75 is Great Responsibility, placed
from Spider-Man's hand facedown on Highway Robbery (host 50) during round 4.
Its printed interrupt is inactive. This is a stale candidate, rather than an
already-initiated continuation permitted by `rr:player-elimination.5`.
Host authorization is unchanged.

`HighwayRobberyWindowTests` uses both actual rows to prove absence before and
after the owner's elimination, restored availability after Highway Robbery's
defeat, facedown-hand event access, and completion when a legally initiated
Great Responsibility eliminates its owner. The focused Highway Robbery, Core
threat/context, self-defeat and would-be-defeated classes pass together (53 tests).
Removing the faceup requirement kills both captured-event cases while the
restored-hand control passes (2 failed, 1 passed in the three-case mutant run).
Logs are `C:/Users/mggar/Source/evidence/highway-window-regression-final.log` and
`highway-window-mutant.log` in the same directory.

The exact four original public cases pass after repair, including ordinary
EngineHost authorization, per-step ordered events/digest/RNG and complete replay:
`dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release
--filter FullyQualifiedName~RonanPublicGameTests`.
The run took 1.05 minutes; log:
`C:/Users/mggar/Source/evidence/ronan-public-games-facedown-fix.log`.
Seed 3 ends in PlayersLose at round 5, after 102 accepted decisions and 174 RNG
words. The source prefix remains separately preserved at
`C:/Users/mggar/Source/evidence/ronan-seed3-original-102.session.json`.

This changes replay semantics: seed 3 first differs in the events of accepted
decision 96, because removing a false optional window allows threat and activation
work to continue in that resolution. A source journal must not be silently
rewritten. This repair belongs to the unpublished aggregate replay-v28 candidate;
the existing v27 rejection boundary and frozen-source admission routes require
integration review. Protocol, save schema, RNG and state-digest formats are
unchanged.

A separate elimination ownership finding remains: `rr:player-elimination.step.4`
says "Place each card owned by the eliminated player in the eliminated player's
discard pile." Current elimination visits the eliminated play area, leaving the
owned captured card on Highway Robbery in the villain's play area. Fixing that
ownership-wide cleanup and its projected counterpart is a separate packet.

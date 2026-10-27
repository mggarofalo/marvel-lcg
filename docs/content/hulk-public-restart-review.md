# Hulk public game review and fresh-host restart

Reviewed source: `19095f40bc0f01f17844924020e16aa2a0e5230b`.
This is a bounded independent review of `HulkGamePolicy`, `HulkGameEvidence`
and `HulkPublicGameTests`, with new public acceptance evidence for two fixed
natural signature boundaries. It does not establish whole-pack or native
acceptance.

## Source review

The harness opens ordinary admitted production `DatasetGameFactory` games and
an independent `EngineHost` game. It checks the physical forty-card Hulk deck
against the selected public recipe, production deck legality, the exact prompt,
raw causal events, RNG word receipts, state fingerprints and terminal result at
each accepted decision. Policy selection is guarded by canonical digest, full
RNG state, prompt, phase, round, outcome, agenda and registered-effect baselines.
Strict durable JSON and production replay verify the terminal trace.

The policy's priority is an authored tactic. It selects legal offered handles,
targets and resource generators; it does not move cards or decide legality from
printed text. The corrected policy control distinguishes Experimental Research
from changing form and has compiled mutant kill/restored-build evidence in the
source packet. Counters observe accepted signature plays and basic attacks;
mandatory discard evidence follows the actual original hand card identities
into Hulk's discard pile. These observations prove representative actions,
not every card branch.

The eight retained terminal archives contain **seven `PlayersLose` and one
`VillainWins`** (multiplayer Rhino/7). All are defeat outcomes. There is no
victory evidence. Their scenarios and seeds remain fixed; no seed search was
added to pursue wins. Public custom multiplayer Rhino/8 uses three Resourceful
extras in a production-legal Hulk deck with Captain Marvel in seat 0.

No production defect was found in this bounded source review. The original
eight games were not repeated by this review; their execution, terminal replay
and policy mutation artifacts remain in the source packet at
`C:/Users/mggar/Source/evidence/hulk-public-games/`.

## New restart evidence

`HulkPublicRestartTests` reaches its boundaries by playing public offered
decisions with the source policy. It never injects, moves or edits cards.
Its observer factory delegates unchanged to a newly loaded production factory.
Every restart uses a new host, new factory and independent store, with a strict
`SessionSaveJson` round-trip and a separate JSON copy of protected authorities.
The private server envelope serializer is not an acceptance-test API.

- Solo Rhino/1, decision 6: naturally drawn Unstoppable Force. Restart before
  committing its offered payment; stale and unpaid commitments leave the exact
  saved record, prompt, digest, RNG, agenda and effects unchanged. Accepted
  continuation readies exhausted Hulk, draws one card and discards the source.
  Printed 10006 says to ready Hulk and draw one card for physical-only payment;
  the vendored zero-price ruling `ruling:5bbba476a198a90c` does not change this
  positive paid case.
- Custom multiplayer Rhino/8, decisions 14–15: Hulk in seat 1 naturally pays
  Thunderclap, then suspends at its mandatory recipient choice. Restart before
  payment and again after payment. Verify the answering seat, suspended prompt,
  discarded payment cards and retained paid hand. Reject a stale answer and an
  invalid non-enemy recipient without changing the record or execution state.
  Selecting the villain deals exactly three damage, completes the action and
  discards its source without charging the hand again. Printed 10005 says to
  choose up to three different enemies and deal three damage to each;
  `rr:choose-game-element.1` assigns the choice to the resolving player.

Each accepted resumed decision compares the complete session save (including
raw events and RNG/digest receipts), projected events, revision, history,
canonical state, full RNG state, prompt and pending work against an uninterrupted
control. Four further accepted decisions are compared in each case. Sync
snapshots are compared with sync snapshots: resolve responses additionally carry
transient faces for their own events. The initial harness comparison of those
two different response kinds was corrected; it was not a production finding.

Command: `dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release --filter FullyQualifiedName~HulkPublicRestartTests --logger "trx;LogFileName=restart-final.trx" --results-directory C:/Users/mggar/Source/evidence/hulk-public-restart`.

Evidence lives outside the repository in
`C:/Users/mggar/Source/evidence/hulk-public-restart/` and the adjacent
`hulk-public-restart-final.log`. Tests have no dependency on those paths or the
source packet archives.

The final focused Release run passed **2/2** cases in 1 minute 28 seconds.
No production source, ability book or setup recipe changed in this review.

## Limits

This establishes fresh-host reconstruction and rejected-input atomicity at
these two natural public boundaries, including a paid non-first-player choice.
It does not establish multiplayer network reconnection, campaign continuation,
Hulk Smash restart, every signature branch, every scenario victory, native
interaction or player comprehension. Native evidence remains a separate packet.

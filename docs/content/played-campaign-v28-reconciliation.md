# Played campaign rejection under replay v28

The integrated Ronan candidate `bb1ec14e00a8658cdb5ab5a54bb6d6360180e5de`
continues rejecting both authentic standard played campaigns and the expert
played campaign. The ownership-wide elimination correction makes the first
Crossbones attempt divergent, before the previously pinned failures in the next
attempt. This packet changes diagnostic expectations and strengthens their
causal and preservation assertions; it changes no runtime, migration or fixture.

| Authentic source | First rejection before elimination correction | First rejection after correction |
|---|---|---|
| Standard v19 and v20 | Attempt index 1, unit 7 decision 2: expected 18 events, got 14 | Attempt index 0, unit 16 decision 6: expected 61 events, got 76 |
| Expert v19 | Attempt index 1, unit 25 decision 0: expected 3 events, got 6 | Attempt index 0, unit 28 decision 3: expected 65 events, got 74 |

Indices are zero-based. The exact current exception messages retain the `events
diverged` stage and unit/decision positions. The standard nested-reveal failure
still occurs with its original 18/14 event counts when its authentic attempt is
verified separately. The expert confused-thwart cause and its synthetic
unconfused control remain asserted against their original recorded events.

## Authority and changed events

`rr:player-elimination.step.4` says: "Place each card owned by the eliminated
player in the eliminated player's discard pile." Step 5 then removes the
player's play area and its game elements. The event spelling and deterministic
ordering are engine choices; strict replay requires the original count, order
and values regardless of whether two executions eventually remove the cards.

For the standard first attempt, the historical resolution moves eight deck
cards and seven hand cards directly to RemovedArea. The corrected resolution
discards those fifteen cards, then removes them from the owner's discard pile.
DiscardPile-to-RemovedArea events increase from 25 to 40, producing 76 rather
than 61 total events. The first differing event is index 4: historical card 1
(identity) moves HeroArea-to-RemovedArea, while current card 17 (`01007`, Spider
Tracer, on minion 79) moves UpgradesArea-to-DiscardPile. Encounter departure now
precedes ownership cleanup. Actual owner-discard order is
`17,11,33,37,39,43,24,22,46,36,21,20,14,42,19,18`.

For the expert first attempt, two deck cards and seven hand cards gain the
discard-before-removal path. DiscardPile-to-RemovedArea events increase from 31
to 40, giving 74 rather than 65 total events. First differing event index 8 is
historical card 35 (`01065`, Heroic Intuition) moving
UpgradesArea-to-DiscardPile versus current encounter minion 86 (`04146`)
moving EngagedEnemiesArea-to-EncounterDiscardPile. Actual owner-discard order is
`38,22,23,42,14,17,18,40,28,35,31`.

The tests strictly verify prompts, ordered events, RNG and state fingerprints
through each first-attempt prefix. They then require the exact changed event
counts, all fifteen/nine deck-or-hand discard moves, their final removal, and
the absence of these discard moves in the untouched historical recording.

## Revision comparison and evidence

Source fixes reviewed for attribution are `561e3904` (enemy-attack defeat
receipts), `a0d94122` (facedown window admission), and `8a35a678` (elimination
ownership). The latter integrates as `c17c5005`. A separate checkout at
`47db16889563f870e087ea21b0cf02d00b9d350d`, immediately before that integration,
already contains the first two fixes. All three original exact-diagnostic tests
pass there. Diagnostic replay also verifies the complete first attempt in all
three archives there. Therefore the new earlier divergence is attributable to
the elimination correction, rather than the other two fixes.

The first two source fixes integrate as `77dea1fe` and `e1dd181d`, respectively.

Full expected/actual ordered event dumps, card identities and per-attempt first
divergences are retained outside the checkout under
`C:/Users/mggar/Source/evidence/`:

- `played-v28-before-elimination-run.log` (three original controls pass);
- `played-v28-before-elimination-{v19,v20,expert}.txt`;
- `played-v28-diagnostic-{v19,v20,expert}.txt`;
- `played-v28-reconciliation-focused.log` (final route and rejection controls).

Diagnostic instrumentation writes only external evidence and is absent from the
committed tests. Authentic archive SHA-256 checks remain pinned. Quarantine is
checked across two fresh host/store restarts, import cannot publish a copy, and
the preservation snapshot now covers every extracted source file, including
access and transfer files, rather than only campaign storage. Existing writer
lease exclusion remains unchanged. Unaffected named-adapter and Absorbing Man
controls continue requiring exact original history preservation.

The final focused Server run passes 43 tests, including both complete fixture
classes, retaliation rejection, campaign content-upgrade rejection, named replay
routes and rule-citation validation. The command is:

```powershell
dotnet test tests/Marvel.Server.Tests/Marvel.Server.Tests.csproj -c Release --filter 'FullyQualifiedName~PlayedCampaignReplayUpgradeTests|FullyQualifiedName~ExpertCampaignReplayUpgradeTests|FullyQualifiedName~RetaliationReplayUpgradeRejectionTests|FullyQualifiedName~CampaignContentUpgradeRejectionTests|FullyQualifiedName~ReplayContractUpgradeTests|FullyQualifiedName~RuleCitationTests'
```

Validation is Windows source-build evidence. This packet does not establish
successful migration of a complete played campaign or packaged/Linux acceptance.

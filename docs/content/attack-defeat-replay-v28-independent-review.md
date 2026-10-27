# Attack-defeat replay v28 independent review

Reviewed source: `1951c19cc750ff9afeb6efef6af34fb4b3dbc4c6`.

Verdict: accept this replay boundary; no blocking correctness finding. The review scope is the replay
compatibility boundary, not new Ronan admission or complete pack acceptance.
The accepted engine timing review is `d87fa3fb` and its
`ronan-enemy-defeat-independent-review.md` evidence. Actual authored Rapid Response
08031 moves from an active enemy attack to its completed response window; a replay
identity change is required even when the runtime dataset bytes are identical.

The boundary changes the current replay identity to `engine-replay-v28`. It adds
explicit frozen Core/RoRS v19/v20/v21 source routes to v28 and a same-v28 additive
route. `ContentUpgradeAdmission` requires the exact frozen source triple, RNG and
digest identities, with unchanged admitted-entry fingerprints in the target.
Historical named routes retain their exact destinations. Unknown, unadmitted and
reversed identities have no inferred route. V27 receives no v28 source admission,
including when its dataset hashes match the current target.

`SessionContentUpgrade.Verify` checks runtime guards, then calls strict
`SessionReplay.Verify` on the original save before returning a new compatibility
record. Strict replay verifies all retained units, including inactive redo,
information exposure and the active pending prompt. No expected prompt, event,
RNG count or digest is generated to replace a recorded fact. Campaign verification
likewise precedes its aggregate compatibility replacement. Schema 7, RNG,
state-digest-v3 and protocol 42 formats are unchanged. The release provenance and
active matrix protocol correction from 41 to 42 aligns metadata with the existing
`EngineProtocol.Version`; historical protocol narratives remain historical.

Independent tests are in `AttackDefeatReplayIndependentReviewTests`:

- The v28 admission matrix rejects each independently changed dataset hash, RNG,
  digest and unknown/unadmitted replay source, with the unchanged authentic v19
  identity admitted as its control. These are synthetic admission probes, not
  recordings of the rejected replay versions.
- A genuine nonempty v19 disk save upgrades to v28 through the production host.
  Every serialized original history/authority value is retained after normalizing
  only the approved compatibility record. The original session and authority
  backup files match their pre-upgrade byte arrays exactly. Restart leaves the
  generation and the single backup pair unchanged.

The source commit changes no historical fixture, dataset, protocol shape, RNG
implementation or digest format. SHA-256 checks match the pinned fixture records:

- `core-session-91098022.zip`:
  `63BD6E38C7668F6789BB48300165026D34CA55432B59ADF08FF2B19793E87ABC`.
- `content-91098022.zip`:
  `7184323687CE9DFB5CAB49AA00447A9D5233D2DF3EC24F7EF58B5C5187C9F880`.
- `played-campaign-91098022.zip`:
  `1199392F3439FE89B6A21AA03D195DF18BEF1CC70F20DD65A998A2E32B29FFE8`.
- `expert-campaign-91098022.zip`:
  `1A7B953709EE3E390863BFD28A6C920EE00B53F7F24853B197013AA72903538B`.

Evidence is focused Windows execution. It does not claim a new Linux, packaged,
full-suite or full-campaign acceptance run. Frozen v20/v21 positive evidence is
explicitly verified derivation from the authentic v19 trace, not a newly claimed
historical recording. Existing divergent campaign quarantines and the absence of
a positive complete compatible predecessor campaign remain unchanged limitations.

Final independent verification passed 76/76 cases across the complete
`AttackDefeatReplayIndependentReviewTests`, `AttackDefeatReplayRouteTests`,
`EngagementReplayFactTests`, `ReplayContractUpgradeTests` and
`LiveEffectsReplayRouteTests` classes. The new class contributes 14 cases. The
selection verifies original initial/decision events, prompts, RNG counts, digests,
current prompt and historical destination guards as well as the independent cases.

A compiled mutant removed the exact source-identity comparison immediately before
`SessionContentUpgrade` construction. The new dataset/RNG/digest probes failed
5/14 cases at the expected `Assert.Null` admission assertion. The other nine cases
passed; the replay-source switch remained intact. After restoring the exact source,
a non-incremental Release build completed with zero warnings/errors, and the same
76-case focused selection passed again. No mutated production source is retained.

Commands used `dotnet test tests/Marvel.Server.Tests/Marvel.Server.Tests.csproj
-c Release --filter` with the five class names above; restoration additionally used
`dotnet build tests/Marvel.Server.Tests/Marvel.Server.Tests.csproj -c Release
--no-incremental` before the final `dotnet test ... --no-build` selection.

Logs outside the repository:

- `C:/Users/mggar/Source/replay-v28-independent-review.log` (initial 76/76).
- `C:/Users/mggar/Source/replay-v28-independent-mutant.log` (compiled mutant,
  five intended admission failures).
- `C:/Users/mggar/Source/replay-v28-independent-restored-build.log`.
- `C:/Users/mggar/Source/replay-v28-independent-restored.log` (final 76/76).
# Doctor Strange response events — MARVEL-803

Content packet on base `25fa8c2e`, branch `feat/doctor-strange-response-events`,
2026-10-26. The content revision is the commit containing this record; the lead
records its hash in the canonical Plane delivery item. This packet is ready for
independent content review, with a known engine prerequisite. It is not a green
integrated candidate and does not establish complete Doctor Strange coverage.

## Inventory and authority

Four actual ability-book rows are appended: 09016 Momentum Shift, 09021 Warning,
09037 Skilled Strike and 09038 Foiled!. Total: 658 rows, 13/40 Doctor Strange
faces. The preceding 654 raw JSON object strings are identical to base
`25fa8c2e` after normalizing Git/worktree line endings; property order,
whitespace within objects, values and object order are preserved. SHA256 of the
concatenated normalized original objects:
`2938f8dabb342ef5a4dc7503a1ed37dbfd02f4006835080ab62f9f742977fe7a`.
The check used JSONDecoder.raw_decode offsets, rather than reserializing parsed
objects. Proof: `%TEMP%/strange-response-raw-proof.txt`.

Printed authority is generated `datasets/cards/cards.json`. Momentum Shift has
printed cost two and a heal-two arrow cost. Vendored ruling
`ruling:ecabf270719f2990` confirms the whole healing cost must be paid. Current
RR 1.8 `rr:initiating-abilities.step.5` requires payment of costs. Warning uses
the corrected generated Interrupt text, without a defense label, hero-form
restriction on its controller or restriction to attack damage. Skilled Strike
requires the controller's basic attack; `ruling:3eb282172bb5df66` places its
window after exhaustion, consistent with `rr:attack-player-ability-type.1`.

Foiled! uses the current RR 1.8 boost ordering and
`ruling:1ea208998735ef4b`: numeric icons including constants/amplify precede the
interrupt, while the star ability resolves afterward. The older
`ruling:9e43ecc7878167bd` predates the changed ordering and is not the governing
timing authority. This packet tests cancellation of a constant-modified boost
and preservation of its separate star ability; it does not add another test of
later icons granted by a Boost ability.

No engine, UI, setup or public-admission changes are included. Existing exact
book-size assertions are updated, previous packet slices retain exact contents,
and the unauthored-card negative uses still-unauthored 09020. The new inventory
test asserts the four exact appended rows and absence of starting counters.

## Actual-row behavior

`DoctorStrangeResponseEventTests` checks:

- Momentum Shift at zero, one and two damage; the valid same-cost two-damage
  case completes its enemy-choice continuation, heals two, deals two, and
  discards the event and payment. Rejected resource payment preserves digest,
  full RNG state and pending agenda snapshots. Query snapshots are taken before
  obtaining offers.
- Warning on the controller's and another player's hero, direct and attack
  damage, and exclusions for an ally, alter-ego and zero damage. The taken
  amount falls by one while the dealt amount remains unchanged.
- Skilled Strike for a non-first player after exhaustion, exclusion of another
  seat's attack, expiry before the next basic attack, and exclusion of a fully
  resolved actual Swinging Web Kick event attack.

`DoctorStrangeFoiledTests` checks a complete ordinary two-boost scheme: the
first numeric contribution is cancelled, Weapons Runner's star still enters
play, the second boost retains two icons, and the main scheme gains three
threat. A separate complete enemy attack excludes Foiled!, retains both boost
icons and deals four damage, with AttackCompleted and empty agenda asserted.
The generic faceup observer replaces a synthetic 01092 body locally; it exposes
the precise interrupt even when Foiled! is absent. Remaining optional observer
prompts are declined before final-state assertions.

The nested case is explicitly a synthetic scheduler suspension: an attack is
paused and a scheme faceup occurrence inserted. It proves Foiled! matches and
cancels the inner scheme's exact physical boost card while an outer attack
exists. It stops after cancellation. It does not prove completion of a legal
nested activation journey. Attempting to complete that artificial scheduler
arrangement loses the activating enemy; that is a fixture limitation and no
engine change is inferred from it.

## Verification and mutation evidence

All commands use Release and run serially in this worktree. Logs remain outside
the repository under `%TEMP%`.

1. Restored nonincremental build:
   `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental`
   — passed, zero warnings/errors (`strange-response-restored-build.log`).
2. Restored focused data, citations and affected content:
   `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-build --filter "(FullyQualifiedName~DoctorStrange|FullyQualifiedName~AbilityData|FullyQualifiedName~BlackWidowBurnNoticeTests|FullyQualifiedName~BlackWidowFinalResponseCardTests|FullyQualifiedName~BlackWidowPreparationCardTests|FullyQualifiedName~BlackWidowPreventionInventoryTests|FullyQualifiedName~RuleCitationTests)&FullyQualifiedName!~MomentumRequiresFullHealing"`
   — 95 passed, none skipped (`strange-response-restored-data.log`). The
   Momentum theory is deliberately excluded from this green subset.
3. Restored complete response classes, filter
   `FullyQualifiedName~DoctorStrangeResponseEventTests|FullyQualifiedName~DoctorStrangeFoiledTests`
   — 15 tests, 14 passed, one failed: damage-one Momentum wrongly receives an
   offer (`strange-response-restored-full.log`). This is the known MARVEL-804
   engine prerequisite, not an authored workaround or a waived failure.
4. Four actual-row mutants loaded/lowered successfully and failed behavioral
   assertions: Momentum attack damage two to three (the valid damage-two case
   fails), Warning prevention one to two, Skilled Strike bonus two to three,
   and Foiled! scheme condition to attack (the full attack-exclusion assertion
   fails). Each uses the characteristic test filter and `--no-build` against the
   compiled test assembly; the runner lowers the mutated book at runtime.
   Logs: `strange-mutant-momentum-damage.log`,
   `strange-mutant-warning-prevention.log`, `strange-mutant-skilled-bonus.log`,
   `strange-mutant-foiled-attack.log`; summary `strange-response-mutants.txt`.
   The initial combined Foiled mutant run also reached the artificial nested
   completion error and is not counted as its kill. The separate attack test
   gives the distinguishing Assert.DoesNotContain failure. Every mutant was
   restored in a finally block, followed by the nonincremental build and
   restored checks above.

Earlier `strange-response-detail.log`, `strange-foiled.log`,
`strange-response-closeout.log` and `strange-response-data.log` are intermediate
revision evidence. In particular, the earlier AbilityDataTests name filter did
not select the renamed ability-data classes; the final AbilityData filter above
runs them. Do not use intermediate runs as final green evidence.

## Remaining integration

MARVEL-804 owns full healing-cost admission and execution, including its shared
engine interactions. Preserve the actual damage-one negative; integrate the
independently reviewed prerequisite, then rerun the complete response classes
without exclusions. This packet does not probe partial healing-cost execution.
Independent review, integrated broader gates and the remaining 27 Doctor Strange
faces remain outstanding. Public admission stays closed. No native/UI or
whole-pack readiness claim is made by this content packet.


## Independent content review — 2026-10-26

Reviewed source `4953b4804da99cce354d5474624a7aa7bd61b24f` against
`25fa8c2e` in isolated worktree `marvel-strange-events-review`. Generated
09016/09021/09037/09038 text, costs and types agree with the four authored
interpretations. Reviewed current RR 1.8 initiating-abilities, prevent, heal,
scheme activation and activation clauses, and the four ruling records identified
above. The historical Foiled! answer does not override current boost ordering.
Existing mutation logs show four successfully lowered, behaviorally killed
actual-row mutants; they are implementation evidence rather than new independent
mutation runs.

Verdict: the four authored rows are correct for the reviewed printed text.
Warning, Skilled Strike and Foiled! have no additional content findings in this
bounded review. Momentum Shift's runtime remains **not accepted**, pending
MARVEL-804: a one-damage hero receives the event offer despite inability to pay
the full heal-two cost. The whole packet is conditional, not an accepted green
candidate. No public pack admission or UI/product acceptance is claimed.

Added `IndependentDoctorStrangeEventTests` provides two distinguishing controls:

- A non-first controller in alter-ego form plays actual Warning for another
  player's hero, preventing all one damage while retaining one damage dealt.
  This closes the eligible alter-ego-controller gap in the original matrix.
- Actual Foiled! cancels the exact scheme boost after a Boost-requested scheme
  waits for the originating attack to finish. The attack faceup excludes Foiled!,
  its completion precedes the scheme faceup, both activations finish, and threat,
  damage, boost contributions and physical cleanup are asserted. Only the local
  Boost-requesting and generic observer bodies are synthetic; the scheduler uses
  ordinary deferred activation ownership, without inserting an activation into a
  paused agenda. This proves a legal scheduler lifecycle, not printed behavior
  for the two replaced fixture bodies.

RR `rr:activation.8` requires an activation initiated inside another to resolve
only after the current activation finishes. The original concurrently suspended
nested occurrence fixture therefore remains useful synthetic contract evidence;
it does not establish a legal simultaneous nested-activation journey. Its stated
limitation is accurate and is not an additional content rejection.

Independent checks in Release, run serially:

- `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release
  --filter FullyQualifiedName~IndependentDoctorStrangeEventTests`: 2 passed.
- The same project with `--no-build --filter
  "FullyQualifiedName~DoctorStrange|FullyQualifiedName~AbilityData|FullyQualifiedName~RuleCitationTests"`:
  81 total, 80 passed, one failed, none skipped. The sole failure is the actual
  Momentum damage-one negative at the offer assertion. It was neither excluded
  nor weakened.
- `git diff --check`: passed.

Logs are `%TEMP%/strange-events-independent-first.log` and
`%TEMP%/strange-events-independent-focused.log`. These results are source plus
the two review tests, without MARVEL-804. The integrated candidate must rerun the
complete event tests after incorporating the independently reviewed cost fix;
older or excluded green subsets cannot establish that result.

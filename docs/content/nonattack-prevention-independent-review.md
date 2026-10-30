# Nonattack prevention independent engine review

Reviewed `59973d61` against `185eaea9`, using the pack-delivery implementation
and proof guidance. This review owns tests and this evidence record. It changes
no engine source, ability row, admission rule or compatibility identity.

Current authority is vendored Rules Reference 1.8: `rr:damage.step.1` through
`.step.5`, `rr:damage.3.2`, `rr:prevent.1`, `rr:interrupt.1` through `.5`, and
`rr:ability.step.2`. The generated catalog confirms Defensive Stance is 08032,
Concussive Blast is 01154, and Energy Barrier is 05017. Stance is hero-only;
Barrier permits either form. Both modify damage taken, not damage dealt.

The new exact-recipient state is reconstructed by replay and is not serialized
as a live object. Forced replacement and Tough precede its optional window.
Remaining damage and recipient incarnation bound eligibility. The scheduler
retains separate parent and child execution ownership through suspension. Attack
damage has a separate resolution/scheduling path; this packet does not alter it.
Existing use/decline, wrong recipient/form, forced replacement, Tough/zero,
reentry, query/rejection and actual card controls remain relevant.

Added distinguishing controls:

- An outer synthetic prevention interrupt starts nested damage to the same
  identity. Actual Energy Barrier suspends the inner assignment at a target
  choice. The inner prevention, reflection and final-use discard complete, then
  the outer assignment retains its separate dealt/taken amounts.
- Two actual Barriers face one point of nonattack damage. Full prevention hides
  the unused copy, which remains available for the following damage instance.
- Four strict-JSON durable cases cover individual and simultaneous damage with
  actual Stance or Barrier. Each restarts a fresh host at pending prevention and
  at the Preparation receipt or Barrier target choice, and again after completion.
  They compare every continued event, digest, full RNG state/count and prompt;
  rejected input preserves the complete stored archive byte-for-byte. Restart
  also preserves the complete stored archive, including history and authority.
  Simultaneous placement remains held until prevention completion. These are
  prepared fixtures using the existing `IDurableGameFactory` seam, not public
  Black Widow admission or a complete played scenario.

Verification commands, all Release on Windows:

```powershell
dotnet test tests/Marvel.Server.Tests/Marvel.Server.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~NonattackPreventionReplayReviewTests
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Nonattack|FullyQualifiedName~BlackWidowPrevention|FullyQualifiedName~EnergyBarrier|FullyQualifiedName~DamageTrigger|FullyQualifiedName~CoreDamagePrevention|FullyQualifiedName~AttackScopedPrevention|FullyQualifiedName~RuleCitationTests'
```

Results: **4 server cases passed; 104 Content cases passed**. External logs are
`C:/Users/mggar/AppData/Local/Temp/nonattack-independent-replay.log` and
`nonattack-independent-content.log`. Initial server test development compared a
stale factory-created game after durable publication; the final helper observes
the current factory game. Those fixture failures establish no engine defect.
No new production branch is added, so this review does not claim a new mutant.
The implementation packet retains its separately recorded compiled mutant.
Normal commit hooks passed: 66 architecture-policy cases, cyclomatic complexity
and file length. Their external log is `nonattack-independent-hooks.log`.

Existing admitted Energy Barrier makes this a real replay behavior change:
historical direct damage could omit an optional decision that is now offered,
and accepting that decision changes damage, counters, discard and reflection.
Unchanged dataset hashes do not imply compatible histories. Integration requires
the lead's replay identity/admission boundary; this older source still identifies
as replay v26. No historical fixtures or archives are rewritten by this review.
`SessionReplay.RequireCompatibility` checks exact replay identity before
publication; a same-version review fixture is not evidence of predecessor
admission or integrated replay v27 behavior.

Separate follow-up: optional prevention during damage costs
--------------------------------------------------------

Actual Core Focused Rage (01027) with actual Energy Barrier (05017) exposes a
preexisting gap. Focused Rage's Action is offered, but resolving it returns to the
turn without offering Barrier during its one-damage cost. `DamageCostPlacement`
is unchanged by this packet and uses its own placement path.

This is not an exemption for costs: current `rr:initiating-abilities.1` permits
interrupts when any initiation step makes their condition true. `rr:cost.12` and
`rr:prevent.1.5` require all taking-damage cost to be taken; prevention leaves that
cost unpaid. The vendored FAQ entry for 01027 explicitly describes preventing
Focused Rage damage with Barrier and denies the draw when that cost is unpaid.
The rule and the historical ruling agree on this point.

The compiled red probe is preserved externally as
`C:/Users/mggar/AppData/Local/Temp/FocusedRagePreventionReviewProbe.cs`, with result
`nonattack-focused-rage-gap.log`. It asserts the actual Barrier offer before
damage placement/draw and fails at that offer assertion after the Focused Rage
Action succeeds. An initial incorrect fixture tried the Spider-Man face on
She-Hulk; the final fixture explicitly selects 01019a and fails on the intended
missing offer. The red probe is not shipped as a skipped or inverted test.

Plane-ready follow-up: **Offer optional damage-taking interrupts while paying
damage costs.** Reproduce with admitted 01027/05017; retain decline/payment and
prevention/unpaid/no-draw controls, simultaneous sibling-cost atomicity, suspended
Barrier target choice and durable replay. This follow-up is distinct from this
packet's individual/simultaneous damage effect scope.

Verdict: the reviewed individual/simultaneous nonattack mechanism withstands this
focused engine and durable replay review. Integration remains conditional on the
lead's replay boundary and normal integrated gates. This is not acceptance of the
separate damage-cost gap, public pack admission, native interaction, full-game
coverage or cross-platform CI.

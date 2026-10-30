# Prepared Kang phase-transition review

Reviewed revision: `cea9db84c68809d1e46c2c4ac11f30e8358d0675`.
This is an independent engine prerequisite review, not Kang admission or a
played scenario. Production sources and the ability book are unchanged.

Original verdict: changes required. The exact phase-frame continuation passes its existing
four combinations of first player and phase boundary. Two reachable unsupported
conditions do not fail closed.

## Findings

1. `KangJoinProcedure.RequireOwner` checks only that every captured play area
   remains in its source. A play area added to that source after `CaptureJoin`
   passes this check. The final loop moves only the captured members, leaving
   the additional member behind. Either reject any membership change by exact
   set comparison or implement an explicitly authorized updated-group policy.
   The review probe uses public prepared topology and capture APIs and expects
   the stated unsupported group change to raise.
2. `PhaseTransitionProcedure.Apply` calls `PhaseEnd` before `KangTransitions.Begin`.
   A synchronous delayed discard of one of two conflicting unique copies runs
   first, removing the required unique-copy choice. The evidence document
   explicitly leaves competing delayed-effect ordering unaccepted, but no
   executable restriction enforces that limit. Reject competing delayed work
   before it resolves, or implement and test its authority-derived ordering.
   The probe registers `DiscardFromPlay` against the source unique copy for
   `WhenPlayerPhaseEnds`; no printed Kang hook is required to reach the path.

The second finding does not say which ordering the rules ultimately require.
It establishes that the prepared API silently picks one in a case expressly
outside the accepted policy. The independent harmless delayed discard in the
existing tests cannot distinguish this behavior.

## Evidence

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~KangPhaseBoundaryReviewTests|FullyQualifiedName~KangPhaseTransitionTests'
```

Windows result: 8 executed, 6 passed, 2 failed. Both review failures report
`Assert.Throws() Failure: No exception was thrown`. The simultaneous-departure
and last-local-group controls passed, as did the existing four phase tests.
The raw log is `C:/Users/mggar/Source/kang-phase-independent-review.log`.
An initial fixture compilation failed because it referenced nonexistent
`Seat.Player` and omitted `static` on a helper; both fixture errors were
corrected before the reported run.

Static review confirms the child uses `ContinueBeforeOwner` at `Stage.Apply`
with `Plan = true`, retains the exact occurrence and original owner, and uses
identity-based answer advancement. This avoids repeating the phase body or
opening child response windows while unique choices repeat. The existing test
also distinguishes an identical later phase frame and retains the original
defeated incarnation after physical reentry. No runtime response observer,
capture-query purity control, public replay, Linux, native, or full managed
suite was added or claimed by this review.

The new failing probes are review evidence and must pass after correction
before this review packet is integrated. No mutation result is claimed here.

## Repair closure

Independently reviewed repair
`56911ea6c125ad256e14953921d7cf2bf2fa0cc8`, applied to this review tree as
`d48715aa`. Verdict: both findings closed for the prepared capability.

Complete source membership is checked before the phase body can expire effects
or execute delayed work, and again before each prepared prompt and answer.
The due-work guard uses the same condition, activation and awaiting-activation
predicate as actual delayed consumption, without consuming registrations or
their remaining uses. The existing unmatched controls distinguish all three
parts of that predicate.

The positive phase fixture's move from a co-due discard to an unrelated
`AttackEnds` registration is accepted. Refusing every actually co-due effect is
a conservative executable scope boundary while competing ordering remains
unimplemented. Retaining the old discard-first expectation would continue to
assert an unaccepted policy. The updated fixture preserves the unrelated card
and registration and retains the same destination, repeated unique choices,
incarnation, owner, whole-group and exact-once assertions.

An additional independent three-case theory covers both phase boundaries and
the round-ending condition. It repeats refusal, checks cards, topology, RNG,
effects, pending departure and events, then resolves a two-use delayed effect
directly. The first actual occurrence retains the registration and the second
retires it. This proves refused work did not silently decrement remaining uses,
which a `Registered` snapshot alone cannot observe.

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~Kang|FullyQualifiedName~Uniqueness'
```

Independent Windows closure result: 55 passed, zero failures or skips. Log:
`C:/Users/mggar/Source/kang-phase-independent-closure.log`. The first compilation
of the additional test hit xUnit2031; its assertion was corrected to the
predicate overload before the reported run. No production changes or new
mutation, full managed, Linux, native, printed-hook or public-replay results
are claimed by this closure.

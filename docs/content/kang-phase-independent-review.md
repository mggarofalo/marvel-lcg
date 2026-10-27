# Prepared Kang phase-transition review

Reviewed revision: `cea9db84c68809d1e46c2c4ac11f30e8358d0675`.
This is an independent engine prerequisite review, not Kang admission or a
played scenario. Production sources and the ability book are unchanged.

Verdict: changes required. The exact phase-frame continuation passes its existing
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

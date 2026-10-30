# Named-form choice projection repair

This repair starts at independent review commit `234c42e5d4589339394e7afdd18dd91358efa086`. It addresses the synthetic reachable-branch regression in [the independent review](ant-man-resize-independent-review.md). A concrete gameplay failure from that finding has not been established, and this packet does not admit Ant-Man content.

Area projection traces each alternative of a plain `choose` containing named form changes on a cloned board. Following face conditions therefore use the face and mutations belonging to that same alternative. No-op alternatives remain reachable, repeated choices propagate every distinct board, and the existing face-inclusive key merges equivalent outcomes. Other unresolved choices retain their existing handling. Explicit destination choices remain unsupported; an unresolved card choice containing a named form change also raises rather than treating the live face as its projected result. Existing actor-rebinding guards remain in force.

The original independent regression and non-first-player Resize gameplay probe are preserved. Additional synthetic tests require both exact-face/trait-correlated branches, repeated-choice behavior, unchanged live identity, and explicit refusal of an unresolved card choice. The direct face test retains explicit-destination rejection and now checks the named-form destination plus unchanged input and live faces.

Verification used Windows Release builds, `MSBUILDDISABLENODEREUSE=1` and `-m:1`. External logs are under `C:/Users/mggar/Source/evidence/`:

- `resize-choice-repair-build.log`: fresh nonincremental Content build passed without warnings.
- `resize-choice-repair-affected.log`: initial Form/Projection/Resize/Choice run passed 1,123/1,125. One failure is the independently confirmed historical `CoreCorrectionChoiceTests` continuation fixture; the other was the named-choice unchanged-object assertion replaced by exact destination and input/live purity assertions.
- `resize-choice-repair-mutant-build.log` and `resize-choice-repair-mutant.log`: the mutant adds `&& repetitions < 0` to the named-form choice tracing condition. It compiled, and all four focused regression, correlation and refusal checks failed.
- `resize-choice-repair-restored-build.log`: the correction was restored and rebuilt nonincrementally without warnings.
- `resize-choice-repair-restored-affected.log`: the same affected keyword filter, excluding only the known historical `CoreCorrectionChoiceTests` class, passed 1,109/1,109. This includes all Resize classes and their independent multiplayer gameplay probe.
- `resize-choice-repair-identity-face.log`: the complete `ProjectedIdentityFaceTests` class passed 9/9, covering methods omitted by the keyword filter.

The integration owner must run the full Content suite after the separate historical fixture repairs are integrated. This record does not replace the independent review verdict, establish broader gameplay or native UI acceptance, or claim a full-pack journey.

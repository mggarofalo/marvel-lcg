# Doctor Strange native witness dependency review

Reviewed base: `fdd16b0f9f3f7c72631ed1d2033db2f6d06ea386`.
PR 454, CI run 40439351007, Linux platform job 121975713061 failed
`PresentationBoundaryTests.GodotUsesOnlyReviewedDependencies` at index 129:
expected `Marvel.View.CardPersistentAbilityDescriptor`, actual
`Marvel.View.CardLocationDescriptor`. The local Release test reproduces that
exact failure before the policy change.

## Ownership decision

Admit exactly `Marvel.View.InvocationDeckDescriptor` and
`Marvel.View.CardLocationDescriptor` for the existing read-only
`NativeGameWitness` in `src/Marvel.Godot/smoke/NativeGameWitness.cs`.
This is a repository architecture choice, justified by the decision each read
owns under [presentation-layer.md](../presentation-layer.md) and the ownership
review requirement in [architecture-invariants.md](../architecture-invariants.md).

`WorldProjection.For` publishes `InvocationDeckDescriptor` as public seat and
pile identities. `PileMatches` reads its typed `Owner`, `Deck` and `Discard`,
then checks that the corresponding authorized areas retain the expected owner.
This establishes that the native test observes the correct physical pile through
the same response the table displays. It does not calculate control, legality,
setup, pile order or an effect.

`CardDescriptorProjection.Filter` removes `CardDescriptor.Location` for concealed
cards. `PublicTopMatches` reads only whether that supplied value is null,
together with anonymous identity, visible face and count assertions. Its
compiled reference to `CardLocationDescriptor` comes from the typed property;
it does not inspect the descriptor's location fields or recover hidden state.
Keeping this assertion checks that the native evidence route retains View's
concealment boundary.

View therefore owns the authorization and facts; the Godot witness owns observing
the displayed response for native acceptance evidence. Both existing typed-owner
and hidden-location assertions remain intact. The exact assembly/type policy
remains an equality check: no wildcard or namespace allowance is added, and no
production source, protocol, schema or gameplay behavior changes.

## Verification

Evidence is outside the worktree at `C:/Users/mggar/Source/evidence/`:

- `strange-witness-boundary-before.log` and
  `strange-witness-boundary/boundary-before.trx`: the unmodified policy fails
  with the same index and type mismatch as Linux CI.
- `strange-witness-boundary-after.log` and
  `strange-witness-boundary/boundary-after.trx`: the entire
  `PresentationBoundaryTests` class passes (1 test). Its compiled metadata
  equality establishes that these two additions exactly match the emitted
  dependency set.
- `strange-witness-architecture.log` and
  `strange-witness-boundary/presentation-architecture.trx`: all 7 tests in
  `PresentationAssemblyPolicyTests`, `PresentationSourcePolicyTests` and
  `PresentationSourceCompilationTests` pass, including the existing rejection
  controls for unreviewed dependencies and source access.
- `strange-witness-presentation-wall.log`: allowed contracts compile and every
  forbidden presentation dependency/configuration probe is rejected.
- `strange-witness-godot-wall.log`: Godot dependency, opt-out and framework
  controls exercise the existing runtime/build boundary.

Checks use Release on Windows; Linux CI remains required. This dependency review
does not establish native journey success or product-owner acceptance.

## Independent integration review

The delivery lead inspected the compiled dependency addition against the actual
`NativeGameWitness`, `CardDescriptorProjection.Filter` and `WorldProjection.For`
reads. The witness observes View-authorized ownership and absence of concealed
location; it does not decide gameplay or visibility. The two exact type additions
are accepted on that ownership basis. The independently rebuilt complete boundary
class passes (1 test), recorded in
`Source/evidence/strange-witness-root-review.log`. Runtime and native journey
sources are unchanged, so the inspected UI media remains applicable.

The completed first CI run failed the same exact dependency pin on Windows and
Linux. Both platforms passed 645 Server tests, 129 architecture tests and 36
release tests; Godot passed 980 with this one failure and seven environment-gated
skips. Managed correctness, all four native journey shards and the Windows
desktop package passed. The fresh PR-head run must establish combined green CI.

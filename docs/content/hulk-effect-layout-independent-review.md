# Independent review: checked pricing vocabulary layout

Reviewed `7aa42043a17f1435df1b9d2fcab0ba493b1e3082` against candidate tree
`eb14ac4db7053ffd351d220903c62761503c0741`. The review covers only that
isolated difference, identified in [the request](hulk-effect-layout-review-request.md).
It does not accept the combined candidate's other content or runtime changes.

Verdict: accepted for source-layout integration. No blocking findings or policy
waiver. This is structural acceptance, not new card or product admission.

## Design and compatibility findings

The six sealed records form a coherent price-modifier vocabulary. Their operands,
init properties, defaults and documentation are preserved. Resource generation
and payment remain with their existing concepts. This removes the large-class
finding by separating a named responsibility: `AbilityEffect` is 285 declaration
lines and `AbilityPricingEffect` is 23. No baseline or enforcement policy changes.

The parameterless `AbilityEffect` constructor changes from private to private
protected. A derived peer in this assembly can now invoke it; an external derived
type cannot invoke that parameterless constructor. The pricing family's own
parameterless constructor is private. The request correctly acknowledges the
protected synthesized record-copy constructor: neither hierarchy is an absolute
compiler-enforced closed union. This is a proportionate, explicit assembly-owned
extension boundary, not a claim that all external derivation is impossible.

All 17 changed consumer files were compared programmatically with the candidate:
each is byte-for-byte equivalent after the six intended qualification
substitutions, with line endings normalized. Concrete-leaf dispatch, exact-type
sets, validation and unknown-operation/unknown-variant fallbacks are preserved.
No consumer dispatches generally on the new abstract family. Repository search
found no remaining old qualified references to the moved leaves.

The six CLR qualified type names deliberately change. The DSL input operation
strings and `AbilityOperationNames` strings are unchanged. `AbilityEffectAddress`
uses explicit DSL field paths and indexes; no CLR-name serialization registration,
save field, protocol field or digest field changes. Searches of Cards, Protocol
and Server identified no serialization of this vocabulary by CLR qualified names.
Existing checked instruction operands remain inert immutable values. The move
adds no gameplay decision, so existing semantic controls are appropriate; no
implementation-mirroring test was added solely for relocation.

## Verification

Independent Release runs in `marvel-hulk-pricing-layout-review`:

- Architecture: 129 passed, zero failures/skips, with
  `MARVEL_ARCHITECTURE_BASE=47dd798491a2a73b07c979c2385c6e126aea633f`.
- Content filter `FirstAllyCost|SelfPlayCost|UpgradeHostCost|AbilityEffectLowering|Helicarrier|AvengersTower|Obligation`:
  164 passed, zero failures/skips. This covers constant and candidate pricing,
  rejection controls and affected lowering.
- Content filter `PhysicalToll|RecipientCost|CoreCardDsl`: 29 passed, zero
  failures/skips. This includes the next-event increase and future-recipient
  reductions, with ordinary Core DSL controls.
- `git diff --check` passed for the implementation difference.

Each filter term above means `FullyQualifiedName~term`, joined with `|`.
Logs are outside the repository under `C:/Users/mggar/Source/evidence/`:
`hulk-pricing-layout-review-architecture.log`,
`hulk-pricing-layout-review-content.log`, and
`hulk-pricing-layout-review-future-prices.log`.

The implementation's supplied logs were also inspected: complete Release
solution build with zero warnings/errors; complete Content suite 7,670 passed,
zero failures/skips; architecture 129 passed. These are source-worktree results,
separate from the independent focused runs above. They support the required
complete assembly regression without claiming that this reviewer reran it.

The review commit runs the normal pre-commit hooks; its external log is
`hulk-pricing-layout-review-commit.log`. No engine, ability-book, setup, policy,
architecture baseline or test source is changed by this review.

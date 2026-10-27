# Hulk effect source-layout review request

Compare the layout remedy against candidate tree
`eb14ac4db7053ffd351d220903c62761503c0741`, based on
`47dd798491a2a73b07c979c2385c6e126aea633f`. The branch contains the supplied
candidate so normal build and commit hooks inspect the integrated source;
only the diff from that candidate tree is this review's implementation scope.

## Design and construction boundary

`AbilityPricingEffect` owns the six checked instructions that modify a card's
price: `ReduceFirstAllyCost`, `ReduceUpgradeCostOnThis`,
`ReduceSelfCostPerEngagedMinion`, `ReduceSelfCostPerControlledTrait`,
`IncreaseNextEventCost`, and `ReduceNextCardCost`. Each remains a sealed nested
record with the same operands, properties, defaults and documentation. The
family's parameterless constructor is private. Resource generation and effect
payment remain separate concepts and stay with their existing vocabulary.

The owning assembly chooses the family boundary. `AbilityEffect` has a
`private protected` parameterless constructor so the peer pricing family can
derive from it. This broadens that constructor's access from the containing
type to derived types in the same assembly. It does not make the parameterless
constructor available to an external derived type. Neither the old nor new
record hierarchy provides an absolute compiler-enforced closed union: abstract
records also have a protected synthesized copy constructor. The remedy does
not claim to seal that pre-existing extension route. Review this explicit
tradeoff against the benefit of a coherent price vocabulary and the documented
source-layout rule.

The consumers continue to enumerate concrete sealed leaf types. No switch now
accepts the abstract family, and unknown-operation lowering and unknown-variant
fallbacks retain their existing behavior. There is no new DSL instruction,
runtime decision, serializer registration, save field, protocol field, digest
field or mutable state. `AbilityOperationNames` retains all six existing string
spellings; effect addresses continue to use DSL paths rather than CLR names.
The CLR qualified names of the six moved variants intentionally change.

## Exact ownership

The remedy owns `src/Marvel.Cards/Dsl/AbilityEffect.cs`, the new
`src/Marvel.Cards/Dsl/AbilityPricingEffect.cs`, and the following consumers,
whose changes are only the six type-qualification substitutions:

- `src/Marvel.Cards/Dsl/AbilityAllyCostLowering.cs`
- `src/Marvel.Cards/Dsl/AbilityEffectLowering.cs`
- `src/Marvel.Cards/Dsl/AbilityProcedureLowering.cs`
- `src/Marvel.Cards/Dsl/AbilitySelfPlayCostLowering.cs`
- `src/Marvel.Cards/Run/AbilityAdmissionAreaDependencies.cs`
- `src/Marvel.Cards/Run/AbilityAllyPlayHistory.cs`
- `src/Marvel.Cards/Run/AbilityConstantQueries.cs`
- `src/Marvel.Cards/Run/AbilityCostReductionDescription.cs`
- `src/Marvel.Cards/Run/AbilityEffectBindingAnalysis.cs`
- `src/Marvel.Cards/Run/AbilityEffectDescription.cs`
- `src/Marvel.Cards/Run/AbilityEffectStructure.cs`
- `src/Marvel.Cards/Run/AbilityImmediateExecution.cs`
- `src/Marvel.Cards/Run/AbilityOperationNames.cs`
- `src/Marvel.Cards/Run/AbilityPlayerBindingAnalysis.cs`
- `src/Marvel.Cards/Run/AbilityPlayerChoiceDescription.cs`
- `src/Marvel.Cards/Run/AbilityRecipientCommitment.cs`
- `src/Marvel.Cards/Run/AbilitySelfPlayCost.cs`

This review request is the only evidence document added by the remedy.
There are no dataset, setup, public admission, architecture baseline or policy
changes. Toe-to-Toe admission and receipt work are outside this ownership.

## Evidence and focused review

The complete Release solution build passed with zero warnings and errors.
All 129 architecture tests passed with zero failures or skips, with
`MARVEL_ARCHITECTURE_BASE=47dd798491a2a73b07c979c2385c6e126aea633f`.
The `AbilityEffect` declaration is 285 lines and `AbilityPricingEffect` is
23 lines; the source-layout guard accepts both without changing its baseline.
The complete Content suite passed: 7,670 tests, zero failures or skips,
in 8 minutes 29 seconds, using the same Release solution binaries.
An exact comparison against the candidate verified that all 17 consumer files
contain only the six intended type-qualification substitutions, including their
existing operation strings. Existing semantic controls are appropriate because
this move introduces no gameplay decision.

External evidence is under `C:/Users/mggar/Source/evidence/`:
`hulk-effect-layout-build.log`, `hulk-effect-layout-architecture.log`,
`hulk-effect-layout-content.log`, and `hulk-effect-layout-commit.log`.
The final handoff records the tested commit, final counts and clean status.

The separate reviewer should inspect the constructor-access tradeoff, family
cohesion, preservation of operation spellings and leaf-only dispatch, the
architecture result with no waiver, and the affected pricing/content behavior.
This is source-layout acceptance, not new card or product admission.

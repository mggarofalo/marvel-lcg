# Effective card values

`CardValues` owns numeric evaluation. `StateFields.Modified`, registered printed
field filling, and maximum HP consumers use its result. Each evaluation contains
the base, its kind, the current quantity, and the ordered operations that produced
that quantity. Printed attachment modifiers precede continuous effects in the
engine's deterministic traversal order. A dash, a lost characteristic, and the
minimum-zero rule are distinct operations, not invented additive adjustments.

An effective profile supplies the base instead of the physical card's printed
characteristics. A facedown Drone therefore has a public Drone base; neither its
underlying face nor its printed HP enters the explanation. Resolved DSL amounts
remain owned by the DSL evaluator: Iron Man's contribution already includes the
Tech count and cap when the quantity evaluator receives it.

Maximum HP and remaining HP are different quantities. `MaximumHealth` includes
active HP contributions. `RemainingHealth` subtracts existing damage without
healing it. The digest's registered-field format stays unchanged; View obtains
remaining HP from the engine for every character kind.

## Projection contract

`CardFaceDescriptor.EffectiveValues` is keyed by printed stat attribute:
`ATK`, `THW`, `DEF`, `REC`, `SCH`, `HS`, and `HP`. It exists for in-play stats.
Each value carries `BaseValue`, `CurrentValue`, `BaseKind`, `IsModified`, and
authorized `Calculation` rows. `HP.CurrentValue` is maximum HP. `Damage` is the
damage on the copy, and `Fields.health` is its remaining HP.

The main face can show `CurrentValue` and mark `IsModified`. Printed facts stay
in their separate contract. Source inspection can show each authorized operation,
its resolved amount, named source, printed source text, and stated duration.
`BaseKind` distinguishes printed characteristics from a replacement identity.
`BaseValue` is the evaluator's numeric base, not a replacement for the printed
label. Variable ink such as Titania's X must come from `PrintedValues`; her
normalized numeric base is zero and her DSL supplies the resolved quantity.
Source text preserves that definition. Consumers must not present a numeric zero
as her printed X or turn that definition into a printed addition equation.

`Calculation` is a source explanation, **not a complete arithmetic equation**.
Private or unavailable sources have no rows, placeholders, counts, original
indices, intermediate totals, or condition strings. A visible source's amount
does not include hidden contributions. Consumers must not sum the disclosed rows
or infer undisclosed values from them. Unsourced minimum-zero operations stay in
the engine trace; public printed dashes may disclose `Unmodifiable`.

## Source identity and disclosure

Registration captures an immutable `CardSourceSnapshot` beside the effect. It
records object ID, incarnation, effective face/title, and exposure facts. Derived
constants and printed attachment modifiers capture their current source. The
snapshot does not alter `ContinuousEffect` equality, registration matching, RNG,
timing, or the state digest.

View authorizes the originating exposure before disclosing the snapshot. A
private hand origin is available only to an authorized seat. A public replacement
identity exposes only that identity. An authorized historical source can retain
its name and printed text, but has no live `CardId` when its incarnation or face
no longer matches, or when the source is out of play. Out-of-play events and boost
cards can move without starting an in-play incarnation; they therefore remain
historical records even if the same physical card is readable in a later hand.
They cannot resurrect a target link to a reused physical card.

The supported Core numeric constants depend on public in-play quantities and
characteristics. An expansion introducing a private-dependent contribution needs
dependency disclosure support before its explanation can be admitted. Source
visibility alone is not authority to disclose arbitrary private predicates.

## Replay and remaining scope

Session saves contain setup and accepted decisions, not serialized continuous
effects or projections. Registration reconstructs source snapshots during replay.
No save member, schema number, RNG contract, or state-digest spelling changes for
this metadata. The ledger restart test verifies reconstruction and an unchanged
serialized save using a deterministic lasting-effect fixture.

This is the current-value/source slice of MARVEL-396. It does not establish the
broader MARVEL-445 authority browser, historical rule/ruling navigation, or
provenance for the ability that originally assigned a replacement profile.
MARVEL-464 owns attachment relationships and persistent-effect summaries beyond
numeric contributions. Passing these engine and projection checks is not native
product acceptance.

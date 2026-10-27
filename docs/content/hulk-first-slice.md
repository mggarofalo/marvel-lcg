# Hulk first executable slice — MARVEL-843

Nonpublic actual-card packet based on `fdd16b0f9f3f7c72631ed1d2033db2f6d06ea386`.
The authored book grows from 685 to 688 faces with `10001a` Hulk, `10001b`
Bruce Banner and `10004` Sub-Orbital Leap. All 685 predecessor raw JSON object
spellings are byte-identical to the base. No setup, recipe, public admission,
runtime mechanism or product scope changes are included. Doctor Strange remains
the integration prerequisite; this branch must be rebased/reviewed after its merge.

Printed authority is `datasets/cards/cards.json` for the three exact faces.
Enraged uses the existing `discardHandCards` with the current hand count, requiring
the entire group and no optional decline; zero cards completes without a prompt.
The mandatory selection resolves before seat advance. Tests cover empty, two-card
and oversized hands, Banner form, non-first seat, optional interrupts/responses
that draw afterward, and both first-player orderings of simultaneous forced
interrupts. The later drawn card remains only when its draw follows Enraged.
Deciding clauses are RR 1.8 `rr:forced.1`, `.4`, `.5`, `.6` and `rr:interrupt`.

Banner draws before the selected discard; the newly drawn card is selectable.
The once-per-round limit survives a form round trip. Its printed text has no
“then”; the two instructions use an ordinary sequence.

Leap reads settled payment declarations. Physical plus wild declared physical
removes five threat; mixed mental/energy or declared wild removes three.
Doubled wild icons retain separate declarations; an unused generated wild icon
is not paid (`rr:cost.4.1`), and does not spoil an otherwise physical payment.
RR 1.8 `rr:wild-resource.1` and `.1.1` decide wild declaration behavior.
The zero-adjusted-price case removes three. Local ruling
`ruling:5bbba476a198a90c` directly addresses **Unstoppable Force**: “you did not
pay for it using a physical resource” and “do not draw 1 card”. Applying its
interpretation of the same printed “using only physical resources” phrase to
Leap is an explicit inference. RR 1.8 cost/wild clauses do not contradict it.
The predicate requires a paid physical resource and excludes each other declared
type; it does not inspect all generated icons.

## Evidence

Release Content-project nonincremental build: passed, zero warnings/errors.
Focused selection `FullyQualifiedName~HulkIdentityCardTests|FullyQualifiedName~HulkLeapCardTests|FullyQualifiedName~AbilityData`:
52 passed. `AbilityData` selects the whole family, including exact authored faces.
Logs are under `%TEMP%/hulk-*.log` on the implementation machine.

Six executed authored-data mutants fail the intended behavioral assertions:
ForcedResponse Enraged (optional-interrupt draw), global turn-end subject
(nonowner turn), fixed four-card discard (oversized hand), unlimited Banner
(second offer), physical-includes-only Leap (mixed payment), and empty payment
qualifying for Leap (zero-price control). The response-only timing probe initially
survived because forced responses also precede optional responses; adding the
optional-interrupt control kills that mutant. Mutants run against the compiled
engine and actual modified data, restoring the original data in `finally`.

The full Content run completed with 7,495 passes and one failure out of 7,496
(6m04s). Its sole failure was the predecessor-specific final Strange nemesis
packet checking an unbounded `Skip(682)` suffix. Both that exact ordered-ID
assertion and its no-starting-counters assertion now use `Skip(682).Take(3)`,
preserving the same three predecessor faces. The full failing log is retained
as `%TEMP%/hulk-full-content.log`. After this mechanical correction, the whole
affected inventory class and the 52 focused cases are rerun together. The final
result and normal-hook commit are reported in the packet handoff. A second full
sweep is deferred to combined integration; no final full-suite green or
public/native/whole-game acceptance is claimed.

## Mechanical inventory maintenance

The existing 16 full-current-book count changes are solely `685` to `688` in:
- `tests/Marvel.Content.Tests/Play/BlackWidowBurnNoticeTests.cs`
- `tests/Marvel.Content.Tests/Play/BlackWidowFinalResponseCardTests.cs`
- `tests/Marvel.Content.Tests/Play/BlackWidowPreparationCardTests.cs`
- `tests/Marvel.Content.Tests/Play/BlackWidowPreventionInventoryTests.cs`
- `tests/Marvel.Content.Tests/Play/DoctorStrangeConstantCardTests.cs`
- `tests/Marvel.Content.Tests/Play/DoctorStrangeFinalNemesisInventoryTests.cs`
- `tests/Marvel.Content.Tests/Play/DoctorStrangeFinalSignatureEventTests.cs`
- `tests/Marvel.Content.Tests/Play/DoctorStrangeFinalSupportInventoryTests.cs`
- `tests/Marvel.Content.Tests/Play/DoctorStrangeInvocationCardTests.cs`
- `tests/Marvel.Content.Tests/Play/DoctorStrangeIronFistCardTests.cs`
- `tests/Marvel.Content.Tests/Play/DoctorStrangeLastSupportInventoryTests.cs`
- `tests/Marvel.Content.Tests/Play/DoctorStrangeNextFourCardTests.cs`
- `tests/Marvel.Content.Tests/Play/DoctorStrangeResponseEventTests.cs`
- `tests/Marvel.Content.Tests/Play/DoctorStrangeSearchCardTests.cs`
- `tests/Marvel.Content.Tests/Play/DoctorStrangeSupportReprintTests.cs`
- `tests/Marvel.Content.Tests/Play/DoctorStrangeUnflappableTests.cs`

`AbilityDataTests.AnUnauthoredCardSaysSoRatherThanDoingNothing2.cs` additionally
admits exactly these three authored faces to its explicit inventory. Historical
prefix positions and raw predecessor objects remain unchanged. Unsupported-card
negative controls and public roster/setup gates remain unchanged.

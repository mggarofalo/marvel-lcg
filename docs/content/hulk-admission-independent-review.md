# Hulk admission and support-threat independent review

Reviewed candidate `48797dd10529cf6a263e2681c2bc445d5f672439`, including
View change `4d5357905d9c1c22f98d594b53f17190c885259b`, against predecessor
`10af2709`. Scope is complete public admission and the narrow support-threat
formatter change; this is not full-pack, native or platform acceptance.

## Authority and implementation review

The pinned offline FFG divider `datasets/rules-packs/mc09/hulk-deck-divider.jpg`
was independently inspected. Its named copies match the appended setup record:
15 signature, 17 Aggression and 8 Basic cards, with identity separate; one Inner
Demons and Abomination/Total Destruction/three Clash Of The Titans outside the
player deck. Extras 10029–10032 are absent from the starter. The source-qualified
authority record resolves those local card numbers to the generated catalog.

Admission checks all 33 exact face identities, pack membership, authored coverage
and the complete required recipe. No partial product whitelist is introduced.
The setup diff only changes the hero count and appends Hulk; predecessor recipes
retain their content and order. Public factory/session tests cover supported
Standard/Expert setups, one through four seats, custom deck seat ownership,
extra-card and singleton limits, and both catalog transports. Their source
evidence remains in `hulk-public-admission.md`; those source runs are not claimed
as fresh independent runs here.

## Independent adverse cases

`SupportThreatProjectionReviewTests` exercises actual engine-to-View projection
for Beat Cop with absent, zero and three threat. A synthetic disagreement between
raw field 9 and typed live count 3 requires exactly the typed count. A concealed
support's synthetic threat remains absent from descriptors and board fields
under cooperative and no-seat visibility scopes.

Real Hero/Villain/Minion projections retain registered raw `k_threat: 0` while
typed `State.Threat` is null, and display no threat field. An initial probe
incorrectly assumed registration populated `Card.Tokens`; it failed on the
expected typed zero versus actual null before reaching presentation. Inspection
confirmed the distinction, and the corrected tests assert both facts. That
discarded probe is not a production defect or mutation verdict.

`HulkAdmissionReviewTests` adds an unexpected authored face to the synthetic Hulk
catalog. This must close every Hulk face while preserving the complete
predecessor playable set and all remaining setup inventories, challenging a
gate that accepts a superset of the reviewed face inventory.

Independent logs are outside the repository under
`C:/Users/mggar/Source/evidence/`. The corrected View run
`hulk-admission-review-view.log` passed 29 cases (nine new review cases plus the
existing threat/live-field classes).
The focused admission run `hulk-admission-review-content.log` passed all 111
cases, including the new unexpected-face case and all existing Hulk recipe,
missing-face and membership admission cases. No production mutation was made
during this independent review; source mutation evidence remains separately
attributed in the implementation packet.

## Scope verdict

No gameplay or View production defect was found within this review. The lead's
combined unit run identified omitted Hulk setup-authority adjudication and
enumeration maintenance; the lead owns that correction and its verification.
Admission review remains contingent on closing that known integration failure.
Rendered Beat Cop interaction and final combined/platform verification remain
separate delivery evidence.

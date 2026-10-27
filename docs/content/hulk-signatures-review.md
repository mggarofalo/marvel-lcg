# Independent review of seven Hulk signature faces

Reviewed source: `86412c8344cb8aff60629b5c3b216138ae630b65`, based on
`fc7a4a0aecb3a93362e23f61e3e13fa51c48ebe8`. Review branch:
`review/hulk-signatures`. This accepts only 10002, 10003 and 10006–10010 as
nonpublic research content. No engine or ability-book correction was needed.
Thunderclap 10005 remains unauthored; this is not acceptance of the full
eight-signature packet, MARVEL-846, public Hulk admission or the complete pack.

## Authority review

I read the seven joined records in `datasets/cards/cards.json`, their actual
ability rows, the Hulk implementation handoff and applicable RR 1.8 clauses.
All seven IDs occur in the vendored MarvelCDB FAQ query inventory and have no
individual FAQ entry. Hall of Heroes rulings `5bbba476a198a90c` and
`8bde2daf20730c27` directly discuss Crushing Blow's adjusted price; the former
also excludes Unstoppable Force's draw at zero price. Both are marked pre-1.5.
Their payment conclusions agree with current `rr:initiating-abilities.step.4`,
`rr:cost.4.1` and `rr:wild-resource.1`; no superseded overkill interpretation
was used. Applying Force's nonempty-payment interpretation to Hulk Smash's
identical condition remains an explicit inference.

| Face | Reviewed interpretation and distinguishing evidence |
|---|---|
| 10002 Crushing Blow | Every resource spent toward the adjusted printed price must be physical, including cost increases; declared wild counts as its declaration. Damage reads the playing hero's modified ATK. Actual-row tests cover a Rage modifier, zero price, rejected nonphysical declarations, and an increased price with a rejected mixed answer followed by a valid retry. |
| 10003 Hulk Smash | The exact basic attack receives +10 and conditional overkill. Mixed, declared-wild and zero-payment controls distinguish the conditional grant. Actual subsequent attacks prove neither bonus nor keyword remains. An additional Tough control proves no spill without defeat and no carryover after prevention. Current `rr:overkill.1` requires actual defeat. |
| 10006 Unstoppable Force | Ready the named Hulk, draw for the controller only with nonempty exclusively physical settled payment. No printed “then” makes draw dependent on ready success. Cross-controller, already-ready and zero-price controls are retained; review adds mixed physical/energy and physical/mental declarations and an unspent doubled-wild icon. |
| 10007 Limitless Strength | Printed RRR remain catalog facts; hand-source eligibility requires hero form. Actual payment/rejection and the earlier complete hand-resource form classes cover eligibility, commit and form changes. No Resource ability is invented. |
| 10008 Banner's Laboratory | Named Bruce Banner alone gains +2 REC (`rr:identity.2`). The controller's alter-ego resource ability generates mental and exhausts the actual support. Cross-controller, form and actual payment controls are retained. |
| 10009 Boundless Rage | Printed hero-only play is checked with a same-price valid control. Hulk alone gains +1 ATK; the source controller's form change forces discard of that physical upgrade, including after leaving hero form. Another player's form change does not discard it. |
| 10010 Immovable Object | The controller's identity gains +4 HP in either form; only faceup named Hulk gains retaliate 1. Cross-controller, both-form and source-departure controls are retained. |

The named-identity interpretation follows `rr:identity.2`; controller identity
references follow `rr:you-your`. Form-only play follows
`rr:form-change-form.7`, rather than suppressing an already-triggered Rage
response when its controller becomes alter-ego.

## Independent inventory and verification

A raw JSON decoder comparison against the source base confirmed **all 693
predecessor objects byte-identical and in order**. The book has **700 rows**,
exactly **15 of 33 Hulk faces**, and precisely the seven claimed appended IDs.
There are no new starting counters or setup/admission changes. The book SHA-256
remains `a94ffc655319c353c5d60c80bd59c7539b106925a381b4b778856cb547743ec4`.
The reprint inventory still checks its exact five-row slice. Synthetic
prerequisite fixtures remain explicit replacements rather than duplicate actual
book rows; actual signature tests use the canonical book.

The independent baseline passed **226 tests**, expanding the source filter to
whole BasicUseWeapon, MsMarvelPayment and VariableEventPayment classes as well
as Hulk, AbilityData, PhysicalPrintedPrice, AttackEventResource,
BasicAttackOverkill, HandResource and RuleCitation classes. The six added cases
are in `HulkSignatureReviewTests`; no production row is overridden there.

Two isolated data mutants were checked with successful builds. Removing Crushing
Blow's physical-price restriction fails the increased-price mixed-payment
rejection. Removing Force's energy exclusion initially survived the singleton
energy-payment case because the separate physical-inclusion predicate still
rejected it. Adding the mixed `RY` declaration kills that mutant with an
incorrect draw. The initial survivor is not counted as a kill. The ability book
was restored byte-for-byte before the non-incremental restored Release build
(zero warnings/errors) and final affected-class run: **232 passed, zero failed**.

Initial review-fixture failures were corrected: the surcharge obligation is an
encounter card and returns to encounter discard, and a surviving Guard minion
prevents the next villain attack. The final Tough scene uses Weapons Runner,
then attacks that surviving minion and the villain to observe keyword and bonus
expiry separately. Neither failure indicated a production defect.

Logs are outside the repository under
`C:/Users/mggar/Source/hulk-signatures-review-*.log`: `baseline`,
`adversarial`, `adversarial-restored`, `mutant-crushing-price`,
`mutant-force-energy`, `mutant-force-energy-strengthened`, `restored-build`
and `restored-tests`. Final restored results and commit-hook evidence are
recorded with the review handoff. This review does not claim a full Content,
whole-game, native, public-admission or cross-platform certification run.

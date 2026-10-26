# MARVEL-725 bounded retrieval prerequisite

Base: `377f261ec7377a68ab367b1383c12fc72875366b`. Engine-only packet; no public
Ms. Marvel admission or ability-book row is included.

Authority: generated `datasets/cards/cards.json`, physical face `05007`, prints
"Action: Exhaust Bruno Carrelli → add up to 3 cards attached here to your hand."
Ruling `6768213a7e0f4606` in `datasets/rulings/rulings.json` says cards attached
to Bruno are out of play. `rr:in-play-and-out-of-play.10` likewise makes facedown
attached cards out of play. The printed destination is the resolving player's
hand; movement of already-out-of-play cards retains physical ownership. Ordinary
host departure still uses existing attached-card discard behavior.

`chooseCardsToHand` uses one existing TargetRequest with zero-to-maximum distinct
physical ids and validates the complete answer before movement. Empty acceptance
retains paid exhaustion; the ordinary suspended continuation resumes once.
Concealed candidates use the established IsSearch projection authorization path.
The DSL spelling and deterministic submitted-id movement order are engine choices.

Focused evidence: `BoundedCardRetrievalTests` and existing `BeneathCardsTests`.
The initial 16-case run passed. A compiled maximum-one mutant failed four of the
11 new tests; a non-incremental restored build and the same 16 cases passed.
External logs: `C:/Users/mggar/Source/bruno-mutant.log`,
`bruno-restored-build.log`, `bruno-restored.log`. Subsequent focused verification
adds empty-source initiation controls and non-first-player answering assertions;
all 18 focused cases passed (`bruno-final-focused.log`).
Normal commit hooks remain enabled. The packet also extracts special-choice
routing into its cohesive owner, keeping structural traversal below 300 lines.

Limits: no broad suite, native UI journey, public product admission, or independent
review is claimed. Restricted-view end-to-end disclosure and selectors beyond the
Bruno beneathThis use remain explicit independent-review concerns.

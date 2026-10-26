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

Independent review reproduced two admission defects on `1c58b671`: cards already
in the destination hand could justify initiation, and a reentered host could supply
stored candidates to an earlier continuation. Retained review tests failed 2/2
before this correction (`bruno-review-red.log`). Candidates now exclude the resolving
hand. The beneathThis query uses the already-captured source incarnation, as
`rr:leaves-play.1` treats a departed card as a new copy. This changes no digest,
wire or continuation state format.

`rr:initiating-abilities.3` says source departure does not stop the initiated
sequence, unless it prevents payment of a required cost. Accordingly the executor
can finish an empty optional retrieval after departure, and a synthetic independent
out-of-play selection can still retrieve its valid card. Source departure/reentry
probes mutate fixtures while a prompt waits: no legal gameplay interleaving that
performs those mutations here has been established. They prove defensive identity
handling, not a newly reachable Bruno sequence.

Corrected focused evidence: 21 content cases and two actual engine-to-View cases
pass (`bruno-review-green.log`, `bruno-review-view.log`). Restricted projections
expose eligible faces only to the answering seat and revoke unselected exposure
after zero- or two-card completion, including foreign physical ownership.

Limits: no broad suite, native UI journey, or public product admission is claimed.
Independent re-review remains required. Generic selectors beyond the current tests
are not claimed as exhaustively verified.

Correction mutants compiled independently: replacing captured source identity with
the current source failed only the reentry control (1 failed, 2 passed); removing
the destination-hand exclusion failed only the initiation control (1 failed,
2 passed). Logs: `bruno-review-identity-mutant.log` and
`bruno-review-hand-mutant.log`. After restoration, an explicit non-incremental
build and all 21 focused content tests passed (`bruno-review-restored-build.log`,
`bruno-review-restored-test.log`).

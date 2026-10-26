# Attacrobatics partial attack review

Reviewed engine revision `194bef8831d64eeefec7307e50361da0afa63880`.
This packet adds independent server tests only; the integrated ability-book and
public product admission remain the wave lead's responsibility.

Authority: RR 1.8 `rr:guard.1`, `rr:target.3.8`, and `rr:labeled-ability.2`
agree with the October 2024 ruling `0fc2a7cc9197e127`: positive boost-icon
cancellation can resolve under Guard, counts as an attack by the identity,
and has no attacked enemy. The older `6919a92cb0376eac` prohibition does not
govern that case. RR `rr:labeled-ability.6` cancels the whole label under stun;
costs remain. Zero-icon cancellation has no effect and cannot initiate solely
to pay its discard cost. These last two cases have existing source tests.

The new synthetic scene uses the existing `IDurableGameFactory` seam and actual
08006/08009 authored rows. Black Widow's identity has synthetic actor-only and
villain-target attack-end observers. A synthetic Action starts Rhino's complete
attack. This is engine/host evidence, not public Black Widow setup or native UI
acceptance.

`AttacrobaticsReplayReviewTests` passes both Guard and no-Guard branches through
strict request/response JSON, stored-session JSON, and fresh factory/host restart
at the pending boost interrupt and the discarded Preparation completion receipt.
The assertions distinguish actor-only attack completion from a real attacked
villain; the ordinary branch retains two damage, retaliation, and a target-aware
observer. Both branches reject an unsolicited villain target without changing the
save, prompt or revision. Replayed state, full RNG state/consumption, and pending
prompt match an uninterrupted control after every accepted continuation.
Synth-Suit's completion receipt precedes attack-end observers. The incoming
activation then completes with zero applied boost icons and no abandoned game.

Focused run: 2 passed, 0 failed. External evidence:
`C:/Users/mggar/AppData/Local/Temp/marvel-attacrobatics-review-final.log` and
`C:/Users/mggar/AppData/Local/Temp/marvel-attacrobatics-review-results/attacrobatics-review-final.trx`.
Initial failures were test-fixture errors: a Spider-Man substitute could not be
readied by the actual named-Black-Widow Synth-Suit effect. The final fixture uses
the actual identity and preserves that card's target requirement.

No production correctness finding was identified in the reviewed diff. The
independent no-Guard control catches a blanket removal of attacked-enemy facts;
the invalid-answer check catches target admission that accepts arbitrary supplied
villain identifiers. Existing targeted attack and Covert Ops regression evidence
must also be retained at wave integration. This review does not claim Linux,
whole-solution, native UI, or complete-pack verification.

# Actual Preemptive Strike face

05014 completes the authored Ms. Marvel inventory: 34 of 34 faces, 576 total.
The actual Hero Interrupt (defense) requires the attack-only boost faceup alias,
villain actor and hero form. It cancels current modified numeric icons on that
physical boost card, then deals the captured amount to the attacking villain.
Earlier faceup modifiers affect the count; earlier cancellations cannot be reused;
the Boost star still resolves and later star-added icons remain. The attack-only
alias belongs to the exact activation and cannot borrow an enclosing attack.

Independent engine reviews cover modified/amplified icons, zero and repeated
cancellation, star ordering, departed boost cards, exact activation ownership,
invalid costs, query purity, minion exclusion and schemes nested in attacks.
The actual row uses the same reviewed instruction and alias; no engine changes
are part of this authoring packet.

IndividualBoostCancellationTests now loads the full book for ordinary actual-row
cases. Explicit synthetic star variants remain isolated. Both paying seats finish
a real two-card activation: two icons cancelled and two reflected damage, the
other card's one icon retained, paying hero established as defender, final event
discard and complete activation cleanup. The focused boost/faceup/card-data suite
passes 179 cases. A data mutant replaces the captured damage amount with one;
both actual two-icon cases fail. Original bytes are restored and all 179 pass.
Logs: `C:/Users/mggar/Source/preemptive-actual-{tests,mutant,restored}.log`.

Independent actual-row review `a9397e56` adds scheme/nested-scheme, minion,
alter-ego and both-payer controls; all 21 focused checks pass and all four
actual-row mutants are distinguished. Native component commit `6d7422c7`
uses actual viewport selection/payment, asserts two cancelled icons, two reflected
damage, remaining attack damage and exactly one commitment. It is a prepared
cross-pack component, not an opening-deal game.

Public Ms. Marvel admission is integrated at `5034a368`. Native inspection found
a raw timing identifier and missing known boost count. `e6600ade` supplies the
readable stage; `76525061` supplies the current revealed card's uncancelled icon
count from the engine receipt, with explicit uncertainty about later effects.
Both payer cases and the next physical card distinguish two icons from one;
the pre-reveal defender choice discloses neither. A compiled constant-zero mutant
fails both cases; restoration passes all 21. Logs outside the repository:
`C:/Users/mggar/Source/boost-current-icons-{mutant,restored}.log`.
The strengthened native contract passes headless at integrated `2388a528`
(`C:/Users/mggar/Source/preemptive-icons-headless/results.jsonl`).

The hosted rendered rerun also passes (100%, 1920x1080, reduced motion).
Lead inspected `C:/Users/mggar/Source/preemptive-icons-hosted-rendered/preemptive-strike-payment.png`:
the payment surface names Rhino, Spider-Man and Sandman's two current icons,
retains uncertainty, and shows the selected resource and explicit commitment.
An earlier direct standalone invocation omitted the prepared authoritative host
and correctly failed scene setup; that is not counted as a gameplay pass.

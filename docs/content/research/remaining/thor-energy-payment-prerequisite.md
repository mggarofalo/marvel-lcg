# Thor E2 variable energy payment prerequisite — MARVEL-744

Candidate from `0d551731`, 2026-10-26. This proves a generic payment composition
using a synthetic safe counter effect on the printed `06006` event, not its full
ability. No ability-book row, setup, product admission or compatibility version
changes are included. E3 ignore-tough damage remains unsupported.

Printed authority: `datasets/cards/cards.json` gives Lightning Strike printed
cost one and “Spend X energy resources” before its arrow. `rr:cost.5.1` explicitly
uses this exact printed-one/X-energy example and permits dividing two generated
energy icons between its two costs. `rr:cost.5` requires simultaneous payment;
`rr:cost.4.1` excludes overpayment from the paid amount. `rr:non-numerical-variable.1`
defines X before modifiers, and `rr:initiating-abilities.step.5` aborts an unpaid
cost without paying anything. The vendored MarvelCDB `06006` erratum and Hall of
Heroes Lightning Strike ruling establish that it is damage, not an attack.

Zero-X interpretation is bounded by `rr:target.3`: a target is valid
“if any part of that ability can affect that target.” Unmodified zero damage
affects none of Lightning Strike's recipients. This packet retains the existing
positive `spendEnergyX` contract rather than widening that spelling for other
zero-cost abilities or damage modifiers; a future consumer needing significant
zero-X resolution requires its own admission proof.

Combined pricing keeps the adjusted printed price and repeated-energy X as
separate allocation components. The full answer is checked before the event or
a generator moves. Event payment captures X and preserves its energy result across
nested resource choices, while the arrow plan does not spend those icons again.
The prior standalone-entry-cost composition guard remains intact.

Evidence: seven new synthetic cases and 43 existing focused compatibility,
standalone-variable and payment cases passed. They distinguish printed-price
payment, discounts, overgeneration, declared wilds, insufficient typed resources,
double allocation, invalid X, rejected-command retry, and suspended lethal
resource-generator completion. Invalid nested input preserves card state, RNG
and pending agenda. Compiled mutants replacing the retained energy result with
zero and omitting the printed component each failed six of seven new cases.
Sources were restored and rebuilt nonincrementally before the restored checks.

Logs: `C:/Users/mggar/AppData/Local/Temp/thor-energy-*.log` (focused, content,
restored-build, mutant-receipt, mutant-printed, commit). Saved-session replay,
window-triggered variable-event composition, cross-platform CI and independent
review remain integration evidence; this packet makes no claim for them.

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
review were outside the implementation packet's evidence.

Independent E2 review examined `125e99a83f052900b6204df4880adcc83c82222e`
in an isolated `review/thor-energy` worktree. No production changes were needed.
Eleven new content cases challenge missing allocations, unselected sources,
wrong resource types, invalid component ids, reused icons and duplicate sources;
each rejected payment preserves the card digest, RNG and pending agenda and
permits the original action to be retried with an explicit valid allocation.
Additional observations cover one-use discount retention and consumption,
interrupt/response event activation, and refusal of both orders of variable
energy combined with the unsupported E1 entry cost.

Two new server cases use the existing `IDurableGameFactory` injection seam with
a deterministic synthetic scene. Actual `EngineHost` sessions accept X=1 and X=2
using a lethal resource generator, suspend at its optional interrupt, serialize
and verify the suspended save, restart the host, and complete payment. Replayed
and uninterrupted games agree on events, card digest, RNG state and consumption,
phase and pending prompt. A second serialized replay after completion retains
the exact chosen energy counter, including overgeneration at X=1. This is a
production server/ledger journey with synthetic content, not public Thor setup
or the actual Lightning Strike effect.

Final focused evidence: 45 content cases and four server cases (including the
existing in-process/socket variable-payment transport cases) passed. The full
Release solution builds without warnings. Independent logs use
`C:/Users/mggar/AppData/Local/Temp/thor-energy-review-*.log`.
No correctness finding was identified within this bounded payment review.
Public Thor admission, full Lightning Strike/E3 behavior, native interaction and
cross-platform CI remain outside its acceptance claim.

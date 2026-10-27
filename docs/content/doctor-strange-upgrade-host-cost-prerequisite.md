# Doctor Strange upgrade-host cost prerequisite — MARVEL-822

Based on `dd689e71815bd8bf740856f1736bc19aab639c90`. This packet supplies engine pricing and test-local 09039 evidence; it does not author the production Iron Man row or admit Doctor Strange publicly.

## Authority and decisions

Generated `datasets/cards/cards.json` prints 09039 Iron Man (Tony Stark), an Ally: “Reduce the cost to play each upgrade on Iron Man by 1.” The current vendored FAQ/rulings have no 09039-specific record. RR 1.8 `rr:initiating-abilities.step.3` determines costs with modifiers, `.step.4` applies them, and `.step.5` requires aborting unpaid costs without paying anything. The host is this physical Iron Man, including when another player plays an upgrade onto him; it is not every ally under his controller or another Iron Man copy.

The engine chooses the DSL spelling `reduceUpgradeCostOnThis` and continuous-effect kind `upgradeHostCostReduction`. Lowering permits a direct unconditional Constant with positive amount. The effect names its physical source as host; the ordinary active-constant query handles in-play lifetime, control and printed-text activity. Cost evaluation requires an upgrade with that eligible attachment host and floors each reduction at zero. The printed reduction does not consume a use.

## Actual play path

`GameCardPlayOffers` enumerates legal attachment hosts before assessing each price. Existing `CostOption.Target` and `DecisionAssessment.CostApplies` expose and validate each host price through protocol 42. Hosts that cannot currently be paid for are excluded. Ordinary attachment offers preserve their existing single-price shape when no destination changes the price. No View, renderer, protocol or historical fixture changes are required.

`CardPayment.CostOf` and `Price` accept an optional exact attachment host. `CardPlay` passes the selected destination before preparing payment, and `GameResolution` rechecks the current host price before clearing information or committing costs. The accepted payment snapshot retains the computed cost through existing resource-payment continuations. Querying without a host applies no host reduction. Invalid host requests supply no discount; `Price` also rejects illegal destinations.

## Evidence

`UpgradeHostCostTests` uses a nonpublic test-local 09039 Constant and explicit attachment metadata on actual printed Inspired/Enraged; the empty upgrade ability rows isolate pricing from their unrelated bonuses. Prepared duplicate Iron Man copies test physical isolation rather than a legal unique-card deployment. Checks cover foreign ally control/ownership, per-host prompt prices, direct payment agreement, wrong hosts, ordinary non-attaching upgrades, non-upgrades, independent copies, floor zero, stacking, repeated discounts, departure, illegal/stale prompt rejection, and query purity against an unprobed control. Purity checks include full RNG state/word count, agenda, effects, prompt identity, digest and subsequent events/state.

Focused Release run: 68 passed across `UpgradeHostCostTests`, self-play-cost classes, Captain America Quinjet and playing-card regressions. Five temporary compiled mutants were killed by intended behavior checks: remove physical-host equality; remove attachment eligibility; remove floor zero; omit the selected host during direct execution; omit host-specific prompt pricing. The first eligibility attempt did not compile and is excluded; the replacement deletes the eligibility guard and fails the ordinary-upgrade control. Mutants were restored, followed by a successful nonincremental Release rebuild and the passing 68-case focused run. A second nonincremental rebuild follows the replacement eligibility experiment.

Logs are retained outside the repository at `C:/Users/mggar/Source/upgrade-host-cost-*.log`, including individual mutants, restored builds, focused, content, rules and architecture runs. Broader suite results are reported separately with the exact candidate commit; no acceptance baseline has changed. Production row integration, independent review and combined wave verification remain with the delivery lead.

## Independent review of e56ef1ffe6708532e08576742cce8f1fc8cab119

Reviewed the exact diff from `dd689e71815bd8bf740856f1736bc19aab639c90`, the
affected pricing, play, offer, validation and constant-query collaborators, generated
09039 text and RR 1.8 initiation/attachment/upgrade authority. No production defect
was found in this bounded engine prerequisite review. The redundant host-effect
pattern in `AbilityAllyCostLowering` does not change the admitted programs.

`UpgradeHostCostReviewTests` adds four independent cases: an otherwise valid but
unaffordable ordinary host is excluded while the foreign Iron Man host remains
playable; two paid ordinary-host choices preserve the single anchor-based legacy
cost shape and spend the acting player's resource; a synthetic direct text blank
removes the discount while leaving the host eligible, and stale free payment is
rejected with prompt, cards, RNG and pending work intact. The direct blank is a
pricing-boundary probe, not a claim of playable ally-blanking content. Setup can
refill short prepared hands; the affordability probe establishes its live resource
pool after setup and refreshes the offer through a legal form change.

The focused Release selection (`UpgradeHostCost`, `SelfPlayCost`, `Quinjet`,
`PlayingCardsTests`) passed 72/72. Two additional compiled mutants were killed:
offering all legal hosts instead of the affordable subset (one intended failing
case), and replacing the ordinary attachment price with separate host prices
(both legacy-shape theory cases failed). Production source was restored exactly,
then rebuilt nonincrementally before the same 72-case selection passed again.
Logs are `C:/Users/mggar/Source/upgrade-host-cost-review-*.log`. This establishes
the reviewed engine scope only; public admission, native interaction and combined
wave verification remain outside this packet.

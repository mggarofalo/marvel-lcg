# Actual Energy Barrier face

05017 is appended to the actual book (572 total; 30 of 34 Ms. Marvel faces).
Printed Uses 3 reflection counters, cost 2 and unlabelled Interrupt come from
joined cards. It uses the independently reviewed damage-taken qualifier; no
hero-form or attack-only restriction is added. Prevented damage remains dealt
while the remaining taken amount controls subsequent interrupts (faq:05017).

RemainingDamageInterruptTests now uses the actual full book. Additional cases
pay its printed upgrade cost through a real player prompt, verify three entry
counters, resolve each use in both forms and show that final Uses disposal does
not prevent the reflected damage from completing. Prior invalid-target failures
were fixture omissions of the upgrade's required identity attachment target;
corrected decisions explicitly supply the offered own identity. Other cases
retain cross-seat remaining-damage and invalid/stale-response purity assertions.
These are prepared engine procedures, not hosted whole-game acceptance.

182 focused/data cases pass. A data mutant removing Uses disposal fails both
actual play-to-final-use cases; restored 182 pass. External logs are
energy-barrier-actual*.log and energy-barrier-uses-mutant.log. Explicit authored
counter inventory includes reflection 3 Uses. Independent actual-row review remains
required. Public Ms. Marvel admission remains closed.

## Independent actual-face review

Reviewed actual row e7c61942 as contained in candidate 5801f3ae in isolated
marvel-lcg-energy-review. Read joined 05017 and raw faq:05017 alongside the
previously accepted remaining-damage contract. Cost 2, reflection 3 Uses,
identity-only recipient, either form, any damage and a chosen enemy agree with
the printed face. The reflected dealDamage has no attack label. The explicit
starting-counter inventory includes reflection 3 Uses.

EnergyBarrierActualReviewTests adds four actual-book discovery cases: nonattack
damage to seat 1's identity in either form offers its barrier despite stunned;
another identity and its controller's ally cannot use it. These are prepared
replacement/discovery probes, not full damage continuation. The existing actual
paid-entry tests cover both forms, all three uses and reflected continuation
after final-counter disposal. Twelve focused cases pass. A temporary data mutant
removing the explicit identity predicate survives all four cases: target:you independently enforces the recipient restriction. This does not establish a killed mutant. Original book bytes were restored, and the same twelve cases pass again.
External evidence: C:/Users/mggar/Source/energy-barrier-review-{final,identity-mutant,restored}.log.

Independent verdict: accept this bounded actual Energy Barrier face. No blocker
remains in this scope. Public Ms. Marvel admission, hosted journeys, native
interaction and integration-wide gates are not established by this review.


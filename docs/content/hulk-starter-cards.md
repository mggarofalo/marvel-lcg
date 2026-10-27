# Hulk starter cards — MARVEL-847

This bounded nonpublic packet appends eight actual printed faces: 10011–10014
and 10016–10019. The book has 708 rows / 23 of 33 Hulk faces. All 700 objects
from accepted signature revision `10336f300c99e83bce00144cbf94af82731c1135`
remain byte-identical; their concatenated UTF-8 SHA-256 is
`106661ce6e5beebaa06ffeab1f077901de20a0a77a85bf197dc6d7a6259a9cd0`.
No engine, setup, starting counter or public admission changes are included.

Printed authority is corrected `datasets/cards/cards.json`; current RR 1.8
attack activation, Stun and Target determine timing and eligibility. Vendored
FAQ 10016 confirms the five-threat cap applies after modifiers. FAQ 10015
confirms enemy stun does not prevent Toe to Toe's later damage. The remaining
assigned queried cards have no FAQ entries. DSL event and selector spellings
are engine choices.

Actual-row evidence distinguishes completed Brawn attacks from Stun cancellation
and damage prevention, with non-first controller and foreign ownership; Sentry's
ordinary paid entry deals exactly one facedown encounter to that controller in
either form and never reveals it; She-Hulk's modified attack tracks damage and
healing. Drop Kick distinguishes physical and mixed actual payment, and draws
when the enemy is already stunned. You'll Pay uses actual identity damage from
the completed villain attack, excluding prior damage and prevention; its cap is
five. RR target.2/.3 requires a valid target that the effect can affect, so a
zero receipt supplies no scheme target; a Tough negative proves the response
is absent. Power of Aggression doubles for Aggression only. Martial Prowess
enters under another player's control and pays actual Drop Kick; its generator
rejects Hulk Smash's Superpower-only trait and a thwart event. Duplicate placement
is excluded. To the Rescue removes two and Confused cancels it.

Toe to Toe (10015) is deliberately absent pending a separate engine prerequisite.
The actual chosen-enemy `attack(seq(enemyAttacks,dealAttackDamage))` composition
raises before payment: `suspends inside a labelled power, which is not implemented`.
Moving activation outside the power conflicts with existing attack-envelope
admission and whole-ability cancellation. Both ordinary/stunned failing probes,
the exact row and fixture are preserved at
`C:/Users/mggar/Source/hulk-toe-probe/`; raw failure is
`C:/Users/mggar/Source/hulk-starter-attack.log`. No engine fix is included.

Windows verification is serialized. Final build uses `--no-incremental`; final
Content regression includes complete twenty affected inventory/test classes,
all HulkStarter tests, AbilityData and rule citations: **189 passed, zero failed or skipped**. Commands/profile are in
`C:/Users/mggar/Source/hulk-starter-filter.txt`; results are in
`hulk-starter-build-final.log` and `hulk-starter-regression-accepted.log` in that
same external directory. Eight valid data mutants are killed, one per added
face: wrong Brawn threat, Sentry count, She-Hulk bonus, Drop Kick damage,
You'll Pay cap, Aggression aspect, Martial restriction and Rescue amount.
Each mutant log contains intended assertion failures; the exact original data
is restored before the final nonincremental build/regression. Logs are
`hulk-starter-mutant-10011.log` through the eight corresponding face IDs and
`hulk-starter-mutants.json`. Normal commit hooks remain enabled.

These focused tests do not establish every interaction. Remaining independent
review should challenge Drop Kick declared wild/zero-price and departed targets,
You'll Pay changed defenders/overkill and boost damage, Brawn departing during
responses, and Sentry changed control/put-into-play. Complete hero gameplay,
public admission, native, replay and Linux/CI evidence belong to integration.

Independent review of `6e758f7df4425d11a8cd78eb2eb9cddfa90ba9e7` loads the
actual complete candidate book. No production or authored-book correctness
finding was found in the reviewed scope. Two reviewer test classes add 15
adversarial cases: doubled wild declarations spent as RR, RY and GG; one spent
physical icon from a doubled source; no payment at zero price; Drop Kick's
independent draw after its enemy is defeated; whole-attack Stun cancellation;
foreign-owned Sentry put into play under the non-first player's control in
both forms; a prepared in-play control transfer retaining its incarnation;
Brawn's optional response before lethal consequential damage versus lethal
forced Retaliate; helping-ally defense transferring the attacked player and
restricting You'll Pay to actual identity overkill; and actual You'll Pay with
actual Shrink applying its printed cap after the bonus.

An initial reviewer expectation that lethal consequential damage prevented
Brawn's response was rejected against current `rr:consequential-damage.1`:
consequential damage follows abilities triggered by the attack. The corrected
paired test proves the response occurs while Brawn still has four damage, and
that forced Retaliate instead defeats him before his optional response. This
was a review-test finding, not a production defect.

The review independently compared the exact raw UTF-8 JSON objects against
`10336f300c99e83bce00144cbf94af82731c1135`: all 700 are byte-identical, with
the same concatenated SHA-256 stated above. It also checked 708 unique current
IDs, the exact eight-face suffix, 23 Hulk faces, and absent 10015 and 90001.
The machine-readable audit is
`C:/Users/mggar/Source/hulk-starter-review-inventory.json`.
A valid data mutant removing only Drop Kick's mental-resource exclusion is
killed by the new RY declaration case. The original book bytes were restored
before a nonincremental Release build. Evidence is in
`hulk-starter-review-mutant-no-mental-guard.log`,
`hulk-starter-review-build.log` and `hulk-starter-review-regression.log` in
`C:/Users/mggar/Source/`; the exact regression filter is
`hulk-starter-review-filter.txt` there. This combines all Hulk cases, the
source packet's complete affected classes, every Content class containing the
current global count or starting-counter metadata, AbilityData and citations:
**355 passed, zero failed or skipped**. The restored Release build passed with
zero warnings or errors.

Remaining independent-review gaps are boost-ability damage versus attack damage,
a defender departing after declaration, and control changing inside Sentry's
entry-response window. The prepared direct control transfer proves no entry is
invented merely by changing an in-play area; it does not exercise a particular
printed control-changing ability. No public setup/admission, Toe to Toe
workaround, complete-game, native, replay or cross-platform claim is added.

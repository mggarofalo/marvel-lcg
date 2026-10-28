# Attack excess receipt: independent review

Source under review: `672ac1ad71e00a42dd8b55cc4df43caeb1663edf`, based on `8c2022e9`. Review worktree: `review/attack-excess`. This is a nonpublic engine prerequisite; the Moment of Triumph interpretations in tests are synthetic and do not admit card 12030.

## Authority and review

Rules Reference 1.8 `rr:overkill.1` requires actual defeat and `rr:overkill.3` uses the same excess value for abilities as overkill. `rr:leaves-play.1` makes a reentered physical card a new copy. Settled damage supplies excess, and both the retained attack recipient and the new `wasDefeated` branch compare the physical card and incarnation. The ordinary fallback for other selectors remains keyed by card ID. An attack-targeted response is instantiated for each recipient; its bound receipt supplies the number. Simultaneous damage maps results back to each recipient and the basic overkill spill explicitly clears the basic recipient.

`wasDefeated` searches all defeats recorded on the occurrence for the same incarnation rather than explicitly testing the cause. A nonattack defeat alone does not create positive attack excess, so this trace is not by itself a demonstrated Moment of Triumph error. The existing reentry-negative case excludes a different incarnation; the nested-attack case checks retained context. Same-incarnation causality and effects on other supported responses remain unproved without a reachable distinguishing outcome. The new recipient entries can alter pending metadata even when zero `ExcessDamage` is omitted from JSON. A genuine predecessor archive replay and the MARVEL-870 compatibility decision remain outstanding; no speculative version change follows from the serialization unit test.

## Independent controls prepared

`AttackExcessIndependentReviewTests` exercises a basic attack after partial prevention with lethal and nonlethal outcomes; an interrupt that heals the enemy before would-be defeat versus allowing the defeat; and two sequential damage placements on one labelled attack. It checks actual response healing and the original enemy's fate. Both absent-offer cases include a synthetic zero-cost response to the same attack as a positive eligibility control. The repeated-placement case uses a synthetic labelled power; it does not claim an admitted card plays that way.

On Windows Release with `MSBUILDDISABLENODEREUSE=1` and `-m:1`, the five independent cases passed. The ten affected whole classes, including these cases, passed 52 tests after a nonincremental restored build with zero warnings and errors. The compiled mutant replaced the excess calculation's `placed.Taken` with requested damage: the partial-prevention lethal case failed its healing assertion (expected hero damage 3, actual 1). After restoration, the same 52 tests passed. Logs are at `C:/Users/mggar/evidence/attack-excess-independent*.log`.

Replacement, other-player causality, same-incarnation unrelated defeat and genuine historical replay remain unverified by this review. Source author evidence is recorded separately in `attack-excess-receipt.md` and is not an independent rerun.

Verdict: accept the receipt prerequisite for the exercised nonpublic scope, subject to the identified integration and replay gates. No production card admission or owner approval is implied.

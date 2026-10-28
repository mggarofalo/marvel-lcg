# Giant Stomp live-choice source packet

MARVEL-875; base `c95154720affac19a402431bd1713beec0bcb1c6`. This is a nonpublic engine capability, not content admission. The test-local `12003` composition uses the Giant identity and resource cards; the production ability book and public setup are unchanged.

The targetless attack body supports exactly a sequence of constant simultaneous minion attack damage followed by `attackAnyEnemy` with a constant amount. Lowering rejects other placements and first-sentence shapes. The first sentence settles before the live choice reads Guard and damage eligibility. A previous recipient may be chosen again; both sentences remain one attack. Stun cancels the body after costs. No general nested choice inside a power is admitted.

On this base the unchanged two-Hydra forced-defeat order probe passes. Tests also cover no minions, repeated recipient, retaliation, surviving and defeated Guard, first-sentence prevention, invalid-answer state/RNG purity, nonfirst-player ownership, villain stage and terminal defeat, both optional recipient response orders, and rejection before payment with an otherwise valid same-cost control. The suspended attack damage events match a fixed-target uninterrupted control in source recipient, amounts, trigger, verb, attack identity and cause. The targetless test records an end boundary with source hero and target `-1` after a minion and villain received attack damage.

Release, `MSBUILDDISABLENODEREUSE=1`, `-m:1`, after the shared build slot was granted:

- Combined `GiantStomp`, `Melee`, `CountedAttack`, and `CountedCardAttack` Content filters: **56 passed**, including the unchanged Hydra probe (`C:/Users/mggar/Source/evidence/giant-finish-restored-test.log`).
- `SimultaneousAttackDamageTests` Rules class: **10 passed** (`giant-finish-rules.log`).
- A compiled mutant that excludes earlier attack recipients from the live choice failed the repeated-minion assertion; the zero-minion control passed (`giant-finish-mutant2.log`). A preliminary mutant using only the old excluded-stage list survived because that list is empty for targetless attacks (`giant-finish-mutant.log`). The source was restored, built with `--no-incremental` and the combined 56-test filter rerun successfully (`giant-finish-restored-build.log`, `giant-finish-restored-test.log`).
- The existing View `EnemyLessAttackKeepsOnlyItsAuthorizedSource` test passed for both policies (`giant-finish-view.log`); it demonstrates that a target `-1` boundary is rendered as “without an attacked enemy”. Combined with the live boundary and damage evidence above, this description is misleading for the splash attack. View correction and independent review are separate from this Cards packet.

The live prompt supplies the source context card, answering player, required enemy selection and the eight attack damage before modifiers. View's active-attack descriptor omits a targetless owner; this observation alone does not establish a UI defect. No native journey or protocol version change is claimed. MARVEL-870 replay compatibility remains open, and public integration/publication is blocked on that gate and the wider pack work.

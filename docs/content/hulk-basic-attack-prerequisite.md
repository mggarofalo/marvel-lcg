# One basic attack's overkill — MARVEL-845

This isolated engine prerequisite extends `BasicUse`, the exact occurrence receipt
already used by Skilled Strike and Mean Swing. No ability-book row, setup entry,
starter, or public Hulk admission is added. Test definitions using `10003` are
synthetic prerequisite programs, including additional choices, cancellation and
nested attacks that do not claim to be printed Hulk Smash behavior.

Printed `10003` in `datasets/cards/cards.json` grants +10 ATK for that attack and
overkill if paid using only physical resources. Current authority is RR 1.8:
`rr:overkill.1` requires actual defeat; `rr:wild-resource.1` makes a wild resource's
declared paid type decisive. No Hulk Smash-specific entry is present in the
vendored FAQ or rulings. Historical excess damage answers do not replace RR 1.8.

The engine-chosen DSL spelling `{"boostBasicUse":"overkill"}` grants only the
current basic attack's keyword. Existing positive integer and `exhaustedAllyPower`
forms retain their meaning. Keyword form is admitted only for a hero basic-attack
interrupt. Unknown words, objects, booleans and nonpositive scalar amounts fail
lowering. The runtime rejects a thwart receipt independently.

The keyword lives beside the occurrence's bonus, never on a character or lasting
effect. Basic resolution snapshots it onto `AttackDamageRequest`; the existing
damage policy and continuation preserve it through damage replacement and defeat.
A canceled attack or nested card attack cannot transfer it to a subsequent basic
attack. Existing activated/conditional `attackScope` admission remains closed.

The synthetic payment branch composes existing `paidWithResource` conditions;
settled payment survives an intervening choice. The description states the +10,
that-attack lifetime, and unresolved resource condition without evaluating a future
payment. Query controls compare the initial card digest, RNG and pending agenda
with an unprobed execution. Stale payment controls reject before either grant.

Focused evidence covers physical and mixed payment, choice and damage suspension,
Tough and selected prevention, cancellation, nested card attacks, second basic
attacks, query purity, stale payment and lowering rejection. Existing weapon,
ally, scoped keyword, payment and Loki RR 1.8 controls are included in the affected
regression filter. Redirect-specific acceptance and actual Hulk content authoring
remain outside this packet.

Verification on Windows: 96 focused/affected tests pass, including 20 new
prerequisite cases. Three temporary behavioral mutants are killed: ignoring the
request grant (4 failures), reversing the non-attack receipt guard (9), and
discarding the receipt grant (4). Each is restored before the final regression.
Raw logs are outside the worktree at `C:/Users/mggar/Source/hulk-p2-*.log`.
Normal pre-commit architecture, complexity and file-length hooks run on the commit.
Linux/CI and independent review remain integration evidence, not local claims.

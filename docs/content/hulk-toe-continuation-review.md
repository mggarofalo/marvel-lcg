# Toe to Toe continuation review

Status: closed after independent repair review. Source repair
`3a016518d43eaa2118e769307134d920897800ec` is cherry-picked as
`89496b50` onto the original independent review branch. The original finding
below records candidate `b2f45d973dba073dafed86f2eca5adf8b353b753` against
`47dd7984`. This is synthetic engine evidence; actual Hulk row `10015` and
public product admission are outside this packet.

## Original blocking finding: lethal followup loses the original attack's defeat

`LabelledPowerActivationReviewTests` exercises the retained chosen minion after
its attack is defended by another player's ally. The followup deals five damage,
defeats and removes the minion, and retains its original incarnation and five
damage in the original attack recipient receipt. At that attack's end response
window, however, `Occurrence.Defeats` is empty. The same fixture with the
`enemyAttacks` instruction removed retains the correct defeat, identity player
and cause. Canceling the enemy attack with Stun also reaches the missing receipt.

This is observable card behavior, not a speculative receipt requirement:
the existing `01052` Chase Them Down row triggers at `WhenAttackEnds` and checks
`wasDefeated: trigger.target`. `AbilityExpressionEvaluation` reads the current
occurrence's defeat collection for that predicate. Printed Chase Them Down in
`datasets/cards/cards.json` requires the hero's attack to defeat an enemy;
RR 1.8 `rr:attack-player-ability-type.2` considers the labeled ability an attack.

Routing evidence: `AbilityAttackDamageExecution` passes the retained power
occurrence for recipient publication, but invokes `DamageAttacks.Attack` with
the ambient occurrence. `DamageAttacks.Begin` selects that ambient cause and
`AttackDamageResolution` records its defeat there. The retained power occurrence
therefore receives the attacked recipient but not its defeat.

## Original verification

Command: `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c
Release --no-restore --filter FullyQualifiedName~LabelledPowerActivationReviewTests`.

At the reviewed revision plus the new tests: four cases compile, three pass and
one fails. The damage-only lethal control, surviving retaliate case, and Tough
retaliate case pass. The suspended lethal case fails at the expected
`Assert.Single(occurrence.Defeats)` with an empty collection. No engine mutation
or mutant was introduced. No full-suite acceptance is claimed.

Final focused evidence is outside the repository at
`../hulk-toe-continuation-review-final-focused.log`. Earlier exploratory logs
record corrected fixture assumptions and are not final verification.

## Independent closure

The repair passes the retained power occurrence as `AttackDamageRequest.Origin`
only when the existing shared classifier recognizes the bounded enemy attack
and fixed chosen-target damage body, and that occurrence differs from the ambient
frame. Ordinary bodies still pass null and use the existing ambient-cause path.
The Rules procedure owns the damage and defeat on that explicit original cause;
the interpreter retains its existing recipient publication. The exposed execution
field adds no serialized state. No unrelated admission gate or ability-book row
changes are part of the repair.

On review revision `89496b50`, the four original independent review cases all
pass, including the lethal receipt's exact incarnation, identity player and
attack cause. Actual authored `01052` Chase Them Down is offered and removes two
threat after the lethal followup, both with and without enemy Stun. Its nonlethal
controls remain unavailable even when the enemy defeats another player's
defending ally; that ally's defeat is absent from the hero attack receipt.

Independent Release runs passed 63 Content tests and 53 Rules tests. The Content
selection includes the complete `LabelledPowerActivation` family, Magic Blast
prerequisite/reveal-boundary tests, `SimultaneousPower`,
`AttackAftermathContinuation` and `LabelledPowerDefeatContinuation`. The Rules
selection includes attack occurrence, multi-target occurrence, damage source,
simultaneous attack damage, active attack and native retaliation lifetime tests.
Existing Magic Blast, ordinary repeated damage, deferred aftermath, attack
identity and retaliation controls pass. No new mutant was introduced during
closure; implementation-worker mutation evidence is separate.

Commands and passing summaries are retained outside the repository in
`../hulk-toe-continuation-review-closure-content.log` and
`../hulk-toe-continuation-review-closure-rules.log`. These focused checks close
this finding and accept the bounded engine repair. They do not claim a full
suite run or complete Hulk pack/product admission.

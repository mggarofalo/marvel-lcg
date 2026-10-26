# Black Widow response composition probes

Work: MARVEL-780. Base: `6b14e2f6`. These are test-local printed interpretations,
not ability-book admission, completed-pack verification or public setup evidence.

`BlackWidowCounterattackCompositionTests` runs a full Shocker attack against
Black Widow at seat 1, with 0/1/2 prevention against printed ATK 2. The expected
returned damage is 2/1/0 and full prevention retains the upgrade. The printed
08030 text includes the attack label; the interpretation therefore wraps attack
damage and pays discard before resolution. Widowmaker is declined.

MARVEL-781 implements the engine-chosen `receivedAttackDamage: "game"` numeric
binding as an occurrence-local actual placement receipt. The unchanged three
characterization bodies pass. Attacker/recipient incarnation and controller are
captured before defeat; overkill binds its actual identity recipient. The number
does not read mutable activation totals or infer a health delta.

`CounterattackReceiptTests` additionally proves Tough, defending ally and other
identity exclusion, direct boost-shaped damage exclusion, Guard, attack-label
retaliation, decline and subsequent-attack isolation, repeated discovery and
invalid handles against an unprobed control, invalid supplied resource payment,
reentered attacker rejection,
overkill, and successful versus Stun-canceled Preparation completion. The
successful completion offers both Widowmaker and Synth-Suit; cancellation pays
the discard cost but publishes no successful Preparation receipt.

Evidence is under `C:/Users/mggar/Source/counterattack-evidence/` with companion
logs outside the repository. The compiled mutant substituting damage dealt for
damage taken fails the partial-prevention original acceptance case (expected 1,
actual 2). A non-incremental restored build and focused 83-test run pass; a later focused
100-test run also passes after payment and layout findings are addressed. No
ability-book, public-admission, protocol, state-digest or replay-version change
is made; the JsonIgnore receipt reconstructs from replay. Independent review and
broader integrated acceptance remain lead-owned.

Authority reviewed: generated `datasets/cards/cards.json` 08030/08031; RR 1.8
`damage` step 9 and 3.2, `leaves-play.1`, and
`attack-enemy-activation.step.6.a`/`.6.b`. The specialized RR 1.8 attack procedure
finishes the attack and groups damage-taking triggers with other attack-end
triggers: forced abilities, including retaliate, precede non-forced abilities.
Vendored rulings `47681f33e542f477` (pre-1.5) and `7ce7004ea74ed42a` (1.6)
describe an older Counterattack-before-retaliate order. They have no recorded
rules-graph modification overriding the current explicit attack procedure and
do not govern that order. The source test asserting final retaliation totals
does not establish when the response was offered. Independent timing review
requires a control observing forced retaliation before the Counterattack prompt.
The MarvelCDB FAQ snapshot has no entries for these two face identifiers.

Independent review of `cc92090f4c1f5d209d526b3658088f76a9062160` rejects the
packet's timing as complete. `CounterattackReceiptIndependentTests` compiles and
runs three controls: the occurrence JSON shape passes; forced retaliation before
the optional response fails (expected attacker damage 1 at its prompt, actual 0);
same-title promotion before that prompt fails (expected the prior stage removed,
actual still in the villain area). Their later response/successor assertions
remain unproved until remediation reaches them. The log is
`C:/Users/mggar/Source/counterattack-review-final-controls.log`.

The received-damage collection is local to the damage execution occurrence;
review found no transfer into the separate attack-end occurrence. Correcting
response timing must retain those causal facts through the attack-end window
and preserve same-character villain succession. This review does not establish
queued-activation isolation, fresh-host restart, archive-byte compatibility or
public Black Widow admission. Replay v27 and its named v19/v20/v21 routes were
inspected only; those routes verify retained history rather than promising every
predecessor trace can migrate, and replay v26 has no admitted upgrade route.

The implementation packet must additionally prove: original attacker/recipient
and incarnation through suspension; full prevention/zero damage, boost ability
damage, damage to another hero and defending ally damage do not qualify; original
attacker departure cannot damage its reentered minion copy. Same-title villain
promotion remains the same character under `rr:villain-defeat.3.1` and keeps
the response bound to that successor; a different-title successor does not;
optional decline pays no cost; invalid answers preserve prompt/state/RNG/pending
work versus an unprobed control; successful Preparation resolution publishes the
receipt and offers Widowmaker/Synth-Suit, while cancelled resolution does not.

Rapid Response remains a separate composition packet: exact defeated ally,
controller at defeat and owner-discard membership, new incarnation and cleared
state, entry reactions then one damage, one-HP re-defeat, defeat response ordering,
ordinary discard exclusion, optional decline and invalid-answer atomicity.
`Defeated` currently carries no controller-at-defeat fact, so current ownership
or discard location must not be presented as proof of former control.

Current-RR timing remediation retains positive placements on the exact enemy attack
and publishes them into its completed attack occurrence, including indirect and
overkill recipients. Damage-triggered forced and optional abilities therefore share
RR 1.8 step six with retaliation. The test-local Counterattack row requires a
positive `receivedAttackDamage` receipt rather than a still-active attack. Actor
binding follows only the captured same-title villain succession and expected
entry incarnations; departed/reentered minions and different-title stages remain
ineligible. The damage quantity continues to use the original recipient/controller
and incarnation, excluding boost damage and later queued attacks.

This changes accepted decision/prompt ordering for enemy-attack damage responses,
including any accepted historical trace reaching such a response. It adds no
persisted JSON member, protocol/schema change or replay-version edit. Replay v27
integration is lead-owned; retain predecessor archives and reject divergent proof
histories instead of rewriting their expected outcomes. Public Black Widow
admission and characteristic fresh-host restart remain unproved in this source
packet: Counterattack is a test-local interpretation, and no admission bypass is
introduced for it.

Remediation verification: 51 focused Counterattack, counted-attack, simultaneous
card-attack and Vibranium-selected tests pass. Logs:
`C:/Users/mggar/Source/counterattack-current-expanded.log` and the non-incremental
restoration run `counterattack-current-restored.log`. A compiled mutant omitting
the expected successor entry-incarnation advance fails the unchanged independent
same-title promotion control (`counterattack-current-mutant.log`); restored source
is rebuilt with `--no-incremental` (`counterattack-current-restored-build.log`).
The admitted ability book's sole direct `WhenDamageDealt` trigger is Vibranium
Armor, whose villain-recipient damage is outside this enemy-attack-recipient
change. Existing delayed damage effects retain their current execution path.
Strict JSON round-trip and queued-attack controls pass; a characteristic fresh-host
mechanism proof through the existing `IDurableGameFactory` test seam remains for
independent review within its execution allowance. No archive fixture is changed.


## Integrated Rapid Response evidence


Work: MARVEL-78223c8c427-bc9a-4b68-b727-ede712d8e204. Printed authority is
`datasets/cards/cards.json` 08031: after an ally you control is defeated, discard
Rapid Response to put that ally into play from your discard pile and deal it one
damage. This packet uses a test-local interpretation, without ability-book or
product admission.

The imported five-case `BlackWidowRapidResponseCompositionTests` packet was red
at lowering for the proposed engine-chosen selector
`defeatedAllyInYourDiscardPile: "you"`. Its acceptance body had not executed.
Controller at defeat, owner, physical departure and current discard membership
must be proved independently; current ownership cannot establish former control.

The implementation captures controller at defeat and selects the matching physical
card and incarnation in its owner's discard. Current RR 1.8 `leaves-play.1`
invalidates an intervening in-play reentry. It does not invalidate out-of-play
movement alone: discard-to-hand-to-discard and reordering within discard are
positive controls, consistent with `SafeHouseCostSuspensionTests`. The older
vendored `ruling:c511a191fe8cc74d` (pre-1.5) also excludes a card currently in
hand; current RR and the printed current-discard condition govern this packet.
The MarvelCDB FAQ snapshot queried 08031 without an entry.

Return uses ordinary ally entry, binds the new incarnation as `chosen`, waits for
entry responses, then deals one damage. Tests distinguish Hawkeye's four fresh
arrows, ready state, one damage, another discarded Hawkeye, owner/controller
differences, decline, stale targets, rejected answers and an unavailable discard
cost. Synthetic entry-choice and one-HP fixtures prove suspension, refusal to
damage a reentered copy, re-defeat before Preparation completion, and completion
before Widowmaker/Synth-Suit. The imported body had not previously run; initial
expanded-run failures from already-ready response targets, an incomplete one-HP
board and cross-world reference equality were fixture findings, then corrected.

Verification: focused Rapid Response plus Safe House suspension tests passed
22/22 after a forced non-incremental Release rebuild (zero warnings/errors).
A compiled mutant removing the captured-controller check failed the original
owned-but-other-controlled case (1 failed / 4 passed); restoring and forcing the
rebuild returned 22/22 green. Logs are outside the tree in
`%LOCALAPPDATA%/Temp/rapid-response-{controller-mutant,restored-build,restored}.log`.
Normal pre-commit hooks remain required for both packet commits.

`Defeated.Controller` is an occurrence fact reconstructed by accepted-decision
replay, not a new persisted, digest, protocol, RNG or card-allocation field.
Existing admitted ability rows and public product admission are unchanged. The
runtime-selected DSL spelling and returned `chosen` binding are new capabilities.
No runtime version is changed by this prerequisite packet; the integrated pack's
history/replay verification remains the delivery owner's responsibility.

Integration control: extraneous resource generators on a discard-only response
were accepted in a diagnostic probe. The Counterattack worker owns that narrow
cost-preflight correction. This packet tests an unavailable discard source and
does not duplicate that shared fix. Re-run the extraneous-payment control when
the reviewed correction is integrated.

Independent review of engine revision `e7739447` found no additional production
defect in the packet's owned behavior. `Defeat.Record` captures controller before
the step-7 defeated-card abilities and departure; current ownership and discard
membership remain separate selector requirements. The return binding captures
the entered incarnation before entry windows suspend the suffix. An intervening
departure invalidates that binding, including departure without reentry.

Independent tests add two departure-only entry cases (hand and discard) and a
nonpublic characteristic restart through the existing `IDurableGameFactory`
seam. The latter uses printed Rapid Response, Hawkeye counters, Widowmaker and
Synth-Suit, plus explicitly synthetic Action and entry-choice scaffolding.
Strict request/response, stored-session and session-save JSON round trips rebuild
fresh hosts at the defeat response, suspended entry choice and Preparation
completion. Both Widowmaker/Synth-Suit orders complete with one returned-ally
damage and one villain damage. The tests compare reconstructed defeat facts,
completion receipts, prompt records, digest, RNG state/consumption and accepted
event journals with an uninterrupted control. An unknown `defeats` save field is
rejected, preserving replay as authority for occurrence facts.

Review verification: `RapidResponseReplayReviewTests` passed 2/2; the combined
Rapid Response, `SafeHouseCostSuspension` and `WreckingDefeatReceiptTests` filter
passed 27/27 in Release. Logs are outside the tree at
`%LOCALAPPDATA%/Temp/rapid-response-review-{server,content}.log`. Initial independent
fixture failures exposed ready-option filtering, a misspelled zone constant and
immutable-collection reference equality; the final fixtures assert semantic
cross-world values. These runs establish this prerequisite's focused behavior,
not full-pack admission, platform certification or predecessor-save migration.
No production ability row, compatibility identity or public content admission
changes in this review. The shared extraneous-payment finding remains owned by
the Counterattack packet.

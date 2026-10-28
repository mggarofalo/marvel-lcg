> Historical investigation at `364660e1`. The fixed-target recipient defect below is repaired by `deac79b5` and independently reviewed at `89702db2`; see [the review](attack-recipient-review.md). The live-choice limitation remains open under MARVEL-875.

# Giant Stomp composition investigation

Base: `b13de4fa0f00d13d158cbd5355c49b3887f80258`. Test-only investigation for
MARVEL-875 (`9c02378a-9373-4fc8-86a8-a7fbc9243908`); no production ability,
engine, setup or admission change. Final focused verification: **4 passed,
0 failed, 0 skipped** on 2026-10-28. These tests record bounded unsupported
behavior and damage controls; they do not establish Giant Stomp support.

Authority is generated `datasets/cards/cards.json`, row `12003`: Giant-only Hero
Action (attack), costing 3, dealing 1 damage to each minion followed by 8 damage to
an enemy. Current vendored RR is 1.8. `rr:ability.4` resolves sentences one at a
time; `rr:attack-player-ability-type.2.1` makes multiple damage instances one
attack; `.5` gives each attacked enemy attack facts; `rr:labeled-ability.6`
cancels the entire labeled ability for Stun after costs. `rr:guard` and
`rr:target.3.8` govern which enemies may be attacked. There is no printed
"another" restriction: a surviving splash recipient can receive the 8.

Local FAQ and ruling searches found no Giant Stomp-specific answer. Targeted
online research on 2026-10-28 found no "Giant Stomp" text on Hall's
[pre-1.5](https://hallofheroeslcg.com/official-ffg-rulings/),
[post-1.5](https://hallofheroeslcg.com/latest-ffg-rulings-post-rrg-1-5/) or
[post-1.6](https://hallofheroeslcg.com/latest-ffg-rulings-post-rrg-1-6/) FFG ruling
pages. The last page's September 25, 2025 Directed Force/Melee answer permits a
later target becoming legal after the first target's defeat. This is analogous
official guidance supporting live sequential selection, not a Giant Stomp
clarification or replacement for RR 1.8. It does not justify selecting the second
sentence's enemy before splash settlement. The distinguishing case is a Guard
minion defeated by the splash before selecting the enemy for 8 damage.

Existing actual-card Melee (`05030`) uses `attackAnotherEnemy` within one attack.
`AbilityStagedAttackLowering.IsBody` restricts that instruction to one chosen
recipient's constant damage followed by another enemy's constant damage. It
cannot express the area-first sentence, nor Giant Stomp's permission to select a
surviving splash recipient again. Counted `attack` is separately limited by
`AbilityPowerLowering.AttackCount` to one simultaneous constant damage effect
over `powerTargets`. Two outer attacks would violate the single attack rule.
Direct nonattack damage before a separate attack would misattribute splash and
would not allow Stun to cancel the whole printed ability.

`tests/Marvel.Content.Tests/Play/GiantStompCompositionTests.cs` uses actual
`12001a/b/c`, turns to Giant `12001c`, pays using actual double resources
`12021`/`12023`, and supplies player/encounter decks and a nonterminal villain
stage. Its candidate uses a supported singular `villain` anchor and the body:

```json
{"seq":[
  {"dealAttackDamage":{"cards":{"query":"minions"},"amount":1,"simultaneous":1}},
  {"chooseCard":{"from":{"query":"attackableEnemies"},"effect":{
    "dealAttackDamage":{"cards":"chosen","amount":8}}}}
]}
```

This bounds the probe to no-Guard single-villain boards. The fixed anchor is not
a proposed global production encoding; it isolates the actual area-then-choice
continuation without the invalid singular `target=minions` used by the earlier
probe. Executed cases use zero and two minions. A same-cost control replaces
only the final live choice with 8 attack damage to the villain and checks actual
resolution, minion splash, source discard, one whole-attack observation, and an
idle agenda. Synthetic `01092` and `01093` observers explicitly replace their
actual book rows locally; production duplicate rejection is unchanged. The first
observes the whole attack; the second observes `WhenAttackEnds` with
`target: enemy` and places a counter on `trigger.target`.

The compiled candidate raises
`'12003' suspends inside a labelled power, which is not implemented` from
`AbilityAdmission.CanInitiate`, via `AbilityAdmissionResolutionPreflight` and
`AbilityPowerSuspension.HasIntrinsicSuspension` (nested `chooseCard`). Both
zero-minion and two-minion boards raise before payment. The prerequisite is a
Cards-owned retained multi-recipient attack continuation: settle simultaneous
minion attack damage/defeat, then select a current legal enemy and deal 8, while
retaining the same Rules-owned attack occurrence, performer, source, causal
damage facts and retaliation. Existing Rules attack/retaliation ownership must
be reused rather than reconstructed from board membership.

The fixed-target control also exposes a bounded response-fact gap. With two
minions, each receives 1 damage and the villain receives 8. The whole-attack
observer fires once and each minion's per-enemy observer fires once, but the
villain's per-enemy observer fires **zero** times. With zero minions, the
villain's observer fires once. The checked-in assertion explicitly records this
known boundary; zero is not the authority-derived expected behavior. This
establishes an observable `WhenAttackEnds`/`target: enemy` failure, not its
internal receipt mutation or any claim about other attack shapes.

Preserved desired-behavior probe: change
`DeferredEnemyChoiceReachesPowerSuspensionRatherThanInvalidSelector` from its
expected-boundary assertion to starting the game and paying as in the fixed
control. Require splash to finish before the enemy prompt, then choose a current
recipient and require 8 additional damage, one attack response, and discard.
Today the boundary test expects admission to raise instead. A complete repair
must additionally cover a one-health Guard defeated by splash making the villain
selectable, a surviving Guard keeping it unavailable, a selected surviving
minion taking 9 total, first-sentence defeat choices settling before selection,
and Stun replacing both sentences. Stage transitions belong in the later
recipient's ordinary attack damage settlement; do not select a stage early or
carry damage into a departed incarnation. These cases are unimplemented and
untested here; the bounded probes do not establish global Giant Stomp support.

Compiled desired-behavior experiments and logs are preserved outside the
repository under `C:/Users/mggar/AppData/Local/Temp/`:

- `GiantStompCompositionTests.desired.cs`: changes only the execution control's
  `Runner(FixedDamage)` to `Runner(LiveChoice)`. Compiled result: **0 passed,
  2 failed**, both with the exact nested-choice exception during turn-offer
  admission after mulligan. Log: `giant-stomp-desired-failing.log`.
- `GiantStompCompositionTests.desired-recipients.cs`: fixed-target execution
  requiring the villain's per-enemy counter to equal 1. A nonincremental rebuild
  and run produced **3 passed, 1 failed**, specifically the two-minion case,
  expected 1 / actual 0. Logs: `giant-stomp-restored-build.log` and
  `giant-stomp-restored-test.log`.
- Final source restores fixed-target execution and explicitly asserts the
  observed recipient boundary. Result: **4 passed, 0 failed, 0 skipped**;
  `giant-stomp-final-test.log`. Runs use Release and serialized MSBuild
  (`MSBUILDDISABLENODEREUSE=1`, `-m:1`). No engine mutation was made.

Focused final command:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release -m:1 --filter FullyQualifiedName~GiantStompCompositionTests
```

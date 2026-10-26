# Mutagen final-face bounded checkpoint

Base: `980d1be3d8670aeeb7c4eb7f011b43fde6052934`.
Five authored research rows: 02026, 02027, 02028, 02029, 02032. Total 55/57 distinct
Green Goblin faces; 02030 and 02031 remain fail-closed. No engine/setup/public
admission changes. This is a partial packet, not complete-product evidence.

Authority: joined `datasets/cards/cards.json`, offline MarvelCDB FAQ and vendored
rulings. The FAQ has only 02030 among these seven, confirming alter-ego attacks.
Rulings 7c484b8434a60f3a and d4def3183850e80f distinguish numeric discard from
searching and require ordinary deck resets. Ruling d855181a23d49683 declines
Overconfidence errata: its printed damage *placed* condition cannot use the
existing activationDamage receipt, which captures DamageDealt. rr:damage.step.5
and rr:damage.3.2 distinguish placement from dealt damage. A separate engine
packet must capture placed damage through completed activations, prevention,
ally defense, overkill and suspended responses. No workaround is authored.

02030 was researched but not authored within this checkpoint. Its extra icon can
use the existing boost_const modifier; that composition still needs actual tests,
including no additional Hysteria boost in alter-ego form and engagement ownership.

Actual authored tests cover all five rows: live Goblin minions across players;
Nation boost entry and live villain/minion ATK; stage-II attack/scheme bonuses and
expiry; Overrun exact-copy/player-order recruitment at defeat; Wicked Ambitions
stage count and separate per-minion damage/entry choices. Existing receipt tests
cover reset/order/nested-copy mechanics; actual-row reset coverage remains useful
review follow-up. Unknown reveal and star-boost controls now use printed 11028
Time-Travel Tactics, an unauthored Kang face with both printed abilities.

Focused Release Windows selection: 57 passed, zero failed. Includes actual rows,
ability-data/lowering, remaining fail-closed faces, negative boost control and
rule citations. Log: `%TEMP%/mutagen-final-focused.log`.
No full suite, replay/native, complete-product or public-admission proof is claimed.

Actual-row mutants are lowered and executed from the canonical book: 02026 threat
zero, 02027 ATK two, 02028 discard one, 02029 bonus zero, and 02032 mill two.
Each fails its characteristic test (02029 fails both forms); restored selection
passes 57 tests. Logs: `%TEMP%/mutagen-final-mutant-<face>.log` and
`%TEMP%/mutagen-final-restored.log`.

## I See You continuation

Base `3c08d494`. 02030 is now authored, bringing the distinct Goblin face count to
56/57. 02031 remains fail-closed pending its separate engine receipt prerequisite.
The earlier five-row checkpoint and its results above remain historical evidence.

Printed 02030 and official FAQ 02030 / ruling:ab735d7ce752f211 establish an attack
against either form. The alter-ego branch sets existing dealBoostCards false;
the hero branch keeps ordinary dealing. A Boost conditional grants this exact
source one boost_const icon until activation end only when a Goblin minion is
engaged with the activation player. Two Goblins still add just one icon.

Twelve actual-row cases cover reveal in both forms with/without Hysteria,
attack/scheme boosts, non-first-player ownership, other-player Goblins,
non-Goblin minions, duplicate Goblins, source/copy isolation and expiry. Both
forms complete one attack; alter-ego draws no ordinary or additional Hysteria
boost; hero draws the printed ordinary and additional cards. The game remains
active and the completed activation is cleared.

Focused Release Windows selection passes 68 tests including prior five rows,
data/lowering, remaining fail-closed 02031, unknown 11028 controls and citations.
Log: `%TEMP%/mutagen-isee-focused.log`.

Three independently lowered actual-row mutants fail the intended observations:
alter-ego boost dealing enabled (2 failures), conditional icon amount two
(3 failures), and all-board engagement substituted for the activation player's
engagement (2 failures). Restored focused selection passes 68 tests.
Logs: `%TEMP%/mutagen-isee-mutant-boost-deal.log`,
`%TEMP%/mutagen-isee-mutant-icon-amount.log`,
`%TEMP%/mutagen-isee-mutant-engagement-owner.log`, and
`%TEMP%/mutagen-isee-restored.log`.

Final direct JSON inventory: 541 authored ability rows, of which 56/57 distinct
printed Goblin pack faces are authored. UPSTREAM total-row metadata is reconciled
against that count.


## Overconfidence actual-row continuation

Base `4ceaab8819473dd3c1618aa433c4149039c0c8c7`. Printed 02031 is now authored,
bringing the direct JSON inventory to 542 total rows and 57/57 distinct printed
Green Goblin faces. Public runtime admission remains closed pending full-pack
proof; this packet changes no engine, setup or public admission code.

Authority: joined 02031 printed text; ruling:d855181a23d49683 declines errata;
rr:attack-enemy-activation.step.3.b includes resolving Boost abilities within the
activation, and rr:damage.step.5 places damage after prevention. The row reads
activationDamagePlaced, including owned boost placement, rather than the legacy
damage-dealt receipt; alter-ego reads activationThreat from its own scheme.
Legal standard Goblin modular choices contain no scheme-boost threat placement.
This packet makes no claim for generic Taskmaster 04101 boost threat scope.

Thirteen actual-row cases use the full encounter queue/reveal procedure in a
two-player research board. They prove two versus three placement, both forms,
non-first-player resolution, prevention, Tough, canceled stun/confuse activations,
actual Concussive Blast boost damage (one boost plus two attack), ally excess,
overkill and unrelated damage exclusion. Each asserts source cleanup, an empty
reveal queue/agenda, completed activation and active game, and exactly one extra
inert Bomb Scare minion reveal when surge applies. Overkill uses a deliberately
supplied generic engine modifier; no printed Goblin attachment is claimed to
grant it. This is characteristic encounter evidence, not production setup or
whole-game proof. Queued/nested ownership isolation remains covered separately
by the independent synthetic activation-placement contract tests in the focused
selection; no natural Goblin boost card queues another activation.

The canonical row mutant activationDamagePlaced -> activationDamage compiles and
fails three intended cases: damage prevention, Tough, and owned Concussive Blast
boost placement. Log: `%TEMP%/overconfidence-mutant-dealt.log` (3 failed, 10 passed).
The restored focused selection and normal pre-commit hooks are recorded below.

Restored focused Release Windows selection passes 184 tests, zero failures,
including all Mutagen cases, ability-data/lowering, unknown 11028 negatives,
rule citations and independent activation-placement probes.
Log: `%TEMP%/overconfidence-final-focused.log`. No full suite or native run.

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

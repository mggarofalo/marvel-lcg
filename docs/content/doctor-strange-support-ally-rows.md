# Doctor Strange support/ally rows — MARVEL-819

Base: `78efa9e0`; bounded implementation packet, 2026-10-26. Only **09020
Unflappable** is appended to the authored book: **672 rows, 27/40 Doctor Strange
faces**. The 671 predecessor raw row objects remain byte-identical (prefix
SHA-256 `cd8b537fa8f117129917740e0b12ef37ae073cd05826fc23db342224ab7f50708`).
No engine, setup, public admission, UI or Plane changes are included. Clea,
The Night Nurse and Iron Man ally remain explicitly unauthored.

Printed authority is the exact generated `datasets/cards/cards.json` records
09013, 09019, 09020 and 09039. The latter is the Leadership ally Tony Stark,
not the Core identity. Local `faq:09019` requires discarding Tough when it is
the only status; `faq:09020` excludes ally defense and distinguishes boost
ability damage from attack damage. RR 1.8 `rr:defend-defense.4` includes
initiating defense-labeled events; `.7` places these responses after the attack
ends. The historical basic-defense-only shorthand is not the current rule.
Local rulings and the research handoff were checked; no online source replaces
the generated printed text.

Unflappable's attachment query supplies any-player control and the printed max
one per player. Its optional attack-end response requires the controller's
identity to have defended and not received attack damage; the exhaust cost
precedes drawing one card. The actual row tests cover basic defense, Backflip
without basic defense, actual Concussive Blast boost damage, positive attack
damage, ally defense, foreign ownership/current control, an attack against the
other seat, transfer at play and recipient max-one enforcement. They assert
completed attack receipts, answering seat, optionality, draw, exhaustion and
full agenda completion. No starting counters are added.

`DoctorStrangeSupportPrerequisiteTests` saves proposed programs and bounded
reproductions outside the authored book:

- **Clea:** optional `WhenCardDefeated` Interrupt with `shuffleInto this
  yourDeck` correctly survives the defeat continuation for the same physical
  copy. Both accept and decline controls pass. With owner 0/controller 1, it
  shuffles into controller 1's deck, demonstrating the missing **source-owner
  deck destination**. Decline goes to owner 0's discard. `ownerDeck` is rejected
  at lowering. The existing incarnation-aware continuation must be retained;
  no cleanup defect is established. The engine prerequisite must additionally
  prove direct nondefeat discard exclusion and new-incarnation protection.
- **The Night Nurse:** the complete proposed row preserves three medical Uses,
  exhaust/remove-one cost, chosen hero and simultaneous independent heal-one /
  status-discard effects. Its proposed `chooseStatusOn` operation is refused at
  lowering. Existing `removeStatuses` removes every physical matching status:
  one-status and two-status controls remove one and two respectively. Global
  `statusCards` plus `replaceStatus` (Vapors) cannot express discarding exactly
  one physical status on the already chosen hero. Selector predicates do not
  expose that status-host relation; the `chosen` binding is replaced by the
  inner choice. Needed: an actual choice of one physical status on that same
  hero, with heal-only/status-only partial effects and counter-cost continuation
  preserved. Zero/last-counter, foreign hero/control and multiple physical
  statuses remain acceptance requirements for the engine prerequisite.
- **Iron Man ally:** a direct unconditional Constant proposed host-scoped
  `reduceUpgradeCostOn this amount 1` is refused at lowering. A valid empty ally
  program is the admission control. Existing next-card and self-card reductions
  do not express a continuous reduction to an upgrade played onto this exact
  ally. Needed: selected attachment target in cost assessment, floor zero,
  foreign controller/payment support and unaffected different allies/upgrades.
  No production row or approximate reduction is admitted.

Verification on the restored candidate:

- Nonincremental Release build: zero warnings/errors.
- 191 affected tests pass: all Doctor Strange named classes, four changed Black
  Widow inventory/behavior classes, CounterattackCurrentTimingTests, ability
  data/program/lowering and rule-citation classes. The synthetic Counterattack
  marker now explicitly replaces the actual 09020 row inside its fixture;
  duplicate-row admission remains strict. Global inventories and genuinely
  unauthored negative controls are updated.
- Offline cards extraction check passes; generated cards and search match.
- Two compiled row mutants are killed: dropping the no-attack-damage condition
  fails the positive-attack-damage case; removing the exhaust cost fails four
  successful-response cases. Each restored candidate is rebuilt
  nonincrementally before its passing run.

Evidence logs are outside the repository at
`C:/Users/mggar/AppData/Local/Temp/strange-support-{test,affected,restored-build,mutant,mutant-exhaust,cards}.log`.
Independent review and broader integration lanes belong to the lead packet;
this bounded evidence does not certify complete-pack/public product readiness.

## Independent review of the Unflappable packet

Reviewed source `f5d4ed512d398db13d2e025ba5c01bfaae9b04e9` against
`78efa9e0`, 2026-10-26. **Accepted for this bounded authored-row packet; no
correctness blocker found.** This accepts actual 09020 and the prerequisite
research evidence, not playable Doctor Strange admission or implementation of
the other three cards. Printed 09020, local `faq:09020`, and current RR 1.8
defense, response, Tough and ownership clauses support the interpretation.

An independent raw-object comparison confirms all 671 predecessor objects are
identical and 09020 is the sole appended row (672 total; 27 Doctor Strange
faces). Inventory changes preserve their exact face sets and no-counter checks;
the unauthored negative control remains genuinely unauthored. The explicitly
synthetic Counterattack replacement preserves its timing assertions without
relaxing production duplicate admission. No engine or public-admission changes
are part of this diff.

`DoctorStrangeUnflappableReviewTests` adds five actual-book cases: positive
damage fully prevented by Tough after basic defense qualifies, the same attack
without Tough does not, and undefended Tough prevention does not count as
defending. Two Backflip cases prove the non-first controller can accept or
decline the optional response while another player owns the upgrade; accepting
exhausts it and draws for its controller, while declining preserves readiness
and draws nothing. Both complete the real attack agenda.

Review verification after restoring the exact ability-book bytes:

- Nonincremental Release Content-test build: zero warnings/errors.
- 196 passing tests across whole Doctor Strange classes, the four changed Black
  Widow classes, CounterattackCurrentTiming, AbilityData, AbilityProgram,
  AbilityLowering and RuleCitation classes; 58 additional effect/cost-lowering
  tests pass. The 196-case selection passes again after mutant restoration.
- A compiled 09020 mutant removing `heroDefended` fails exactly the undefended
  Tough control (one failure, four passing review cases). The ability book is
  restored byte-for-byte and has no Git diff.
- Offline cards extraction check passes; generated cards and search match.
- Full Content, whole-game, native and public-admission verification are outside
  this review and remain integration evidence, not inferred passes.

Logs are outside the repository at
`C:/Users/mggar/AppData/Local/Temp/unflappable-review-{focused,affected,mutant,restored-build,restored-affected,additional-lowering,cards}.log`.
The `ownerDeck` rejection probe records the current MARVEL-819 boundary only;
it must migrate to positive capability evidence when MARVEL-820 integrates.
The Night Nurse single-status choice and Iron Man exact-host upgrade discount
remain separate engine prerequisites; this acceptance does not settle them.

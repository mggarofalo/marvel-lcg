# Black Widow four-face independent review

Reviewed source: `c5e13668db3d89a72928cc47a21ff30ba704a5eb`, against
`9880d167`. Review tree: `marvel-widow-fourfaces-review`.

Verdict: Quincarrier 08023, Burn Notice 08025 and Deadly Shot 08029 have no
identified correctness finding in this review. **Taskmaster 08026 is blocked**
by the live lasting-effect amount finding below. This is a four-face source
review, not whole-pack admission, public setup, complete-game or native evidence.

## Taskmaster boost freezes its upgrade count

RR 1.8 `rr:lasting-effects.3` says: "Lasting effects update whenever the game
state updates." Its example changes a per-minion ATK bonus when minions enter or
leave. Printed 08026 similarly grants +1 ATK/SCH per controlled upgrade for the
activation; it does not fix the number when the Boost ability resolves.

A compiled actual-row probe gives Rhino one predealt 08026 boost and then draws
01173 Electric Whip Attack as its ordinary boost. The attacked nonfirst player
controls exactly one 01004 Web-Shooter. Decline defense; Taskmaster resolves for
+1; Electric Whip's actual Boost offers that upgrade and discards it. The same
attack then completes. Both boosts reach encounter discard, Web-Shooter reaches
its owner's discard, the activation is null and the agenda is empty, with no
game-ending condition. Identity damage is **3, expected 2**. The upgrade count is
zero before attack calculation, so Taskmaster's lasting bonus must be zero.
This is ordinary engine scheduling and answer flow with actual authored cards,
not direct pending-world movement or a synthetic response.

`AbilityImmediateExecution.GrantUntil` registers a `ContinuousEffect` with
`Amount: resolveAmount(amount)`, capturing the initial count. The authored
Taskmaster composition therefore needs a reviewed shared capability or another
correct implementation before 08026 can be accepted. No engine or ability-book
change was made by this review.

Retained compiled red probe:
`C:/Users/mggar/Source/bw-taskmaster-live-review-red.cs`;
log: `C:/Users/mggar/Source/bw-taskmaster-live-review-red.log`.
The probe is external because committing a known-red regression alone would make
this review branch fail. The lead must carry it into the prerequisite fix.

## Authority and distinguishing evidence

Read the complete Black Widow implementation handoff and this batch's evidence,
raw generated four-face text/stats, local FAQ query results and Taskmaster
rulings `c46741ceaa9f5e18` and `ead76dd0b3c966ee`. Current RR 1.8 governs;
those rulings address reminder stars and uniqueness, not a fixed upgrade count.
Checked obligation, choose-option, first-player, you/your, resource-ability,
unique-icon, boost, scheme activation, leaves-play and lasting-effect clauses.
Taskmaster counts **every upgrade**, not only Preparations.

The independent committed controls add eight cases: global Quincarrier
uniqueness at either seat with a payable same-cost control; Taskmaster includes
ordinary upgrades, excludes hand/discard/supports and foreign seat, follows a
changed engagement, and supplies the right attacked-seat amount; numeric Burn
Notice/Deadly Shot boosts use printed 2/1 icons in attack and scheme without
running their reveal abilities. Existing actual-row tests supply both forms,
resource declaration/exhaustion, identity-Avenger play restriction, Burn Notice
recipient/flip/exhaust/removal/surge/tied printed-cost choices and invalid retry,
and Deadly Shot own-upgrade selection and independent form consequences.

The PreparationPrintedFixture intentionally removes actual 08023 before adding
its explicitly synthetic defeat-suspension observer; actual Quincarrier coverage
uses the unmodified authored runner. Inventory stays 632 rows / 21 Black Widow
faces. Quake 08012 remains unauthored; this review neither proves its legal nested
reentry reproduction nor recommends a particular retained-actor remedy.

24 source plus independent focused cases passed before the additional compiled
Taskmaster probe. After preserving that red externally, restored focused checks
and normal-hook results are recorded in the review handoff. No broad suite ran.

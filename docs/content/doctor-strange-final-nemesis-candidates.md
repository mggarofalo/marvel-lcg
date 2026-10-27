# Doctor Strange final nemesis candidates

Current migration: actual 09028 and 09031 programs, canonical tests and evidence
are recorded in [the actual-row packet](doctor-strange-mordo-thoughtcasting.md).
The record below describes the candidate baseline; only Counterspell remains
test-local in `doctor-strange-final-nemesis.json`.

Base: `99d4b16e`, branch `feat/strange-final-nemesis`. These are test-local
compositions, loaded by `DoctorStrangeAbilityFixture.WithReplacements`. The
production inventory remains 682 rows / 37 Strange faces; no admission claim.

Printed authority: `datasets/cards/cards.json`, faces 09028, 09030 and 09031.
Current Rules Reference is 1.8. Local Counterspell rulings
`638d99c719a38af3` (alter-ego reveal), `4432bdf7194d532c` (stun priority),
and `810e462c7d971efe` (attached seat only) agree with the current form,
event and status rules. The printed interrupt has no Hero qualifier after
attachment; flipping does not create that restriction.

## Proven candidates

`DoctorStrangeMordoCandidateTests`: six cases prove physical stun, energy
two damage, mental confuse, wild all three, exact attacked player's deck,
isolation from an older wild discard and another player's top, status priority,
and final-card immediate reset with an encounter penalty while retaining the
same wild receipt. `WhenAttackInitiated` is the existing engine spelling;
RR `attack-enemy-activation.5` equates "when [enemy name] attacks" with attack
initiation. The ordinary attack remains a separate two damage.

`DoctorStrangeThoughtcastingCandidateTests`: ten cases prove both forms,
both highest-cost tie choices, resolving seat 1 despite first player 0, another
seat's higher-cost card excluded, and empty hand, zero cost and undefined X.
A three-resource price discount reduces the selected card's payable cost to
zero without changing its highest printed rank or three-point penalty.
The faithful candidate binds `chosen` through discard and reads
`sumPrintedCost: "chosen"` inside that choice. The alternative retained
`doctor-strange-thoughtcasting-receipt-probe.json` discards the correct copy
but `printedCostDiscarded: "game"` reads zero: direct discard does not record
that receipt. This is a limitation of that composition, not a required new
engine feature for Thoughtcasting.

`DoctorStrangeCounterspellCandidateTests`: both reveal-form cases and ordinary
Invocation Special resolution pass. Counterspell attaches to seat 1's hero;
alter-ego reveal discards it unattached. Spell Mastery resolves Winds, draws
three, exhausts the hero and retains Counterspell.

## Counterspell prerequisite / failing probes

The candidate preserves the printed independent AND, followed by Then:

```json
{"then":{"effect":{"and":[{"cancelOccurrence":"trigger"},
  {"discard":"trigger.subject"}]},"then":{"discard":"this"}}}
```

`AttachedSeatEventCancelsAndDiscardsBeforeAttachment` (attached hero and
attached-then-flipped alter-ego), `StunnedAttackEventRemovesStunAndRetainsCounterspell`
and `InvocationResolutionIsNotEventPlayButMasterIs(true)` currently raise:
`'09030' uses 'cancelOccurrence' before dependent text, whose none/partial/full
resolution is not implemented`. The diagnostic is at
`src/Marvel.Cards/Run/AbilityResolutionAdmission.cs:228`, reached from
`AbilityEventInitiation.Open` admission before an initiation frame is created.
`StructuralResolutionOf` combines AND and seq through the same outcome path;
both faithful spellings were probed and rejected. The independent AND variant also reaches runtime `AbilityEffectResults` and raises `has no completed-result semantics for cancelOccurrence`. No sequence weakening or
engine change is included.

The event is moved to `RevealingArea` and its resource/arrow costs are paid
before `AbilityEventInitiation.Open`. Its candidate occurrence has subject
the exact event and player the playing seat; an actual `EventPlayInitiation`
cancellation receipt is created only after waiting candidates are admitted.
The repair must preserve that scope and distinguish cancellation from ordinary
event cleanup. RR `event.2`: "Only the effects are canceled"; costs remain
paid. RR `then`: pre-Then text must be fully true or resolved; `then.1`
requires attempting the following effect only after full resolution. The
printed event discard precedes the attachment discard.

The other-seat negative probe is retained separately as part of the same
whole class; its current result is recorded in the external test log. Do not
claim this trigger scope, stun behavior or Master cancellation verified until
the complete class passes after a separately implemented and reviewed repair.

## Evidence and remaining work

External logs are under `C:/Users/mggar/Source/marvel-strange-final-nemesis-*`.
`compositions.log`: 28 passed / 5 failed across the three candidate classes,
whole `WreckingDiscardedFactsTests`, `EventPlayCancellationTests` and
`AbilityDataTests`. Four failures are the Counterspell prerequisite above;
the other-seat case initially needed its mandatory enemy choice answered, and was corrected before the final run. The final tests
remain deliberate red behavioral probes, not skip-marked passing claims.

Candidate mutants were restored: changing Mordo's extra energy damage 2→1
killed three cases (3 failed / 3 passed); ranking Thoughtcasting by minimum
instead of maximum killed all four tie/form cases (4 failed / 6 passed).
Restored nonincremental Content build and normal pre-commit hooks were run;
the Content build passed with zero warnings/errors and the restored 28-case selection passed. Their logs carry the exact result. No production files were mutated.

Remaining authoring work: repair and independently review Counterspell's
dependent cancellation composition, complete its scope/status/ordering probes,
review these full candidate programs, then separately authorize ability-book
rows, provenance and exact inventory changes. Public admission, setup,
save/restart and whole-pack evidence remain outside this packet.


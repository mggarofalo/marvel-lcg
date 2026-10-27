# Doctor Strange final support rows â€” MARVEL-819

Base `22d678049b5c6b646c7c69453b2d8fe80d57207b`, integrating the independently
reviewed owner-deck, physical status-host and exact upgrade-host prerequisites.
This packet appends actual **09013 Clea**, **09019 The Night Nurse**, and
**09039 Iron Man (Tony Stark, Leadership ally)**: **675 rows, 30/40 Doctor
Strange faces**. All 672 predecessor raw JSON objects are unchanged. SHA-256 of
their concatenated raw object text is
`36a87b708901993b6779b8687dcd40c95adf04c405fab055f625c4893aecdd3aa`.
No engine, setup, source-rule dataset, public admission or UI changes are included.

Authority is the exact generated `datasets/cards/cards.json` record for each face.
Clea's optional Interrupt runs before defeat and shuffles the same physical ally
into her owner's deck. RR 1.8 `rr:ownership-and-control.7.2` sends a departing
foreign-controlled card to its owner's equivalent area, or removes it when that
owner has left the game. The Night Nurse has three medical Uses, pays exhaustion
and one counter, chooses a hero, heals one and discards exactly one physical
status on that hero. Current `rr:and.1` resolves each independent effect as far as
possible. Local `faq:09019` and `ruling:ad2db09c24f9a27f` require discarding Tough
too; that answer agrees with current RR 1.8. Iron Man's constant reduces the cost
to play each upgrade on this exact ally by one, including foreign ownership and
control; RR initiation steps 3â€“5 assess the selected host before payment.
The existing DSL spellings and ordering are engine choices.

`NightNurseStatusHostTests.Board` now uses the actual book instead of appending a
duplicate 09019. All its effect-order, physical-status, last-counter, eligibility,
rejected-answer and paid-prompt query-purity assertions remain. The actual played
support pays one resource and enters ready with three medical Uses. The independent
starting-counter inventory includes the expected medical Uses rather than deriving
its expectation from the new row.

`DoctorStrangeSupportPrerequisiteTests` uses actual Clea for defeat acceptance and
decline under both same and foreign control. Deliberately alternate Night Nurse
programs and unknown-operation probes explicitly replace their production row in
the synthetic fixture. Unknown spellings are still rejected for the specified word,
without weakening duplicate-row admission. Global authored inventories are 675/30;
the Unflappable assertion remains bound to index 671 and its no-counter check.
The complete independent Doctor Strange face set includes all three additions.

`DoctorStrangeIronManCardTests` plays actual Inspired and Enraged through the real
root prompt onto either foreign Iron Man for zero or an ordinary ally for a paid
resource. It checks host-specific prices before payment, invalid free ordinary-host
payment, exact attachment identity, ownership/control and actual printed attack /
thwart bonuses. A second actual-play test proves floor zero, repeated discounts,
two real attachment bonuses together, an unaffected ordinary upgrade and drained
continuations. No empty upgrade programs substitute for production bonuses.

`DoctorStrangeCleaCardTests` compares the accepted actual defeat path with the exact
manually prepared owner-deck shuffle, checking physical deck order, all RNG words,
index and consumption, the unaffected controller deck, one movement and complete
agenda drainage. Decline, actual eliminated-owner and direct nondefeat discard
controls retain their distinct expected outcomes.

## Open engine finding

The actual eliminated-owner **decline** case fails on this base: ordinary defeat
cleanup sends Clea into the eliminated owner's discard, and `PlayerDeck.Reset`
then moves her to that player's empty deck. Expected destination is RemovedArea.
The accepted Interrupt already removes her correctly. The failing probe is
`DoctorStrangeCleaCardTests.DefeatInterruptUsesExactOwnerDeckAndSeededShuffle`
with `accept: false, eliminated: true`; its assertion is preserved. The lead owns
this separate engine repair. This packet is not accepted for integration until
that finding and the final combined checks are resolved.

## Verification

Evidence is outside the repository under `C:/Users/mggar/Source/strange-final-*`:

- `cards.log`: offline generated cards and search match the generator.
- `mutant-clea.log`: a valid actual-row ownerDeck â†’ yourDeck mutant fails the
  foreign-control owner assertion (one failure, eight passes).
- `mutant-nurse.log`: a valid actual-row exhaust-source â†’ exhaust-hero mutant
  fails source exhaustion (six failures, thirteen passes).
- `mutant-iron.log`: deleting Iron Man's Constant leaves a valid empty program
  and fails actual host pricing (five failures). An earlier unsupported-operation
  experiment was rejected at lowering and is excluded from mutation evidence.
- `restored-build.log`: exact candidate book restored, then nonincremental Release
  build succeeds with zero warnings/errors before final affected verification.
- `affected-final.log` and `results/strange-final-affected-final.trx`: whole related
  classes, all Doctor Strange classes, migrated Black Widow inventories, ability
  data/program/lowering and rule citations; final outcome is recorded below.

Focused tests and mutations do not establish full Content, native, second-platform,
complete-pack or public-product acceptance. Normal commit hooks are retained.

Final bounded verification: **243 passed / 244 total**, with the sole failure
being the preserved eliminated-owner decline finding above. Both inventory
corrections pass. No assertions were removed to accommodate that failure.


## Final defense, obligation and hosted Invocation rows

Base `3df6bdf0`, branch `feat/strange-last-supports`, adds actual 09015 Desperate
Defense, 09027 Physical Toll and 09029 Open the Dark Dimension. The book has 682
rows and 37/40 Doctor Strange faces; 09028, 09030 and 09031 remain unauthored.
All 679 predecessor raw objects, including their whitespace, remain unchanged.
The complete predecessor prefix before the array closing delimiters is preserved
byte for byte, SHA-256
`b03718b04c8dde86d212820197abe739f56b9f0bc61d7364a4eb941c5b62bf37`.
No engine, Runner, Agenda, generated card, setup or public admission code changes.

Printed authority is the exact generated datasets/cards/cards.json records.
Desperate Defense is a cost-one Hero Interrupt (defense) for your own defending
hero. Its +2 DEF expires with that exact attack; the delayed ready tests actual
damage from that attack and the original hero incarnation, respecting prevention
and readiness prohibition. RR 1.8 defend-defense.2 and delayed-effect.1 govern;
no current conflicting ruling was identified. The independently accepted
prerequisite and review are linked in
[the defense review](doctor-strange-desperate-defense-independent-review.md).

Physical Toll follows the printed optional alter-ego flip and Stephen-only
exhaust/remove choice, or a persistent +3 next-played-event increase. RR 1.8
obligation.1/.4 binds the associated player, initiating-abilities.step.4 applies
modifiers before payment, and event.2 preserves paid costs and event play despite
canceled effects. Historical ruling 277ab401b1269ec7 distinguishes resolving an
Invocation Special from playing an event; current printed Spell Mastery and event
rules agree. Master of the Mystic Arts is itself played and consumes the increase.
The [independent surcharge review](doctor-strange-physical-toll-review.md) supplies
accepted prerequisite evidence. Source and paid-event receipts retain independent
incarnations and cannot discard a reentered obligation.

Open the Dark Dimension stores the exact physical top Invocation facedown. RR 1.8
in-play-and-out-of-play.10 conceals hosted facedown cards; when-defeated-abilities.2.1
returns and shuffles the card before host departure. Ordinary nondefeat departure
uses discard cleanup under attach-to.1, including the Invocation deck's own
immediate recycle if empty. The [hosted prerequisite and independent review](doctor-strange-hosted-invocation-prerequisite.md)
cover exact ownership, separate discard preservation, multiple synthetic hosts,
all-five-hosted return and seeded RNG. No card-specific FAQ entry supplies a
contrary instruction. DSL spelling and receipt storage are engine choices.

PhysicalTollFixture.Runner and DesperateDefensePrerequisiteFixture.Runner now load
the canonical complete book. HostedInvocationFixture uses the canonical program
for choice:false; choice:true explicitly replaces 09029 with its documented
synthetic draw-choice continuation. The queued-attack independent defense test
replaces only synthetic Armored Vest and uses canonical 09015, preserving the
full original/queued-attack assertions and duplicate-row rejection. Existing
behavioral assertions remain; inventory negatives use still-unauthored nemesis
faces. All explicit authored-face inventories include the three new rows.

HostedInvocationPrivacyTests now runs actual 09029 reveal and defeat through the
scheduler instead of calling its primitives. It preserves four-policy assertions
for hidden physical IDs, faces, locations, state, persistent metadata, event
subjects and shuffled order, while keeping the public top readable. Actual
nondefeat discard and recycle controls remain in the whole hosted classes.

Verification logs are outside the repository, under C:/Users/mggar/Source/ with
prefix strange-last-. The three actual-row lowered-program mutants reduce the
DEF bonus from two to one, reduce the surcharge from three to two, and omit the
When Defeated return. Each runs through the canonical book; exact results and
restored verification follow below. Public setup, save/replay, native UI,
complete-pack acceptance, full Content and cross-platform certification are not
claimed by this packet.


Final verification of this packet (Release, Windows, serialized PTY execution):

- Actual-row mutants are killed: `strange-last-mutant-defense.log` fails four of
  ten cases on the smaller DEF grant; `strange-last-mutant-toll.log` fails eight
  of nine on the smaller quoted/paid surcharge; `strange-last-mutant-host.log`
  fails two of three on wrong physical return/deck contents. These mutate only
  interpreted JSON rows and run the lowered canonical programs, not engine source.
- The exact original ability bytes are restored in a finally block. The subsequent
  nonincremental Content build succeeds with zero warnings/errors:
  `strange-last-restored-build.log`.
- Whole affected Content classes plus Doctor Strange, Black Widow, defense,
  cancellation, payment, obligation, Invocation, hosted/captive, ability-data and
  citation controls execute 650 cases: 649 pass and one fails solely for missing
  `invocationDeck` vocabulary documentation (`strange-last-restored-content.log`).
  The documentation is then added; both whole ability-data classes pass all 33
  cases (`strange-last-ability-data-final.log`, filter `FullyQualifiedName~AbilityData`).
  No behavioral source changes follow the combined run. This is the recorded
  combined result and doc-only repair, not a claim of a separate 650-case green run.
- Whole hosted privacy/Invocation visibility and citation classes pass all 38
  cases (`strange-last-restored-view.log`), including actual-row four-scope secrecy.
- `strange-last-cards.log` confirms cards.json and search.json match the offline
  generator. The initial test runs exposed stale authored inventories and unknown
  face controls; those inventory-only repairs are included in the restored run.
- Whole InvocationDeckTests, InvocationDeckBoundaryTests, ContinuousEffectTests,
  CardPlayAnOmittedCostTests and citation checks pass all 52 cases
  (`strange-last-rules.log`). Normal enabled commit-hook outcomes accompany the
  final handoff. No full Content, full solution, native or platform certification
  is implied.

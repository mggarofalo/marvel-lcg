# Hulk signature rows — MARVEL-846

Source base: `fc7a4a0aecb3a93362e23f61e3e13fa51c48ebe8`; isolated branch
`feat/hulk-signatures`. This bounded packet appends exactly seven actual research
faces: 10002 Crushing Blow, 10003 Hulk Smash, 10006 Unstoppable Force,
10007 Limitless Strength, 10008 Banner's Laboratory, 10009 Boundless Rage and
10010 Immovable Object. The book grows from 693 to 700 rows and Hulk coverage
from 8 to 15 of 33 faces. All 693 predecessor raw objects are preserved.
There are no starting counters, engine, setup or public-admission changes.

## Authority and interpretation

Authority is the joined printed records in `datasets/cards/cards.json`, current
RR 1.8 and the vendored rulings. The FAQ queries cover all seven faces but
supply no individual FAQ entries. Printed names, prices, traits and resource
icons remain catalog facts, including Hulk Smash's Superpower-only trait and
Limitless Strength's RRR.

- Crushing Blow uses the P1 adjusted printed-price restriction and current
  modified controller hero ATK. `rr:initiating-abilities.step.4` applies cost
  modifiers before payment; `rr:wild-resource.1` makes declared type decisive.
- Hulk Smash uses the P2 exact basic-attack occurrence for both +10 and overkill.
  Current `rr:overkill.1` requires actual minion defeat. Neither grant persists
  to the character or a later attack. A physical-only receipt requires at least
  one physical paid and no mental, energy or wild paid. `rr:cost.4.1` excludes
  unused generated icons from payment.
- Unstoppable Force has two independent sentences. Its conditional draw does
  not require a successful ready; the printed text contains no “then”.
  `rr:target.3.4` allows the effective draw to support initiation when Hulk is
  ready. An explicitly mixed allocation that leaves no effect rejects before
  commitment even if another available wild declaration could qualify.
- `ruling:5bbba476a198a90c` directly permits zero-price Crushing Blow without
  physical payment and directly denies the draw for zero-price Unstoppable Force.
  It is tagged pre-1.5, but agrees with current cost/payment rules. Applying its
  nonempty physical-payment requirement to Hulk Smash's identical condition is
  explicitly an inference, not a Hulk Smash-specific ruling.
- Limitless Strength uses reviewed Q3 `handResourceForm: "hero"` metadata.
  It grants no Resource ability and never erases its printed triple physical.
- `rr:identity.2` makes Bruce Banner's +2 REC and Hulk's ATK/retaliate bonuses
  refer only to those named faceup identities, even across source controllers.
  The Laboratory resource belongs to its controller and requires alter-ego form.
- Boundless Rage's hero-only play comes from printed catalog metadata. Its
  form-independent Forced Response observes its controller's form change and
  discards this physical upgrade after the change. Immovable Object's +4 HP
  follows its controller's identity in both forms; only retaliate names Hulk.

The JSON spellings, branch composition and explicit face inventory are repository
choices using existing reviewed mechanisms.

## Thunderclap remains pending

10005 is deliberately unauthored. It says “Choose up to 3 different enemies.
Deal 3 damage to each of them.” This is ordinary damage, with one distinct group
chosen before resolution. The current DSL has single `chooseCard`, bounded
`chooseStatusRecipients` for statuses, and counted `attack` powers whose
`AbilityPowerLowering.AttackCount` requires simultaneous `AttackDamage` over
`powerTargets`. `AbilityDamageLowering.DamageEffect` has only cards, amount,
attack and ignoresTough fields. None supplies this ordinary-damage subset.
An attack workaround would add rules the printed card does not have. A separate
bounded engine prerequisite and review must supply group selection before the
eighth signature face can be authored. This packet does not complete MARVEL-846.

## Evidence

Actual canonical-book tests are `HulkSignatureEventTests`, `HulkSmashCardTests`
and `HulkSignaturePermanentTests`. The Basic reprint inventory retains its exact
five-row slice and includes the new seven faces in the total inventory.
Existing prerequisite synthetic definitions remain explicitly synthetic and are
not appended to the full book, preserving duplicate-row rejection.

Focused Release verification, mutation results and raw-prefix evidence are
recorded below after restoration. Logs stay outside the repository at
`C:/Users/mggar/Source/hulk-signatures-*.log`. Independent review and combined
pack integration are separate from this source evidence.


Restored Content-project Release build passes with zero warnings/errors using
`--no-incremental`. The affected filter passes **183 tests**, covering all Hulk,
AbilityData, PhysicalPrintedPrice, AttackEventResource, BasicAttackOverkill,
HandResource and RuleCitation cases. This is focused integration evidence, not a
full Content-suite or complete-pack certification.

Twelve distinct compiled data mutants are killed: remove physical-only price
(2 failures); replace current ATK with printed 3 (2); +10 to +9 (1); allow empty
Smash payment (1); allow empty Force payment (1); replace named Hulk ready with
controller ready (1 on added cross-controller case); remove Limitless form
restriction (1); reduce Lab REC (1); swap Lab resource form (1); broaden Rage
form-change ownership (1); reduce Object health (1); and apply Object retaliate
to its controller instead of named Hulk (1). Each mutant log contains a successful
build and intended assertion failures. The controller-ready mutation initially
survived the own-Hulk fixture; the added multiplayer case kills it. It is not
counted as killed by the earlier fixture.

A raw JSON-object comparison against the source base verifies all **693**
predecessor objects byte-identical in order. The 700-row ability-book SHA-256 is
`a94ffc655319c353c5d60c80bd59c7539b106925a381b4b778856cb547743ec4`.
Normal commit hooks and clean worktree evidence accompany the source commit.

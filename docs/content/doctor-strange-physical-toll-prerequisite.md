# Physical Toll event-cost prerequisite

MARVEL-831 supports MARVEL-825. Base: `1abb96a2`; branch:
`feat/physical-toll-event-cost`. This is an engine prerequisite and a synthetic
actual-text candidate, not production ability-book admission or pack completion.

Printed authority is generated `datasets/cards/cards.json` 09027:
give to Stephen Strange; optional alter-ego flip; exhaust Stephen and remove, or
the next event costs three additional resources and the obligation is discarded
after that event. No 09027 FAQ entry exists in the vendored queried FAQ corpus.
`ruling:277ab401b1269ec7` distinguishes resolving Invocation Specials from
playing a card. Current RR `rr:event.2` says canceled event effects still leave
the event played and costs paid. `rr:initiating-abilities.step.4` applies modifiers
before payment; `rr:obligation.1` and `.4` bind the named player.

The existing optional-form and exhaust/remove instructions are reused. Existing
next-card reductions expire with a phase and cannot express this surcharge.
The new bounded `increaseNextEventCost` instruction only accepts an obligation
in the resolving player's play area. `NextEventCost` stores a one-use surcharge
in `World.Effects`, bound to that player's identity and the source incarnation.
`CardPayment` adds it before discounts and before affordability/payment checks.
Event payment reserves the exact applied surcharge in a completion receipt and
spends it before effects, so a nested event cannot pay it again. Common event
discard completes the receipt after normal or canceled effects, discarding only
the original source incarnation. It is not a triggered Forced Response.

These are ordinary deterministic continuous-effect records, reconstructed by
accepted decisions, with no process-owned mutable table or live-card delegate.
The v3 card digest intentionally does not serialize the continuous-effect list;
no digest keys, card allocation, RNG or format are changed. Tests compare the
effect records, RNG, agenda and card digest, and compare queried/unprobed rebuilt
boards' completion events. Public session/save admission remains outside this
nonpublic candidate.

`PhysicalTollFixture` contains the reviewable 09027 candidate for a future book
author. Tests exercise the full encounter reveal from the other seat, named
association, optional flip/exhaust/remove, retained hero form and persistent
surcharge, repeated pure queries and phase expiry, insufficient/sufficient
combined payment, production Master of the Mystic Arts, Spell Mastery Special,
actual Swinging Web Kick with/without stun, reconstructed query controls and
source departure/reentry before application and after receipt reservation.

Evidence logs are outside the repository at
`C:/Users/mggar/Source/physical-toll-*.log`. Independent review and actual ability
book integration remain separate.

Validation: the nine new tests passed. A compiled mutant that omits the surcharge
addition failed four of them (including expected cost 4 versus actual 1).
Sources were restored and rebuilt with `--no-incremental`; the restored affected
Content classes and citations passed. The exact selected classes are
PhysicalTollCostTests, EventPlayCancellationTests, CoreLastingEffectCardsTests,
DoctorStrangeInvocationCardTests, SpellMastery*, MasterOfMysticArts*,
ObligationTests, CoreObligationChoiceMeaningTests and RuleCitationTests.
The original Rules filter named `CardPlayTests` matched only the two citation
checks; it did not exercise `CardPlayAnOmittedCostTests`. Independent review
corrected that coverage gap: the actual whole class (16 cases) and Rules
RuleCitationTests (2 cases) passed. See
[the independent review](doctor-strange-physical-toll-review.md) for the exact
restored run, additional boundary cases and compiled receipt mutant. Normal
pre-commit hooks are enabled. Broad Content/solution/platform/native/public-session
acceptance is not claimed by this prerequisite packet.

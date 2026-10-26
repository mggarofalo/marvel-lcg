# Thor executable authoring handoff — MARVEL-563

Assessment base: `69459de2` (2026-10-26). This is a source/code assessment,
not executed Thor coverage. All **35 playable faces** are enumerated below;
none has an actual row in the ability book at this base. Printed authority is
[`datasets/cards/cards.json`](../../../../datasets/cards/cards.json).
The [face inventory](563-568-inventory.md) supplies quantities and the
[starter source review](thor-starter-source.md) settles composition: forty
player cards `06002–06025`, identity `06001a/b`, obligation `06026`, nemesis
`06027×1, 06028×1, 06029×2, 06030×1`. Customization `06031–06034` remains in
complete-pack scope. Do not reopen the resolved divider question.

**Recommended next implementation:** after the engagement correction receives
independent review, author the eleven-face batch A below. It needs no new engine
mechanism and makes real ability-book progress without pretending Thor is admitted.
Keep research setup and public product admission closed until complete coverage.

## Status vocabulary and inspected contracts

**A** means an actual row can be authored with the inspected existing contract;
its acceptance example still needs an actual-row test. **C** means the necessary
operations exist, but their specified composition needs a focused nonpublic
behavior probe before committing the row. **P** names a concrete engine/DSL gap;
do not approximate that text with an effect sequence. **G** is the independently
reviewed engagement prerequisite, not a fresh implementation request.

Code paths checked, rather than inferred from primitive names:

- `AbilitySelectorLowering.InAreas`, `AbilitySelectorEvaluation.CardsIn` and
  `AbilitySearchDescription`: multi-area selectors support deck/discard, title,
  kind and trait filters. Use `chooseCard` for multiple matches; singular `Find`
  throws when several match. A no-match branch must still shuffle the deck.
  `CaptainAmericaSetupTests` demonstrates the named deck/discard search and
  unconditional shuffle. The specialized description currently recognizes only
  deck-only trait searches: verify useful deck-plus-discard wording in the row
  tests and extend the engine description if its generic result is insufficient.
- `AbilityCostLowering`, `AbilityCostPreparation`, `AbilityCostPayment` and
  `AbilityPaymentPricing`: named exhaustion exists; chosen exhaustion accepts
  only hero/ally/controlled-character queries. Deck arrow costs accept fixed
  `discardTop`, not discard-until/entry. `CombinedPrice.TryCombinedAmounts`
  rejects a variable arrow price combined with a printed event price.
- `AbilityExpressionEvaluation.PaidWith` reads the resolution payment and the
  source's `paid:<type>` effects; this is a real entry-response route, not just
  an event-only conditional. Actual Valkyrie entry still needs a receipt probe.
- `CardPayment.CostOf` reads lasting card-cost reductions and first-ally history;
  there is no self-priced in-hand constant proportional to engaged minions.
- `AbilityEncounterInspection` exposes top cards with a single acknowledgement;
  it leaves their order unchanged and records a treachery count. It cannot
  implement Heimdall's discard-and-order instruction.
- `AbilityModifierLowering` supports `target_threat` and end-of-attack duration;
  `AbilityEffectLowering.AttackDamage` accepts `overkill`. `DamageRequest` has
  no ignore-tough flag, and the ordinary DSL damage path has no such option.
- `AbilityDiscardIterationExecution` retains exact discarded physical cards for
  `forEachDiscarded`; `that` survives immediate encounter resets. The would-be-
  defeated interrupt procedure resumes after its abilities; existing Helmet and
  `WouldBeDefeatedTests` establish the health-change route. This permits a Loki
  composition probe, not an assertion that Loki is already covered.

## Every face and its distinguishing acceptance example

| Face | Card | State / route | Required actual-row observation |
|---|---|---|---|
| `06001a` | Thor | **G**, E0: `WhenMinionEngaged`, `player:you`, response, per-phase limit | Another seat's engagement draws zero; Thor's real transfer offers two; a second engagement in the phase draws zero; both phase changes reset usage. Use corrected Quickstrike order. |
| `06001b` | Odinson | **A**: deck/discard Mjolnir choice, add-to-hand, shuffle, `limitPerRound:1` | Find the physical `06009` in either zone; never take another player's copy or an in-play copy; a missing match still shuffles; form changes do not refresh use. |
| `06002` | Lady Sif | **A**: self entry response, ready controller's identity | Ready Thor and Odinson in separate cases; a second player's matching identity stays exhausted; declining leaves the identity exhausted. |
| `06003` | Defender of the Nine Realms | **P**, E1: discard-until plus minion entry is the **arrow cost** | No scheme able to lose threat means no paid event; successful cost enters the exact milled minion, without When Revealed, before removing three threat; reset cannot lose the physical match. |
| `06004` | For Asgard! | **A**: filtered multi-area `chooseCard`, add-to-hand, shuffle; alter-ego event | Offer Asgard cards in deck and discard, exclude non-Asgard; choose a non-first physical match; pay printed one once; no-match shuffle still happens. |
| `06005` | Hammer Throw | **C**: `exhaustTitled:Mjolnir`, attack damage eight with `overkill`, return named upgrade as effect | Ready Mjolnir is required before paying three; eight against a five-HP minion spills three; Mjolnir returns after damage, ends ATK/Aerial, and remains returned if the enemy is defeated. Test suspended defeat responses and stun cancellation. |
| `06006` | Lightning Strike | **P**, E2 then E3 | Printed one is separate from X energy; an Aerial strike damages villain and Thor's minions while preserving their tough cards; another seat's minions and attack-triggered responses are excluded. |
| `06007` | Asgard | **A**: constant `hand_size +1` to controller | Modified hand size increases in either form, changes refill, and drops on departure; another player is unaffected. |
| `06008` | God of Thunder | **A**: hero-only Resource, exhaust source, fixed `Y` | Produces exactly one energy in hero form; absent in alter-ego; exhausted source cannot pay again, including generator suspension/retry. |
| `06009` | Mjolnir | **A**: named Thor constant attack +1 and Aerial; printed Restricted | Thor gets both while Odinson gets neither; exhaustion preserves bonuses, departure ends both; third Restricted play uses ordinary restriction handling. |
| `06010` | Thor's Helmet | **A**: constant controller hit-points +5 | Fourteen maximum becomes nineteen in both forms; leaving at low remaining health settles through ordinary lost-health/defeat work. |
| `06011` | Hercules | **P**, E4 | With zero/two/seven own engaged minions cost is six/four/zero; other seats' minions do not reduce it; repeated pricing consumes nothing. |
| `06012` | Valkyrie | **C**: entry response plus `paidWithResource:Y` | Ordinary ally play with energy deals three, without energy two; wild declared energy qualifies; put-into-play without payment deals two; leaving/reentering cannot reuse an old paid marker. |
| `06013` | Chase Them Down | **A**: copy reviewed `01052` behavior under new ID | Hero basic or labelled attack defeating an enemy offers two threat removal; an ally defeat or a surviving enemy does not. |
| `06014` | Get Over Here! | **C**, E0: attack one, then conditional Aerial transfer | Full event payment/attack path, not synthetic direct effect: Aerial transfer from another seat triggers Thor once; own minion still takes one but does not re-engage; lethal damage leaves no transferable enemy. |
| `06015` | Mean Swing | **P**, E5 | Only a hero basic attack offers it; choose and exhaust one ready attached Weapon; +3 applies to this attack only; no Weapon or already exhausted Weapon leaves payment/state untouched. |
| `06016` | The Power of Aggression | **A**: reviewed `01055` resource multiplier | Two wild icons for an Aggression card's printed cost, one for another aspect or a standalone ability cost; Max 2 remains deck construction data. |
| `06017` | Hall of Heroes | **A**: defeat-credit response, glory counters; alter-ego exhaust/remove three cost | A credited non-attack upgrade defeat earns glory; another player's defeat does not; two glory cannot pay, three pays once and draws three. Check ally-credit cases against `rr:you-your`, not an assumed hero-only restriction. |
| `06018` | Battle Fury | **C**: transfer-control play metadata plus attack-defeat response, `takeDamage:you`, discard-source costs, ready | Play under seat B; only B's hero attack defeating a minion triggers. Tough/prevention cannot pay the damage cost; source is not discarded and hero is not readied on a rejected cost. Preserve max one per player. |
| `06019` | Jarnbjorn | **A**: hero `WhenAttackEnds`, typed physical arrow cost, choose enemy/direct damage two | Basic and labelled attacks trigger; ally attacks and Lightning Strike do not; the second enemy may differ; physical-declared wild pays; the resulting defeat is credited without recursively becoming an attack. |
| `06020` | Heimdall | **P**, E6 | Inspect top three privately, discard chosen middle card, return two in chosen order; reject duplicate/stale/foreign IDs atomically; zero/one/two remaining deck cards must follow ordinary look/discard/reset rules. |
| `06021` | Invulnerability | **A**: hero Action, choose own hero tough status | Pay three and place one tough; already tough prevents a no-effect play; another identity is not offered. |
| `06022` | Energy | **A**: explicit empty abilities; printed `YY` | Actual Thor ID generates two energy; same-title Max 1 deck rule still holds across reprints. |
| `06023` | Genius | **A**: explicit empty abilities; printed `BB` | Actual Thor ID generates two mental; no text-box resource ability is invented. |
| `06024` | Strength | **A**: explicit empty abilities; printed `RR` | Actual Thor ID generates two physical; no text-box resource ability is invented. |
| `06025` | Avengers Mansion | **A**: reviewed `01091` exhaust/identity-choice/draw | Choose seat B and only B draws one; invalid target or exhausted source cannot commit. |
| `06026` | Odin's Anger | **C**: existing obligation flip/option machinery; discard selector across own hand/play | Delivery follows Odinson owner at seat B; optional flip then ready Odinson branch exhausts/removes; other branch discards Mjolnir from either legal location, stuns and discards obligation; absent Mjolnir does not silently remove remaining effects. |
| `06027` | Family Feud | **P**, E7: complete in-play Asgard census | Include both identities, another player's Asgard ally/upgrade/support and Loki; exclude hand/discard/set-aside/facedown out-of-play cards; add the count after ordinary printed starting threat. |
| `06028` | Loki | **C**: self Forced Interrupt at would-be defeat; exact one-card discard receipt, treachery conditional heal-all | Treachery saves/heals the same incarnation, non-treachery permits defeat; deck reset keeps the examined receipt; healing is mandatory and suppresses defeat-credit responses. Check overkill against current authority before freezing expected spill. |
| `06029` | Frost Giant | **C**: printed Toughness plus existing Boost delayed-stun route | Entry gains tough; scheme boost never installs stun; completed villain attack with prevented damage stuns nobody, damage to ally/identity stuns that recipient; later unrelated damage cannot consume the delayed effect. |
| `06030` | Trickster | **A**: `discardTop:yourDeck,count:3`, `distinctCardTypesDiscarded:game` threat | Ally/Event/Event yields two threat; three same types one; payment discards do not join receipt; player deck reset preserves discarded-card facts. |
| `06031` | Under Surveillance | **A**: main-scheme attachment, constant attached `target_threat +4` | Threshold increases by four total in two-player game, not four per player; one per scheme; departure restores it and ordinary completion checks apply. |
| `06032` | Teamwork | **P**, E5b after E5 | Exhaust chosen own ally; add its **modified** matching ATK/THW to this one basic use, without ally consequential damage; event attacks never offer it. Keep captured ally identity through a suspension. |
| `06033` | Second Wind | **A**: identity choice/heal four or five from event paid mental receipt | Heal another player's alter-ego as well as hero; mental-declared wild selects five, no mental selects four; no damaged identity prevents a no-effect play. |
| `06034` | Enhanced Physique | **A**: reviewed `03034` uses/resource shape with physical type | Starts three physical counters; exhaust/remove one generates physical in hero form; final counter discards source yet committed resource completes; no alter-ego offer. |

## Smallest prerequisite packets and boundaries

**E0 — complete and independently review engagement correction first.**
`69459de2` supplies physical subject, destination seat, no same-seat engagement,
and entry/transfer continuation, demonstrated by synthetic `ThorEngagementTests`.
Independent review `dd609c45` found interpreted transfer offered Thor's optional
response before pending Quickstrike. `rr:quickstrike.1` defines the keyword as a
forced response; `rr:ability.step.4` orders forced responses before optional
responses. Correction `64448751`, independently approved in `67314c81`, finishes
Quickstrike before Thor's response and retains the surrounding interpreted
continuation through nested choices or decline. The next delivery still needs
replay-v26 compatibility proof because real Core Hawkeye ordering also changed.
Using the reviewed correction,
prove actual `06001a`/`06014` rows and replay/save across suspended engagement,
including per-phase usage and destination seat. No new starter research needed.

**E1 — one resumable arrow-cost discard-until-and-entry mechanism (`06003`).**
Own `AbilityCost` lowering, whole-cost validation and receipt/continuation; reuse
encounter discard and corrected minion entry. It is not valid to put these two
instructions at the front of an effect: they pay the arrow cost. Do not search
hidden deck contents during offer generation or require a known minion. Resolve
the no-minion/deck-exhaustion case from `rr:cost` and encounter-deck authority;
test it without guessing completion. Choose legal threat removal and preflight
before costs; cost suspension must resume once, and invalid answers retain state,
RNG and pending work. Test minion unable to enter and a Quickstrike minion.

**E2 — variable typed arrow allocation with printed event price (`06006`).**
Extend combined pricing/payment, not a Lightning-Strike-only manual debit. Retain
separate generic printed-one and X-energy components; wild declarations and
generators cannot pay both with one allocated icon. Existing standalone
`spendEnergyX` has positive-X bounds; settle permitted zero-X initiation against
effect-significance rules. Test printed-price discounts, insufficient energy,
overgeneration, generator suspension, rejection and replay. This packet can prove
payment with a synthetic safe effect while E3 remains closed.

**E3 — non-attack damage that ignores tough (`06006`, after E2).**
Add an occurrence-bound damage property through live execution and effect
preflight/projection; do not remove tough or label damage as an attack to borrow
piercing. With Aerial, tough remains and damage lands; without Aerial, ordinary
tough cancels damage and is discarded. Other prevention still works. Damage
recipients are the villain and own engaged minions; each is processed correctly
through defeat windows and suspended choices. No retaliate or attack responses.

**E4 — in-hand self play-cost reduction (`06011`).**
Add a read-only pricing hook for this physical candidate using its future
controller's current engagement count; preserve discounts, floor zero,
uniqueness, ally-limit admission and final payment revalidation. A generic live
constant attached to an in-hand ally is not a supported substitute. Test a second
seat, a changing minion count, query purity and rejected payment. Winter Soldier
is a later consumer, not part of this packet.

**E5 — basic-use interrupt plus selected attached Weapon cost (`06015`).**
Existing `WhenAttackInitiated` includes card attacks, and `WhenBasicAttackEnds`
is too late. Provide an explicit basic-use fact/window usable before damage,
choose a ready Weapon upgrade attached to the acting hero, and apply +3 until
this use ends. `exhaustChosen` currently cannot select upgrades; merely broadening
its relation without attachment/control/trait validation is incomplete. Test an
event attack, ally attack, wrong host and form as negative controls.

**E5b — selected ally's matching modified power (`06032`, after E5).**
Reuse the basic-use occurrence; extend it to thwart and expose which power is
being used. Existing `exhaustChosen:alliesYouControl` can pay, but only a numeric
count/ID cost receipt is insufficient to author “that ally's matching power”:
provide an incarnation-bound selected-cost-card value and the modified matching
field. Test two differently modified allies, ATK versus THW, suspension, and
expiration. Do not add two identical offers or infer the current power in UI.

**E6 — bounded private inspect/discard/order (`06020`).**
Reuse prompt ownership/visibility, but introduce a physical inspected group,
one-card discard and a validated permutation of survivors. Freeze membership
across suspension, preserve seeded deck-reset behavior, and provide an engine
contract the client can explain. This is a new interaction: add relevant
View/Decisions/Protocol/native evidence after engine tests, and prepare focused
owner review if its working UI is materially new. An acknowledgement-only look
is insufficient even if top cards are visible.

**E7 — complete in-play card selection (`06027`).**
Existing queries cover characters, supports, owned cards, and enemies; there is
no general all-in-play-card relation. Add one visibility-safe engine census with
the existing game-area semantics and trait filter, then count Asgard once per
physical card. Avoid summing overlapping ad hoc collections, or iterating every
world card without excluding out-of-play areas. Test hosted upgrades, another
seat, Loki and an out-of-play identical control. Ordinary unmodified starting
threat remains setup's responsibility.

## Next real-row batch and proof order

Batch **A** owns only eleven new rows plus focused tests: `06001b, 06002,
06007, 06008, 06009, 06010, 06013, 06016, 06022, 06023, 06024`.
It deliberately mixes the immediately useful Odinson/Mjolnir/constants slice
with three exact resource reprints and two admitted behavior reprints. No
ability-book-wide allowlist expansion and no public Thor admission. Do not count
empty resource rows as unimplemented cards: printed resources are the behavior.

Use the existing nonpublic `CoreCardTestBase`/local-book seams for probes while
the pack is incomplete, then load the **actual authored book** for row tests.
`CaptainAmericaSetupTests`, `CoreSearchDescriptionTests`, `PowerOfAspectTests`,
the existing `03009` constant tests and `03034` uses tests are behavioral
references. Do not copy their assertions without substituting Thor physical IDs,
forms and opponent controls. Cover no-match shuffle, duplicate-title selection,
named-Thor form restriction, per-round use and payment query purity. Inspect
search offer wording as well as resulting movement.

Then take C rows in small thematic packets: Hammer Throw with Mjolnir;
Valkyrie with paid-entry receipt; full Get Over Here after E0; Hall/Jarnbjorn/
Battle Fury credit and damage-cost composition; obligation/nemesis with Loki's
forced save. A failed composition probe becomes the narrow prerequisite it
demonstrates, not permission to claim primitive support. In particular, run
projected labelled attacks into Loki: the preflight may intentionally reject
unresolved forced defeat behavior and needs a safe narrow extension if reached.

For every data batch run `AbilityDataTests` (including explicit face and starting-
counter inventories), focused actual-row behavior and rule-citation gates; kill
a behavior-changing compiled mutant for each new decision. Documentation-only
assessment ran no gameplay tests. At complete admission, author the pinned
recipe, update research setup/expansion authority inventories and behavior catalog
checks, then prove production setup, legal custom decks, removed-required-face
negative control, characteristic Thor actions and save/restart on the final
combined revision. Preserve adjacent unsupported-pack exclusions. Reuse managed,
platform and packaging gates; do not advertise early rows as a playable pack.

## Ruling checks retained for implementers

Vendored MarvelCDB FAQ entries exist for `06006` (damage, not attack), `06014`
(no same-seat engagement), `06018` (fully paid damage cost), `06019` (basic and
labelled attacks; upgrade defeat credit), and `06028` (Forced Interrupt).
No other `060xx` FAQ entry is present at this base. The rulings corpus also
contains Loki/overkill and Teamwork/modified-power examples: reconcile those
against the current Rules Reference and provenance ordering before asserting
timing. In particular, older Loki overkill answers are not a substitute for
checking current `rr:overkill`. Generated corrected text remains the authoring
source; the research inventory and external component images do not supply card
behavior. E0's independent correction resolves its cited timing concern separately.

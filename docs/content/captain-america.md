# Captain America implementation and evidence ledger

Work item: MARVEL-560. Product code: `cap`. This is a work-in-progress ledger,
not an admission or acceptance record. The complete pack remains unsupported until
all required mechanics, content, client journeys and compatibility checks pass.

## Authorities and component reconciliation

Printed behavior comes from `datasets/cards/cards.json` (35 faces, 03001a/b and
03002–03034). Acquisition-only composition comes from
`datasets/marvelsdb/pack/cap.json`. The latter has 30 records with quantity sum54,
including both sides of the one physical identity, and omits obligation/nemesis
records supplied by the joined dataset. Therefore 54 is **not** the physical pack
count. Reconcile the remaining physical quantities and supplied starter deck
against the official rulesheet before creating executable setup; do not infer
physical completeness from the 35-face count.

The official insert is vendored at `datasets/rules-packs/mc04/leadership.md`, with
source pin in `sources.manifest.json`. Its extracted paragraph describes play but
does not itself enumerate the complete contents or starter deck. This source gap
blocks component acceptance until the pinned PDF's printed list is checked.

FAQ entries under `datasets/marvelcdb-faq/faq.json` for 03001a, 03006, 03024,
03025, 03032 and 03033 are material. In particular: Shield setup cannot retrieve
an Ultron Drone; Living Legend cannot discount a later ally after a hero-form
ally play; Shield Toss is simultaneous; Honorary Avenger needs the current form's
trait; Followed is an Interrupt, before the defeated scheme leaves play; Expert
Defense follows actual basic defense declaration before boost reveal. Reconcile
rules-graph relationships before claiming these as resolved authorities. The
Hall of Heroes Shield Toss control clarification also requires the Shield to be
controlled by the paying player.

## Per-face obligations

Every entry below is an implementation obligation. Unless named in the evidence
section, its checks are planned, not executed or satisfied by similar Core cards.
Reprints need pack-face admission and representative execution, not empty rows.

| Face | Required distinguishing observations |
|---|---|
| 03001a Captain America | Discard exactly one held card before ready; no ready effect if already ready/no legal change; no hand, wrong hand and duplicate payment reject; once per round including change-form loops; round reset. |
| 03001b Steve Rogers | Search deck and discard for Shield after scenario setup/mulligan; shuffle deck; already-held/Drone/unavailable Shield not retrieved; Living Legend affects first ally played globally each round, only while active; prior hero-form ally consumes first position; put-into-play does not. |
| 03002 Agent13 | Optional enters-play response removes2 threat from legal chosen scheme; decline; empty scheme; crisis and cross-player timing. |
| 03003 Fearless Determination | Hero-only +1THW through phase then expires; draw1 even where modifier has no immediate use; cumulative copies; deck exhaustion. |
| 03004 Heroic Strike | Attack6; physical or wild-as-physical payment stuns same surviving enemy; no physical no stun; tough/stunned/guard/steady/stalwart and defeated target boundaries. |
| 03005 Shield Block | Exhaust controlled Shield before prevent-all imminent damage; exhausted/missing/other-controller Shield rejects; nonattack damage versus defense label; no effect beyond this occurrence. |
| 03006 Shield Toss | DiscardX and return controlled Shield; distinctX enemies; simultaneous4 damage; Guard forbids villain even when guard dies; target/count/payment rejection atomic; X and stun boundaries. |
| 03007 Steve's Apartment | Alter-ego-only exhaust, draw1 and heal1; undamaged still draws; exhausted rejects; owner binding and deck exhaustion. |
| 03008 Helmet | Optional imminent Captain America defeat replaced withHP1; discard Helmet; no substitution for other character/form; defeat from damage and changed maximumHP; nested interruption and replay. |
| 03009 Shield | Restricted; +1DEF and retaliate1 on Captain America; changes with active form/control/blanking/departure; stacked retaliate and exhausted Shield. |
| 03010 Serum | Exhaust for physical; available as resource while resolving eligible payments; no double-use; leaves play/cancel/replay boundaries. |
| 03011 Falcon | Private look at top3 encounter cards preserves order;0..3 treacheries each permits1 threat removal; deck-shortage/empty, multiple schemes and crisis; no hidden data leak or unauthorized reorder. |
| 03012 Hawkeye | Reprint of Core ally: four non-Uses counters; minion entry response; zero counters stays; global uniqueness with Hawkeye identity. |
| 03013 Squirrel Girl | Optional entry response1 damage to every enemy; each affected enemy, tough, defeat/retaliate distinctions and simultaneous timing. |
| 03014 Wonder Man | Additional hand discard before each basic attack; not before thwart; no-hand/stale/duplicate reject; consequential attack damage once. |
| 03015 Avengers Assemble! | Ready controlled Avengers only; phase buff affects all Avengers in play across seats; newly-entering/exiting trait cases per duration rules; title max1/round including other player copy. |
| 03016 Make the Call | Reprint: printed-cost payment, any player's discard, controller versus owner, uniqueness/ally-limit; no play-cost discount consumed. |
| 03017 Strength In Numbers | Select any number of own ready allies; exhaust exact selected set and draw that count; no opponent/exhausted/duplicate selection; zero/no-change initiation and deck exhaustion. |
| 03018 Power of Leadership | Reprint: doubles only paying Leadership card; other aspect/basic and ability costs controls; deck max2 across reprint faces. |
| 03019 Quinjet | Optional own-turn-start counter; Avenger ally in own hand within printed-cost threshold enters play; discard Quinjet afterward; count0/exact/above; no discount, no ally-play accounting. |
| 03020 Mockingbird | Reprint entry stun; optional/decline, legal enemies, steady/stalwart; uniqueness across faces. |
| 03021 Energy | Two energy resources; title max1 across reprints, legal resource payment. |
| 03022 Genius | Two mental resources; title max1 across reprints, legal resource payment. |
| 03023 Strength | Two physical resources; title max1 across reprints, legal resource payment. |
| 03024 Avengers Tower | Conditional ally limit when every ally Avenger, including empty set; entering non-Avenger enforces changed limit before response; exhaust next Avenger ally discount this phase; another player may consume per printed wording/authority; expiry and matching kind/trait. |
| 03025 Honorary Avenger | Current identity Avenger prerequisite; friendly character attachment max1; +1HP and trait while attached; trait removal/form change/health defeat and cross-player owner/control. |
| 03026 Man Out of Time | Optional flip; legal exhaust-and-remove alternative; discard half hand rounded down otherwise then discard obligation; odd/even/zero hand, exhausted identity and eliminated owner. |
| 03027 Hit Squad |3 threat/player and acceleration; player-order encounter-top discard and boost-icon damage; boost-star treatment, deck exhaustion and per-player interruption windows. |
| 03028 Baron Zemo | Quickstrike hero only; engaged player's identity cannot thwart including event thwart, but allies remain allowed; re-engagement, defeat and blanking remove prohibition. |
| 03029 Hydra Soldier | Guard; defeat deals encounter card to engaged player before departure; defeat by another player; boost has no invented ability. |
| 03030 Hail Hydra! | Every Hydra minion engaged with a hero attacks that hero; each player not attacked searches deck/discard for Hydra minion; only searched deck shuffles; alter-ego, none/many, defeated/eliminated and attack-cancellation accounting. |
| 03031 Enraged | Ally attachment max1; +2ATK and +1 attack consequential damage; thwart unchanged; damage-prevention/defeat and control changes. |
| 03032 Followed | Side-scheme max1; optional interrupt before scheme defeat;4 to legal enemy while scheme still in play; Madame Hydra immunity negative control; attached card discarded afterward. |
| 03033 Expert Defense | Hero actual basic defense declaration, before boost reveal; +3DEF for that attack only; no ally defense or defense-event-only trigger; multiplayer defender. |
| 03034 Enhanced Awareness | Uses3 mental, Hero Resource exhaust+counter cost; removes final counter and discards; no alter-ego or exhausted use; generator remains committed through final departure. |

## Confirmed mechanic blockers

- Named controlled-card return cost: child742c9041-2c41-4f8a-b283-e060f16c88de.
- First-ally accounting: child932952c5-1cdd-4ec5-b816-a4933808655d.
- Shield Toss variable simultaneous composition: childd673eba4-e4a8-4b37-8715-811cc21d9dc9.
- Falcon private multi-card look and Wonder Man mandatory attack cost:
  child30e1d631-8063-4a66-8025-9df30695213e.

All are in Content Expansion, linked as blockers of MARVEL-560. Additional
implementation findings must become actionable dependencies, not exemptions from
this ledger. Helmet replacement and Hail Hydra bookkeeping still need a complete
mechanic audit before deciding whether existing primitives suffice.

- Official component/starter reconciliation: child8bc2b9ea-544e-496a-b4d9-ed2f4a26fb6f.
- Additive content save/campaign compatibility: shared MARVEL-630.
- Event source/hand-discard and resource-payment atomicity: MARVEL-633,
  child `a6a7f37b-e591-4bdb-8f40-51fd4cc2862a`, blocks MARVEL-627 and this pack.
  The reproductions use synthetic event interpretations; admitted Core/RoRS
  discard-hand costs belong to in-play Alpha Flight Station and Hawkeye.

## Evidence presently available

`NamedReturnCostTests` covers a synthetic Core-card rendering of the required
named return cost: ready/exhausted payment, missing/uncontrolled/out-of-play/
ambiguous rejection, stale departure, returning a borrowed card to its owner and
attachment discard. `AbilityCostLoweringTests` covers typed title retention and
empty-title rejection. These tests **do not** prove Shield Toss or pack gameplay.
The compiled controller-to-owner substitution mutant is killed by the
other-controller case. Existing named-exhaust/source-return tests protect sibling
cost classes. Exact-revision execution results belong on the Plane child.

`FirstAllyCostTests` exercises the history-dependent discount using a synthetic
Core identity, including cross-seat play, active/inactive forms, reset, rejected
payment, actual DSL put-into-play, no-marker Core/RoRS controls and paired query
purity including subsequent events/digest/RNG/pending work. The compiled mutation
that tracks only the active face is killed by the hero-form prior-play case.
These are prerequisite observations, not a complete Captain America game or a
saved-session upgrade proof. Exact revision and counts are recorded in Plane.

`SimultaneousAttackDamageTests` exercises an engine damage-batch prerequisite
with synthetic targets. All selected shares are placed before the first defeat
interrupt; replacement and defeat suspension retain the batch, stale target
incarnations are excluded, and recipient results include tough, retaliation,
ranged and overkill outcomes. The compiled sequential-placement mutation is
killed by observing the second recipient at the first defeat interrupt; removing
incarnation validation is killed by the departure-and-return case. This is not
yet a Shield Toss transcript: its cost-to-target count binding, Guard admission,
stun boundary and separate per-target attack response opportunities still need
implementation and acceptance under MARVEL-627.

`EventDiscardCostTests` distinguishes event-source exclusion, source-only hand
payability, resource/discard overlap rejection before mutation, a real
`Game.Resolve` payment with distinct cards, and repeated-query/subsequent-play
controls. The source-inclusion and payment-overlap mutations each fail the
corresponding observation. This does not establish the entire event-cost space:
intervening resource-ability effects and actual Shield Toss cost/target
composition remain separate acceptance work. Joint offer admission now searches
for a hand-discard selection that leaves sufficient distinct resources, including
typed and multi-icon payments; removing receipt subtraction is killed by the
insufficient joint-payment cases. `EventDiscardPaymentContinuationTests` proves
a resource ability's lethal damage cost may suspend for an interrupt and resume
before paying the event's remaining hand discard. An adversarial synthetic
interrupt that removes that reserved discard remains unsupported; MARVEL-633
records the reproduction and unresolved nested-payment abort authority.

The numeric expression `{"cardsDiscarded":"game"}` reads the physical discard
receipt, separately from resource payment and printed resource icon counts.
Synthetic cost cases cover one, two and three double-resource cards; a compiled
icon-count substitution fails all three. This binding does not itself provide
Shield Toss's target selection or per-enemy attack response instances.

`AttackTriggerBindingTests` and `MultiTargetAttackOccurrenceTests` exercise a
synthetic multi-recipient occurrence contract. Targeted responses receive one
incarnation-bound fact per matching enemy; untargeted attack responses retain one
instance. Attack/defeat and attack/damage clauses correlate the same recipient.
A second-enemy-first response can suspend for a choice without losing its target,
and repeated read-only queries preserve subsequent results. Four compiled
negative controls cover lost continuation keys, stale incarnations, cross-target
defeat correlation and repeating whole-attack responses. Eight existing Core/RoRS
journals replay 214 decisions without divergence; those simulator journals do
not record RNG counts, so that replay is not an RNG audit. This contract is not
yet published by the actual multi-target power/batch path, nor is it full Shield
Toss acceptance. `OccurrenceResolutions` owns ability/card status transitions;
`Occurrence` retains its read-only serialized resolution array and ignores the
new lifecycle owner during serialization. This decomposition removes its former
large-class baseline exception. Exact revisions and suite counts are in Plane.

MARVEL-635 retains a labelled attack's aftermath while retaliation damage resolves
an interrupt choice. Eight synthetic real-game cases compare queried and unqueried
continuations, including two recipients, repeated damage to one enemy, ranged,
prevention and accepted damage. State, events, pending work and RNG remain paired;
attack modifiers are consumed once and the containing effect runs only after the
last retaliation. Compiled skip-wait and duplicate-recipient mutants fail. Removing
only the caller's early return survives because the suspended cast independently
prevents outer advancement and the scheduled capture already retains its power
state; that mutation is equivalent for this completion boundary. Eight old journals
still replay 214 decisions without divergence (not an RNG audit). This is a shared
synthetic capability checkpoint, not Shield Toss publication or pack acceptance.
The separate stale-recipient, response-window, save-continuation and integrated
multi-villain acceptance work remains open in the linked prerequisites.

No pack starter, complete game, native journey, upgrade fixture, cross-platform
certification or independent acceptance has passed. Product admission remains
closed while this work proceeds.

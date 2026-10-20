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
against the physical product's divider and contents before creating executable setup; do not infer
physical completeness from the 35-face count.

The official insert is vendored at `datasets/rules-packs/mc04/leadership.md`, with
source pin in `sources.manifest.json`. Its extracted paragraph describes play but
does not itself enumerate the complete contents or starter deck. Both pages of the
[official PDF](https://images-cdn.fantasyflightgames.com/filer_public/2c/59/2c59220a-6df3-4f39-8104-abc0ed61e8e6/mc04_captain_america_rulesheet.pdf)
were visually checked on 2026-10-20; its SHA-256 matches the vendored pin
`a8e3bffc7953b7ca0a6182cdcaaaa80f3d79710efc3b0e289c9ff46c0110a87a`.
Page one directs the reader to the physical divider: “There is a complete
decklist on the reverse side of the divider card for your reference.” Page two
is artwork. Neither page lists starter quantities or a physical component
inventory. MARVEL-629 therefore still requires a source for that divider and
component reconciliation; repeating extraction of this PDF cannot supply it.

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

The explicit `simultaneous: 1` attack-damage marker connects the interpreter to
the shared damage barrier. Real-game synthetic tests prove all recipient damage
is visible in the first When Defeated choice, one response per attacked enemy,
one whole-attack response, and damage-only responses excluding a Tough recipient.
The actual scheduled power occurrence is retained separately from the containing
ability origin. Compiled sequential-route and Tough-as-damaged mutants fail.
The default ordinary damage path is preserved; multi-target choice, Guard,
discard-receipt cardinality and Shield Toss cost composition remain unfinished.

Count-only hand-discard admission binds candidate receipt sizes for Any, UpTo and
Exact costs and correlates the same count with independent event resources. It
uses the existing memoized payment search rather than enumerating physical hand
subsets. Explicit selections bind their actual cards; exact-one receipt queries
retain printed icon/trait semantics. Variable multi-card receipt queries that
read physical traits/icons or recover physical discarded cards raise explicitly.
Eleven focused cases include bounds, an unchanged-state rejected real-game choice
followed by a valid play, and unsupported physical-receipt refusal. Dropping the
receipt-count/payment correlation is killed by Any and UpTo negative controls.
This does not settle MARVEL-633's intervening resource-ability discard authority.

An explicit aggregate card-attack invocation validates all distinct recipients
against the same pre-attack board before scheduling its interpreter continuation.
Eight focused rules cases cover Guard in either target order, duplicate targets,
non-enemy secondary targets, aggregate contract errors and one scheduled attack.
Compiled duplicate and secondary-eligibility mutants fail; all 991 Rules tests
pass. The default single-target invocation is unchanged. This contract does not
yet connect an authored multi-target choice or establish Shield Toss acceptance.

The target-count design follows rr:choose-game-element.3: choose as many legal
targets as available, up to the specified number. An X larger than the available
enemy count does not itself make initiation illegal. Guard still excludes the
villain from the simultaneous selection, and targets must be distinct.

Counted attack selection now connects the physical discard receipt to one
engine-owned target prompt and the simultaneous damage body. Fourteen focused
cases cover distinct targets, fewer available enemies than X, Guard, separate
event resources, stun after costs, unsupported DSL shapes, query purity and a
legal synthetic discard/reentry interrupt. Checkpoint 4732eb97 refused the
absent-recipient completion case before damage; the subsequent completion evidence
below replaces that refusal. Counted
bodies are restricted to one simultaneous constant-damage instruction over
powerTargets, with a constant or physical receipt count. Arbitrary dynamic counts
and compound bodies raise during lowering.

Compiled uncapped-cardinality and omitted-incarnation-validation mutants fail
three and one cases respectively. All 2,687 Content tests pass and eight old
journals replay 214 decisions without divergence (not an RNG audit). No authored
Captain cards or setup are added by this interpreter checkpoint. MARVEL-633's
adverse cost continuation and MARVEL-635's forced aftermath ordering remain open.

Counted selection captures each original public recipient before attack interrupts.
Completion retains those facts when a recipient leaves play, assigns zero Taken
to absent recipients, and correlates remaining damage results by physical card.
A reentered copy cannot take the old attack's damage, receive its target-bound
status response, or retaliate for it. Six real Game.Resolve cases place Whiplash
first/last, remove every recipient, and interrupt either the attack or damage.
They verify per-recipient versus whole-attack responses, damaged-only eligibility,
and no retaliation from the new copy. A seventh case advances Zola I to II,
resolves the authored Test Subjects search, and verifies the surviving villain's
retaliation after its stage continuation.

Compiled lost-binding, stale-retaliation and cross-recipient-result mutants fail
three, three and four cases respectively. A proposed special villain exception
survived its stage control because departure retains incarnation until reentry;
the unnecessary exception was removed. The shared stage-retaliation procedure
already supplies the required behavior. All 2,693 Content tests pass; eight old
journals replay 214 decisions without divergence (not an RNG audit). No accepted
pack or replay schema change follows from this interpreter checkpoint.

No pack starter, complete game, native journey, upgrade fixture, cross-platform
certification or independent acceptance has passed. Product admission remains
closed while this work proceeds.

## Authored pack work in progress

The follow-on draft interprets nine faces: Captain America, Steve Rogers, Agent
13, Fearless Determination, Heroic Strike, Shield Block, Steve's Apartment,
Captain America's Shield and Super-Soldier Serum. These rows are research
implementations, not a complete playable pack.

Prepared card-execution scenes exercise the actual authored interpreter and
Game decisions for readying costs and round limits, first-ally accounting across
form changes, setup searches and unconditional shuffling, optional Agent 13
responses, temporary THW bonuses, resource generation, healing and draw.
Heroic Strike's declared payment controls distinguish physical, mental and wild
allocation, and prove that Tough prevention does not suppress its separate stun.
Shield Block's real enemy-attack path prevents damage, exhausts the named Shield,
leaves the identity ready and permits the Shield's retaliation after zero damage.
Its nonattack tests explicitly supply the imminent-damage occurrence; they prove
window eligibility and prevention in either form, not a complete encounter
transcript. Cost controls reject missing, exhausted, foreign and wrongly named
upgrades.

MARVEL-645 adds an explicit runtime product policy independent of authored
interpretations. Partial and unknown products remain unavailable to executable
discovery, deck assessment, quantities and game creation, while their printed
research cards remain browseable with runtime support false. Product-owned
research recipes are excluded; references from retained Core recipes still fail
validation. The authored-book gate names only these nine additional research
faces. This boundary does not establish Captain America's completeness or
acceptance.

Verification for the nine-face checkpoint: 35 focused Captain cases and the full
2,782-case Content suite pass. Fourteen compiled authored-data mutations are
caught: ready cost, round limit, first-ally amount, missing setup shuffle, Shield
DEF, Serum resource type, THW expiry, apartment healing, Agent 13 forced timing,
Heroic Strike damage and payment type, and Shield Block form, cost and prevention.
This covers the named observations above, not every per-face obligation.

The separate admission checkpoint `4ac8d021` passes 397 Server tests, including
full prior-history upgrade controls. Four compiled admission bypasses are caught:
all authored faces playable, unknown products accepted, incomplete RoRS accepted,
and retained-recipe validation omitted. The solution builds without warnings;
eight immutable predecessor Core/RoRS journals replay 214 decisions without
divergence (not an RNG audit). Independent review found no admission-boundary
blocker. These additive research rows introduce no new replay contract beyond the
separately reviewed v21 attack-completion baseline.

## Helmet defeat replacement (MARVEL-646)

The tenth research face, 03008, uses a typed positive hit-point assignment.
Excess lethal damage does not change the result: damage 11 or 20 against a
maximum of 11 becomes damage 10, leaving one hit point. The optional interrupt
names Captain America; it does not rescue Steve Rogers or another hero. The
Helmet discards only after the assignment changes the dial in full.

Focused evidence covers modified maxima, loss of a maximum modifier causing
defeat, declining the interrupt, and a real enemy-attack continuation through
Helmet discard and Shield retaliation. Repeated offer queries preserve digest,
RNG and pending work; their completed event stream matches an unprobed control.
A synthetic labelled attack verifies that setting a villain to one hit point
changes subsequent lethal-stage prediction: an unprojected successor-health
read is refused before exhaustion. The ordinary admission and structure-entry
boundaries share explicit shape validation. Absolute assignments inside repeated
frames remain unsupported instead of being treated as additive damage transfers.

This is card/mechanic evidence, not complete pack, starter-deck or native-client
acceptance. MARVEL-629 and MARVEL-633 remain required blockers.

The checkpoint passes 21 focused Content cases, seven Rules arithmetic cases,
all 2,803 Content tests, all 1,010 Rules tests and 129 architecture tests. The
solution builds with zero warnings/errors, and eight immutable predecessor
Core/RoRS journals replay 214 decisions exactly (not an independent RNG audit).
Eleven compiled mutants are caught: heal-one substitution, wrong maximum, lost
completion, ignored repeated-frame refusal, omitted power/area assignment,
unchanged assignment counted as full, missing maximum validation, forced timing,
wrong character and premature discard. Independent source review found no
blocking finding within this bounded mechanic scope. Evidence-producing entry
methods now have their own owner; the prior admission class shrinks from 425 to
398 lines without relaxing its debt gate.

## Reprinted faces

Nine further research interpretations cover Hawkeye (03012), Make the Call
(03016), The Power of Leadership (03018), Mockingbird (03020), Energy/Genius/
Strength (03021–03023), Avengers Tower (03024) and Hydra Soldier (03029).
Each face's canonical printed text matches its existing Core or RoRS printing;
the Captain version of Hydra Soldier retains its distinct nemesis metadata.

Existing lifecycle, payment and Tower controls run against both printings.
They distinguish four arrows from Uses, arrow payment and damage, foreign ally
ownership versus control, resource doubling for the chosen ally, conditional
ally limits, cross-player discounts and phase expiry. The Hydra Soldier control
supplies its defeat occurrence and proves encounter-card recipient attribution;
it is not a complete nemesis reveal/defeat journey. Additional game-decision
tests play Mockingbird, decline or choose a villain/another player's minion,
and verify that only the chosen enemy is stunned. The three double resources
pay for a support, and Leadership's doubled output is excluded for Basic cards.

These additions remain behind the closed product gate. Complete deck limits,
nemesis setup, starter composition and client journeys are still pack acceptance
obligations, not conclusions from these bounded card tests.

Verification: 49 focused cases and all 2,820 Content tests pass. Eight executable
authored mutations are caught: arrow count, arrow damage, Uses, resource aspect,
forced Mockingbird timing, encounter-card recipient, Tower reduction amount and
Tower exhaustion target. Independent review verified the canonical text matches,
the interpreted rows, the counter pool and the tests without finding a blocker.

## Enhanced Awareness

03034 uses the existing resource-ability and Uses contracts: three mental
counters on entry, hero-only exhaustion plus one counter for one mental resource.
Actual game decisions play the upgrade and pay for a support with it. The last
counter discards the upgrade while its committed resource still pays for the
support. Alter-ego, exhausted and foreign-controlled copies are excluded; these
availability checks preserve digest and RNG, without claiming the stronger
paired pending-work/event-stream purity evidence used for Helmet above.

Six focused cases distinguish those outcomes. Six executable authored mutations
are caught: two starting counters, missing Uses, unrestricted form, wrong
exhaustion target, physical output and a two-counter cost. Independent source
review found no blocker. The full Content suite passes 2,826 cases. This
twentieth research face does not open the pack.

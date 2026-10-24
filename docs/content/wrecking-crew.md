# Wrecking Crew admission ledger

Canonical pack: MARVEL-562. Complete-product acceptance remains blocked by
MARVEL-625 (runtime integration), MARVEL-630 (prior-save compatibility),
MARVEL-631 (content admission and complete card behavior), and MARVEL-632
(four-villain presentation and native journeys). MARVEL-624 owns source corrections and
extractor text-override behavior. Nothing in this ledger admits the scenario.

## Components and authorities

The generated `datasets/cards/cards.json` supplies 60 printed faces. The double-sided
Breakout contributes two faces but one physical card. The pinned acquisition inventory
`datasets/marvelsdb/pack/twc_encounter.json` records 73 physical cards: one main
scheme, eight villain versions, four signature side schemes and four separate
15-card encounter decks. Copy counts require reconciliation against printed set
numbers; they are not inferred from the number of distinct faces.

Official insert `mc03_wrecking_crew_rules_insert-compressed.pdf`, pages 2–7,
SHA-256 `443f135a8c5d90a8bfd3719323e6c00501ed166d88f517bec37b59ee9d10124c`.
The published booklet excludes other encounter sets, nemesis cards and obligations.
Standard uses four A versions; Expert four B versions; custom selects A/B per
villain; extreme begins with four A versions with corresponding B versions beneath.
All 16 custom A/B combinations belong in setup tests. Neither Standard/Expert sets
nor a modular-set picker belongs in this scenario's setup.

`ruling:a683efe4c30d11b5` in `datasets/rulings/rulings.json` confirms the
insert's distinction: card abilities referring to “the villain” refer to the active
villain, while basic attacks and abilities targeting enemies can select inactive
villains. This confirms the printed rule; it does not apply Tower Defense semantics.

Source-level checks are `WreckingCrewAuthorityTests` and
`PrintedTextCorrectionTests.PileItOnNamesItsOwnVillainInBothDisplayForms`.
They verify the corpus, not live gameplay. Generated printed text for 07034 names
Piledriver, as shown by its illustration on booklet page 3.

An explicit `text` field on a supplement correction replaces upstream text and
requires a nonempty `authority`. Omission preserves upstream text; an explicit
empty string removes it. Malformed or unsourced replacements fail acquisition.
Printed attributes remain separately authored facts: a text correction that changes
a keyword or stat also needs its corresponding attribute correction and tests.

## Integration obligations

### Authored setup recipe boundary

`ScenarioDealer` binds explicit `EncounterSetupRecipe` component recipes to the
rules-owned four-group setup plan. The isolated `WreckingRecipeTests` use real
printed cards and all sixteen custom A/B combinations plus Extreme; all four
player counts preserve independent HP, fixed starting threat, and deterministic
separate deck shuffles. Malformed groups, foreign schemes, short decks, added
modular sets and missing authored group faces are refused. Ordinary scenario
allocation is checked separately. These setup tests intentionally use
`NoCardAbilities`; they establish physical composition and setup placement, not
complete card-text execution or public scenario admission.

### Projected stage continuity

MARVEL-638 owns multi-villain card projection. `WreckingPowerProjectionTests`
checks inactive Extreme promotion, final defeat and paired-scheme departure,
all-villain enemy selection, unique active-counter replacement using projected
threat, and refusal to invent an answer to a tied replacement. The immutable
group stage map is projection-only; it is not a new digest or save field.
`WreckingContinuationReferenceTests` requires a captured selection to follow
only its own group's same-character successor, even with an unrelated same-title
card in the shared villain area. Both controls preserve the live digest and RNG.
Compiled mutants that move the active counter on inactive promotion or choose
the first tied replacement fail these tests.

These arranged contracts do not complete MARVEL-638. Area projection, repeated
selector consumers, integrated legal card journeys and replay controls remain
required before full-pack admission.

### Four-group presentation contract

`EncounterGroupProjectionTests` verifies visibility filtering, detached group
descriptions, and readable active-counter presentation. The native
`encounter-groups-*` cases render the real projection of a synthetic world in
Standard and Extreme layouts at 100% and 150% interface scale. They check four
named groups, eight distinct encounter piles, nonoverlapping cards, exact full
printed hover text, and each group's own future-stage inspector. A compiled
mutation routing every group to the first encounter deck fails with
`missing or conflated encounter piles`.

These fixtures deliberately use repeated encounter cards and arranged threat;
they establish layout and projection behavior only. They do not establish
legal setup, card abilities, game completion, save compatibility, target
selection, or user comprehension. MARVEL-631 and MARVEL-632 still require
admitted games covering legal menu/drag targets, active-counter ties, stage
promotion and defeat, independent pile inspection, and restart/resume.

| Obligation | Decisive authority | Distinguishing observation | Status |
|---|---|---|---|
| Four independent enemies | pack:mc03:additional-setup.prepare-villains-and-dials | Four live HP totals scale independently at 1/2/3/4 seats; initial active counter is Wrecker | Blocked MARVEL-625 |
| Four independent decks | pack:mc03:additional-setup.prepare-encounter-decks | Each has exactly 15 physical cards; no nemesis/obligation/Standard/Expert cards; seeded shuffles replay identically | Blocked MARVEL-625 |
| Reference versus target | pack:mc03:new-rules.the-active-villain; ruling:a683efe4c30d11b5 | Basic attack and enemy-targeted event may hit inactive villain; villain-specific event resolves active villain | Blocked MARVEL-625 |
| Active villain changes | card:07001b; pack:mc03:new-rules.the-active-villain | After step one add threat before selecting maximum; all tied eligible villains offered to first player; defeated villains excluded | Blocked MARVEL-625 |
| Encounter ownership | pack:mc03:new-rules.multiple-villains-and-encounter-decks | Card drawn from Wrecker discards to Wrecker after active counter moves; exhaustion shuffles only its owning pile and adds acceleration | Blocked MARVEL-625 |
| Signature scheme lifetime | pack:mc03:signature-side-schemes | Zero threat does not discard; owner defeat removes scheme; other encounter cards remain | Blocked MARVEL-625 |
| Difficulty progression | pack:mc03:adjustable-difficulty | Mixed A/B versions start correctly; extreme A defeat introduces its B instead of premature final victory | Blocked MARVEL-625 |
| Terminal outcomes | card:07001b; pack:mc03:new-rules.multiple-villains-and-encounter-decks | Main-scheme threshold loses; all required villain versions defeated wins; one surviving villain prevents win | Blocked MARVEL-625 |
| Source-local UI and saves | MARVEL-562 acceptance criteria | Native four-villain targeting, first-player ties, explicit inactive activations and deck-owner inspection; pending choices survive fresh-process restart with same replay | Blocked MARVEL-625 |

## Runtime prerequisite evidence

The engine accepts a typed four-group setup plan with one or two physical stages
per villain, a paired side scheme, and exactly fifteen owned encounter cards.
Validation runs before game allocation or ability execution. The content dealer
must provide that plan; it does not yet admit this pack. Initial scheme placement,
printed starting threat and active-counter placement belong to this setup path;
the eventual interpretation of Breakout's setup text must not repeat them.

`EncounterTopologyTests`, `WreckingSetupTests` and `WreckingVillainDefeatTests`
exercise independent piles, stable ownership after movement/defeat, deterministic
setup, active-villain ties, suspended defeat settlement, Extreme promotion and
all-four victory. These fixtures establish engine prerequisites, not the printed
card ledger below. Full setup still needs actual pack recipes for every custom
combination and player count, and full card/native/restart acceptance.

Ordinary-game compatibility was checked against commit `84d326a9`: complete
Rhino, Ultron, Crossbones and Red Skull games in Standard and Expert (seed 700,
policy seed 13) replayed all 214 decisions without prompt, event or digest
divergence. Temporary diagnostics separately compared seeded-generator words
consumed after every decision; all 214 counts matched. The diagnostic changes
were restored and are not part of the implementation. This runtime comparison
uses the same corrected card dataset on both sides; it does not establish prior
save compatibility across the printed-source hash change (MARVEL-630).

Compiled negative controls killed an incorrect first-deck boost source and an
incorrect automatic tie resolution. Integration must additionally prove the
simultaneous-attack batch settles a mandatory Wrecking Crew tie before finishing.

## Per-face behavioral obligations

Each row names the exact corrected printed clause to implement and prove.
For every clause, runtime evidence must distinguish the legal effect, rejection
or no-effect boundary, forced/optional timing, and relevant multiplayer/zone cases.
Pending MARVEL-625 means there is no executable gameplay proof yet. Duplicate
printings retain their own deck ownership even when their effect text agrees.
Boost text is a separate obligation from When Revealed text; neither implies the other.

| Face | Card / encounter owner | Physical copies | Printed obligation | Evidence |
|---|---|---:|---|---|
| 07001a | Breakout / wrecking_crew | 1 | Scenario Contents: Wrecker A, Thunderball A, Piledriver A, and Bulldozer A as villains. (Version B for increased difficulty.) Wrecker, Thunderball, Piledriver, and Bulldozer encounter decks. | Pending MARVEL-625 |
| 07001a:2 | Breakout / wrecking_crew | 1 | Setup: Put the Day of Reckoning, Thunderstruck, Pile It On!, and Clear the Road side schemes into play. Place the active counter on Wrecker. Advance to stage 1B. | Pending MARVEL-625 |
| 07001b | Breakout / wrecking_crew | back of 07001a | star Forced Response: After step one of the villain phase, place 1 threat on each side scheme. Move the active counter to the villain whose scheme has the most threat. (If there is a tie, the first player chooses.) | Pending MARVEL-625 |
| 07001b:2 | Breakout / wrecking_crew | back of 07001a | If this stage is completed, the players lose the game | Pending MARVEL-625 |
| 07002 | Wrecker / wrecker | 1 | star When Wrecker schemes, place the threat on his side scheme instead of the main scheme. | Authored; `WreckingWreckerTests` own-scheme routing while another villain is active; full-pack/native pending |
| 07002:2 | Wrecker / wrecker | 1 | star While Wrecker is attacking, he gets +2 ATK if the attack is undefended. | Authored; MARVEL-647 typed attacker condition; undefended/hero/ally/stun, boost-defeated defender and Backflip composition; independent/full-pack/native acceptance pending |
| 07003 | Wrecker / wrecker | 1 | star When Wrecker schemes, place the threat on his side scheme instead of the main scheme. | Authored; `WreckingWreckerTests` own-scheme routing while another villain is active; full-pack/native pending |
| 07003:2 | Wrecker / wrecker | 1 | star While Wrecker is attacking, he gets +2 ATK if the attack is undefended. | Authored; MARVEL-647 typed attacker condition; undefended/hero/ally/stun and boost-defeated defender; independent/full-pack/native acceptance pending |
| 07004 | Day of Reckoning / wrecker | 1 | Wrecker's Side Scheme. | Pending MARVEL-625 |
| 07004:2 | Day of Reckoning / wrecker | 1 | This card cannot leave play while Wrecker is in play. | Pending MARVEL-625 |
| 07004:3 | Day of Reckoning / wrecker | 1 | Hard Hitter — Forced Response: After threat is placed here, if there is 10 or more threat here, deal 2 damage to each friendly character. Remove all but 3 threat from this scheme. | Pending MARVEL-625 |
| 07005 | Held Hostage / wrecker | 1 | Attach to the active villain's side scheme. | Authored; WreckingHeldHostageTests and permission/projection controls; MARVEL-659 full-product integration pending |
| 07005:2 | Held Hostage / wrecker | 1 | Threat cannot be removed from attacked scheme by thwarting. | Authored; WreckingHeldHostageTests and permission/projection controls; MARVEL-659 full-product integration pending |
| 07005:3 | Held Hostage / wrecker | 1 | Hero Action: The villain corresponding to the attached side scheme attacks you. Then discard this card. | Authored; WreckingHeldHostageTests and permission/projection controls; MARVEL-659 full-product integration pending |
| 07006 | Magic Crowbar / wrecker | 1 | Attach to Wrecker. | Pending MARVEL-625 |
| 07006:2 | Magic Crowbar / wrecker | 1 | star Forced Response: After Wrecker attacks, place 1 threat on the side scheme with the least threat. | Pending MARVEL-625 |
| 07006:3 | Magic Crowbar / wrecker | 1 | Hero Action: Exhaust your hero and discard 1 card at random from your hand → discard this card. | Pending MARVEL-625 |
| 07007 | Wrecker's Command / wrecker | 1 | Attach to Wrecker. | Pending MARVEL-625 |
| 07007:2 | Wrecker's Command / wrecker | 1 | star Forced Response: After Wrecker schemes, place 1 threat on each other villain's side scheme. | Pending MARVEL-625 |
| 07007:3 | Wrecker's Command / wrecker | 1 | Hero Action: Spend physical physical resources → discard this card. | Pending MARVEL-625 |
| 07008 | Corrupt Prison Guard / wrecker | 1 | Guard. (While this minion is engaged with you, you cannot attack the villain.) | Authored; guard-query contract verified, full-pack journey pending MARVEL-631 |
| 07009 | Escaped Convict / wrecker | 1 | Surge | Pending MARVEL-625 |
| 07009:2 | Escaped Convict / wrecker | 1 | star Boost: Move the active counter to the villain whose side scheme has the least threat. If you are in hero form, that villain attacks you after this attack. That attack does not get a boost card. | Pending MARVEL-625 |
| 07010 | Buddy System / wrecker | 1 | When Revealed: Choose the villain whose side scheme has the least threat. Reveal the top card of his deck (top 2 cards instead if he is the only villain in play). | Authored; WreckingBuddySystemTests, MARVEL-651 verification pending |
| 07010:2 | Buddy System / wrecker | 1 | star Boost: Move the active counter to the villain whose side scheme has the least threat. | Authored; WreckingBuddySystemTests, MARVEL-651 verification pending |
| 07011 | Chaos In the Prison / wrecker | 1 | When Revealed: Choose to either discard an upgrade you control or place 1 threat on the active villain's side scheme for each upgrade you control. If you do not control any upgrades, this card gains surge. | Pending MARVEL-625 |
| 07011:2 | Chaos In the Prison / wrecker | 1 | star Boost: If this attack is undefended, discard an upgrade you control. | Pending MARVEL-625 |
| 07012 | Crowbar Toss / wrecker | 2 | When Revealed (Alter-Ego): Wrecker schemes. Then, move the active villain counter to the villain whose side scheme has the least threat. | Authored; WreckingCrowbarTossTests verifies both forms, cancellation, unique/tied ranked movement after activation; full-pack acceptance pending MARVEL-657 |
| 07012:2 | Crowbar Toss / wrecker | 2 | When Revealed (Hero): Wrecker attacks you. Then, move the active villain counter to the villain whose side scheme has the least threat. | Authored; WreckingCrowbarTossTests verifies both forms, cancellation, unique/tied ranked movement after activation; full-pack acceptance pending MARVEL-657 |
| 07013 | Get Wrecked! / wrecker | 1 | When Revealed (Alter-Ego): The villain whose side scheme has the most threat schemes. | Authored; WreckingGetWreckedTests (two-seat choices, activation routing, boost ownership); MARVEL-648 integration pending |
| 07013:2 | Get Wrecked! / wrecker | 1 | When Revealed (Hero): The villain whose side scheme has the least threat attacks you. | Authored; WreckingGetWreckedTests (two-seat choices, activation routing, boost ownership); MARVEL-648 integration pending |
| 07014 | I've Been Waiting For This! / wrecker | 1 | When Revealed: The active villain heals 3 hit points. Give that villain a tough status card. | WreckingWaitingTests; authored reveal/boost, MARVEL-648 whole-pack verification pending |
| 07014:2 | I've Been Waiting For This! / wrecker | 1 | star Boost: Move the active counter to the villain with the least threat on his side scheme. That villain schemes. | WreckingWaitingTests; authored reveal/boost, MARVEL-648 whole-pack verification pending |
| 07015 | Mystical Link / wrecker | 2 | When Revealed: Place 2 threat on each side scheme. | Pending MARVEL-625 |
| 07015:2 | Mystical Link / wrecker | 2 | star Boost: Wrecker gets +3 ATK for this activation unless you place 2 threat on his side scheme. | Pending MARVEL-625 |
| 07016 | You're Dead Meat! / wrecker | 2 | When Revealed: Deal 1 damage to the hero or ally with the fewest remaining hit points. If that character is defeated this way, place 3 threat on Wrecker's side scheme. | Pending MARVEL-625 |
| 07017 | Thunderball / thunderball | 1 | star When Thunderball schemes, place the threat on his side scheme instead of the main scheme. | WreckingThunderballTests: both stages scheme onto own scheme with independent active counter/deck; full-pack journey pending |
| 07017:2 | Thunderball / thunderball | 1 | star Forced Response: After Thunderball attacks you, deal 1 damage to each character you control. | WreckingThunderballTests: undefended, ally defense, another player defending, defeated allies, stun suppression and unaffected other player; chained-activation/native composition pending |
| 07018 | Thunderball / thunderball | 1 | star When Thunderball schemes, place the threat on his side scheme instead of the main scheme. | WreckingThunderballTests: both stages scheme onto own scheme with independent active counter/deck; full-pack journey pending |
| 07018:2 | Thunderball / thunderball | 1 | star Forced Response: After Thunderball attacks you, deal 1 damage to each character you control. | WreckingThunderballTests: undefended, ally defense, another player defending, defeated allies, stun suppression and unaffected other player; chained-activation/native composition pending |
| 07019 | Thunderstruck / thunderball | 1 | Thunderball's Side Scheme. | Pending MARVEL-625 |
| 07019:2 | Thunderstruck / thunderball | 1 | This card cannot leave play while Thunderball is in play. | Pending MARVEL-625 |
| 07019:3 | Thunderstruck / thunderball | 1 | Gamma Blast — Forced Response: After threat is placed here, if there is 10 or more threat here, stun each friendly character. Remove all but 3 threat from this scheme. | Pending MARVEL-625 |
| 07020 | Ball and Chain / thunderball | 1 | Attach to Thunderball. | Pending MARVEL-625 |
| 07020:2 | Ball and Chain / thunderball | 1 | star Forced Response: After Thunderball attacks, place 1 threat on the main scheme. | Pending MARVEL-625 |
| 07020:3 | Ball and Chain / thunderball | 1 | Hero Action: Exhaust your hero and discard 1 card at random from your hand → discard this card. | Pending MARVEL-625 |
| 07021 | Held Hostage / thunderball | 1 | Attach to the active villain's side scheme. | Authored; WreckingHeldHostageTests and permission/projection controls; MARVEL-659 full-product integration pending |
| 07021:2 | Held Hostage / thunderball | 1 | Threat cannot be removed from attached scheme by thwarting. | Authored; WreckingHeldHostageTests and permission/projection controls; MARVEL-659 full-product integration pending |
| 07021:3 | Held Hostage / thunderball | 1 | Hero Action: The villain corresponding to the attached side scheme attacks you. Then, discard this card. | Authored; WreckingHeldHostageTests and permission/projection controls; MARVEL-659 full-product integration pending |
| 07022 | Radioactive Buildup / thunderball | 1 | Attach to Thunderball. | Pending MARVEL-625 |
| 07022:2 | Radioactive Buildup / thunderball | 1 | Excess damage dealt by Thunderball is placed as threat on his corresponding side scheme. | Pending MARVEL-625 |
| 07022:3 | Radioactive Buildup / thunderball | 1 | star Forced Response: After Thunderball attacks, discard this card. | Pending MARVEL-625 |
| 07023 | Corrupt Prison Guard / thunderball | 1 | Guard. (While this minion is engaged with you, you cannot attack the villain.) | Authored; guard-query contract verified, full-pack journey pending MARVEL-631 |
| 07024 | Escaped Convict / thunderball | 1 | Surge | Pending MARVEL-625 |
| 07024:2 | Escaped Convict / thunderball | 1 | star Boost: Move the active counter to the villain whose side scheme has the least threat. If you are in hero form, that villain attacks you after this attack. That attack does not get a boost card. | Pending MARVEL-625 |
| 07025 | Buddy System / thunderball | 1 | When Revealed: Choose the villain whose side scheme has the least threat. Reveal the top card of his deck (top 2 cards instead if he is the only villain in play). | Authored; WreckingBuddySystemTests, MARVEL-651 verification pending |
| 07025:2 | Buddy System / thunderball | 1 | star Boost: Move the active counter to the villain whose side scheme has the least threat. | Authored; WreckingBuddySystemTests, MARVEL-651 verification pending |
| 07026 | Chaos In the Prison / thunderball | 1 | When Revealed: Choose to either discard an upgrade you control or place 1 threat on the active villain's side scheme for each upgrade you control. If you do not control any upgrades, this card gains surge. | Pending MARVEL-625 |
| 07026:2 | Chaos In the Prison / thunderball | 1 | star Boost: If this attack is undefended, discard an upgrade you control. | Pending MARVEL-625 |
| 07027 | Energy Projectiles / thunderball | 2 | When Revealed: Deal 1 damage to each friendly character you control. | Authored; owner/form resolution contracts verified; full journey pending MARVEL-631 |
| 07027:2 | Energy Projectiles / thunderball | 2 | star Boost: Deal 1 damage to the defending character. | Authored; current defender and absent/departed defender contracts verified; full journey pending MARVEL-631 |
| 07028 | Get Wrecked! / thunderball | 1 | When Revealed (Alter-Ego): The villain whose side scheme has the most threat schemes. | Authored; WreckingGetWreckedTests (two-seat choices, activation routing, boost ownership); MARVEL-648 integration pending |
| 07028:2 | Get Wrecked! / thunderball | 1 | When Revealed (Hero): The villain whose side scheme has the least threat attacks you. | Authored; WreckingGetWreckedTests (two-seat choices, activation routing, boost ownership); MARVEL-648 integration pending |
| 07029 | I've Been Waiting For This! / thunderball | 1 | When Revealed: The active villain heals 3 hit points. Give that villain a tough status card. | WreckingWaitingTests; authored reveal/boost, MARVEL-648 whole-pack verification pending |
| 07029:2 | I've Been Waiting For This! / thunderball | 1 | star Boost: Move the active counter to the villain with the least threat on his side scheme. That villain schemes. | WreckingWaitingTests; authored reveal/boost, MARVEL-648 whole-pack verification pending |
| 07030 | Lightning Blast / thunderball | 2 | When Revealed (Alter-Ego): Place 3 threat on the side scheme with the most threat. | Pending MARVEL-625 |
| 07030:2 | Lightning Blast / thunderball | 2 | When Revealed (Hero): Thunderball attacks you. If this attack is undefended, place 3 threat on Thunderball's side scheme. | Pending MARVEL-625 |
| 07031 | Tactical Prowess / thunderball | 2 | When Revealed: Move all threat from the side scheme with the least threat to the side scheme with the most threat. If that scheme's "Forced Response" ability is not triggered this way, this card gains surge. | Pending MARVEL-625 |
| 07032 | Piledriver / piledriver | 1 | Retaliate 1. | WreckingPiledriverRetaliationTests: actual offered basic attack promotes A to B and retaliates once for active/inactive group; native/full-pack journey pending |
| 07032:2 | Piledriver / piledriver | 1 | star When Piledriver schemes, place the threat on his side scheme instead of the main scheme. | WreckingPummelTests actual authored activation targets own scheme; remaining MARVEL-643 composition checks pending |
| 07033 | Piledriver / piledriver | 1 | Retaliate 1. | WreckingPiledriverRetaliationTests: actual offered basic attack promotes A to B and retaliates once for active/inactive group; native/full-pack journey pending |
| 07033:2 | Piledriver / piledriver | 1 | star When Piledriver schemes, place the threat on his side scheme instead of the main scheme. | WreckingPummelTests actual authored activation targets own scheme; remaining MARVEL-643 composition checks pending |
| 07034 | Pile It On! / piledriver | 1 | Piledriver's Side Scheme. | Pending MARVEL-625 |
| 07034:2 | Pile It On! / piledriver | 1 | This card cannot leave play while Piledriver is in play. | Pending MARVEL-625 |
| 07034:3 | Pile It On! / piledriver | 1 | Pile Drive — Forced Response: After threat is placed here, if there is 10 or more threat here, each player discards the upgrade or support they control with the highest cost. Remove all but 3 threat from this scheme. | Pending MARVEL-625 |
| 07035 | Distracting Taunts / piledriver | 2 | Attach to Piledriver. | Pending MARVEL-625 |
| 07035:2 | Distracting Taunts / piledriver | 2 | Piledriver gets +3 hit points. Players cannot attack other villains. | Pending MARVEL-625 |
| 07035:3 | Distracting Taunts / piledriver | 2 | Response: After your hero attacks Piledriver, spend physical physical resources → discard this card. | Pending MARVEL-625 |
| 07036 | Held Hostage / piledriver | 1 | Attach to the active villain side scheme. | Authored; WreckingHeldHostageTests and permission/projection controls; MARVEL-659 full-product integration pending |
| 07036:2 | Held Hostage / piledriver | 1 | Threat cannot be removed from attached scheme by thwarting. | Authored; WreckingHeldHostageTests and permission/projection controls; MARVEL-659 full-product integration pending |
| 07036:3 | Held Hostage / piledriver | 1 | Hero Action: The villain corresponding to the attached side scheme attacks you. Then, discard this card. | Authored; WreckingHeldHostageTests and permission/projection controls; MARVEL-659 full-product integration pending |
| 07037 | Corrupt Prison Guard / piledriver | 1 | Guard. (While this minion is engaged with you, you cannot attack the villain.) | Authored; guard-query contract verified, full-pack journey pending MARVEL-631 |
| 07038 | Escaped Convict / piledriver | 1 | Surge. | Pending MARVEL-625 |
| 07038:2 | Escaped Convict / piledriver | 1 | star Boost: Move the active counter to the villain whose side scheme has the least threat. If you are in hero form, that villain attacks you after this attack. That attack does not get a boost card. | Pending MARVEL-625 |
| 07039 | Buddy System / piledriver | 1 | When Revealed: Choose the villain whose side scheme has the least threat. Reveal the top card of his deck (top 2 cards instead if he is the only villain in play). | Authored; WreckingBuddySystemTests, MARVEL-651 verification pending |
| 07039:2 | Buddy System / piledriver | 1 | star Boost: Move the active counter to the villain whose side scheme has the least threat. | Authored; WreckingBuddySystemTests, MARVEL-651 verification pending |
| 07040 | Get Wrecked! / piledriver | 1 | When Revealed (Alter-Ego): The villain whose side scheme has the most threat schemes. | Authored; WreckingGetWreckedTests (two-seat choices, activation routing, boost ownership); MARVEL-648 integration pending |
| 07040:2 | Get Wrecked! / piledriver | 1 | When Revealed (Hero): The villain whose side scheme has the least threat attacks you. | Authored; WreckingGetWreckedTests (two-seat choices, activation routing, boost ownership); MARVEL-648 integration pending |
| 07041 | I've Been Waiting For This! / piledriver | 1 | When Revealed: The active villain heals 3 hit points. Give that villain a tough status card. | WreckingWaitingTests; authored reveal/boost, MARVEL-648 whole-pack verification pending |
| 07041:2 | I've Been Waiting For This! / piledriver | 1 | star Boost: Move the active counter to the villain with the least threat on his side scheme. That villain schemes. | WreckingWaitingTests; authored reveal/boost, MARVEL-648 whole-pack verification pending |
| 07042 | Oversized Hands / piledriver | 2 | When Revealed: Discard the support you control with the highest cost. If no support was discarded this way, place 2 threat on the active villain's scheme. | Pending MARVEL-625 |
| 07042:2 | Oversized Hands / piledriver | 2 | star Boost: Discard a support you control. | Pending MARVEL-625 |
| 07043 | Escape Plan / piledriver | 2 | When Revealed: You are confused. If you are already confused, Piledriver schemes. | Authored; WreckingEscapePlanTests covers both forms, both resolving seats, existing statuses, all four active scheme targets and active-deck boost routing; full-pack journey pending |
| 07043:2 | Escape Plan / piledriver | 2 | star Boost: Give the active villain a tough status card. If they already have a tough status card, place 2 threat on their scheme. | Authored; WreckingEscapePlanTests covers both forms, both resolving seats, existing statuses, all four active scheme targets and active-deck boost routing; full-pack journey pending |
| 07044 | Pummel / piledriver | 2 | When Revealed (Alter-Ego): Piledriver schemes. If Piledriver is tough, he gets +2 SCH for this activation. | WreckingPummelTests: both stages, inactive Piledriver, tough modifier, own scheme placement, confused cancellation, expiry and boost ownership; full native journey pending |
| 07044:2 | Pummel / piledriver | 2 | When Revealed (Hero): Piledriver attacks you. If Piledriver is tough, he gets +2 ATK for this activation. | WreckingPummelTests: both stages, inactive Piledriver, own/opposing tough status, exact damage, modifier expiry, active-deck boost; full native journey pending |
| 07045 | Uncanny Resilience / piledriver | 1 | When Revealed: Remove each stunned and confused status card from each villain. If no status cards were removed, this card gains surge. | Pending MARVEL-625 |
| 07045:2 | Uncanny Resilience / piledriver | 1 | star Boost: You are confused. | Pending MARVEL-625 |
| 07046 | Bulldozer / bulldozer | 1 | star When Bulldozer schemes, place the threat on his side scheme instead of the main scheme. | WreckingBulldozerTests: both stages scheme onto own scheme with independent active counter/deck; full-pack journey pending |
| 07046:2 | Bulldozer / bulldozer | 1 | star Forced Interrupt: When Bulldozer attacks, the attack gains overkill. | WreckingBulldozerTests: ally overkill, undefended, stun replacement, unrelated attacker and expiry controls; full-pack journey pending |
| 07047 | Bulldozer / bulldozer | 1 | star When Bulldozer schemes, place the threat on his side scheme instead of the main scheme. | WreckingBulldozerTests: both stages scheme onto own scheme with independent active counter/deck; full-pack journey pending |
| 07047:2 | Bulldozer / bulldozer | 1 | star Forced Interrupt: When Bulldozer attacks, the attack gains overkill. | WreckingBulldozerTests: ally overkill, undefended, stun replacement, unrelated attacker and expiry controls; full-pack journey pending |
| 07048 | Clear the Road / bulldozer | 1 | Bulldozer's Side Scheme. | Pending MARVEL-625 |
| 07048:2 | Clear the Road / bulldozer | 1 | This card cannot leave play while Bulldozer is in play. | Pending MARVEL-625 |
| 07048:3 | Clear the Road / bulldozer | 1 | Charge! — Forced Response: After threat is placed here, if there is 10 or more threat here, each player must discard the top 10 cards of their deck. Remove all but 3 threat from this scheme. | Pending MARVEL-625 |
| 07049 | Bulldozer's Helmet / bulldozer | 1 | Attach to Bulldozer. | Pending MARVEL-625 |
| 07049:2 | Bulldozer's Helmet / bulldozer | 1 | star Forced Response: After Bulldozer attacks you, discard 1 card from the top of your deck for each point of damage dealt by this attack. | Pending MARVEL-625 |
| 07049:3 | Bulldozer's Helmet / bulldozer | 1 | Hero Action: Exhaust your hero and discard 1 card at random from your hand → discard this card. | Pending MARVEL-625 |
| 07050 | Held Hostage / bulldozer | 1 | Attach to the active villain side scheme. | Authored; WreckingHeldHostageTests and permission/projection controls; MARVEL-659 full-product integration pending |
| 07050:2 | Held Hostage / bulldozer | 1 | Threat cannot be removed from attached scheme by thwarting. | Authored; WreckingHeldHostageTests and permission/projection controls; MARVEL-659 full-product integration pending |
| 07050:3 | Held Hostage / bulldozer | 1 | Hero Action: The villain corresponding to the attached side scheme attacks you. Then, discard this card. | Authored; WreckingHeldHostageTests and permission/projection controls; MARVEL-659 full-product integration pending |
| 07051 | Ramming Speed / bulldozer | 2 | Attach to Bulldozer. | Pending MARVEL-625 |
| 07051:2 | Ramming Speed / bulldozer | 2 | star Forced Interrupt: When Bulldozer attacks you, you must defend against Bulldozer's attacks with an ally you control, if able. | Pending MARVEL-625 |
| 07051:3 | Ramming Speed / bulldozer | 2 | star Forced Response: After Bulldozer attacks you, discard this card. | Pending MARVEL-625 |
| 07052 | Corrupt Prison Guard / bulldozer | 1 | Guard. (While this minion is engaged with you, you cannot attack the villain.) | Authored; guard-query contract verified, full-pack journey pending MARVEL-631 |
| 07053 | Escaped Convict / bulldozer | 1 | Surge. | Pending MARVEL-625 |
| 07053:2 | Escaped Convict / bulldozer | 1 | star Boost: Move the active counter to the villain whose side scheme has the least threat. If you are in hero form, that villain attacks you after this attack. That attack does not get a boost card. | Pending MARVEL-625 |
| 07054 | Buddy System / bulldozer | 1 | When Revealed: Choose the villain whose side scheme has the least threat. Reveal the top card of his deck (top 2 cards instead if he is the only villain in play). | Authored; WreckingBuddySystemTests, MARVEL-651 verification pending |
| 07054:2 | Buddy System / bulldozer | 1 | star Boost: Move the active counter to the villain whose side scheme has the least threat. | Authored; WreckingBuddySystemTests, MARVEL-651 verification pending |
| 07055 | Bull Rush / bulldozer | 2 | When Revealed (Alter-Ego): Bulldozer schemes. Discard the top card of your deck for each threat placed by this activation. | Pending MARVEL-625 |
| 07055:2 | Bull Rush / bulldozer | 2 | When Revealed (Hero): Bulldozer attacks you. Discard the top card of your deck for each damage dealt by this attack. | Pending MARVEL-625 |
| 07056 | Chaos In the Prison / bulldozer | 1 | When Revealed: Choose to either discard an upgrade you control or place 1 threat on the active villain's side scheme for each upgrade you control. If you do not control any upgrades, this card gains surge. | Pending MARVEL-625 |
| 07056:2 | Chaos In the Prison / bulldozer | 1 | star Boost: If this attack is undefended, discard an upgrade you control. | Pending MARVEL-625 |
| 07057 | Get Wrecked! / bulldozer | 1 | When Revealed (Alter-Ego): The villain whose side scheme has the most threat schemes. | Authored; WreckingGetWreckedTests (two-seat choices, activation routing, boost ownership); MARVEL-648 integration pending |
| 07057:2 | Get Wrecked! / bulldozer | 1 | When Revealed (Hero): The villain whose side scheme has the least threat attacks you. | Authored; WreckingGetWreckedTests (two-seat choices, activation routing, boost ownership); MARVEL-648 integration pending |
| 07058 | Headbutt / bulldozer | 2 | When Revealed: Discard 1 card at random from your hand. If you are in hero form, take damage equal to that card's printed cost. If you are in alter-ego form, place threat on Bulldozer's side scheme equal to that card's printed cost. | Pending MARVEL-625 |
| 07059 | Leading the Charge / bulldozer | 2 | When Revealed: Discard the top X cards of your deck, where X is Bulldozer's ATK. Place 1 threat on Bulldozer's side scheme for each different card type discarded this way | Pending MARVEL-625 |

## Escape Plan and active scheme verification

The active-villain-side-scheme query follows physical pairing through counter movement and promotion on live, area, power and repeated projections. Missing topology or a missing live scheme raises. Thirty-nine focused cases pass; the full Content suite passed 3,089 cases before two additional resolving-seat cases. Compiled stale-active projection and incorrect Escape Plan threat mutants failed 2 and 4 cases respectively; restored focused tests pass. Independent Captain-agent review found no blocker in this bounded relation/card scope. This does not establish full-pack acceptance.

Shared-book synchronization includes Squirrel Girl and Enraged rows plus consequential-field admission from Captain commit cce68429; their independent tests and full product gate remain on the Captain branch for integration.

## Activation completion (MARVEL-657)

Crowbar Toss now uses completed activation receipts for its then clause. Sixteen focused cases cover attack/scheme cancellation, zero-damage completed attacks, post-activation target timing, sequential receipt isolation, and partial multi-enemy activation collections. The full Content suite passes 3,107 tests. Compiled any-activation-is-full and damage-is-activation mutants fail 1 and 4 cases. Held Hostage and whole-pack saved/native journeys remain required before acceptance.

### Held Hostage contract verification (MARVEL-659)

All four printed reprints attach to the active villain's physical side scheme.
`preventThwarting` distinguishes labeled/basic thwarting from other threat removal;
stacked copies continue to prohibit until every relevant copy departs. Area and
power projections follow departed sources, and area projections follow changed
attachment hosts. Unsupported conditional projected prohibitions raise explicitly.
`villainOfAttachedScheme` resolves the current stage in that physical group,
independent of the active counter and source deck, including projected promotion.

Actual-card tests cover four faces, both seats, normal attacks, Stunned
cancellation, Tough preventing all damage and alter-ego rejection (28 cases). Seventeen further controls
cover permissions, action offers, stacked sources, projection, four physical pairs,
promoted stages, projected reattachment and otherwise activation completion. Admission treats static activations as
suspensions with potentially reachable dependent text; runtime completion receipts
remain the authority for executing that text. Full-product/native/replay acceptance
is still pending and this checkpoint does not admit the pack.

Verification: full Content suite 3,151 green, followed by the final projected-host
control (nine permission cases green). Compiled mutants killed: blanket removal
prohibition (four failures), ignored departed source (one), active-villain fallback
(four). Independent bounded review found and verified remediation of the otherwise
admission gap. This evidence does not replace whole-product acceptance.

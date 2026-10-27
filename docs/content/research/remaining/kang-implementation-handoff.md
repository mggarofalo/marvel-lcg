# The Once and Future Kang: implementation handoff (MARVEL-568)

Baseline: `f199e5fe` (2026-10-27). **Ready for the bounded engine slice below;
not implemented or accepted.** This handoff owns no runtime changes. The parent
delivery record remains MARVEL-568; ordinary content and engine acceptance are
agent-owned. The materially new split-table interaction has a separate owner
review obligation after executable/native evidence exists.

## Scope and authority

Use [the existing narrative](563-568.md#marvel-568-the-once-and-future-kang--scenario),
[face/quantity ledger](563-568-inventory.md), [source review](source-review.md)
and [independent review](independent-review.md) as source context. Printed facts
are `datasets/cards/cards.json`, pack `toafk`; quantities below reconcile that
ledger. Scenario authority is the six pinned, offline `datasets/rules-packs/mc11/`
records: `setup`, `areas`, `game-areas`, `rules-clarifications`,
`adjustable-difficulty`, `new-keywords` (each carries its source page/hash).
Use current RR **1.8**, especially `rr:per-player-icon`, `rr:you-your`,
`rr:wild-resource.3`, `rr:villain-phase`, `rr:player-elimination`,
`rr:unique-icon`, `rr:when-completed-abilities`, `rr:piercing`, `rr:villainous`,
`rr:incite-x` and `rr:overkill`. Check citation IDs before authoring tests.

There are **58 faces, 80 summed face quantities, 73 physical cards**. Seven
main schemes have two faces each; count each `a/b` pair once physically. No
hero, starter, hero-specific obligation/nemesis set or campaign is bundled.
Existing heroes still bring their nemesis minions, needed by stage 4B; their
normal obligation cards are removed by 1A. This does not remove the pack's
encounter obligations.

| Set | Exact face inventory; quantity per physical card | Physical total |
|---|---|---:|
| `kang`: Standard villains | `11001` Conqueror I; `11002` Immortus II; `11003` Iron Lad II; `11004` Rama-Tut II; `11005` Scarlet Centurion II; `11006` Conqueror III — each ×1 | 6 |
| `kang`: main schemes | `11007a/b` Kang's Arrival 1; `11008a/b` The Master of Time 2; `11009a/b` The Chronopolis 3; `11010a/b` Inexorable Fate 3; `11011a/b` The Realm of Rama-Tut 3; `11012a/b` The Present Future War 3; `11013a/b` Kang's Wrath 4 — each pair ×1 | 7 |
| `kang`: attachments/minion | `11014` Temporal Shield ×2; `11015` Future Weapon ×2; `11016` Frozen in Time ×1; `11017` Macrobots ×3 | 8 |
| `kang`: obligations | `11018` Weakened; `11019` Stolen Memories; `11020` Depowered; `11021` Time-Travel Hijinks — each ×2 | 8 |
| `kang`: side schemes | `11022` Corrupted Timestream ×1; `11023` Kang's Dominion ×4; `11024` Pinned Down ×1; `11025` Rampage ×2 | 8 |
| `kang`: treacheries | `11026` Energy Blast ×2; `11027` Manipulated Timestream ×1; `11028` Time-Travel Tactics ×2; `11029` Past Machinations ×1 | 6 |
| `temporal` | `11030` Ancient Warrior ×3; `11031` Chitauri Soldier ×2; `11032` Tyrannosaurus Rex ×1; `11033` Time Portal ×1 | 7 |
| `exp_kang` | `11034–11039`: Expert replacements of `11001–11006`, respectively; each ×1 | 6 |
| `anachronauts` | `11040` Apocryphus; `11041` Deathunt 9000; `11042` Sir Raston; `11043` Terminatrix; `11044` Wildrun — each ×1; `11045` The Anachronauts ×2; `11046` Kang's Chosen ×2 | 9 |
| `mot` | `11047` Kang (Master of Time) ×1; `11048` Time-Displaced Soldier ×2; `11049` Fear of Kang ×2; `11050` Light of Centuries Sphere ×1; `11051` Ancient Grudge ×2 | 8 |

`kang` totals 43 physical cards / 36 faces / 50 face quantities. The other four
sets total 30 physical cards/faces/face quantities. Whole-product admission must
cover all 58 faces, including both modes and all three modular sets.

Standard uses six `kang` villains, Kang + Core Standard encounter cards and
one chosen modular set; Temporal is recommended. Expert replaces **all six**
villains with `11034–11039` and additionally includes Core Expert. It does not
use the ordinary sequential II/III Expert stage recipe. Both modes need 1–4
seats and each of Temporal, Master of Time and Anachronauts (24 setup
combinations), plus substitution of these modules into supported ordinary
modular slots. There is no additional pack-specific campaign or Extreme mode.

The initial encounter pool is the 26 non-Dominion copies `11014–11029`, Core
Standard's seven cards, chosen module (7/8/9), and Expert's three when selected:
40/41/42 before seat-specific setup effects in Standard, 43/44/45 in Expert.
All four Dominions start aside. Only Kang I starts in the villain deck; the
four Kang II variants and Kang III start aside. Allocate all seven physical
schemes without making the four stage-3 alternatives a sequential scheme deck.

## First source packet to execute

Assign one Sol implementer, then an independent Sol reviewer, a bounded
**Kang I defeat → deterministic two-seat split → phase-end join** engine packet.
Use nonpublic engine-context tests while complete-product admission remains
closed. Own cohesive Kang orchestration/types/tests, not unrelated Ronan paths;
serialize any eventual `abilities.json`, `Agenda.cs` or interpreter edits with
the current engine worker. Do not add a public test bypass or admit a partial
pack. The production public journey follows complete admission, below.

Start with the actual `11001`, `11008a/b`, `11009a/b–11012a/b` instructions and
selected II villain rows. Prepare a two-player state with one side scheme, one
engaged minion per seat, and enough encounter cards to finish the phase. Defeat
Kang I during a player phase. Assert no split until that phase ends; then the
side scheme is discarded and produces one central acceleration token, two
distinct stage-3 schemes are selected by the seeded RNG in turn order, each
matching II enters with toughness and its printed starting threat, each seat
receives one encounter card, unused 3s leave play, and 2B flips only after both
3A resolutions finish. Kang II HP is flat 18 (Expert 22), not per-player;
starting threat is Chronopolis 0, Inexorable Fate/Rama-Tut 1, Present Future War 2.

Next defeat one II mid-phase: remove its matching scheme as printed, retain
isolation until phase end, then offer the legal destination and move the
whole group with its side schemes and engaged minions. Repeat with the other
seat as first player and with both IIs defeated during the same phase. Compare
events, pending work, RNG consumption and next decisions with an uninterrupted
control across session replay/restart. This first packet proves a mechanism;
it does not claim complete setup, victory or native usability.

## Packets and characteristic executable proofs

Paths in this table are concrete existing entry points to inspect/extend, not
claims that Kang is already supported. Each packet ends in reviewed behavior
tests and a compiling distinguishing mutant for each new decision.

| Packet / actual rows | Existing entry points and reuse | First distinguishing proof / dependency |
|---|---|---|
| A: phase-bound stage controller — `11001–11013a/b`, paired Expert `11034–11039` | `Marvel.Rules/State/WorldTopology.cs` (`CreateGameArea`, `Join`, `Detach`), `Places.cs`; `Play/PhaseEnd.cs`, `PhaseTransitionProcedure.cs`, `MainScheme.cs`, `MainSchemeCompletion.cs`, `Defeat.cs` | Execute the first packet above. Generic `MainSchemeCompletion.Commit` advances a sequential deck or ends the game; prove Kang's 1/3/4 completion policies and delayed transitions explicitly. `EncounterVillainDefeat` is Wrecking Crew group logic and must not turn temporary absence of a Kang into victory. |
| B: local effects and central 2B — stage 2, every local villain and encounter row | `Places.CanAffect`, `EachPlayer`, `MainSchemes`; `Play/VillainPhase.cs`, `ActivationProcedure.cs`, `MainScheme.Acceleration`; `Cards/Run/AbilityAreaVillainProjection.cs` | With two separated areas, reject cross-area attack/defense, resolve each local villain once per appropriate player, apply local “each player” only locally; redirect an encounter-exhaustion acceleration token onto central 2B. After joining, shared-area effects reach the combined seats. A central token must not accidentally become a second threat recipient. Requires A. |
| C: obligations and hosting — `11018–11022`, `11024`, `11027–11029` | `Cards/Run/AbilityResourceIconDiscard.cs`, `AbilityBeneathExecution.cs`, `AbilityBeneathReleaseExecution.cs`, `AbilitySelectedHandDiscard.cs`; DSL `storeBeneath`, printed-resource discard and receipts | Stolen Memories with only three cards left stores eight across deck exhaustion, deals the normal exhaustion penalty, hides stored faces even from their owner, and discards those exact physical cards when the obligation leaves. Printed mental discard succeeds; wild-only hand cannot pay. Depowered blocks hero-specific **play** but permits Strange resolving an Invocation; Corrupted Timestream blocks obligation AE actions. Add Weakened basic-power damage, Hijinks highest-cost tie/capture/cleanup, local obligation counts, random-hand choice, distinct-obligation search/reveal and no-event surge. Requires B for local scope. |
| D: attack/ready effects — `11014–11017`, `11026`; villain modifiers in A | `Play/AttackProcedure.cs`, `AttackDamageReceipts.cs`, `VillainStageInheritance.cs`; `Cards/Run/AbilityResolutionPreventionAndDefeat.cs`, `AbilityDelayedExecution.cs`; `Timing/AttackRecipients.cs` | Temporal Shield prevents only damage to attached Kang during a multi-target attack and leaves tough intact when no damage reaches it; max one Shield per attack. Future Weapon retains the exact attack/hero-damage receipt for stun and discard, including ally defense/overkill; Frozen in Time replaces one ready and works as a boost attachment. Macrobots boost targets local Kang. Prove no statuses/attachments transfer between I, II, III. Requires A/B, current RR 1.8 overkill/piercing. |
| E: defeat/recruit/search — `11023`, `11025`, `11030–11033`, `11050` | `Play/SideSchemeDefeats.cs`, `SideSchemeDeparture.cs`; `Cards/Run/AbilityDiscardUntilExecution.cs`, `AbilityDiscardedRecruitment.cs`, `ShuffleDiscardCommand.cs` | Complete a stage 3: place one physical Dominion facedown under 4A immediately, retain II/scheme until phase end, then remove/join; contrast II defeat, which adds none. At 4A reveal every stored Dominion; while any remains local Kang cannot take damage. Its defeat deals the credited player an encounter card. Rampage/Sphere put a discarded minion into play (do not reveal it); Time Portal shuffles itself into the encounter deck on defeat, including Red Skull's separate side-scheme deck. Chitauri's discard damage counts numeric boost icons, not stars; prove quickstrike and piercing on Temporal minions through full entry/attack. Depends on A/B for stage path. |
| F: Anachronauts — `11040–11046` | `Play/AttackBoost.cs`, `AttackBoostDescription.cs`, ordinary reveal lifecycle and the discard/recruit paths in E | Deathunt 9000 receives a boost on both attack and scheme activations; resolve a boost that grants another boost before cleanup. Cover all five unique boost bodies: exhaust +1 boost, tough +1 boost, damage +1 boost, +2 boosts, random discard +1 boost. Kang's Chosen **reveals** the first discarded Temporal minion (including its entry effects); The Anachronauts returns all discarded Temporal cards, not merely its own set. Depends on keyword/reveal lifecycle, not Ronan. |
| G: Master of Time — `11047–11049`, `11051` (Sphere in E) | `Timing/AttackRecipients.cs`, activation context in `Cards/Run/AbilityDelayedExecution.cs`, printed-resource and random-discard paths; existing uniqueness checks | Ancient Grudge activates Master of Time against B while engaged with A: bonuses use B's obligations. Fear of Kang blocks B's identity attacking Kang while B's ally remains legal. Absent Master of Time is searched/put into play instead of activated; villain Kang and this differently subtitled minion coexist. Time-Displaced Soldier incites/surges normally and its boost deals the exact seat a facedown card. |
| H: complete setup/admission/public journey — all 58 faces and recipes | `Content/Setup/Dealer.cs`, `ScenarioDealer.Prepare`, `SetupCatalogAdmission`, `RuntimeProductAdmission`, `StandaloneProductAdmission`; `Server/DatasetGameFactory.cs`; `Session/SessionSave.cs` and replay | All 24 setup combinations; the six Expert replacements, quantities and set-asides match exactly. Removing any required face or changing a required recipe fails whole-product admission. Then public Standard + Temporal setup → two-seat split → join → stage 4 → save/restart → outcome, plus Expert/alternate-module controls. Keep adjacent unsupported products rejected. Depends on A–G. |

## Reuse versus questions that need probes

Topology already exists: `PlacesTests.TheVillainsPlay1.cs` proves ordinary
single-area behavior, cross-area reach, central-card reach, “each player”, and
whole-play-area joining; `PlacesTests.Detaching2.cs` proves detachment events.
`GameAreaDescriptor` already exposes area membership to View. Do not replace
these with a new generic multi-area architecture.

The unproved composition points are narrower:

1. A scheme and villain must belong to each independently joinable area while
   retaining one shared encounter deck/discard. `WorldTopology` groups play
   areas, so merely putting all encounter cards in `PlayArea.Villains` cannot
   establish local scope. Probe placement, target legality and encounter
   resolution context with actual split cards before selecting the minimal
   representation. Include minion/side-scheme movement when an area with
   **multiple** players joins; moving just one seat is insufficient.
2. Central 2B has no printed threat threshold/escalation; it replaces placement
   of acceleration tokens elsewhere. Existing symmetric central reach is an
   engine choice, not proof that a client may offer meaningless thwarting of
   2B. Probe threat placement/target offers and local icon versus central token
   arithmetic; preserve ordinary single-main-scheme behavior.
3. Join destination and duplicate unique-card discard need real decisions.
   `docs/places.md` explicitly leaves in-play join reconciliation unsupported.
   Offer all legal other areas; forbid central reunion while other local areas
   remain. Exercise last area, solo, simultaneous end-of-phase departures,
   combined groups, duplicate uniques and first-player disagreement resolution.
   Preserve the original phase frame while any nested choice suspends.
4. Replay must reconstruct game areas, scheme/villain associations, pending
   phase-end operations, stored Dominions and hidden obligation cards. Digest
   v3 serializes cards and does **not** encode topology; identical digests
   alone cannot prove restoration. Schema-7 sessions replay setup/decisions,
   so first prove reconstruction with existing replay before choosing any
   digest/schema change. Compare topology, pending prompt/agenda, RNG, events,
   legal targets and visibility independently. Make any necessary replay/wire
   version choice explicit and preserve genuine predecessor fixtures.

## Rulings and bounded authority checks

The local MarvelCDB FAQ has no `110xx` entries (queried codes are recorded).
Hall of Heroes `datasets/rulings/rulings.json` supplies the following relevant
answers; absence from MarvelCDB does not mean no ruling. These are source
inputs for the implementer/reviewer, not permission to copy old timing rules:

| Ruling ID | Governing fact / executable control |
|---|---|
| `ruling:0c9221b6940b4007` | Per-player multipliers retain the initial seat count, unlike local “each player”; confirmed by current `rr:per-player-icon`. |
| `ruling:97d62cdb9ce436a7` | Resolve villain-phase steps across seats in turn order: all threat placements, then activations, then dealing/reveals; do not complete an entire villain phase per area. |
| `ruling:1aef8ab51150d2c4`, `ruling:4799b99ecc0e5dea`, `ruling:b8f301225cd5d7dc`, `ruling:7b48d5829d4d0f62` | Uniqueness/join, local players, no stage attachment/status inheritance, distinct Master of Time subtitle; also explicit in `pack:mc11:rules-clarifications`. |
| `ruling:437680ffd6d92fb8` | Stolen Memories requires printed mental discard; current `rr:wild-resource.3` confirms wild cannot substitute. |
| `ruling:a31d988626f0879e`, `ruling:57fc672ad0921420` | Continue Stolen Memories across deck reshuffle; its stored faces remain hidden. Test ordinary deck-exhaustion consequences separately. |
| `ruling:277ab401b1269ec7`, `ruling:2ace2379284bf746` | Depowered does not prohibit resolving an Invocation. Use the now-admitted Doctor Strange as a control. |
| `ruling:13454feafb65378b`, `ruling:af0e9693cc645666` | Fear of Kang restricts identity, not allies; Master of Time bonuses follow activation recipient, not engagement owner. Identity/ally distinction agrees with current `rr:you-your`. |
| `ruling:a806907d1743e0d1`, `ruling:310af249be272877` | Temporal Shield resolves before damage/tough consumption, protects only Kang in a multi-target attack. |
| `ruling:22d159cabcc98d45` | Time Portal returns to encounter deck even from Red Skull's side-scheme deck. |

Several answers are marked pre-1.5 or 1.5/1.6. Current RR 1.8 governs keyword,
you/your, resource and timing definitions; in particular overkill requires actual
defeat and piercing has a zero-damage exception. The inspected ruling IDs above
are not relationship records in `datasets/rules-graph.json` at this baseline.
When a ruling modifies/extends a general rule rather than reiterating printed
text/current RR, add the reviewed provenance relationship in the implementation
packet before relying on it as an override. This is bounded source adjudication,
not a request to reharvest the pack.

No missing source prevents packet A. Phase-end simultaneous departures, all
players at one stage eliminated, and competing stage-completion/II-defeat timing
need small authority-derived transcripts before production policy is chosen.
Use printed 3B's explicit completion on all local players defeated, plus current
player-elimination/forced-window rules; distinguish local-area failure from
whole-game defeat. If these leave an actual ambiguity, record the exact competing
orders and isolate that transition; do not infer a missing architecture or guess.

## Completion evidence and new interaction review

Required outcome matrix: Standard and Expert complete victory after I → II →
III; loss by 1B threat and 4B threat; all identities defeated; local stage-3
completion by threat and by loss of all its players while another area survives.
Local stage-3 failure adds one Dominion and rejoins; it is not itself whole-game
loss. Cover clean II defeat, mixed defeated/completed areas, multiple Dominions,
last-area/solo reunion, and III win without an unintended earlier generic win.
Use focused deterministic tests for order-sensitive cases, then complete public
session journeys for both modes; do not claim an outcome merely from setup.

For setup changes run setup-data, `RiseOfRedSkullSetupDataTests`,
`ExpansionSetupAuthorityTests`, behavior-authority/catalog generation checks and
public catalog tests on both transports. Keep expansion obligations
`outside-core`. Run ability admission/lowering/data tests as well as
characteristic rows, then managed lanes and existing Windows/Linux CI gates
appropriate to the integrated candidate. Preserve exact tested revision,
commands, mutant/restored results and artifacts in the parent evidence record.

View/Decisions/Protocol/Client/Godot should consume engine-supplied local and
central relationships, answering seat, current versus pending group, join
destinations and uniqueness choice. Inspect `View/GameAreaDescriptor.cs`,
`BoardPresentation.cs` and `BoardLayout.cs` before extending contracts. Never
infer legality from a Kang name or renderer-side counters. Use the native testing
skill for actual input and evidence: two simultaneous local villains/schemes,
shared central 2B, inaccessible cross-area target/defense, visible phase-end
join, shared-area defense after join, duplicate-unique choice, restart and
hidden stored cards. Test an answering seat other than the first player.

After fixing agent-diagnosable defects, prepare a **specific Plane owner-review
child under MARVEL-568** with exact build, accessible inspected screenshots/video
and focused questions: can the player identify their local villain/scheme,
central 2B, who must answer, why another area is inaccessible, and where/when the
group will join? Include a short optional hands-on path. This handoff makes no
Plane writes and requests no generic owner regression pass. Owner acceptance
applies to this materially new interaction; ordinary card/engine correctness
remains the implementation/reviewer pair's responsibility.

# Black Widow executable handoff — MARVEL-564

Assessment base: `1c2a63a4`, 2026-10-26. This is a source/code assessment,
not Black Widow implementation or acceptance. The generated catalog contains
**34 playable faces**, and none has an ability-book row at this revision.
MARVEL-564 remains the canonical pack issue; preserve its acceptance criteria
and existing module. The [research packet](563-568.md) and
[quantity inventory](563-568-inventory.md) remain supporting research.

**Next concrete packet: B1, a nonpublic Preparation ability-resolution receipt.**
Use test-local Widow's Bite, Widowmaker and Synth-Suit interpretations to prove
one completed Preparation ability creates one correctly owned response window.
Do not begin with whole-pack admission or re-research the starter. Independently
review this shared mechanism before authoring its dependent production rows.
The independent source-ready reprint batch A below can follow without waiting
for B1, provided shared ability-book edits remain serialized with Thor.

## Sources and scope

Printed text is [cards.json](../../../../datasets/cards/cards.json), not this
document. The generated text still says “trigger” on Widowmaker/Synth-Suit;
admit the relevant errata/ruling relationship rather than silently rewriting it.
Inspect the relevant records and their relationships in `datasets/rules-graph.json`
before adding executable expectations. Principal local authorities are:

- RR 1.8 `rr:resolve.2` (an ability triggers and at least one effect resolves),
  `.5` (constants never resolve), `.7` (fully canceled effects do not resolve),
  `rr:ability`, `rr:status-cards`, `rr:initiating-abilities`, `rr:limit`,
  `rr:triggering-condition`, `rr:reveal`, `rr:quickstrike`, `rr:surge`,
  `rr:damage`, `rr:overkill`, `rr:leaves-play`, and `rr:you-your`.
- `ruling:420bd5b1763b9506`, scope **1.7–1.8**, observed February 2026:
  Widowmaker and Synth-Suit both use **resolve**, including the missed Suit
  correction. `ruling:8e9d2bc285063f28`, scope **1.6**, October 2024,
  places Widowmaker after the Preparation and excludes a stunned replacement.
  The older contradictory `ruling:a693e5ba5379e414` is not the expected behavior.
- Attacrobatics: `ruling:1ea208998735ef4b` (1.5, May 2024),
  `ruling:0fc2a7cc9197e127` (1.6, October 2024), and
  `ruling:33d8c914aa86d19f` / `ruling:e90f1dff70bb0a76`
  (pre-1.5). Reconcile against current initiation, label and boost rules;
  old examples are not blanket authority for current timing.
- Spycraft/Espionage: `ruling:84569f1a5fcccb6f` (1.6, January 2025)
  says canceled surge offers no Espionage. `ruling:466074a868d96ed2`
  and `ruling:3b62c62b7cbcf49e` are pre-1.5; do not carry their
  immediate-extra-reveal wording into RR 1.8.

[FFG's RR 1.8 explanation](https://www.fantasyflightgames.com/mission-updates/)
was inspected on 2026-10-26. It explicitly changes overkill to transfer after
actual defeat, surge to dealing a facedown encounter card, and reveal timing
so responses wait for the reveal process. Thus `ruling:d123bfafc396ade3`
(1.5, October 2023), whose Quickstrike example inserts Bite between attack
initiation and resolution, needs current-rule reconciliation rather than direct
copying. Keep RR 1.8 forced-response ordering and the reviewed Thor entry path.

Stealth Strike's Loki question is partly settled, not a reason to reopen
overkill: `ruling:c55100b5139ab3a5` is pre-1.5. Its statement that healed
Loki was not defeated remains consistent with printed Stealth Strike and RR 1.8;
its old overkill explanation is superseded. The separate source review in
`docs/content/overkill-rr18-adjudication.md` is being integrated by the lead
(inspected read-only in `C:/Users/mggar/Source/marvel-overkill-rr18`).
Its test-local full Stealth Strike trial is a **failed capability probe**, not
authored-card evidence: choosing a scheme inside the labeled attack hits the
explicit unsupported suspension; moving the follow-up outside loses the defeat
receipt. B2 below owns that real prerequisite, independently of overkill.

The starter is already verified in
[`mc07/UPSTREAM.md`](../../../../datasets/rules-packs/mc07/UPSTREAM.md),
with the original divider SHA-256
`47b74c8eb47614e6a247f273e88eb15217f884138f7082519430a31aeffcc728`.
The earlier broad research narrative's starter-candidate warning is stale:
**15 hero + 17 Justice + 8 basic = 40 player cards**, `08002–08024`.
Identity `08001a/b` is separate; obligation `08025×1` and nemesis
`08026×1,08027×1,08028×2,08029×1` stay outside the player deck.
Customization `08030–08033×3 each` is complete-pack scope, separately from
the printed starter. Dividers, tokens and marketing totals add no packaging gate.

## Inspected reusable contracts

Existing operations are evidence of a candidate composition, not card coverage.
The face table distinguishes **A** (source-ready existing/reprint composition),
**C** (composition requires its first focused probe), and **P** (a concrete
shared capability is missing). Every row still needs actual-book verification.

- Thor's `AbilitySelfPlayCost`/`AbilitySelfPlayCostLowering` and
  `SelfPlayCostTests`/`SelfPlayCostReviewTests` supply a pure in-hand pricing
  seam, but only `ReduceSelfCostPerEngagedMinion`; they do not yet price Winter
  Soldier by controlled Preparation cards.
- `cardsInPlay`, `cardsYouControl`, `upgradesYouControl`, `withTrait`,
  `cardsIn`, `maxBy`, `count` and `modified` exist in `AbilitySelectorLowering`,
  `AbilityLowering` and their evaluators. `InPlayCardCensusTests` and its
  review cover the Thor census. Reuse them for ownership and live card kinds;
  do not count hand/discard or the other seat's cards.
- `StepConditions` includes `WhenCardPlayed`, `WhenCardEntersPlay`,
  `WhenSchemeEnds`, damage and defeat conditions, and individual boost-faceup
  conditions. There is **no ability-resolved condition**. The execution ledger
  already records ability resolution status; it is not itself a response window.
  B1 must bridge the real completion boundary, not infer it from card departure.
- Thor payment/entry/continuation work and the existing `EventCardEntryCompletionTests`,
  `EncounterEntryContinuationTests`, `MinionEntryArrowCostTests`, and
  `WindowResourcePaymentContinuationTests` are regression consumers. Retain
  source incarnation, resolving seat and one-use commitments across suspension.
- `BoostFaceupOperation`, `cancelBoostIcons` and `cancelledBoostIcons` receipts
  already support numeric cancellation. `IndividualBoostCancellationTests` and
  `BoostCancellationIndependentReviewTests` are the starting evidence for
  Attacrobatics, not a missing general boost-faceup mechanism. Canceling a
  star Boost ability is a different operation, absent from `AbilityFixedInstruction`.
- `ResourceSourceEligibility` supports unrestricted, event, Arrow-event and
  aspect-card restrictions, but not Preparation-card restrictions. Preserve the
  existing source/payment/retry contracts when adding B3.
- `AbilityPowerSuspension` rejects arbitrary choice inside a labeled power;
  `AbilityPowerResolution` explicitly raises. `attackAnotherEnemy`/Melee's
  staged continuation remains **one attack**, not Dance of Death's three.
- `PrintedPlayRestrictions.IdentityTrait` already supplies Quincarrier's
  identity-Avenger gate. It does not express Spycraft/Espionage's controlled
  Spy-character gate. `RevealKeywords` resolves surge at a keyword ordinal;
  it exposes no dedicated surge-resolution interrupt condition to authored rows.

## All 34 faces and first distinguishing probes

| Face | Card | Route | Required actual-row observation |
|---|---|---|---|
| `08001a` | Black Widow | **P B1** | A resolved own Preparation ability offers one damage after completion; another seat's Preparation, mere play, constant, canceled attack and fully canceled ability offer none. Choose a different enemy from Bite's target. |
| `08001b` | Natasha Romanoff | **C**, play window + trait + phase limit | Playing a Preparation draws once; put-into-play does not; other seat excluded; form changes do not refresh use and both phase transitions do. |
| `08002` | Winter Soldier | **P B4**, reuse Thor pricing/census | Zero/two/five own Preparations price four/two/zero; an ally/support Spy is not a Preparation; other seat and discard do not reduce; repeated price queries consume nothing. |
| `08003` | Covert Ops | **C**, labeled thwart plus confuse | Four threat removal and confusion in either form; current ruling permits confusion-only when no legal threat-removal target. Do not require a scheme choice that prevents the legal partial effect. |
| `08004` | Dance of Death | **C**, three separate attack continuations | Choose targets anew for 1/2/3 attacks; resolve intervening defeat/retaliate responses; apply modifiers and stun at the correct individual attack. A single 6-damage or one three-part attack is a failing control. |
| `08005` | Safe House #29 | **C**, filtered discard selection + exhaust | Return a non-first own Preparation; no eligible card cannot exhaust; stale/foreign selection changes neither source nor discard. Validate the arrow's chosen card before commitment. |
| `08006` | Attacrobatics | **C** numeric boost receipt, then **B1** integration | Cancel the exact faceup card's current numeric icons, retain its star ability and later-added icons; deal exactly canceled count. Zero icons/previous cancellation cannot pay; test Guard, stun and attack/scheme context against current authority. |
| `08007` | Black Widow's Gauntlet | **P B3** | Wild pays a Preparation card; a same-cost non-Preparation and standalone unrelated ability cost cannot spend it; both forms allowed; suspension cannot reuse the exhausted source. |
| `08008` | Grappling Hook | **C** reveal cancellation, then **B1** | Own treachery reveal cancels effects/discards once, preserving later follow-ups; another seat's reveal excluded. Use full reveal lifecycle and a surge-bearing control, not a direct When Revealed call. |
| `08009` | Synth-Suit | **P B1** plus named DEF constant | Only Black Widow gets +1 DEF; resolved Preparation offers optional exhaust/ready after it finishes; both response orders with Widowmaker work; no ready from a canceled Preparation or exhausted Suit. |
| `08010` | Widow's Bite | **C** minion-entry labeled attack, then **B1** | Damage/stun the exact entered minion even at another seat; entry is not reveal-only; lethal damage never stuns a reentered copy. Verify RR 1.8 Quickstrike/reveal ordering, including When Revealed and stun replacement. |
| `08011` | Agent Coulson | **C**, entry search composition | Choose Preparation from own deck or discard, add physical choice and always shuffle; missing match still shuffles; another player's copy excluded. |
| `08012` | Quake | **C**, `WhenSchemeEnds` | After a minion finishes scheming, exhaust to damage that same minion; villain scheme excluded; another seat's minion can qualify; departed/reentered subject cannot redirect the effect. |
| `08013` | Stealth Strike | **P B2** | Full printed-cost four-damage attack defeats ordinary minion then permits choosing either scheme for two removal; healed Loki/survivor yields none; suspended defeat response and save/retry retain the causal defeat. |
| `08014` | The Power of Justice | **A**, copy `01062` | Two wild for Justice printed-card payment, one for unrelated cost; Max 2 deck legality follows printed catalog. |
| `08015` | Interrogation Room | **A**, copy existing same-title row | Credited minion defeat offers exhaust/remove one; another seat's defeat excluded; use reviewed Hall of Heroes responsibility semantics for allied/nonattack credit. |
| `08016` | Surveillance Team | **A**, copy existing same-title row | Three snoop counters, exhaust/remove one and remove one threat; final counter discards source yet committed effect finishes; empty/exhausted cannot pay. |
| `08017` | Counterintelligence | **C**, `ThreatWouldBePlaced`/`preventThreat` | Prevent up to three from one main-scheme placement in either form, including phase step one and ability placement; side scheme excluded; multiple copies cannot overprevent by stale amount. B1 follows resolved use. |
| `08018` | Spycraft | **P B5** play gate + **C** cancellation/reveal | An own Spy ally permits play even without Spy identity; no controlled Spy rejects. Cancel/discard one own encounter card, including surge, then reveal exactly one replacement; preserve seat through nested reveal. B1 integration follows. |
| `08019` | Nick Fury | **A**, copy `01084` | All three entry options work, then end-round discard applies to the retained incarnation only; Rapid Response reentry does not inherit an old delayed discard. |
| `08020` | Energy | **A**, explicit empty abilities | Actual reprint produces `YY`; same-title deck maximum remains one across printings. |
| `08021` | Genius | **A**, explicit empty abilities | Actual reprint produces `BB`; no invented resource ability. |
| `08022` | Strength | **A**, explicit empty abilities | Actual reprint produces `RR`; no invented resource ability. |
| `08023` | Quincarrier | **C**, existing identity-trait play gate/generator | Avenger identity allows play, a controlled Avenger ally alone does not; form matters at play-restriction check; generator is usable in either form after legal play. |
| `08024` | Target Acquired | **P B6** | At faceup response cancel that physical card's Boost ability only; preserve numeric icons, other boost cards and reveal text; no star ability cannot produce a significant effect. B1 follows resolved use. |
| `08025` | Burn Notice | **C**, obligation + ranked own Preparation | Non-first Natasha receives it; optional flip then exhaust/remove branch; alternative chooses among tied highest printed costs, excludes other seat/non-Preparation, gains surge only when discard cannot occur. Full cleanup/deal lifecycle. |
| `08026` | Taskmaster | **C**, owned upgrade census + scoped boost modifier | Live minion ATK/SCH count engaged player's upgrades, including changing count; boost counts activation's player, modifies correct ATK/SCH through suspended choices, expires after activation. |
| `08027` | Killer for Hire | **A**, revealed per-player threat | Printed three plus one per player is four at one seat/five at two; printed acceleration remains live; do not replace starting threat with the extra amount. |
| `08028` | Hydra Mercenary | **A**, explicit empty abilities/printed Guard | Entry protects villain only from engaged hero's attacks, including labeled attacks; another seat remains eligible and nonattack damage is not Guard-blocked. |
| `08029` | Deadly Shot | **C**, own upgrade choice + form branch | Other seat's upgrades excluded; alter-ego adds one main threat, hero takes one damage; no upgrade does not skip the later nondependent sentence; form evaluated in reveal context. |
| `08030` | Counterattack | **C**, received-attack-damage receipt/labeled return | Return the actual damage taken, not printed/calculated/dealt-before-prevention amount, to the original attacker; boost damage and ally damage excluded; attacker departure cannot hit its new incarnation. B1 follows resolved use. |
| `08031` | Rapid Response | **C**, defeated-ally receipt/reentry | Exact own defeated ally returns from own discard as a new incarnation, enters normally, then takes one damage; ordinary discard is not defeat; other seat's ally excluded; handle entry responses and one-HP ally defeat. B1 follows. |
| `08032` | Defensive Stance | **C**, damage-taken interrupt/prevention | Discard to prevent up to three damage hero would take, including nonattack damage; alter-ego and ally targets excluded; Tough/other prevention ordering respects current rule. B1 follows. |
| `08033` | Espionage | **P B5 + B7** | Controlled Spy permits play; interrupt actual surge resolution to draw two, once per discarded copy; canceled surge offers none; RR 1.8 deals the extra facedown card and never immediately reveals it in player phase. B1 follows. |

## Bounded engine and authoring packets

Paths below are proposed ownership boundaries, not permission for overlapping
workers. Serialize scheduler/interpreter changes and ability-book writes.
Each packet returns source, focused behavioral tests, normal-hook commit and
evidence; a separate bounded reviewer checks raw authorities and plausible wrong
implementations. Stop after one packet or the assigned checkpoint, without
recursive delegation. A failed composition probe narrows the next engine packet;
it does not license unsupported approximation or whole-pack readiness claims.

**B1 — first shared prerequisite: resolved-ability receipt.** First admit the
card clarification in `datasets/rules-graph.json` with its pinned ruling/card
hashes and provenance tests: the inspected graph has no Widowmaker/Synth-Suit
relationship at this base. This is a bounded source-admission step using already
vendored evidence, not new acquisition or a generated-card hand edit. Then own
`src/Marvel.Cards/Run/AbilityResolutionExecution.cs`, completion/support and
resource completion owners as actually needed; introduce a cohesive receipt
owner alongside `src/Marvel.Rules/Play/StepConditions.cs` and the typed agenda
operations/occurrence family, plus explicit matching/lowering in
`src/Marvel.Cards/Dsl/AbilityTriggerReader.cs` and its matching owner.
Tests: new `tests/Marvel.Content.Tests/Play/PreparationResolutionTests.cs` and
fixture, with focused receipt tests beside existing resolution-status tests.
First probe: seat B resolves Bite, discards its Preparation as cost, completes
damage/stun and nested defeat work, then offers Widowmaker and Suit in either
order. Retain original source face/traits/controller/incarnation despite discard;
constant, play-only, declined, stunned/fully canceled, and other-seat controls
must not fire. Use the existing execution status ledger to decide resolution,
not “card left play” or “ability started.” Prove exactly once across nested
choices, invalid answers, restart/replay and optional decline. Initial mechanism
tests are nonpublic test-local rows. Add production rows only after review.

**B2 — retained attack defeat through follow-up scheme choice.** Own
`src/Marvel.Cards/Run/AbilityPowerSuspension.cs`, `AbilityPowerResolution.cs`,
`AbilityPowerContinuation.cs`, `AbilityDefeatReceiptProjection.cs` and their
continuation capture/decoding owners only as required; new
`tests/Marvel.Content.Tests/Play/StealthStrikeContinuationTests.cs`.
Reproduce the failed full printed-card trial from the overkill adjudication
first. The fix must allow the legitimate scheme choice while retaining which
enemy/incarnation this attack actually defeated. Test ordinary lethal,
nonlethal, Loki treachery heal, no eligible scheme, suspended defeat response,
invalid/foreign scheme and replay/restart. Reject a mutant that grants threat
removal from lethal arithmetic alone. Do not change overkill or weaken the
unsupported-suspension guard globally to make the row execute.

**B3 — Preparation-restricted resource.** Own
`src/Marvel.Rules/Prompts/ResourceRestriction.cs`,
`src/Marvel.Rules/Play/ResourceSourceEligibility.cs`,
`src/Marvel.Cards/Dsl/AbilityResourceRestriction.cs`, relevant source semantic
descriptions and `tests/Marvel.Content.Tests/Play/RestrictedResourceTests.cs`
or cohesive new tests. Reuse Thor's real payment/source ledger. Same-cost
Preparation/non-Preparation controls, either form, wild declaration, two
Gauntlets, nested generator use, stale payment and query purity distinguish it.

**B4 — Winter Soldier self pricing.** Extend the reviewed self-cost seam in
`src/Marvel.Cards/Dsl/AbilitySelfPlayCostLowering.cs`,
`src/Marvel.Cards/Run/AbilitySelfPlayCost.cs` and the corresponding effect
model/lowering, with `SelfPlayCostTests`/new Soldier tests. Reuse census and
final pricing validation; zero floor, other seat, changing live Preparation
count, discount composition, uniqueness/ally limit and query/RNG purity matter.

**B5 — controlled-character trait play restriction.** Own
`src/Marvel.Content/PrintedPlayRestrictions.cs`, the facts contract and engine
card-play validation owners if needed; extend existing admission tests using
actual Spycraft/Espionage and same-cost legal controls. A Spy identity in either
form or own Spy ally qualifies; another seat's Spy, discarded Spy, and a
noncharacter with the trait do not. Keep legality in engine contracts, never UI.

**B6 — exact boost-ability cancellation.** Own
`src/Marvel.Rules/Play/BoostFaceupOperation.cs`, the typed boost completion and
execution owners, `AbilityFixedInstruction`/lowering/execution as needed, and
new focused content tests adjacent to individual boost cancellation tests.
First probe Target Acquired in its printed **Response**, before the star effect
runs: cancel one star while numeric icons remain. Preserve timing tier, multiple
faceup cards, attack/scheme ownership, source receipt, stale rejection and B1.
Do not reuse numeric icon cancellation as star cancellation.

**B7 — actual surge-resolution interrupt.** Own
`src/Marvel.Rules/Play/RevealKeywords.cs`, `RevealProcedure.cs`, the typed
reveal-ability/agenda operations and `StepConditions` only as required, plus
new `EspionageSurgeTests`. Interrupt one exact keyword resolution, preserving
reveal ownership, optional multiple copies, cancellation and encountered-deck
reset. Base behavior is RR 1.8 facedown dealing. `WhenCardRevealed` alone is too
early and cannot distinguish a canceled keyword. Integrate B5 and B1 afterward.

**A — first source-ready authoring batch.** Append only
`08014,08015,08016,08019,08020,08021,08022,08027,08028` to
`datasets/abilities/abilities.json`; add matching authored-face/starting-counter
inventory updates and actual-row content tests. Existing same-title rows remain
the implementation patterns, generated text the authority. No new setup or
admission policy in this packet. Review reprint IDs and quantities, independent
minion-defeat responsibility, final counter, Nick Fury incarnation and Guard.
`08029` can join only after its C probe passes. Other C rows should be authored
in small coherent groups after their named first probe, with B1 integration for
every Preparation. Dance of Death and Covert Ops deserve separate composition
reviews because labels/partial effects distinguish them from familiar events.

## Public completion and evidence

Until all 34 faces and the exact recipe are implemented and reviewed, keep
Black Widow out of public product discovery and setup. A test-local book is
nonpublic mechanism evidence. Do not add an admission bypass to manufacture an
early public slice. Thor MARVEL-563 is an integration dependency for its reviewed
shared contracts, not a fresh research dependency; rebase on the lead's integrated
revision and run the affected Thor/Core regression consumers before delivery.

After complete coverage, one admission/setup packet owns
`datasets/setup/setup.json`, its `UPSTREAM.md`,
`src/Marvel.Content/Setup/RuntimeProductAdmission.cs`,
`StandaloneProductAdmission.cs`/recipe owners, and their setup/catalog/server
tests. Follow existing complete-product tests: remove a required customization
or nemesis face, alter a required starter quantity, and prove the entire product
closes. Preserve legal custom decks and adjacent unsupported products. Update
the reviewed expansion setup authority inventory/adjudication, offline behavioral
catalog and global `RiseOfRedSkullSetupDataTests` counts without claiming
executable Core transcripts for expansion recipes.

Public proof uses the production catalog → legal deck → game/session factory:
one through four seats in supported Core/Rise of Red Skull scenarios, exact
starter and legal customization; Mission Prep play, a Preparation ability,
Widowmaker/Suit order, ordinary nemesis/obligation lifecycle, and save/restart
with a pending nested response. Compare the uninterrupted control's events,
state/RNG and pending work. Exercise client/embedded/socket paths for affected
contracts; extend View/Decisions/Protocol only where the engine's semantic facts
do not explain source, answering seat, optionality, committed discard/exhaust,
known damage and next choice.

Verification per packet: focused behavioral tests with readable `[Rule]` clauses,
actual-row data/admission tests, compiled distinguishing mutants for new decisions,
restored nonincremental verification, and architecture/normal hooks. At integration
run the required managed lanes and existing Windows/Linux gates against one exact
candidate; record failures and untested scope rather than combining old passes.
Native scripts must use the real viewport and inspected media for changed
interaction. Generic source/target/payment/reaction flows need regression evidence;
they add no automatic owner-approval gate. Reserve a focused owner review item
with exact build, accessible screenshots/video and concrete questions only if the
working nested Preparation response flow introduces a materially new interaction
or leaves a specific judgment unresolved. Reuse Thor's native contract work.

This handoff itself was checked for inventory coverage, source links and owned
paths only. No Black Widow gameplay, public admission or native acceptance has
been executed by this documentation packet.

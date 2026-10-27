# Hulk executable handoff — MARVEL-566

Research baseline: **`95e3ae8a`**, 2026-10-26. The current executable state and
source/reviewer revisions are in the [delivery checkpoint](../../hulk-delivery-checkpoint.md).
Local nonpublic Hulk authoring and reviewed prerequisite integration proceed from
the frozen Doctor Strange candidate during its CI run. Public Hulk admission
follows complete pack coverage and Doctor Strange acceptance. The canonical
pack record remains MARVEL-566; this handoff supplies authority and face scope,
while the delivery checkpoint supersedes historical sequencing and gap claims.

## Complete playable scope and sources

The generated `datasets/cards/cards.json` contains exactly **33 `hlk` faces**.
The [existing inventory](563-568-inventory.md) supplies all printed text and
quantities; the [research packet](563-568.md#marvel-566-hulk--hero) supplies the
earlier research. Its outstanding divider check is superseded by
`datasets/rules-packs/mc09/UPSTREAM.md`: the original divider is already pinned,
visually reviewed and mapped to canonical IDs. Its local
`hulk-deck-divider.jpg` SHA-256 is
`8babe0b3a1862b7d4b34f7e06d4591a426a13df98932f7040264d9a6c2efd278`,
rechecked here. MARVEL-702's remaining acquisition queue does not block Hulk.

The 40-card original starter is 15 signature copies (`10002–10010`), 17
Aggression copies (`10011–10018`) and 8 Basic copies (`10019–10024`). The
identity `10001a/b` is separate. Add Inner Demons `10025` ×1 and the five-copy
nemesis set (`10026` ×1, `10027` ×1, `10028` ×3) through their normal setup
paths. All four extras `10029–10032` ×3 are part of complete pack coverage and
custom decks, although absent from the original starter. This is 59 physical
cards and 60 summed face quantities; neither a packaging count nor extra
starter copies is a gate. There is no Hulk scenario, campaign or auxiliary deck.

Authority for authoring is local printed text plus current RR **1.8** and
applicable vendored rulings. In particular use `rr:wild-resource.1` and `.3`,
`rr:forced.4–.6`, `rr:interrupt`, `rr:attack-enemy-activation.1.1–.3`,
`rr:stunned`, `rr:then`, and `rr:overkill.1–.4`. Read the actual clauses and
quote the deciding clause beside new rule-cited tests.

## Exact face-to-work map

Every ID below needs its own admitted ability-book face; reprints do not gain
coverage from a matching title. **R** is a directly reusable authored row;
**A** is actual card authoring using existing primitives, with characteristic
tests; **P** names a demonstrated capability boundary below; **Q** is a bounded
composition probe before deciding whether an engine change is needed. No P or Q
means a new generic engine subsystem is authorized.

| Face | Copies / role | Execution map |
|---|---|---|
| 10001a Hulk | 1 identity face | A/Q1: forced interrupt at `WhenPlayerTurnEnds`; discard the entire current hand before turn advance. |
| 10001b Bruce Banner | 1 reverse face | A: draw, then selected hand discard; once-per-round action. Existing `discardHandCards` and limits. |
| 10002 Crushing Blow | 2 signature | P1: physical-only adjusted printed payment; existing attack damage from current modified hero ATK. |
| 10003 Hulk Smash | 2 signature | A + P2: reuse `09037` / `06015` basic-use interrupt for +10; payment-conditioned overkill must bind the same attack. |
| 10004 Sub-Orbital Leap | 2 signature | A/Q2: settled-payment branch removes 3 or 5 threat; use existing conditional/preflight path. |
| 10005 Thunderclap | 2 signature | A: choose up to three distinct enemies and resolve three damage each; existing multi-recipient damage machinery. |
| 10006 Unstoppable Force | 2 signature | A/Q2: ready Hulk, then conditional draw from settled payment. Do not make draw depend on ready completion. |
| 10007 Limitless Strength | 2 signature | Q3: printed `RRR`, usable for resources only in hero form; inspect/extend source eligibility, never blank its printed icons globally. |
| 10008 Banner's Laboratory | 1 signature | A: Bruce-only +2 REC constant and alter-ego exhaust resource ability generating mental. |
| 10009 Boundless Rage | 1 signature | A: hero-only play, Hulk +1 ATK, forced response discarding this physical upgrade after form change. |
| 10010 Immovable Object | 1 signature | A: identity +4 HP and Hulk-only retaliate 1; preserve form distinction. |
| 10011 Brawn | 1 starter | A: completed ally attack response choosing a scheme for one threat removal. |
| 10012 Sentry | 1 starter | A: forced entry response deals controller one encounter card; no reveal and no duplicate entry receipt. |
| 10013 She-Hulk | 1 starter | A: continuous ATK equal to printed base plus damage tokens; unrelated to She-Hulk identity text. |
| 10014 Drop Kick | 3 starter | A/Q2: 4 attack damage, then physical-only payment branch stuns that enemy and draws; departed-target handling uses existing sequence semantics. |
| 10015 Toe to Toe | 3 starter | A/Q4: chosen enemy attack followed by 5 attack damage to that same enemy, including canceled-attack control. |
| 10016 "You'll Pay for That!" | 3 starter | A/Q4: villain attack response, actual damage taken by you capped at 5; existing occurrence damage receipt, not enemy ATK or total damage to an ally. |
| 10017 The Power of Aggression | 2 starter | R: copy `01055` / `06016` conditional resource doubling, retaining Hulk ID and printed deck maximum. |
| 10018 Martial Prowess | 3 starter | P3: exhaust for physical only toward an Attack event; existing any-player placement and one-per-player maximum. |
| 10019 To the Rescue! | 3 starter | A: ordinary hero thwart for 2. |
| 10020 Energy | 1 starter | R: `01088`, empty ability list and printed `YY`, max 1. |
| 10021 Genius | 1 starter | R: `01089`, empty ability list and printed `BB`, max 1. |
| 10022 Strength | 1 starter | R: `01090`, empty ability list and printed `RR`, max 1. |
| 10023 Avengers Mansion | 1 starter | R: `01091`, exact exhaust/choose-player/draw row. |
| 10024 Helicarrier | 1 starter | R: `01092`, selected player's next-card phase discount, consumed only on play. |
| 10025 Inner Demons | 1 obligation | A: normal GiveTo routing, mandatory flip from either form, then branch on resulting form: Banner discards up to two hand cards; Hulk exhausts. Discard obligation. |
| 10026 Abomination | 1 nemesis | A: after its attack, discard top player card, inspect printed physical icon and damage that player if present; printed wild is not physical here. |
| 10027 Total Destruction | 1 nemesis | A: starting threat 2 per player, hazard icon and threat-removal prohibition while Abomination is in play. |
| 10028 Clash Of The Titans | 3 nemesis | Q5/P4: rank enemy and hero/ally pools by current ATK, preserve all ties for first player, attack exact character, surge only if no attack made. |
| 10029 Beat Cop | 3 extra | A: exhaust and move one threat onto this support; exhaust/discard for damage to a minion using captured threat count before source departure. |
| 10030 Inspiring Presence | 3 extra | A: Avenger identity play condition, heal ally then ready it; avoid inventing a `then` prerequisite absent from text. |
| 10031 Electrostatic Armor | 3 extra | A/Q4: any-player placement, max one, completed defense response damages actual attacking character. |
| 10032 Resourceful | 3 extra | A: discard-source resource ability for wild through existing prepared cost and generation receipt. |

## Reuse, demonstrated boundaries and bounded probes

### Q1 — Enraged is authoring before it is a timing prerequisite

`src/Marvel.Rules/Play/PlayerTurnTiming.cs` names the end-turn occurrence.
`tests/Marvel.Content.Tests/Play/CampaignObligationTests.cs` proves a suspended
choice completes before the active seat advances; `PlayerTurnTests.cs` proves
turn/phase ordering. `SelectedHandDiscardAreaReviewTests.cs` and the
`discardHandCards` contract in `docs/card-dsl.md` cover selected groups, capping
and zero-card completion. Use the current hand count or a direct whole-hand
selection through the existing DSL, not a fixed hand-size number.

First prove actual Enraged with two cards, empty hand, Banner form and Hulk as
non-first player. It is a **Forced Interrupt**, so it precedes optional
interrupts and all responses to that occurrence. An optional end-turn response
that subsequently draws a card does not retrospectively add it to Enraged's
already completed discard. The earlier research's phrase "after an optional
end-turn response" is not the ordering authority. A simultaneous forced
interrupt must use the existing first-player ordering rule. Only a failing
actual composition justifies changing turn timing.

### Q2 / P1 / P3 — resources retain declarations; restrictions need distinct treatment

`src/Marvel.Cards/Run/AbilityEventPayment.cs`, `ResourceAllocationLedger.cs`,
`AbilityPaidResourceQuery.cs` and `AbilityPaymentReceiptAdmission.cs` retain
settled paid types. `MsMarvelPaymentReviewTests.cs`,
`VariableEventPaymentReviewTests.cs`, and
`tests/Marvel.Rules.Tests/Prompts/ResourcePaymentTests.cs` supply existing
branch/preflight and allocation evidence. `paidWithResource` means **includes**
that type, not "paid exclusively with it". Compose the existing boolean
conditions over declared paid types for the enhancement if that faithfully
covers the full payment; do not add a duplicate receipt system.

Characteristic probe: Leap with physical plus wild declared physical removes 5;
the same wild declared wild or a mixed mental payment removes 3. Include doubled
wild resources with separate declarations and overpayment with unused icons.
Distinguish resources generated from resources actually spent. Confirm the
zero-adjusted-price case from the current authority before fixing the predicate;
do not silently impose a nonempty-payment condition or infer one from an empty
preview receipt. Existing preflight must explore payment-dependent effects
before costs are committed, and rejected answers preserve the decision state.

**P1 is narrower than typed payment:** `AbilityPaymentPricing.EventPrice` derives
printed requirements from `Resources.Required`, which implements printed
Requirement icons, and does not express Crushing Blow's restriction on every
resource used toward its adjusted printed price. Existing repeated-resource
components in `ResourceCost.cs` are reusable. Prove legal physical/wild-physical
payment and rejection of mental/mixed payment before any discard, including a
Helicarrier reduction; do not model a separate arrow cost or require the
undiscounted number of physical resources.

**P3 is a concrete source restriction gap:**
`src/Marvel.Rules/Prompts/ResourceRestriction.cs` and
`src/Marvel.Cards/Dsl/AbilityResourceRestriction.cs` admit ArrowEvent,
AspectCard, Event and PreparationCard, with no AttackEvent case. Extend this
existing restriction owner for Martial Prowess rather than permitting all
events. Reuse `RestrictedResourceTests.cs`, `RestrictedResourceAdmissionTests.cs`
and `PreparationResourceIndependentTests.cs` as regression controls. Prove use
for Drop Kick and Crushing Blow; reject Hulk Smash (it has only the Superpower
trait), a thwart event, basic attack and unrelated
ability cost. Resourceful and Banner's Laboratory remain ordinary resource
ability authoring. Q3 separately inspects printed hand-source eligibility for
Limitless Strength, including switching form while payment is pending.
`AbilityProgramQueries.ResourcesGeneratedBy` currently returns printed icons
and optional class doubling without a form check; `AbilityOfferQueries` checks
form for resource abilities. A resource ability's `form` guard alone therefore
does not establish the required printed hand-source restriction. Trace the
ordinary hand-source caller and isolate a failing source-selection probe before
choosing the narrow eligibility repair.

### P2 — one basic attack's bonus and keyword

Actual rows `09037` Skilled Strike and `06015` Mean Swing,
`BasicUseWeaponTests.cs` / `BasicUseWeaponReviewTests.cs`, and
`src/Marvel.Cards/Run/AbilityImmediateExecution.cs` prove the existing
`WhenHeroBasicAttackInitiated` / `boostBasicUse` occurrence-bound bonus.
`ScopedAttackKeywordTests.cs` and `src/Marvel.Rules/Play/AttackKeywords.cs`
prove constant basic/source-scoped keyword grants. However,
`src/Marvel.Cards/Dsl/AbilityAttackKeywordLowering.cs` explicitly rejects
`attackScope` on a conditional or activated grant. A permanent/basic-wide grant
or a round-duration effect does not implement Hulk Smash's **that attack**.

Extend the exact basic-use receipt with a conditional overkill grant, retaining
the settled payment through suspension. Prove +10 and overkill on one attack,
no spill after prevention/Tough prevents defeat, no keyword/bonus on a second
readied attack or a card attack, and nonphysical payment retaining +10 without
overkill. Current `rr:overkill.1` requires actual defeat. Reuse
`LokiRr18OverkillTests.cs`; historical pre-1.8 excess-damage answers do not justify
changing this rule. Do not reopen the rejected Loki interpretation.

### Q4 / Q5 / P4 — attack continuation and exact recipients

For Toe to Toe use an ordinary sequence, **not an arrow cost or dependent
`then`**. FAQ `10015` (updated 2020-08-28) and
`ruling:f182a5fac69b442d` (pre-1.5) say stun cancellation still permits its 5
damage. This agrees with current `rr:stunned` and the printed absence of
`then`; it does not supersede RR 1.8. Reuse existing static `enemyAttacks`
continuations and `WreckingActivationCompletionTests.cs`,
`NestedActivationOrderChoiceTests.cs` and `AttackChainTests.cs`. Test stunned
enemy, a completed defended attack, boost/response suspension and the chosen
enemy's departure. Finish the full activation before checking the following
damage. For You'll Pay and Electrostatic Armor, use captured causal attack
damage/defense/attacker facts rather than current board ATK or current defender.

`maxBy` already ranks modified `attack` and retains ties (`docs/card-dsl.md`;
`ActionAbility.RepeatedTrace.6.ARankedSelector.Tests.cs`). Q5 should compose two
ranked pools and explicit first-player choice, including a non-resolving first
player, tied enemies, tied hero/ally and a single maximum. Do not select the
first object ID. **P4 concerns the attacked character, not generic ranking:**
the authored activation target `src/Marvel.Cards/Run/ActivationTarget.cs`
contains only enemy and seat, so this seam cannot alone retain Clash's selected
ally. Trace the existing lower-level attack target support before extending the
DSL to carry the exact character and its owner through defense and continuation.
No new generic enemy-attack subsystem is indicated.

Historical `ruling:4579a25303904b85` (pre-1.5) assigns an undefended Clash attack
to the selected ally. Current `rr:attack-enemy-activation.1.1–.3` explicitly
permits an ability to target an ally and describes target changes on defense;
use that current rule as the deciding authority. Prove an undefended selected
ally takes damage, another character defending changes the target, and the
surge branch distinguishes an attack actually made from stun cancellation.
These are execution probes, not evidence that the existing ranking is absent.

For Beat Cop, reuse threat movement choice behavior established by
`ThreatMovePairChoiceTests.cs` and `ThreatMovePairReviewTests.cs`, with actual
support-host threat storage and a captured pre-discard amount in its authored
card tests. This is a card composition task, not evidence for a new generic
counter-transfer prerequisite.

## First reviewable packet and delivery gates

After Doctor Strange integration, first take **actual Enraged and Bruce Banner
plus Sub-Orbital Leap** in nonpublic engine tests. Own only those authored rows,
their focused tests and the smallest demonstrated payment-condition repair, if
one is required. This packet establishes hand/turn ordering and all-physical
declared-payment branching before bulk signature authoring. It does not admit
Hulk or add a public setup bypass. Review against raw local card/RR authority;
new decisions earn distinguishing tests and compiled/restored mutants under
normal repository policy. Next serialize payment restriction packets P1/P3 and
the Hulk Smash receipt packet P2, then complete ordinary rows and the separate
obligation/nemesis/extra compositions. Do not file engine issues from an
unexecuted hypothesis; carry Q results into the existing canonical work record.

Once all 33 faces are reviewed, author the source-qualified exact starter,
signature/obligation/nemesis setup records and whole-pack admission. Follow
`docs/setup-dataset.md` and the pack skill's authority-inventory requirements:
setup/ability data checks, expansion adjudication and behavior-catalog totals,
global recipe inventory, and both-transport public catalog counts. Prove a
missing required face and a wrong/incomplete recipe reject the whole pack;
legal custom decks include extras with correct quantities while supported
identity faces have zero deck quantity. Keep adjacent unsupported content
excluded. Do not advertise a partly authored pack.

Use the normal `DatasetGameFactory` and session path for the verified 40-card
starter, representative complete games, obligation/nemesis and characteristic
payment/attack examples. Record seeds, exact revision, outcomes and replay
artifacts. Hero acceptance needs representative complete games, not a newly
invented Hulk-specific victory requirement. Fresh-host continuation should
cover end-turn discard, a declared payment/conditional branch and Hulk Smash's
single attack; compare prompts and causal events as well as final digest.
Preserve genuine historical replay bytes and incompatible negative controls.

Native work is proportional: inspect payment type/bonus explanations, the
forced hand discard and any new exact-character/tie choice through actual
viewport input under the native testing skill. Reuse established ordinary card
interactions. If existing contracts lack the necessary meaning, extend the
owning visibility-safe contract; never reconstruct legality in Godot. Ordinary
Hulk content does not create a generic owner-approval gate. A materially new
interaction or concrete judgment concern gets its own focused review evidence.
Finish with the repository's required managed/platform/CI gates against one
integrated candidate, keeping untested boundaries explicit.

## Handoff evidence boundary

This pass inspected local inventory, pinned starter, generated printed cards,
authored rows, current RR, FAQ/rulings and the named mechanisms/tests. It ran no
runtime tests and changed no engine, setup or ability data. The capability
observations are source evidence, not newly passing actual-Hulk probes. The
optional Plane lookup using `MARVEL-566` failed CLI identifier resolution (404);
no Plane description or state was changed. All shell calls used the Windows
PTY path and were harvested; no helper or test process remains running.

# Doctor Strange executable handoff — MARVEL-565

Assessment base: `29aa293f`, 2026-10-26. This is an implementation packet,
not implementation or acceptance. All **40 generated faces** remain without
`090xx` ability-book rows at this revision. Preserve MARVEL-565 as the canonical
complete-pack issue and its acceptance criteria. The existing
[research](563-568.md) and [face/quantity inventory](563-568-inventory.md)
remain the full printed-text ledger; this packet supersedes their stale starter
warning and adds current capability/authority decisions.

**Next packet D1: a nonpublic, player-owned Invocation lifecycle, using Winds of
Watoomb as the first Special.** Prove ordinary Spell Mastery and the different
Master of the Mystic Arts continuation against that same physical card. Review
the shared mechanism before bulk authoring. Do not admit the incomplete pack or
add a setup bypass to call this a public slice.

## Sources already present, and exact remaining source work

Printed behavior comes from `datasets/cards/cards.json`. The printed starter is
already visually verified and pinned in
[`mc08/UPSTREAM.md`](../../../../datasets/rules-packs/mc08/UPSTREAM.md): divider
SHA-256 `81a2470bfb508bc0971b1e1289ccb7c7a1368fcb04d9d689575cdf47b93fa402`.
It establishes **15 signature + 17 Protection + 8 basic = 40 player cards**,
`09002–09026`. Identity `09001a/b`, obligation `09027×1`, nemesis
`09028×1,09029×1,09030×2,09031×1`, and the five separate Invocation cards
`09032–09036×1 each` are outside that player deck. Extras are
`09037×3,09038×3,09039×1`; preserve these quantities in whole-pack admission.
The inventory's summed face quantities are not a physical-card total because
the two identity faces share one card.

The official rulesheet is **already pinned**, not an unacquired product:
`datasets/rules-packs/sources.manifest.json` records
`mc08_doctor_strange_rulesheet.pdf`, 7,181,027 bytes, SHA-256
`15dba31ffeafaedac36cfd81b4f95e635c929d87775f52152595adebcd3a1cb3`.
The PDF files are local inputs outside the committed snapshot. The complete
three-paragraph Invocation section is now admitted as
[`pack:mc08:the-invocation-deck`](../../../../datasets/rules-packs/mc08/the-invocation-deck.md),
alongside the existing `pack:mc08:protection` record. This is a narrow source
repair against the unchanged PDF pin, not executable hero admission.

The [official FFG PDF](https://images-cdn.fantasyflightgames.com/filer_public/6c/40/6c4068b6-00ee-4060-9855-335b826de33d/mc08_doctor_strange_rulesheet.pdf)
was inspected on 2026-10-26, page 1, “The Invocation Deck”. The current official
product resource page links the
[relocated identical PDF](https://cdn.svc.asmodee.net/production-fantasyflightgames/uploads/2026/09/mc08_doctor_strange_rulesheet.pdf).
Its rules establish
five named cards shuffled separately beside the identity; a public faceup top
during play; a separate discard; and immediate empty-deck recycling without a
penalty. The local input hash, reviewed transcription, source-completeness tests
and offline snapshot pin now cover this section. Before D1 executable
expectations, read that complete section and the reconciled rulings below;
source completeness alone does not implement its lifecycle or visibility.

## Authority reconciliation before coding

Read the actual local records, including scope/date and
`datasets/rules-graph.json`, rather than carrying forward an older answer:

- **Master of the Mystic Arts:** `ruling:b07d53e12625ab23`, RR scope **1.6**,
  September 2025 (source says September 15), explicitly permits seeing the next
  top after the resolved Invocation enters discard and requires recycling if
  that emptied the deck, before returning the original physical card on top.
  This conflicts with pre-1.5 `ruling:ea32bb34b1e7b1b0`'s no-look/no-reshuffle
  answer. The newer answer matches the insert's empty-deck rule and printed
  Special discard/“Then” sequence. It remains on the
  [official-answer HoH page](https://hallofheroeslcg.com/latest-ffg-rulings-post-rrg-1-6/),
  inspected 2026-10-26. **Do not implement Master as leaving its card on top.**
- **Invocation initiation without a useful main effect:**
  `ruling:8c9c9d30f4c01189` (1.5, November 2023) and
  `ruling:8883bc6b0af1c9b8` (1.5, May 2024) allow the self-discard to supply
  a valid target, including Vapors with no status. Pre-1.5
  `ruling:5bafad5aafa9093b` says the opposite. Current RR 1.8
  `rr:choose-game-element.2`, `rr:target.2–.3` and
  `rr:initiating-abilities.step.2` require a valid target for **any part**;
  they support the later ruling. Empty status selection must skip only that
  effect and still discard the Invocation, not deny Spell Mastery outright.
- No matching clarification/supersession rows for these later/older
  ruling pairs were found in the current rules graph. Add reviewed,
  hash-pinned `card_clarifications` for `09005` and `09035` (and `09033`
  as applicable), with explicit older-ruling supersession, before treating
  the adjudication as executable authority. Run `CardRulingRelationshipTests`.
- `rr:special` permits resolution only through another ability's explicit
  instruction. `ruling:277ab401b1269ec7` / `2ace2379284bf746` (pre-1.5)
  distinguish resolving an Invocation from playing it; `2b8ccf2ab5f6391e`
  keeps “your deck” as the player deck. Do not emit an ordinary hand-event play
  or let player-deck search/discard operations consume Invocations.
- `ruling:70055fca52b1ae18` (pre-1.5) exposes the first top **after mulligan**,
  consistent with the insert's “during the game”. This is a setup visibility
  boundary; do not reveal it while a player is deciding mulligans.
- Vapors legality: `ruling:dd639f2e28801fbc` (pre-1.5),
  `17ea4e93b0ab8a25` (pre-1.5), `66a4e0d2b3a71959` (1.5, June 2024)
  and `0416488a6eb376e3` (1.5, May 2024), together with current
  `rr:status-cards.1`, `rr:stalwart`, `rr:replacement-effect`, prohibit
  replacing Tough with an already-held or forbidden status. Replacement is
  not a fresh “give” status for Mister Sinister's trigger. These restrictions
  do not remove the separate legal self-discard effect.
- Iron Fist's generated text already says **Interrupt**; FAQ `09014` and
  `ruling:c1b381643ec4fad9` explain the correction. `9f118093b95dd3f1`
  (1.5, January 2024) distinguishes exhausting to initiate from completing an
  attack: an enemy defeated by his interrupt yields no consequential damage.
- Counterspell: `ruling:638d99c719a38af3` (1.6, June 2025) confirms no
  attachment in alter-ego. `4432bdf7194d532c` (1.6, October 2024) and
  `rr:status-cards.2` give stun cancellation priority, leaving Counterspell
  attached; `810e462c7d971efe` limits its trigger to its attached identity.
- Unflappable FAQ `09020`, `ruling:2aa6a853fcebd8da` and
  `60c2b9d1a312e376` distinguish hero defense from ally defense and damage
  from the attack from separate boost damage. Check current `rr:defend-defense`
  and causal attack receipts, not total HP difference. Momentum Shift's full
  two-heal arrow cost is confirmed by `ecabf270719f2990` and `rr:cost`.

Quote the narrow RR clauses in eventual test comments and use existing `[Rule]`
IDs; product/ruling IDs are separate authority references. Old ruling scopes
are observations, not blanket authority for every current timing detail.

## Reusable mechanisms and actual gaps at the assessment base

- `src/Marvel.Rules/State/DeckType.cs` already has `AdditionalDeck` and
  `AdditionalDiscardPile`; `DeckTypes.cs` makes the former concealed and the
  latter faceup. Generic places do not define a player-owned Invocation deck.
  `Play/AuxiliaryDeck.cs` and `AuxiliaryDeckKind.cs` support only scenario-owned
  Experimental Weapons/Side Schemes; setup validation rejects player ownership.
  `Marvel.Cards/Dsl/AbilityAuxiliaryDeckLowering.cs` recognizes only those two.
  Reuse deterministic movement/shuffle patterns, not encounter ownership or
  encounter penalties. Choose typed identity/owner/lifecycle semantics before
  choosing new zone names; names affect digest compatibility.
- `Marvel.Cards/Run/AbilitySpecialSequence.cs`, `AbilitySpecialChoiceDispatch.cs`
  and DSL `resolveSpecials` already support explicit Special execution. Probe
  their single-card continuation with an out-of-play Invocation; do not create
  a second generic Special subsystem or inherit Wakanda Forever's final-step
  semantics accidentally. Missing composition is capture of exact top and its
  payable cost, atomic hero exhaustion/payment, Invocation discard routing,
  empty reset, and return of the same physical copy for Master.
- `AbilityCost.Spend`, `AbilityCost.Heal`, cost preparation/payment and resource
  continuations exist. `AbilityCost.Spend.Generic` is an authored integer;
  this does not prove dynamic payment of a captured Invocation's cost. Compare
  Spell Mastery's cost wording with Master's **printed** cost; do not let
  ordinary event-play discounts reduce an ability payment without authority.
- `AbilityCardDiscardExecution.DiscardTop`, discard receipts,
  `AbilityCondition` resource predicates and `AbilityExpressionEvaluation`
  already retain printed resource facts. Magic Blast/Mordo should first use a
  focused composition probe, with wild explicitly selecting all branches.
  `AbilityDeckPeek` supplies prompt-scoped top inspection; it is not persistent
  public Invocation-top visibility. Astral Projection must keep its chosen
  scheme and inspected encounter top stable across its continuation.
- `Marvel.View/WorldProjection.cs` gives faceup cards public audience;
  `CardDescriptorProjection.cs` and `VisibilityEventFilter.cs` filter results.
  Concealed pile exposure/order remains separately constrained. D1's follow-on
  contract must describe the visible top and owner without exposing deeper
  cards; merely toggling a faceup flag is not proven end-to-end disclosure.
- Current damage/attack receipts at `29aa293f` are reusable for Desperate
  Defense/Unflappable; prove their particular defense identity and attack-only
  damage conditions. Do not file a generic missing attack receipt. Likewise
  Warning can start from existing damage reduction, Foiled! from numeric
  boost cancellation, Momentum Shift from full healing costs, and Iron Man
  from the existing attachment-price behavior; each needs actual-row evidence.
- `src/Marvel.Content/Setup/RuntimeProductAdmission.cs`,
  `StandaloneProductAdmission.cs` and `StandaloneProductRecipes.cs` are the
  public admission boundary. No Doctor Strange starter/Invocation setup record
  was found. Keep public setup closed until all faces and exact recipes work.

## Full face ledger and dependency groups

**A** = existing/reprint route; **C** = candidate composition needing its first
focused probe; **P** = specific new shared capability. These are routing labels,
not authored or verified card coverage. D1 owns lifecycle/captured-top payment;
D2 owns remaining Invocation/status/hosted-card contracts; D3 owns independent
signature/starter/extra/encounter composition; D4 owns complete public admission
and client integration. Serialize shared book/agenda/authority edits with Widow.

| Face | Card | Route and distinguishing observation |
|---|---|---|
| `09001a` | Doctor Strange | **P D1**: top-only Special, exact cost plus exhaust; zero-cost Winds still exhausts; failed payment leaves all state unchanged. No event-play trigger. |
| `09001b` | Stephen Strange | **P D1**: setup five, Natural Talent top-discard once per phase; form changes do not reset use, phase transitions do; no player-deck discard. |
| `09002` | Wong | **P D1/C**: exhaust once for either one controller-identity heal or the game's typed Invocation top discard; at full health, discard branch remains available. Under foreign control, "your identity" follows the controller but "the Invocation deck" remains Strange's bound deck (reviewed in `doctor-strange-top-discard-evidence.md`). |
| `09003` | Astral Projection | **C D3**: chosen scheme loses 3 plus printed numeric boost count; star is not numeric; encounter top stays in place and only authorized viewer sees it. |
| `09004` | Magic Blast | **C D3**: 5 then discard player-deck top; physical/energy/mental/wild branches, separate energy 2-damage instance, departed enemy not retargeted. Final player-deck card resets before event cleanup (`d1f9f33e3ae4ba2f`, `e4ea1bf855e41aac`). |
| `09005` | Master of the Mystic Arts | **P D1**: pay event and printed Invocation costs together; Special discards, exposes/recycles, then same physical card returns to top. No hero exhaustion cost. |
| `09006` | Mystical Studies | **C D3**: search own deck/discard for Doctor Strange card; search misses still shuffle; Invocation deck excluded; alter-ego only. |
| `09007` | Protective Ward | **C D3**: interrupt treachery revealed from encounter deck, cancel all effects/discard; distinguish another origin and full reveal/keyword cleanup under RR1.8. |
| `09008` | Sanctum Sanctorum | **C D3**: own discard Spell shuffled then draw; empty match handling follows actual valid-target rules; suspended selection cannot exhaust twice. |
| `09009` | Cloak of Levitation | **C D3**: constant Aerial plus hero-only exhaust/ready; ready identity cannot justify an otherwise ineffective action; discarded Cloak loses trait. |
| `09010` | Magical Enhancements | **C D3**: enter another seat's control, modify that seat's hero +1/+1/+1, discard at round-end Forced Interrupt; no alter-ego stat leakage. |
| `09011` | The Eye of Agamotto | **C D3**: hero-only wild generator; payment suspension cannot reuse exhausted source; alter-ego has no offer. |
| `09012` | Brother Voodoo | **C D3**: top-five event search selects exact copy then shuffles; fewer than five/none still handled; no search of entire deck. |
| `09013` | Clea | **C D3**: defeat interrupt shuffles physical Clea into owner's deck; defeat continuation cannot also discard her or move a new incarnation. |
| `09014` | Iron Fist | **C D3**: starts with two counters, optional counter interrupt stuns/deals one; zero counter absent; lethal interrupt leaves exhausted/no attack completion/consequential damage. |
| `09015` | Desperate Defense | **C D3**: basic-defense initiation gets +2 for that attack; ready only after zero attack damage, even if separate boost damage occurred; preserve original defender identity. |
| `09016` | Momentum Shift | **A/C D3**: full heal-two cost; zero/one damage cannot pay, two can; rejected cost neither heals nor spends resources. |
| `09017` | The Power of Protection | **A D3**, `01079/05016`: double only for Protection card payment; unrelated ability cost stays single. |
| `09018` | Med Team | **A D3**, `01080`: three medical uses, exhaust plus counter, heal friendly character, last counter discards; no full-health-only offer. |
| `09019` | The Night Nurse | **C D3**: heal one and discard one chosen status from a hero, including another seat; status-only partial effect/full-health boundary; discard on third use. |
| `09020` | Unflappable | **C D3**: may enter another seat's control, Max 1, hero defense with zero attack damage offers exhaust/draw; ally defense excluded, separate boost damage not conflated. |
| `09021` | Warning | **A/C D3**: interrupt any hero's pending damage, reduce one; zero pending damage and ally damage excluded; distinguish direct damage from attack context. |
| `09022` | Energy | **A D3**, `01088`: two energy, Max 1 per deck, correct reprint identity/copy limits. |
| `09023` | Genius | **A D3**, `01089`: two mental, Max 1; preserve declared payment. |
| `09024` | Strength | **A D3**, `01090`: two physical, Max 1; preserve declared payment. |
| `09025` | Avengers Mansion | **A D3**, `01091`: Max 1, exhaust/choose player/draw, including non-first seat. |
| `09026` | The Sorcerer Supreme | **C D3**: Mystic play restriction at initiation, +1 hand size only in hero form; flipping removes bonus without discarding it. |
| `09027` | Physical Toll | **C D3**: correct identity recipient, optional alter-ego flip then exhaust/removal or persistent next-event +3; actual event consumes/discards obligation, Invocation resolution does not. |
| `09028` | Baron Mordo | **C D3**: attack interrupt discards attacked player's deck top; printed resource branches/wild, both identity forms where attack legal; discarded face captured across status/damage windows. |
| `09029` | Open the Dark Dimension | **P D2**: remove exact current Invocation face down beneath scheme; remaining deck top updates/recycles; defeat shuffles hosted copy into correct owner's Invocation deck, never player/encounter deck. |
| `09030` | Counterspell | **C D3**: attach to hero only, attached seat's event canceled then attachment discarded; other seat excluded; stunned attack event clears stun and retains attachment. |
| `09031` | Thoughtcasting | **C D3**: player chooses among highest printed-cost hand ties; alter-ego threat versus hero damage uses same discarded receipt; empty hand changes neither. |
| `09032` | Crimson Bands of Cyttorak | **P D2/C**: stun then 7 direct damage, separate Special self-discard; no attack label, stun/Guard on identity do not turn it into an attack. |
| `09033` | Images of Ikonn | **P D2/C**: villain confuse plus scheme remove 4 and self-discard; already-confused/no-threat still resolves self-discard. |
| `09034` | Seven Rings of Raggadorr | **P D2/C**: up to three distinct characters, including enemies and other seats; status eligibility/zero meaningful targets must not prevent self-discard. |
| `09035` | Vapors of Valtorr | **P D2**: choose physical status and legal different replacement on same character; cannot/duplicate restrictions; no statuses still permits self-discard; replacement does not emit give-status trigger. |
| `09036` | Winds of Watoomb | **P D1/C**: draw three from player deck then Invocation discard; player-deck exhaustion penalty stays separate from penalty-free Invocation recycling. |
| `09037` | Skilled Strike | **A/C D3**: basic hero attack only, +2 for that attack; event attacks/other seat excluded; no premature commitment before legal target/cost. |
| `09038` | Foiled! | **A/C D3**: exact faceup boost during scheme activation cancels numeric icons; star ability remains; attack activation excluded. |
| `09039` | Iron Man ally | **C D3**: reduce each upgrade played onto this Iron Man by one, floor zero; another ally's upgrade unaffected. `01029a` is a hero with the same title, **not a reprint implementation**. |

## D1 executable acceptance and subsequent integration

Start in `tests/Marvel.Rules.Tests/Play/` with typed owner/deck lifecycle and
`tests/Marvel.Content.Tests/Play/` with a test-local Winds/identity/Master book.
Use the existing supported nonpublic test fixture seams. Proposed production
owners are `Marvel.Rules/State` and `Play` for lifecycle, and `Marvel.Cards/Dsl`
and `Run` for top capture/payment/Special composition. Keep all five physical
cards in the test setup; only Winds needs a characteristic executable body in
the first bounded slice. Unsupported other Specials must raise explicitly.

1. Seeded setup shuffles exactly five once, apart from the 40-card player deck;
   first top stays hidden through mulligans. Public top appears at game start.
   Same seed/decisions yields same order/events/digest; repeated offers consume
   no RNG. Reject duplicate physical cards, another world's card and wrong owner.
2. Normal Winds Spell Mastery exhausts even at zero resource cost, draws three,
   moves Winds into Invocation discard, and reveals the next top. Natural Talent
   and Wong use the same routing/reset, without player-deck encounter penalties.
   Test final card, one-card discard reset, and empty deck/empty discard fail-closed
   boundary without looping or fabricating a card.
3. Capture original top object/incarnation/owner and required cost before payment;
   wrong/deeper/discarded top, stale/foreign target, insufficient resources,
   exhausted hero, repeated answer and illegal resource use must preserve prompt,
   digest, RNG, counters, sources and pending work. Include a valid same-cost
   control, not an already-ineligible offer. Preserve live resource-generator
   effects and existing simultaneous-cost rules through suspension.
4. Master's two-card test observes Special discard and new faceup top before
   original returns; the final-card test observes seeded recycle before original
   is moved out of its shuffled position to top. Never duplicate/drop a physical
   card or bypass the prescribed shuffle. Its event cost and Invocation arrow
   cost share simultaneous resource payment; do not exhaust Strange. Compile
   mutants suppressing the intermediate reveal/reset and accepting the second
   card as top; the distinguishing tests must fail, then restored code pass.
5. D2 adds status replacement, multiple distinct tough recipients and physical
   hosted-card return. Integrate D3's independent compositions in bounded groups
   (search/discard; defense/damage; constants/resources; encounter/obligation),
   each with actual ability-book/data evidence. D1 does not resolve those groups.

Before public admission, extend View/Protocol/Decisions only as needed for typed
Invocation owner, separate piles/counts, visible-top source/action identity,
captured cost and continuation meaning. Hide future faces/order from other seats,
offer candidates, event reorder payloads, logs, accessibility and inspectors.
The legitimately observed temporary top under Master may appear in authorized
history; it does not authorize inspecting the rest of the deck. Open the Dark
Dimension's facedown hosted card must not become an ordinary readable attachment.

Restart obligations belong in `tests/Marvel.Server.Tests/`: mid-payment, suspended
Special target/status choice, Master before return, final-card recycling, and
Dark Dimension with hosted Invocation. Compare uninterrupted decisions/events and
digests, exactly-once costs/return, Natural Talent use and RNG order. Replay-based
reconstruction must preserve legitimately observed information as well as hidden
current state. Reuse MARVEL-630's additive upgrade path; prove compatible saved
Core/RoRS/accepted-pack sessions stay intact and genuine incompatibility rejects
without changing archives. Do not impose an unnecessary schema migration.

Once all faces/recipes are complete, use production factory/session paths and
`RuntimeProductAdmissionTests`/`PartialExpansionAdmissionTests` patterns. Include
missing-required-face and wrong Invocation/starter quantity controls, custom
Protection/basic decks, unsupported adjacent Hulk identity, embedded and socket
catalog/restart coverage. Reconcile setup authority enumeration, outside-Core
adjudication and behavior catalog counts; run the setup/ability/citation/authority
gates named by the pack-delivery skill before the normal final managed/platform
gates. No broad suite is needed merely to publish this document.

Native acceptance after admission: a non-first Strange seat can inspect only the
public Invocation top, choose Spell Mastery or Natural Talent from the correct
source, understand top cost/exhaustion versus Master, cancel/change a payment
draft, resume a suspended choice, and see discard/recycle/hosted return without
double submission. Exercise right-click, drag where offered, keyboard alternative,
restart, 80/100/150% scale and reduced motion; capture and inspect actual renders.
The new auxiliary-deck interaction needs focused owner review only once concrete
agent/native evidence exists, with screenshot/video and a specific Plane decision
item. Conventional cards do not acquire a generic owner gate.

## Blocking questions and next action

The complete pinned Invocation extract and the reviewed current-ruling graph are
integrated. MARVEL-794 supplies the reviewed nonpublic typed deck lifecycle;
MARVEL-796 supplies the three scoped ruling relationships. No source acquisition
question blocks the next D1 slice. Implement normal Spell Mastery and actual Winds
Special resolution first, then Master with captured-card discard, intermediate
reveal/recycle and exact return. Dynamic top costs and suspended Special execution
remain implementation obligations, not permission to infer card behavior.

The canonical parent is MARVEL-565 (`e2c1acfd-5962-40d0-89c9-bb2b5a18e376`).
The historical MARVEL-630 blocker is Done. Whole-pack admission, all 40 faces and
native auxiliary-deck interaction remain open; the foundation alone is not playable
Doctor Strange content.

Independent source integration review (2026-10-26): the delivery lead verified the relocated official PDF has the existing7,181,027-byte/SHA-256 pin and inspected its rendered first page against all three new section paragraphs. Separate five-card setup, faceup top, dedicated discard and penalty-free empty-deck recycling are preserved; no named subclauses were invented. The source repair changes no playable admission. Full62-PDF library check remains unavailable locally; existing offline snapshot integrity and source-preservation gates apply.

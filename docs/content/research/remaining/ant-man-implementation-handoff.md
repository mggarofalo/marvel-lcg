# Ant-Man implementation handoff — MARVEL-569

Baseline `9fc46f541fb1466880766131f47e3872890c5c8a`, 2026-10-27. This is source research, not implementation or passing gameplay evidence. The canonical delivery record is MARVEL-569; [research](569-574.md#569-ant-man-form-identity-and-conditional-effects) and [inventory](569-574-inventory.md) are historical inputs. `datasets/cards/cards.json` is the printed-text authority; use current RR 1.8, `pack:mc12:ant-man-hero-pack` and applicable vendored rulings. No 120xx ability rows or public setup were found at this baseline.

## Verified physical scope

The original printed divider, [photograph hosted by Hall of Heroes](https://hallofheroeslcg.com/wp-content/uploads/2020/11/antmanstarterdeck.jpg), was visually inspected. Acquired 2026-10-27 as external evidence `C:/Users/mggar/Source/ant-man-evidence/antmanstarterdeck.jpg`, SHA-256 `632722492c79f9588dd381565f9da3b10444746baf7c2173b745304c72624bd7`. It independently establishes the original list and quantities; it is not a dataset refresh. The [official rulesheet](https://images-cdn.fantasyflightgames.com/filer_public/f7/9a/f79a7553-b16f-4d5a-aea4-d828d798bc41/mc12_ant-man_rulesheet.pdf) explains forms and Team-Up but does not enumerate the starter. Its vendored extraction is `datasets/rules-packs/mc12/ant-man-hero-pack.md`.

The starter has 15 signature copies: `12002` ×1, `12003` ×2, `12004` ×1, `12005` ×2, `12006` ×2, `12007` ×3, `12008` ×1, `12009` ×2, `12010` ×1; 16 Leadership copies: `12011`–`12014` ×1 each, `12015`–`12018` ×3 each; 9 Basic copies: `12019` ×2, `12020`–`12023` ×1 each, `12024` ×3. Identity `12001a/b/c` is one physical card, separate from the 40. Obligation `12025` ×1; nemesis `12026` ×1, `12027` ×1, `12028` ×2, `12029` ×1. Additional customization `12030`–`12033` ×3 each is part of pack coverage but not starter. There are 35 faces and 59 physical playable cards. The three identity rows must not be summed as separate physical cards.

## Dependencies and engine boundaries

The explicit-destination foundation `9a333982` and independent review `023cc323` are integrated into the research baseline. Subsequent Ant-Man candidate `265db6c4` integrates ability-driven timing source `7a9b38e3`, rejecting review `6671e7ac`, repair `3b186f64` and independent closure `5e1cbc20`, including real Game.Resolve continuation. The voluntary pre-flip interrupt defect remains separately tracked as MARVEL-863 (`be351ebb-0c5b-4121-8ac6-8860aa154682`); its prepared source is unverified. Face-specific DSL/projections and the post-Ronan replay publication boundary remain open. Recheck the baseline capability inspections below against this integrated candidate before extending them.

**F — form-specific authoring and projection.** `AbilityEffectLowering.FormEffect` only accepts `hero`/`alter-ego`; the reviewed `AbilityImmediateExecution.ChangeForm` now schedules an explicit destination internally, but an ambiguous generic hero destination still raises for three faces. `Forms.OfFace` returns only hero/alter-ego; Tiny and Giant are printed traits of the active hero face. `AbilityPowerOutcomeTrace.ChangeFormState` projects a seat bit rather than a destination face. Extend destination selection and live/projected trait/form predicates in their owning engine/DSL contracts, including other-hero selection and face-specific trigger qualification. Do not treat Tiny as a third `CardKind` or confuse a trait with generic hero form. Preserve state, once-per-turn voluntary receipt, precise destination and causal events through pending choices/replay. Check the separate timing repairs first. Tests must distinguish all six directed changes, invalid/same destinations, Resize's nonvoluntary change, active-only response, and change followed by draw/ready when one instruction cannot complete.

**R — generated versus spent resources.** `Resources.Paid` and the entry receipt described in `docs/card-dsl.md` retain resources *paid*, excluding excess. `AbilityEventPayment` and `ResourceAllocationLedger` distinguish generated and allocated types. For Pym Particles prove a response bound to the exact physical card actually spent, including an ability cost, generated-but-unused and form at the response window; inspect the spend occurrence and resource-generator paths before selecting the smallest extension. For Hank Pym the paid-only entry receipt cannot by itself count overpayment: retain actual generated amount and adjusted price across entry/limit suspension, cap at four, test double-icon, resource abilities, cost reductions, zero and put-into-play/no-payment. Consult `ruling:dc330186a6c8ca8e` and `ruling:e8c109f4af3b7aac` against current RR, and do not use printed icons as a proxy for actual generation. Mark exact needed extension only after actual-card probes.

**T — ally and trait composition.** `CardPlayLegality.TeamedUp` already checks both named friendly identity/ally characters in play and `DeckConstruction` checks an identity name across faces; test actual Swarm Tactics including another player's Wasp, absent/departed character, and illegal identity deck. This is not a new generic Team-Up subsystem. `CardEntry.CheckAllyLimit` currently counts every card in the ally area, so Stinger's exclusion requires a card-specific continuous eligibility mechanism, including leaving/blanking/control and a full limit check. `withTrait` reads current traits (`docs/card-dsl.md`); exercise face changes and trait-sharing at both offer and execution for Team-Building Exercise. Do not use the played card's traits as the hero's. Prove ally-attachment maximum and target ownership via the existing attachment path.

## Face-to-packet map

All rows below require their own authored face, including reprints. A packet may reuse existing primitives, but Q denotes an actual-card composition probe before asserting support or an engine gap. Dependencies F/R/T are as above. Every packet owns its actual rows and focused behavioral tests; serialize shared DSL/ability-book edits.

| Face (copies) | Bounded authored packet and observable requirement |
|---|---|
| 12001a (identity) | F1: Tiny entry response removes 1 threat from chosen scheme; trigger only after change to this face. |
| 12001b (identity) | F1: Scott response heals 1 after change to alter ego; preserve physical identity state. |
| 12001c (identity) | F1: Giant entry response deals 1 to chosen enemy, not Tiny's response. |
| 12002 (1) | F2: Wasp enters; Giant deals 2 damage or Tiny removes 2 threat using current form, neither in alter ego; verify entry and ally consequential stats. |
| 12003 (2) | F2: Giant-only attack, 1 each minion then 8 to selected enemy; test simultaneous damage/defeat and separate targeted attack. |
| 12004 (1) | F2: Tiny-only thwart 2 plus one per controlled Army of Ants; test multiple controllers and live count. |
| 12005 (2) | F1: change to other hero face then draw 1; no voluntary receipt consumed, resolve correct response windows and replay. |
| 12006 (2) | R1/F: exact spent-card response; Giant heals 2 or Tiny draws 1; discard without spending is not a trigger. |
| 12007 (3) | F2: Tiny-only action exhausts this support, damage chosen enemy; test wrong form and exhausted cost. |
| 12008 (1) | F1: two distinct after-change responses, Giant heal 2 and Tiny draw 1; no alter-ego response. |
| 12009 (2) | F1: after changing to Giant, +1 ATK until turn end; copies stack and expire independently. |
| 12010 (1) | F2: two form-dependent exhaust plus two typed-resource costs, stun in Giant / confuse in Tiny; validate atomic cost and wild declarations. |
| 12011 (1) | R2: Hank Pym entry interrupt and generated overpayment counters, each adds HP; distinguish Scott identity by subtitle and test zero HP/entry. |
| 12012 (1) | T2/Q: Giant-Man +2 ATK while at least 3 remaining HP; damage/heal and modifiers change live threshold. |
| 12013 (1) | T2/Q: Ronin +1 THW/ATK while an upgrade attached; detach/blank/control boundaries. |
| 12014 (1) | T1: Avenger identity play restriction plus exclusion from ally limit; other allies still count. |
| 12015 (3) | T2/Q: discard top player deck until an Avenger ally, then add that physical ally to hand; deck exhaustion, discard response window and none-found path. |
| 12016 (3) | F3/Q: hero response after form change gives hero +1 THW/ATK/DEF through round; alter-ego destination and timing/expiry. |
| 12017 (3) | T2/Q: attach only to Avenger ally, max 1 per ally; after that ally attacks or thwarts deal 1 to an enemy, including controller/performer distinction. |
| 12018 (3) | T2/Q: attach to ally, max 1 per ally, +2 HP while attached; test actual entry, loss of attachment and lethal HP changes. |
| 12019 (2) | B1: reprint First Aid `01086`; author same heal-any-character row under 12019 and test both damaged and invalid target. |
| 12020 (1) | F3/T: Team-Up admission and play; change to other hero face, ready hero; verify separate sequential effects and Wasp ally/identity. |
| 12021 (1) | B1: Energy reprint `01088`, empty ability list, printed double energy, max one per deck. |
| 12022 (1) | B1: Genius `01089`, empty ability list, printed double mental, max one per deck. |
| 12023 (1) | B1: Strength `01090`, empty ability list, printed double physical, max one per deck. |
| 12024 (3) | T3/Q: exhaust to play hand card sharing a current hero trait at cost reduced by 1; test selected card legality, form change, zero floor and suspension. |
| 12025 (1) | N1/F: GiveTo Scott, optional alter-ego flip, choose exhaust/remove or discard one and prohibit changing form until end of next turn; mandatory discard path and duration, including invalid choices. |
| 12026 (1) | N2/Q: starting threat 2 per player; blank printed text of each Tech player card while active, including attachment/continuous effects, restoration on departure; inspect existing timed blanking. |
| 12027 (1) | N2/F/Q: Yellowjacket gains Giant + retaliate 1 or Tiny +1 ATK and Tiny according to engaged player's form; dynamic traits and multiple players. |
| 12028 (2) | N2/Q: attach Yellowjacket if able else villain; uses 3 size, +2 ATK/SCH and forced response removes counter after attached enemy activates; depletion/discard and full activation. |
| 12029 (1) | N2/Q: discard encounter cards until Ant-Man nemesis-set card then reveal it; empty deck, reset, recursion and exact set. |
| 12030 (3) | E1/Q: after you attack and defeat an enemy heal by actual excess damage of that attack; distinguish prevention/defeat and captured source. |
| 12031 (3) | E1/F/Q: after form change remove 3 threat, 4 if paid with mental; settled paid allocation, response timing and target legality. |
| 12032 (3) | E1/T/Q: Avenger identity prerequisite, up to villain-stage-number friendly characters (max 3) receive tough; stage, zero, already-tough and multiplayer. |
| 12033 (3) | E1/Q: +1 hand size until phase end; verify phase expiration and real hand-size use. |

Q is deliberately not a claim of missing machinery. Read actual definitions, tests and printed cards before deciding whether each composition is supported. In particular inspect `AbilityEffectLowering`, trigger matching, continuous-effect scopes, attachment/entry and encounter discard/reveal paths; fail closed for an unrepresented case. Quote the applicable RR clause alongside each rule-cited test, and check current RR against older rulings before adopting their timing.

## First executable packet and admission

B1 is independently reviewed and integrated at `265db6c4`: actual First Aid `12019`, Energy `12021`, Genius `12022` and Strength `12023`. Source `8f013108` and review `ec761521` preserve the original 723-row byte prefix; the book now has 727 rows, and all 24 historical exact-count assertions remain through one independently maintained expected total. Source checks pass 111 and review checks pass 215, including ally/minion ownership and scheme exclusion; the compiled healing mutant is killed and restored. This does not admit Ant-Man. After MARVEL-863 is reviewed and integrated, prepare F1: use actual identity faces, Resize, Helmet and Giant Strength with nonpublic engine tests and explicit destination/trait projection. Review raw authority and actual diff independently; mutations must compile, fail for the intended behavior, then pass restored code under normal policy. Do not introduce a public-admission bypass to claim an early vertical slice.

After all 35 faces are authored and reviewed, create the divider-qualified starter, identity, obligation and nemesis recipes and complete-pack admission. Follow `docs/setup-dataset.md` and pack delivery skill: ability and setup checks; reviewed expansion adjudication and regenerated offline behavior catalog; global recipe/authority counts including `RiseOfRedSkullSetupDataTests`, `ExpansionSetupAuthorityTests`, `BehaviorAuthorityTests`; public catalogs on both transports. Prove missing required face and incomplete/wrong recipe reject; correct 40-card starter and legal custom deck with extras succeed; identity faces have zero deck quantity; adjacent unsupported content remains rejected. Update stale unsupported-content fixtures without weakening their rejection.

Exercise production `DatasetGameFactory` and session setup, characteristic Tiny/Giant actions, obligation/nemesis and representative complete games. Capture exact revision, seed, outcomes, causal events and fresh-host save/restart/replay, including pending form/payment choices. Native acceptance should use actual viewport input to verify destination choice, visible active form, expected response and next commitment, plus any new payment/ally choice; follow the native testing skill and inspect screenshots. Extend visibility-safe contracts if meanings are missing, never duplicate rules in Godot. Reserve owner review for a materially new interaction or concrete judgment concern, with focused accessible evidence. Run applicable managed and CI/platform gates on one final integrated candidate.

## Evidence limit

This pass inspected source, local generated card data, current pack research, original divider and representative engine seams; no builds, tests, hooks, gameplay, dataset refresh or Plane mutation were performed. F/R/T and Q identify reviewable work, not passing probes. Do not represent the reviewed destination foundation as completion of authored Ant-Man or either separate timing repair.

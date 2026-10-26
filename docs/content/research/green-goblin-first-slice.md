# The Green Goblin: bounded implementation research for MARVEL-559

This earlier bounded packet is retained for its initial code-slice framing. Use the [expanded research handoff](green-goblin-deep-research.md) and current Plane record for updated source findings and dependency IDs.


Status: **ready for a bounded first slice; whole pack is not implementation ready**. Research compared the committed authority snapshot with the engine worktree at `1eb9767f` (2026-10-25). The existing MARVEL-559 issue states the product goal and names risks, but does not yet resolve these implementation decisions. This packet is a handoff for one first implementation assignment, not an admission or acceptance claim. No runtime code or Plane record was changed here.

## Authority and source completeness

- Printed behavior: [`datasets/cards/cards.json`](../../../datasets/cards/cards.json), generated from the pinned MarvelSDB snapshot and supplement. Quantities below come from [`datasets/marvelsdb/pack/gob_encounter.json`](../../../datasets/marvelsdb/pack/gob_encounter.json), upstream pin recorded in [`datasets/marvelsdb/UPSTREAM.md`](../../../datasets/marvelsdb/UPSTREAM.md). Card text in the implementation must come from the generated card corpus.
- Official local insert: [`how-to-use-this-pack.md`](../../../datasets/rules-packs/mc02/how-to-use-this-pack.md) (`pack:mc02:how-to-use-this-pack`), [`new-rules.md`](../../../datasets/rules-packs/mc02/new-rules.md) (`pack:mc02:new-rules`), and [`rules-clarification.md`](../../../datasets/rules-packs/mc02/rules-clarification.md) (`pack:mc02:rules-clarification`). Their front matter pins source PDF, page, and section hash; the product rules snapshot is vendored under [`UPSTREAM.md`](../../../datasets/rules-packs/UPSTREAM.md).
- General clauses: [`rr:replacement-effect.1`](../../../datasets/rules-reference/entries/replacement-effect.md), [`rr:status-cards.2`](../../../datasets/rules-reference/entries/status-cards.md), [`rr:indirect-damage.1-.4`](../../../datasets/rules-reference/entries/indirect-damage.md), and [`rr:villain-defeat.3`](../../../datasets/rules-reference/entries/villain-defeat.md). The replacement clause says a replaced effect is no longer imminent; the status clause gives status cards timing priority; indirect damage is assigned among controlled characters before simultaneous resolution, without overassignment.
- Relevant vendored rulings: [`rulings.json`](../../../datasets/rulings/rulings.json) IDs `ruling:e8098d547f8f4d99` (stun prevents Norman's activation), `ruling:3f30cb0a4ea08236` (Green Goblin's replacement prevents a later scheme interrupt), `ruling:a3c80b14b14ac3a0` (mid-attack flip continues with Norman's printed `*` treated as zero plus boosts), `ruling:703a2911a4de217a` (indirect allocation), `ruling:7a351232497fdcb6` (stage III damage is direct), `ruling:38866d7410bd7f78` (I See You / Hysteria boost), and `ruling:c5516c82788ea8d77` (Media Coverage does not double a villain stage reveal). [`faq.json`](../../../datasets/marvelcdb-faq/faq.json) includes official FAQ entries on `02001b`, `02002b`, `02003b`, and `02030` covering some of these questions.

**Completeness is partial/unverified.** The generated corpus has 57 distinct face IDs and the MarvelSDB pack snapshot has 86 face-copy records. Eight reverse faces share physical cards, suggesting 78 physical cards, but the official insert sections available locally do not contain a component quantity manifest. This is an arithmetic observation, not official physical reconciliation. The locally harvested insert names two standalone scenarios and four modular sets; it does not establish a linked campaign mode. The MARVEL-559 campaign language is a general acceptance requirement, not evidence that this product has campaign rules. Verify against the full official component list or original insert before whole-pack admission. No hero starter, obligation, or nemesis faces are identified in this `gob` snapshot.

## Playable inventory observed locally

`ID × quantity` gives the MarvelSDB pack record count. For double-sided cards, each face is listed and shares one physical copy with its matching reverse; do not add both faces when counting physical cards. The catalog's `pack=gob` filter found these 57 face IDs.

| Set | Face IDs and observed copies | Face-copy total |
| --- | --- | ---: |
| Risky Business | `02001a/b ×1`, `02002a/b ×1`, `02003a/b ×1` (Norman/Green Goblin stages I–III); `02004a/b ×1` Hostile Takeover; `02005a/b ×1` Corporate Acquisition; `02006a/b ×1` Criminal Enterprise/State of Madness; `02007 ×3` Hired Gun; `02008 ×4` Private Security Specialist; `02009 ×1` Collapsing Bridge; `02010 ×2` Oscorp Manufacturing; `02011 ×2` Payoff; `02012 ×4` All in a Day's Work; `02013 ×2` Mad Genius | 30 |
| Mutagen Formula | `02014 ×1`, `02015 ×1`, `02016 ×1` Green Goblin I–III; `02017a/b ×1` Unleashing the Mutagen; `02018a/b ×1` Mutagen Cloud; `02019 ×1` Goblin Glider; `02020 ×1` Hysteria; `02021 ×1` Pumpkin Bombs; `02022 ×1` Goblin Knight; `02023 ×4` Goblin Soldier; `02024 ×6` Goblin Thrall; `02025 ×1` Monster; `02026 ×2` Goblin Reinforcements; `02027 ×1` Goblin Nation; `02028 ×2` Overrun; `02029 ×2` Death from Above; `02030 ×1` I See You; `02031 ×1` Overconfidence; `02032 ×2` Wicked Ambitions | 33 |
| Goblin Gimmicks | `02033 ×2` Goblin Glider; `02034 ×2` Pumpkin Bombs; `02035 ×2` Intimidation; `02036 ×2` Regenerative Healing | 8 |
| A Mess of Things | `02037 ×1` A Mess of Things; `02038 ×1` Scorpion; `02039 ×1` Gang-Up; `02040 ×2` Tail Sweep | 5 |
| Power Drain | `02041 ×1` Power Drain; `02042 ×1` Electro; `02043 ×1` Electromagnetic Pulse; `02044 ×1` Lightning Bolt; `02045 ×1` Shock Therapy | 5 |
| Running Interference | `02046 ×1` Running Interference; `02047 ×1` Tombstone; `02048 ×1` All Tied Up; `02049 ×2` Media Coverage | 5 |

The main-scheme Contents on `02004a` and `02017a` are the scenario composition authority. Each calls for its own villain stages and encounter set, the Core Standard set, and **one** modular set (Goblin Gimmicks recommended). Risky Business setup puts Criminal Enterprise into play. Mutagen Formula setup puts one Goblin Thrall into play engaged with each player. Both have Standard I/II stages and Expert II/III stages. Stage 1B leads to a second main scheme; completing stage 2B loses the game (`02005b`, `02018b`). The four modular sets are independently selectable as the insert says. Product-wide admission also needs those modules usable with eligible already-supported scenarios, not just the two new ones.

## Capability and prerequisite map at `1eb9767f`

| Capability | Evidence in integration worktree | Decision for this pack |
| --- | --- | --- |
| Public setup and modular recipes | `Marvel.Content/Setup/SetupCatalogAdmission.cs`, `ScenarioDealer.cs`, `Dealer.cs`; the candidate includes Core/RoRS and Captain America/Wrecking Crew, but no Green Goblin recipe | Framework exists. Author both scenario/mode recipes and four modular rows, then pass complete-product admission. The production factory always applies RuntimeProductAdmission; there is no test-only admission injection seam for gob. Use nonpublic engine probes first and defer the production journey until complete admission. |
| Fail-closed abilities | `tests/Marvel.Content.Tests/Cards/AbilityDataTests.AnUnauthoredCardSaysSoRatherThanDoingNothing2.cs` explicitly uses `02001a` as an unauthored card that throws; no `gob` rows in `datasets/abilities/abilities.json` | Replace that test's unsupported sentinel when authoring Goblin. Every reachable face needs an authored interpretation before public admission; a research-only world is insufficient. |
| Indirect damage composition | `Marvel.Cards/Dsl/AbilityEffect.cs` has `IndirectDamage`; `Marvel.Cards/Run/AbilityChoiceAnswer.cs` handles allocation; composer tests cover repeated target allocations | Reuse and verify in actual `02001b`/`02002b` reveal context. Stage III `02003b` uses direct damage and must not offer ally allocation. |
| Villain stage transition | `Marvel.Rules/Play/VillainStageInheritance.cs`, `EncounterVillainDefeat.cs` carry same-title attachments/status/tokens | Supported general stage path; prove Risky Business's next stage enters on the defeated stage's Norman/Green side as `pack:mc02:new-rules` directs. Existing title-based inheritance alone does not establish this side choice. |
| Replacement activation and villain-side flip | `Marvel.Rules/Timing/TimingPriority.cs` gives status forced interrupts priority; `AgendaActivationScheduling.ReplaceActivationWait` exists. `Marvel.Rules/State/Forms.cs` is specifically for **player** forms. | New engine capability/integration likely required for a villain face changing during attack/scheme/damage, preserving object, damage, statuses, attachments and pending activation. Do not apply player form APIs by analogy. Verify actual attack/scheme windows before selecting a code design. |
| Saves/restart/client | `Marvel.Server/SessionRestoreAttempt.cs`, `Marvel.Session/SessionReplay.cs`, Godot lifecycle/recovery tests exist for supported products | Reuse the public hosted-session path and test Goblin-specific pending choices and flip state. A test-only `World` cannot prove admission or restart. No new protocol field is assumed; assess if semantic event/offer data is sufficient for an understandable flip and allocation UI. |

The first dependency is the villain-side flip and replacement window, then the Risky Business ability rows and setup recipe, then public admission and client/restart proof. Shared timing/agenda, ability-book and setup/admission paths require serialized ownership. `docs/card-dsl.md`, `docs/affordances.md`, `docs/session-ledger.md`, `docs/presentation-layer.md`, and scoped `src/AGENTS.md`/`tests/AGENTS.md` apply to implementation. There is no verified pack-specific child dependency ID in the supplied Plane snapshot; inspect MARVEL-559's children, links, comments and relations before claiming that a blocker exists or creating duplicate work.

## First bounded engine assignment; later public integration

Implement Norman I/II's would-attack replacement and stun precedence in nonpublic engine states. Cite `02001a`, `02002a`, `rr:replacement-effect.1`, `rr:status-cards.2` and `ruling:e8098d547f8f4d99`. Prove one/two infamy respectively, no boost or attack damage/response, and stun consuming the activation before infamy. Own the relevant `Marvel.Rules` activation/timing path and focused tests; add a distinguishing mutant. Do not include the full ability book, villain flip system, scenario recipes or product admission in this first code assignment. Flip/damage boundaries below are subsequent prerequisites requiring their own reviewed packets.

The later integration target is a normally dealt Risky Business Standard game with a supported hero and Goblin Gimmicks. `DatasetGameFactory` always calls `RuntimeProductAdmission.Discover`; no current test-only catalog injection admits gob. Keep public admission closed until both scenarios, all four modular sets and their modes are complete. Then test production setup, Norman replacement and villain/environment flips. For pending-choice restart, construct a legal Green Goblin reveal with a hero and an ally able to receive its indirect damage, and save at the actual allocation prompt. Norman's replacement and the flip themselves are forced effects, not invented player choices. A separate save before activation can prove deterministic replacement/flip continuation. Compare restored and uninterrupted event streams, digest and RNG under the same seed/decisions.

The following examples define later integration and mechanic obligations; they are not all assigned to the first bounded engine change.

Distinguishing checks across the planned slices:

1. Standard one-player setup has Norman I (`02001a`), Hostile Takeover 1B (`02004b`), Criminal Enterprise (`02006a`) with `2 × player count` infamy, and the exactly composed recommended modular set. A second seat starts with four infamy. Invalid set/stage/recipe requests reject without creating a plausible game. Source: `02004a`, `02006a`.
2. When unstunned Norman I would attack, place **one** infamy instead. No boost, attack damage, or "villain attacked" response occurs. Norman II would place two. A stun cancels the activation before Norman's replacement and consumes the stun, leaving infamy unchanged. Sources: `02001a`, `02002a`, `pack:mc02:new-rules#when-norman-osborn-attacks`, `rr:replacement-effect.1`, `rr:status-cards.2`, `ruling:e8098d547f8f4d99`.
3. Damage aimed at Norman removes that many infamy instead of reducing his HP; at zero, flip him and Criminal Enterprise to Green Goblin/State of Madness, retaining associated damage, attachments, statuses and boost cards. Do not invent how excess damage over remaining infamy behaves; resolve that boundary from printed wording, general rules and any ruling before implementing it. Sources: `02001a`, `02006a/b`, `pack:mc02:new-rules#when-the-villain-changes-form`.
4. On first Green Goblin I reveal, three indirect damage reaches each player **in hero form**; an alter-ego player receives none. Each affected player may allocate among their controlled characters without overassigning. Green Goblin II hits each player irrespective of form; III is direct damage, not ally-allocatable. Sources: `02001b`, `02002b`, `02003b`, `rr:indirect-damage.1-.4`, `ruling:703a2911a4de217a`, `ruling:7a351232497fdcb6`.
5. When Green Goblin I would scheme, remove one madness instead; no threat from the scheme and no "villain schemes" response. When the last madness is removed by a boost during an already begun attack, continue that attack as Norman with zero base ATK plus its boost icons; do not rewind into Norman's would-attack replacement. Sources: `02001b`, `02006b`, `pack:mc02:new-rules#when-green-goblin-schemes`, `ruling:3f30cb0a4ea08236`, `ruling:a3c80b14b14ac3a0`.

Owning layers: `datasets/setup` for composition; `datasets/abilities` and `Marvel.Cards` for typed card clauses; `Marvel.Rules` for activation replacement, villain face transition, counters and legality; `Marvel.View` for visibility-safe cause/consequence; `Marvel.Decisions` for any allocation draft; `Marvel.Server`/`Marvel.Session` for public admission, replay and persistence; `Marvel.Client`/`Marvel.Godot` for the visible choice and recovery journey. Tests should cite RR clauses and quote narrow decisive text in comments. For a new timing decision, add a negative control or mutant that would catch a plausible wrong implementation, such as dealing a boost on the replaced Norman attack.

## Bounded unresolved investigations before broader assignment

- **Playable completeness:** verify the face identities and copy quantities used by both scenario recipes and all four modular sets against printed cards and scenario Contents. The inferred 78-card total is not a separate marketing or packaging gate. Continue engine research while resolving any quantity that could change setup.
- **Damage/replacement edge:** determine whether excess damage beyond current infamy is fully replaced or has a remainder, and the exact timing when infamy reaches zero during an attack or multi-point damage. Inspect `datasets/rules-graph.json` for linked clarifications and add a small legal transcript before coding that branch. Keep a `RulesNotImplementedException` for any reachable unresolved case.
- **Product modes:** verify whether any linked/campaign play exists in the official product source before assigning campaign persistence work. The local insert has only the two standalone scenarios and modular-set guidance.
- **Current Plane dependencies:** the supplied audit snapshot establishes MARVEL-559's description only. Fetch child, link, relation and comment endpoints once before naming blockers or editing Plane; this packet makes no assertion that those endpoints are empty.

Completion of this packet means an implementer can start the identified engine capability and understand the later public journey dependencies. It does not certify either scenario, any modular set, whole-pack authority, or player acceptance.


## MARVEL-690 main schemes and research setup (2026-10-25)

The completion candidate based on reviewed engine revision `c0ed938c` authors
02004a/b and 02005a/b. Risky Business now has nineteen distinct authored faces:
six villain, four main-scheme, two environment and seven encounter faces.
Its physical inventory is eighteen encounter copies plus six double-sided
villain/scheme/environment components; each mode allocates only its chosen two
villain stages. Standard/Expert research recipes retain Standard, add Expert
in Expert mode, and recommend exactly one eight-card Goblin Gimmicks set.
Goblin Gimmicks has four authored research faces (02033–02036). Green Goblin public admission and
complete-game acceptance remain outstanding.

`RiskyBusinessSetupTests` uses `ScenarioDealer.Prepare` and `WorldSetup.Deal`
with actual authored abilities, normal complete Core starters and one chosen
Bomb Scare modular set, at every one-through-four-player count in both modes.
It observes Criminal Enterprise entry with 2 per-player infamy, scheme 1B
with 2 per-player threat, exact encounters/selected villains, mulligan entry,
and the seeded stream after player-deck and both encounter-deck shuffles.
`RiskyBusinessMainSchemeTests` observes 1B completion adding per-player infamy
before milling by the resulting total, a State of Madness no-effect branch,
2B entry with per-player starting threat and final loss. The existing completion
fixture now uses the actual authored 02004b row for missing-environment and
player-deck exhaustion cases; generic ordering fixtures remain synthetic.
The three new setup sources are explicitly outside the executable Core corpus.

Windows evidence for this authored-data/test candidate:

- `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter
  'FullyQualifiedName~RiskyBusiness|FullyQualifiedName~MainSchemeCompletion|FullyQualifiedName~AbilityData|FullyQualifiedName~AbilityProgram|FullyQualifiedName~SetupDataset|FullyQualifiedName~RiseOfRedSkullSetupData'`:
  124 passed, zero failed. TRX: `%TEMP%/marvel690-tests/risky-main-setup-final.trx`.
- `dotnet run --project tools/Marvel.Behavior.Index -c Release --no-build -- check`:
  catalog and generated Core card-face specification current.
- Executed data mutant: change only 02004b `placeCounters.count.perPlayer` from
  1 to 2. Run the no-build content test filter
  `FullyQualifiedName~RiskyBusinessMainSchemeTests|FullyQualifiedName~HostileTakeoverAddsPerPlayerInfamyThenDiscardsTheResultingCount`:
  seven failed and two passed, process exit 1; the one-player observation was
  expected 3 infamy, actual 4. TRX: `%TEMP%/marvel690-tests/risky-main-mutant.trx`.
  Original ability-file bytes restored in `finally`; catalog check passed afterward.

These are nonpublic content/setup checks. They do not claim Linux execution,
full-suite results, native UI evidence, public setup or complete Green Goblin games.


## Goblin Gimmicks authored research packet (2026-10-25)

Based on integrated engine revision `ffffb969`, this packet authors all four
Goblin Gimmicks faces (02033–02036), two copies each in the existing eight-card
recipe. It does not change setup or public product admission. Actual authored
rows replace the test-local interpretations for Pumpkin Bombs, Intimidation
and Regenerative Healing; 02042 remains an unauthored boost negative control.

Printed `datasets/cards/cards.json` supplies the authority. Goblin Glider selects
the highest printed-hit-point enemy without another same-title Glider, adds its
printed +1 ATK and spends energy/energy only in hero form to remove it. Tests
distinguish printed from remaining health, exclude another encounter set's
same-title Glider, fall back to a minion, and verify no-host surge and discard
for a non-first revealing player. Pumpkin Bombs attaches to the villain, has a
physical/physical Hero Action, and binds indirect damage to the completed attack
player across source discard, other-player defense and stun cancellation.
Both removal actions reject wrong-type resources without changing the prompt or
card state and are absent in alter-ego form. Intimidation proves actual generic
payment, invalid underpayment, persistent facedown boosts and its activation-only
boost effect. Regenerative Healing exercises stages I/II/III, partial/zero healing,
zero-heal surge and stage-independent boost healing. The queried MarvelCDB FAQ
contains no entries for these faces; vendored `ruling:e6531edb529e342d` supports
the Pumpkin Bombs ally-defense interpretation alongside current `rr:you-your.7`.

MARVEL-714 fixes shared encounter cleanup: an attachment whose authored attach-to
instruction cannot place it resolves its reveal text and is discarded before
reveal responses. An entered attachment or one moved elsewhere remains where it
went. A synthetic attachment with no attach-to instruction retains the distinct
`rr:reveal.1` out-of-play table behavior. The scheduled operation spelling remains
unchanged. The distinguishing actual Core 01163 test exercises surge before
cleanup, with absent-attach-to and existing attachment/reveal regression controls.

Windows verification of this source/data candidate:

- Release content filter `GoblinGimmicks|AbilityData|AbilityProgram|RiskyBusinessSetup|
  TeenSpirit|AllTiedUp|UnattachedRevealCleanup|AttachToTests|AttachmentEventIntegrity|
  NestedRevealCompletion|EncounterAttachmentContext`: 137 passed, zero failed.
  Each term uses `FullyQualifiedName~`. Final TRX:
  `%TEMP%/goblin-gimmicks-results/goblin-gimmicks-final.trx`.
- Executed authored-row mutant: change only 02036 healing multiplier from 2 to 3.
  Stage-healing filter: three failed, two passed; process exit 1.
  `%TEMP%/goblin-gimmicks-results/stage-heal-mutant.log`.
- Executed engine mutant: disable the failed-attachment cleanup scheduling branch.
  Actual Core failed-placement test failed; process exit 1.
  `%TEMP%/goblin-gimmicks-results/attachment-cleanup-mutant.log`.
- Both source files restored byte-for-byte in `finally`, then rebuilt for the
  final 137-test run. Final log:
  `%TEMP%/goblin-gimmicks-results/restored-final.log`.

These checks cover nonpublic authored content and shared reveal behavior. They do
not establish public Green Goblin admission, complete games, Linux or native UI
execution. Independent review of the shared lifecycle change remains required.

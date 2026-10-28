# Fear No Evil campaign rules: independent review

2026-10-27. Review of [campaign rules context](fne-campaign-rules-context.md). Disposition: corrections required; campaign progression is suitable for a bounded implementation handoff after those corrections. Full campaign research readiness is not established: Kingpin's defeat qualifiers still need authoritative clarification. This review changes no source packet, card candidate, dataset or runtime.

The source baseline is `3e4c919d482def31ed808aba0adf24e6d50f911f`; worktree HEAD during review was `89679f0293004d42c76f031faa4def50db8a3f03`. The reviewed campaign/admission/contract source files showed no diff from that baseline. This is a source review, with no builds, tests, native evidence or Plane access.

## Evidence and coverage

I visually inspected all retained original rulebook renderings for printed/PDF pages 5, 8, 9, 13, 15, 17, 19, 21, 23 and 28. PDF SHA-256 is `18b29a608418f41b5b690109bf43de29f31c2b6567c8e4dd5c2fee71c3b78b2c`; the accepted campaign candidate SHA-256 is `e953ed28ef1a9bc53c1d6b267b43b4701bf15e1cd627a93558a7bcf71eeddb46`. Both hashes were independently recomputed. I compared the twelve accepted candidate face texts with the context's effect summaries; I relied on the earlier independent card proofread for scan-to-candidate fidelity and did not repeat its twelve-scan proofread.

Additional original scans inspected here: Bullseye 60069, 60071, 60074, 60075; Raft 60143-60150; Stop the Presses 60152-60158. Their individually recomputed hashes match `scan-manifest.json` and are retained in the companion review JSON. The inspected Raft footer range covers its non-main-scheme positions 2/9-9/9; the Presses range covers 2/9-8/9, with 60158's duplicate 9/9 represented by the existing manifest, not newly proofread here. This establishes the six-minion/four-support pools without marketing or packaging counts. Main-scheme setup faces 60142A/B and 60151A/B were not visually reviewed in this assignment, so this is not full scenario-card research.

| Context claim group | Review result |
|---|---|
| Identity lock, deck changes, five interchangeable schemes and final Kingpin | Supported by pp. 5, 8-9. Selection may be any unresolved scheme, not only a drawn one. No fixed five-win route is implied. |
| Before-game draw, progress, third-mark failure, sole remaining card | Supported by p. 9 steps 1-4. Defeat and failed scenario remain distinct. Retries repeat campaign setup; a scheme may fail before it can be selected. |
| Assignment reuse, first-playthrough recommendation and random unrecorded villains | Supported by p. 9 step 5 and p. 8 list. The Purple Man/Typhoid Mary alternative is a player choice. |
| Setup order, removed titles, expert healing, outcome environments and Mary | Supported by p. 9 steps 6-14, with expert HP/elimination omissions below. |
| Five underling victory/defeat sequences | Supported by pp. 13/15/17/19/21. Expert defeat adds one progress before returning to general setup. Victory records unique ally/support titles and flags in the printed order. |
| Environment 205-209 effects and Mary 210 | Context matches accepted candidate summaries, including after-mulligan timing, scenario-expert conditions, named source sets, controller reveal triggers and Victory -1. RR `victory-x.1.1`, `.2` and `victory-display` support Mary's defeat/display explanation. No inspected FNE authority assigns a campaign score consequence to -1. |
| Scenario campaign setup and final thresholds | Supported by relevant pages, with the Getaway target correction below. Raft's PRISONER-in-play condition differs from 208B's named-set pool. Kingpin's >=3 / >=4 thresholds are cumulative. |
| Log fields | p. 28 supports the five rows, completed checkbox, villain, progress 1/2/Failed boxes, identity/HP and Mary/trust flags. Exact removed-card heading is **Allies and Persona Supports Removed from the Campaign**. |

## Concrete corrections

1. Add p. 9's expert persistent-damage ceiling: after each game record remaining HP, capped at the identity's base HP. The context correctly contrasts FNE's each-game record with the current RORS outcome implementation, but omits that ceiling. Also make explicit that an expert-campaign player defeated while teammates win does not participate in that scenario's Victory steps, and may rejoin during the next setup by the printed healing instructions. The capability table's brief mention of rejoining does not supply these operative requirements.
2. Specify that the Getaway's two-progress additional 1-per-player threat (2-per-player in expert mode) is placed **on Jackknifed Tanker Truck** (p. 19). It is not extra main-scheme threat. The context currently omits the target.
3. Narrow the provenance claim: `src/Marvel.Rules/Setup/Creation.cs` describes `AssociationSet` as the association for identity-linked encounter cards, and `DeckConstruction.ValidateAssociatedMembership` uses it for nemesis membership validation. It does not establish generic named-set sourcing for 208B/209A. Existing printed set lookup is `CardCatalog.EncounterSet` through `ICardFacts.EncounterSet`; authored `SetupCatalog` pools and `Dealer` are the relevant setup infrastructure. Neither is evidence that complete FNE pools are admitted or allocated.
4. Replace the shortage/exhaustion/removal items in the open-question list with the resolved requirements below. Preserve the log's exact printed heading when claiming exact log fields. The Kingpin flip representation is an engine choice and should not be listed as an unresolved authority question.

## Questions resolved from existing authorities

**209A support shortage.** Original scans 60153-60156 are Ben Urich, Betty Brant, J. Jonah Jameson and Robbie Robertson: four distinct unique DAILY BUGLE / PERSONA supports at printed positions 3/9-6/9. The surrounding inspected positions are an environment, side scheme and treachery. The accepted 209A instruction selects one eligible support per player; it does not request a random selection. Under `rr:each-player.1`, the first player chooses resolution order; under `.2`, a player unable to fully resolve an effect resolves it as fully as possible. Thus, with two eligible supports for three players, the first player determines the player sequence; the first two can each put an eligible support into play and the last cannot. Do not duplicate a support, reinstate a removed title, deal a substitute encounter card or invent a replacement. This is an application of general rules, not a new FNE ruling gap. Prove availability and ordering through engine authority when implemented.

**208B source pool.** Scans 60144-60149 are Absorbing Man, Baron Zemo, Drang, Mysterio, Proxima Midnight and Rhino, one each at consecutive set positions 3/9-8/9. 60143 is Master Key and 60150 is Imprisoned, completing the nine-position recipe with its main scheme. Six minions suffice for every supported one-to-four-player table: in expert mode deal one random minion to each player without replacement, then shuffle the remainder into the encounter deck. The Raft is Failed when 208B is present, so p. 9 excludes it from selection as the current scenario. Its source pool must be brought from the named set into other scenarios, including Kingpin; a prior attempt's zones do not carry across games. There is no ordinary pool-exhaustion problem in this printed setup. A missing/duplicated source pool is incomplete acquisition or setup, not justification for a new game rule. This does not certify arbitrary custom combinations or every possible earlier-setup movement; any concrete such interaction requires its actual enabling authority before being called a blocker.

**Removal on defeat and retry.** Bullseye p. 5 explicitly makes its ally/PERSONA-support removals permanent for the campaign; `rr:modes-of-play.10` explicitly includes retrying the scenario where removal occurred. Verified printed removal effects include Raise the Stakes 69, Incessant Pursuit 71, Everything is a Weapon 74, and Unerring Desire 75. The four DAILY BUGLE supports have PERSONA, so the last effect can remove them. Removal does not wait for a victory log-writing instruction: the ongoing campaign must retain the actual removed titles even if that game ends in defeat. Victory's printed bookkeeping does not revoke the general retry rule. Conversely, an ordinary discard or removal of Bullseye's encounter attachment is not automatically a permanent ban on all such cards. Derive permanence for the affected ally/support from p. 5, and honor other effects' own wording. How software captures a durable event and projects the log is an engine choice; no extra human ruling is needed for these inspected effects.

**Kingpin environment flip.** p. 23 says flip a Completed environment to Failed before replay. p. 9 rebuilds later games from campaign outcomes. Persisting the changed outcome is therefore required. Replacing the completed checkbox or storing a distinct outcome override is an implementation representation, not an authority blocker. Never reconstruct the same environment on both sides.

## One genuine remaining clarification

Kingpin p. 23 uses **Standard Mode Only** for replay and **Expert Campaign Only** for flip-a-completed-environment-or-lose. `rr:modes-of-play.11-12` and the RR campaign FAQ permit scenario and campaign difficulty independently. The literal four cases are:

| Scenario mode | Campaign mode | Literal p. 23 coverage |
|---|---|---|
| Standard | Standard | Replay. |
| Expert | Expert | Flip one Completed environment and replay, or lose campaign if none exists. |
| Standard | Expert | Both bullets apply. With a Completed environment, replay and flip agree; with none, unconditional replay and campaign loss conflict. |
| Expert | Standard | Neither qualified bullet supplies campaign continuation. |

The ambiguity is narrower than saying every overlap is contradictory. The printed page does not itself authorize rewriting Standard Mode as Standard Campaign, nor making Expert Campaign force expert scenario mode.

I searched the current local `datasets/rulings/` and `datasets/marvelcdb-faq/` for Kingpin, campaign/mode qualifiers, Daily Bugle, Fear No Evil and Raft context. Relevant FNE rulings present concern King's Gambit, Echo's nemesis Kingpin and A Change of Venue; none answers this defeat question. I inspected the current [Hall of Heroes FFG rulings page](https://hallofheroeslcg.com/latest-ffg-rulings-post-rrg-1-7/), including its Kingpin hits, and performed targeted exact-qualifier/FNE-errata searches. No applicable official answer was found. The FFG product support URL redirected to its new product-line site, and a follow-up fetch failed; this is a bounded negative search, not proof that no answer exists anywhere. Community playthroughs and another engine were not used as rules authority.

Next investigation: obtain an FFG clarification specifically covering Standard scenario + Expert campaign with zero Completed environments, and Expert scenario + Standard campaign. This review sends no message or ruling request. Until authoritative text resolves those cases, preserve the exact labels and fail explicitly on unsupported combinations rather than silently selecting an interpretation. Other campaign progression work can proceed independently.

## Repository capability review and limits

The map correctly identifies a RORS-specific campaign rather than FNE support: `CampaignStage` is the fixed Crossbones-through-RedSkull enum; `CampaignOutcomeRules.Advance` increments it; `CampaignRules.Create` requires the RORS composition; `CampaignSeed` uses `marvel-rors-attempt-v1`. `CampaignOutcomeRules.Finish` returns on defeat before `RecordPlayers`, and the final-stage win also returns before that method, so it is not an implementation of FNE's each-game HP recording. `CampaignProgressRules` retains owned campaign-card removals and `CampaignDeckExclusions` accepts printed allies only, not arbitrary removed support titles.

`RuntimeProductAdmission` remains a closed policy excluding FNE. `CampaignSave`/`CampaignReplay` and `FileCampaignStore` establish existing persistence/replay infrastructure, not FNE behavior. `CampaignDescriptor` exposes state and reward choice; `CampaignCreationForm`/`CampaignPresentation` explicitly name Rise of Red Skull. These observations support the identified campaign/contract/UI prerequisites. No behavioral or save compatibility claim was tested. No claim is made that a complete named-set selector is absent from all engine code; the corrected map says the cited primitive does not prove the required FNE interaction.

After the listed corrections, deterministic progress/assignment/selection and title-removal state can be implemented against these authorities in a bounded slice. Full scenario recipes, the twelve faces' admission, all scenario/main-scheme mechanics, Kingpin's unresolved defeat cases, and product/native acceptance remain outside that readiness claim. The companion `campaign-rules-review.json` retains exact hashes, coverage, decisions and search limits.


## Independent correction closure — 2026-10-28

**Disposition: corrected campaign-context handoff accepted for a bounded implementation slice.** This closure supersedes the opening corrections-required disposition only for the exact corrected artifacts below. The original review and its findings remain above. Full Fear No Evil campaign research readiness, admission, implementation and product acceptance remain unestablished.

I compared the complete Markdown and JSON diffs against their retained before-repair versions and the original review findings. Independently recomputed SHA-256 values:

| Artifact | SHA-256 |
|---|---|
| Corrected `fne-campaign-rules-context.md` | `1e7a27a74489e646a68d8dda01cf91ba6eaa45af87b7bdea44c90286c4818061` |
| Corrected `campaign-rules-context.json` | `2a23cf07313217a517646af8dc97996686cd440370a02f90ab8fef67de69d5bf` |
| Before-repair Markdown | `f9b75e9243154996596da5ec17b85bb33c7781e8e6539582bd333b4782333595` |
| Before-repair JSON | `a47dff755d18bee9abbaf22f8d10f609f85e46c01093057174a4585377d8965a` |

The corrections are complete in both repaired artifacts: per-game expert HP capped at base HP; defeated-player Victory exclusion and next-setup rejoin; Getaway additional threat on Jackknifed Tanker Truck; named-set lookup/pool provenance distinguished from identity association; DAILY BUGLE shortage resolved in first-player-selected order under `rr:each-player.1–2`; the six-card Raft pool and allocation across games; Bullseye removal durability through defeat/retry; and the exact printed removed-title heading. Persistent Kingpin flips remain required, with storage representation explicitly an engine choice. No concrete correction remains from the original correction list.

Kingpin's four-case question remains **OPEN**. Standard scenario + expert campaign conflicts only when no Completed environment remains; expert scenario + standard campaign has no qualified continuation instruction. The matching-mode cases and the non-conflicting overlap with a Completed environment remain distinct. The repaired packet preserves the printed labels, the bounded negative-search limits and the requirement to fail explicitly on unsupported cases. No new ruling, forced mode coupling or guessed precedence is supplied by this closure.

This closure re-read the cited local each-player and modes-of-play clauses and reused the original review's visual coverage of all ten rulebook pages and its inspected source-card pools. It performed no new source acquisition, PDF/card proofread, network search, Plane operation, build, test or native run. The retained original review hash `d4abf52102a172627745a600c38ee5c0f532b46cef122fb2fd8f18f701fb5c06` in the corrected JSON identifies this file's exact pre-closure bytes; this append does not rewrite that pinned historical review. The corrected artifacts' pending-closure status describes their pre-closure state; this section and the companion closure JSON record the subsequent disposition without changing those artifacts.

The accepted handoff is deterministic five-row progression, scenario/villain choice and title-removal state through public campaign authority with save/replay, subject to card/data admission dependencies and explicit unsupported Kingpin branches. Full scenario recipes, original main-scheme mechanics, the twelve faces' admission, other FNE research and product/native acceptance retain the original review's limits. No packaging gate is added. The companion `C:/Users/mggar/Source/evidence/fne-acquisition/campaign-rules/campaign-rules-review-closure.json` records this closure's exact inputs and per-finding decisions.

# The ability dataset — authored

| | |
|---|---|
| Kind | authored |
| Product boundary | Core Set, Rise of Red Skull, Captain America and Wrecking Crew; Green Goblin and Ms. Marvel research rows remain publicly excluded |
| Records | 658 printed card faces, including explicit empty rows |
| Authorities | Generated `datasets/cards/`, vendored Rules Reference, expansion rulebooks and published rulings |
| Gated by | Ability admission/lowering tests, complete-product admission, actual-card tests, campaign acceptance and executable Core behavioral transcripts |

`abilities.json` records executable interpretations of printed text. It is not
a copied rules source or generated printed-card catalog. Authoring follows
[the card DSL](../../docs/card-dsl.md) and
[rules provenance](../../docs/rules-provenance.md); ambiguous timing requires a
ruling check. No network access is required to load or validate this dataset.

`AbilityProgramTests.TheCompleteCoreBookLowersWithEveryFaceAndAbility` checks
the compiled book's complete face and ability membership. Ability-data and
lowering tests reject unknown syntax and unsupported meanings. The executable
Core corpus under `specs/behavior/core/` checks card and rules outcomes; a
successful parse alone does not establish correct card behavior.

Four Wrecking Crew Corrupt Prison Guard faces have complete keyword-only rows
under MARVEL-631. Their presence does not admit Wrecking Crew: the scenario has
no public setup recipe and its remaining card obligations are incomplete.
`WreckingCardQueryTests` verifies that each guard blocks all four villains and
that removing it restores inactive-villain attack targets.

New products require the complete admission work described in
[scope.md](../../docs/scope.md). UI layout, gestures, art and transport settings
do not belong in this dataset.

Risky Business has nineteen authored faces: six villain faces (02001a–02003b),
four main-scheme faces (02004a–02005b), two environment faces (02006a/b),
and seven encounter faces (02007–02013). A Mess of Things adds four authored
research faces (02037–02040). Green Goblin remains outside public admission.
The main-scheme interpretation follows printed card text, Appendix II setup,
main-scheme advancement and `rr:then`: completion places per-player infamy before
reading the resulting total for each player deck. Criminal Enterprise absent
(including its State of Madness face) cannot complete that antecedent.
`RiskyBusinessMainSchemeTests`, `MainSchemeCompletionTests` and
`RiskyBusinessSetupTests` exercise the authored rows through ordinary engine
operations; no public complete-game claim follows from these research tests.

All Tied Up (02048) is an authored Running Interference research face. Its
attachment targets the revealing player's identity in either form. The constant
host prohibitions use the reviewed readiness/form mechanism (MARVEL-709), and
its printed Action spends mental and physical resources in either form.
`AllTiedUpProhibitionTests` exercises the actual row, host-only permissions,
live blanking/departure, dependent projections and multiplayer reveal placement.
The interpretation includes vendored ruling `ruling:6cc2957c6bbe4596` on
card-forced flips; the queried MarvelCDB FAQ has no entry for this face.
Green Goblin remains outside public admission.

Goblin Gimmicks adds four authored research faces (02033–02036), with two physical
copies each in its existing eight-card recipe. Goblin Glider ranks eligible enemies
by printed hit points and has an energy/energy Hero Action; Pumpkin Bombs attaches
to the villain and has a physical/physical Hero Action. Completed attack binding,
persistent facedown boosts and actual-healing receipts are exercised against the
authored rows. The queried MarvelCDB FAQ has no entry for these four faces;
vendored ruling `ruling:e6531edb529e342d` supports Pumpkin Bombs with ally defense.
These rows do not admit Green Goblin publicly.

Power Drain adds five authored research faces (02041–02045), one physical copy
each. Actual-card tests cover defeat hand-resource selection, exact Electro
recruitment, numeric boost receipts through exhaustion, indirect damage and
healing. The vendored MarvelCDB FAQ for Electro requires its response after ally
defense; ruling `ruling:a9007ff3a4727a80` confirms Power Drain resolves before
leaving play. Other faces have no card FAQ entry. Green Goblin stays publicly excluded.
Running Interference completes research authoring of 02046–02049. The scheme
uses each-player payment, Tombstone requires a completed attack that actually
damaged the identity, and Media Coverage attaches to the revealing identity,
repeats reveal effects finitely and has an alter-ego mental removal action.
The printed authority is `datasets/cards/cards.json`; `faq:02049` and vendored
rulings `ruling:49dc5a965dcd4028` and `ruling:c516c82788ea8d77` distinguish
cancellation and villain promotion. Green Goblin remains publicly excluded.

Mutagen Formula adds seven authored research faces: Green Goblin I–III
(02014–02016), Unleashing the Mutagen (02017a/b), and Mutagen Cloud (02018a/b).
Printed cards are authoritative; ruling `ruling:9a1cb4d914aa6778` confirms that
attack overkill to the identity satisfies "attacks and damages you".
`MutagenStageAbilityTests`, `MutagenEscalationTests` and
`MutagenFormulaSetupTests` exercise actual authored rows. All 57 Green Goblin
faces are authored research data; public admission remains excluded.


Mutagen Formula encounter research adds 02019–02025. Glider and Bombs are exact
printed-text, stat and annotation reprints of 02033/02034. Hysteria separates
attack initiation (actor) and scheme initiation (subject) in the existing engine
trigger vocabulary. Knight uses the physical discard receipt; Soldier and Thrall
use ordinary boost entry; Knight and Monster defer their exact source shuffle
until activation completion. Monster branches on the initial stunned state.
The vendored MarvelCDB FAQ contains no entry for these seven faces. Relevant
vendored rulings include ruling:e6531edb529e342d (attacks with ally defense),
ruling:6fd4652a06512d03 (Thrall Guard limits initiation), and
ruling:cef917d276a31694 (Soldier defeat resolves between printed sentences).
Focused tests cover characteristic paths; the evidence document explicitly
records outstanding review and proof. Public Green Goblin admission stays closed.

Final Mutagen research packets add 02026–02032 (57/57 distinct Goblin faces).
Authority is joined printed cards. Overrun uses the reviewed ordered discard receipt;
Wicked Ambitions uses immediate each-time choices and double printed stage.
Rulings 7c484b8434a60f3a and d4def3183850e80f cover discard/reset.
02030 attacks either form and suppresses ordinary/additional boost deals in alter-ego
form. Its exact-source boost_const modifier adds one icon only with an engaged
Goblin minion. Official FAQ 02030 and ruling:ab735d7ce752f211 confirm alter-ego
attacks; ruling:9e43ecc7878167bd concerns icon cancellation. Actual-row tests prove
form/Hysteria, attack/scheme, non-first-player engagement and source isolation.
02031 reads activationDamagePlaced after an attack and activationThreat after a
scheme. Damage placement includes activation-owned boost abilities, after
prevention, plus attack and overkill placement. rr:damage.3.2 distinguishes it
from damage dealt; ruling:d855181a23d49683 declines errata. Actual-row full
encounter tests prove the two/three threshold, one additional reveal, cleanup,
both forms, stun/confuse cancellation, Tough, prevention and ally/overkill cases.
Public admission remains closed. See docs/content/mutagen-final-face-evidence.md.

Ms. Marvel adds seven research faces: 05001a/b, 05003–05005 and 05010–05011.
Printed text comes from `datasets/cards/cards.json`. Morphogenetics uses the
exact completed physical event in its owner discard (rulings
`ruling:7f69f006cddce07e`, `ruling:556ecad3abe55ce9` and
`ruling:b144656def387a1e`). Teen Spirit searches the identity-specific set,
with the normal player-deck exhaustion boundary. Event modifiers use each
instance (`rr:event.5`, `faq:05010`, `faq:05011`) and preserve printed caps
(`ruling:88369b11361157e4`). `MsMarvelEventCardTests`,
`MsMarvelIdentityEventTests` and `MsMarvelTeenSpiritTests` exercise these actual
rows. This partial authoring keeps Ms. Marvel outside public admission.

The persona/resource packet adds actual rows 05008 Nakia Bahadir and 05009
Biokinetic Polymer Suit, from the joined printed text in `datasets/cards/cards.json`.
Nakia uses the controlled player's one-use player-phase reduction, preserving it
across form changes and rejected payments. Suit uses the event resource restriction
and explicit hero form; ruling:5b44b2adcc60d4b7 distinguishes an event ability's
own cost from a selected ally's cost. `MsMarvelPersonaTests` and
`MsMarvelSuitCardTests` exercise these rows. Bruno (05007) uses reviewed optional bounded stored-card selection. See `docs/content/ms-marvel-persona-evidence.md`.


Aamir/resource reprint packet adds seven actual Ms. Marvel research faces:
05006, 05016, 05019–05022 and 05024 (16 of 34 Ms. Marvel faces; 558 total).
Printed authority is joined `datasets/cards/cards.json`; the vendored MarvelCDB
FAQ has no entries for these seven faces, and the Hall of Heroes search yielded
no card-specific clarification. Aamir uses reviewed bottom placement followed by
dependent draw. Resource cards read printed icons; Protection doubling qualifies
the card being paid. Mansion chooses a player; Reflexes uses three energy
counters, Hero Resource, exhaustion and discard at the last counter.
`MsMarvelAamirCardTests`, `MsMarvelResourceReprintTests` and
`MsMarvelReflexesCardTests` exercise their actual rows. See
`docs/content/ms-marvel-aamir-resource-evidence.md`. Public admission stays closed.

The ally/condition packet adds actual rows 05002 Red Dagger, 05023 Endurance,
05032 Morale Boost and 05033 Down Time (20 of 34 Ms. Marvel faces; 562 total).
Printed authority is joined `datasets/cards/cards.json`. Defeat payment uses two
different resource types; the controller pays and the owner receives the ally
(rulings 10293662ffefd5b2 and 2368dda1610740b3). The upgrades use any-player
control and the printed one-per-player limit. Morale Boost expires at round end.
`MsMarvelRedDaggerTests`, `MsMarvelConditionUpgradeTests` and
`MsMarvelAdditionalEventTests` exercise the unmodified full book. Melee, Tackle
and Concussive Blow remain unauthored pending the engine gaps recorded in
`docs/content/ms-marvel-ally-condition-evidence.md`. Public admission stays closed.

Bruno Carrelli (05007), Tackle (05015) and Concussive Blow (05031) add three
research faces (23 of 34 Ms. Marvel faces; 565 total). Bruno stores one own-hand
card facedown with his alter-ego action and retrieves zero through three stored
cards with his separate Action in either form; both abilities pay exhaustion.
The events use committed physical-resource payment and RR1.8 `rr:target.3.4`.
Bruno ruling `ruling:6768213a7e0f4606` confirms stored cards are out of play.
The queried FAQ has no entry for these three faces. Actual full-book tests are
`MsMarvelBrunoCardTests` and `MsMarvelStatusCardTests`. Public admission stays closed.

Home by Dawn and Thomas Edison add two whole Ms. Marvel research faces (25 of 34; 567 total). Printed authority is `datasets/cards/cards.json`. Thomas follows the engaged player per vendored `ruling:3aba0c6abbd5c2ad`; tests cover foreign damage sources and live engagement. No public admission changes.

Nova (05012) and Get Behind Me! (05013) add two Ms. Marvel research faces. Printed authority is datasets/cards/cards.json. MsMarvelDefenseCardTests exercises actual rows, energy payment, multiplayer cancellation, nested attack completion and Nova defeating its attacker (faq:05012). Energy Barrier, Preemptive Strike and Lockjaw remain unauthored pending dedicated engine support; see docs/content/research/ms-marvel-defense-basic-evidence.md. No public admission follows.


Generation Why? (05026) adds one Ms. Marvel research face (28 of 34; 570 total).
Authority is joined datasets/cards/cards.json and vendored
ruling:e8654dd34b58be12, which fixes the ally/Persona-support count at initiation.
The reviewed global support query and existing each-player discard instruction
provide that quota; rr:player-deck.3 bounds each deck at its immediate reset.
GenerationWhyCardTests exercises the actual full book and encounter lifecycle,
printed starting threat and hazard, short-deck reset and no eligible cards.
See docs/content/generation-why-evidence.md. Public admission stays closed.

Melee (05030) adds one Ms. Marvel face (29 of 34; 571 total). Its two
sentences share one attack while choosing the second enemy after the first
settles. Printed authority is datasets/cards/cards.json; rulings e0333bc6316bd7f0,
9aa6221cd0950fbb, f32c53356db718a6 and a43333f9f9d3634a establish Guard,
villain stage identity, final responses and Embiggen behavior. StagedAttackTests
and the actual full-book cases in StagedAttackReceiptTests and
StagedAttackIndependentTests exercise this row. Public Ms. Marvel admission
remains closed; native and hosted persistence proof remain separate.

Energy Barrier (05017) adds one face (30 of 34 Ms. Marvel; 572 total).
Its typed damage-taken trigger observes remaining damage after prevention,
following faq:05017 and rr:prevent.1.1 while retaining distinct dealt amounts.
The actual full book drives RemainingDamageInterruptTests. Three reflection
counters and Uses disposal follow the printed dataset; arbitrary damage in
either form qualifies, and the reflected damage is not an attack.

Edison's Giant Robot (05028) adds one actual research face (31 of 34 Ms. Marvel;
573 total). Printed authority is joined `datasets/cards/cards.json`; vendored
`ruling:d1087a7e030d2fd9` confirms blanking applies to all players.
`PrintedTextBlankingTests` uses the unmodified full book for typed mental payment,
form restriction, multiplayer damage, suppression, expiry and reentry. The
standalone timed self-blank opcode has independent bounded engine review; the
actual row requires separate content review. Public admission stays closed.

Harvest (05029) adds one face (32 of 34 Ms. Marvel; 574 total). Its printed
When Revealed exhausts all Persona supports, heals for each actual exhaustion
and gains surge only when none exhausted. The committed effect receipt differs
from exhaustion costs and remains zero when every Persona is already exhausted.
HarvestEffectReceiptTests uses the full book for normal and zero/surge reveals;
synthetic continuation variants isolate the receipt. Public admission stays closed.

Lockjaw (05018) adds one face (33 of 34 Ms. Marvel; 575 total). Joined printed
text grants ordinary own-discard play during the player's turn, paying the
printed resource cost. DiscardPlayDecisionTests uses the full book for actual
permission, second-seat payment/discount and rejection-purity cases; synthetic
entry variants remain isolated. Public Ms. Marvel admission remains closed.

Preemptive Strike (05014) adds the final Ms. Marvel face (34 of 34; 576 total).
The exact attack-only boost occurrence, villain actor and hero form implement
the printed Hero Interrupt (defense). Numeric cancellation uses current modified
icons and amplify, captures only icons actually cancelled, and leaves later
Boost-star resolution intact. Vendored rulings 1ea208998735ef4b,
33d8c914aa86d19f and 937d6ff4f970a1da govern ordering and cancellation.
IndividualBoostCancellationTests uses the actual full book except for explicitly
synthetic boost-star variants. Full inventory does not itself admit the product;
whole-pack public setup, gameplay, restart and native evidence remain separate.

Thor adds eleven nonpublic research faces: 06001b, 06002, 06007–06010, 06013, 06016 and 06022–06024. Actual full-book tests exercise searches, named-character entry responses, constants and resources. See `docs/content/thor-batch-a-evidence.md` for the semantic description gap and proof limits. Public admission remains closed.

Defender of the Nine Realms (06003) and Family Feud (06027) add two actual
Thor research faces (15 authored Thor faces; 591 total). The standalone
engagement arrow cost precedes the chosen scheme thwart and uses current
Defender clarifications 6e301a347a5392fb and a1f4982cfebcd768. Family Feud
adds the complete in-play Asgard census to ordinary printed starting threat.
ThorDefenderCardTests and ThorFamilyFeudCardTests use the full canonical book.
See docs/content/thor-next-actual-evidence.md. Public admission remains closed.

Thor utility research rows 06021, 06025, 06033 and 06034 bring the exact inventory to 19 Thor faces and 595 total. Full-book behavior and mutation evidence: `docs/content/thor-utility-evidence.md`.

Valkyrie (06012) and Heimdall (06020) add two actual Thor research faces
(24 Thor faces; 600 total on base 46827c67). Joined printed authority is
`datasets/cards/cards.json`; faq:06006 is the Lightning Strike non-attack
erratum, and ruling:2001c6705b63bf4f confirms private looking.
`ThorValkyrieCardTests`, `ThorHeimdallCardTests` and
`ThorHeimdallPrivacyTests` exercise the unmodified full book.
See `docs/content/thor-valkyrie-heimdall-evidence.md`. Public admission remains
closed; Lightning Strike remains unauthored pending its simultaneous target group.

Black Widow identity/Preparation research adds five actual rows: Black Widow
(08001a), Safe House #29 (08005), Black Widow's Gauntlet (08007), Synth-Suit
(08009), and Widow's Bite (08010). Printed authority is datasets/cards/cards.json;
RR 1.8 resolve and ruling:420bd5b1763b9506 require resolved abilities for
Widowmaker and Synth-Suit. The 623 existing rows are unchanged; there are 628
rows, including 17 of Black Widow's 34 playable faces. No new starting counter
pools are printed on these five faces. BlackWidowPreparationCardTests,
PreparationPrintedResolutionTests, PreparationContinuationIndependentTests,
PreparationResourceTests/IndependentTests and SafeHouseCostChoiceTests/SuspensionTests
exercise actual rows with explicitly labelled synthetic suspension support.
Public Black Widow admission remains excluded pending complete pack delivery.
See docs/content/black-widow-preparation-batch.md for evidence and limits.


The next Black Widow batch adds actual 08023, 08025, 08026 and 08029: 632
rows and 21/34 Black Widow faces. The existing 628 rows retain their exact raw
prefix/order/semantics. No starting counters or public admission changes.
Quake 08012 remains unauthored: a compiled characteristic incarnation probe
found that the completed scheme actor selector damages a departed/reentered
copy. See `docs/content/black-widow-allies-encounters.md` for bounded evidence.


The seven-face Black Widow batch adds actual 08003, 08004, 08012, 08013,
08018, 08024 and 08033, giving 639 rows and 28/34 playable Black Widow faces.
The preceding 632-row text is preserved. These rows print no starting counters.
Printed authority is datasets/cards/cards.json; current RR 1.8 governs Surge,
defeat and labeled attacks. The reviewed mechanism authority and bounded actual-row
evidence are linked in docs/content/black-widow-seven-faces.md. Public admission
remains closed. This later batch includes the reviewed exact-incarnation Quake
mechanism; the preceding batch's unauthored status describes that earlier batch.

Counterattack (08030) and Rapid Response (08031) add two actual Black Widow
research rows: 634 rows, 23/34 Black Widow faces on base 29aa293f. The exact
632-row prefix is retained. Printed authority is datasets/cards/cards.json;
neither face prints starting counters. Current RR 1.8 attack step 6(a)/(b)
places forced responses and retaliate before optional Counterattack, superseding
the pre-1.5/1.6 Counterattack rulings. Rapid Response binds captured controller
at defeat, the exact discard incarnation and ordinary ally entry before damage.
The characteristic, receipt, timing, entry and durable restart tests now use
the actual canonical rows. See docs/content/black-widow-final-response-rows.md.
This packet changes no engine mechanism, product admission, setup or version.

The combined Black Widow book contains 645 unique rows and all 34 printed faces.
Its raw predecessor rows and incoming source row objects are retained; four
prevention rows and the final two response rows complete the seven-face batch.
Every row has independent authority review. Exact membership and starting-counter
checks remain explicit. The product is not yet publicly admitted; focused source
and combined evidence is recorded in `docs/content/black-widow-delivery-checkpoint.md`.

Doctor Strange reprint research adds six faces (MARVEL-799): 09017 The Power of
Protection, 09018 Med Team, 09022 Energy, 09023 Genius, 09024 Strength and
09025 Avengers Mansion. Their generated printed rules, attributes and traits
match the Core printings exactly. Actual-row payment, counter lifecycle, target
and non-first-player recipient tests gate these interpretations. See
[the reprint evidence](../../docs/content/doctor-strange-reprints-evidence.md).
Doctor Strange remains outside complete-product public admission.

Doctor Strange constant/resource research adds 09009, 09011 and 09026 (MARVEL-801). The preceding 651 raw row objects remain intact; 9/40 faces are authored. Printed authority is datasets/cards/cards.json; no starting counters or public admission changes. See docs/content/doctor-strange-constants-evidence.md.

Doctor Strange response-event research adds 09016, 09021, 09037 and 09038 (MARVEL-803), preserving the preceding 654 raw row objects. There are 13/40 authored faces; public admission remains closed. See docs/content/doctor-strange-response-events-evidence.md.
Doctor Strange's next four actual rows (MARVEL-810/813) append 09006 Mystical
Studies, 09010 Magical Enhancements, 09032 Crimson Bands of Cyttorak and 09034
Seven Rings of Raggadorr. There are 671 authored rows and 26/40 Strange faces.
All 667 predecessor raw objects remain byte-identical. Printed authority is
`datasets/cards/cards.json`; current RR 1.8 search, control, forced, target and
status clauses govern, with the current-compatible Invocation rulings recorded
in `docs/content/doctor-strange-next-four-rows.md`. The rows add no starting
counters and change no engine capability, setup or public admission. Actual-book
search, multiplayer ownership/form/round-end and both Invocation caller lifecycle
tests gate these authored interpretations. Synthetic alternate effects remain
explicit fixture replacements; duplicate admission remains strict.

MARVEL-819 appends actual 09020 Unflappable only: 672 rows, 27/40 Doctor Strange faces. All 671 predecessor raw objects remain intact. Printed authority is datasets/cards/cards.json and faq:09020; current RR 1.8 includes defense-labeled events. No starting counters, engine changes or public admission. Clea, The Night Nurse and Iron Man ally remain unauthored pending the precisely bounded prerequisites in docs/content/doctor-strange-support-ally-rows.md.


MARVEL-819 final support packet appends Clea (09013), The Night Nurse (09019)
and Leadership ally Iron Man (09039): 675 rows, 30/40 Doctor Strange faces.
All 672 predecessor raw objects remain intact. Printed authority is generated
datasets/cards/cards.json; faq:09019 confirms mandatory Tough removal. Current
RR 1.8 ownership, interrupt, AND and initiation rules govern these programs.
The accepted owner-deck, physical status-host and exact upgrade-host pricing
prerequisites supply their execution. Actual-row evidence and inventory gates
are recorded in docs/content/doctor-strange-final-support-rows.md. This authors
research content only; setup and public product admission remain unchanged.

MARVEL-823 appends Astral Projection (09003), Magic Blast (09004) and Protective
Ward (09007): 678 rows, 33/40 Doctor Strange faces. All 673 predecessor row
objects outside Core 01004/01075 retain their raw text. Those two Core rows add
only the scoped encounter-reveal origin condition; their other programs remain
unchanged. Generated datasets/cards/cards.json supplies printed authority.
Current RR 1.8 costs, AND, labeled powers and cancellation govern; the September
14, 2026 ruling 7075a85e02f2203e governs encounter-origin eligibility. Actual-row
behavior, preserved inventory and mutation evidence are recorded in
[the signature event evidence](../../docs/content/doctor-strange-final-signature-events.md).
No starting counters, setup, engine or complete-product public admission changes.


The final reviewed support/defense packet appends Desperate Defense (09015),
Physical Toll (09027), and Open the Dark Dimension (09029): 682 rows and 37/40
Doctor Strange faces. All 679 predecessor raw objects remain byte-identical.
Generated datasets/cards/cards.json supplies printed authority. Current RR 1.8
obligation, event, defense, delayed-effect and When Defeated rules govern;
ruling:277ab401b1269ec7 distinguishes Invocation resolution from event play.
Accepted event-surcharge, exact-attack delayed-ready and hosted Invocation
prerequisites supply execution. There are no starting counters or public product
admission changes. Canonical fixture, secrecy, actual-row mutation and restored
verification evidence is recorded in docs/content/doctor-strange-final-support-rows.md.

Baron Mordo (09028) and Thoughtcasting (09031) append two actual research rows: 684 rows and 39/40 Doctor Strange faces. All 682 predecessor raw objects remain byte-identical. Generated datasets/cards/cards.json supplies printed authority; RR 1.8 attack initiation, printed values, resolving-player choice and deck reset govern. The exact discard receipt supplies Mordo resource branches; Thoughtcasting retains the chosen physical card across discard and reads its printed cost. No starting counters, engine, setup or public admission changes. Counterspell (09030) remains a test-local candidate pending MARVEL-835. Actual-row evidence is recorded in docs/content/doctor-strange-mordo-thoughtcasting.md.

Counterspell (09030) appends the final actual Doctor Strange research face:
685 rows and all 40 printed faces. All 684 predecessor raw objects remain
byte-identical. Generated datasets/cards/cards.json supplies printed authority;
current RR 1.8 event, AND, Then and status priority rules govern. The hero-only
attachment has a form-independent Forced Interrupt for its attached player.
Cancellation and event discard are independent; attachment discard depends on
both completing. No starting counters, engine, setup or public admission changes.
Canonical and explicitly synthetic fixture evidence is recorded in
[the Counterspell actual-row packet](../../docs/content/doctor-strange-counterspell-row.md).

Hulk Basic reprints (10020–10024) append five research faces, bringing the book
to 693 rows and Hulk coverage to 8/33 faces. Corrected generated printed records
match Core 01088–01092 apart from IDs and pack, so their execution bodies are
reused. All 688 predecessor raw objects remain byte-identical. Actual-row resource,
recipient, exhaustion, discount consumption/expiry and title-limit evidence is in
[the Basic reprint packet](../../docs/content/hulk-basic-reprints.md). No engine,
starting-counter, setup or public admission changes are included.


MARVEL-846 appends seven actual Hulk signature research faces: 10002, 10003 and
10006–10010. The book has 700 rows and 15/33 Hulk faces; all 693 predecessor
raw objects remain byte-identical. Joined generated printed records, current
RR 1.8 and ruling:5bbba476a198a90c govern the interpretations. The seven faces
print no starting counters. Actual-row payment, exact attack lifetime, independent
draw and multiplayer form/ownership tests gate these rows. The signature packet
has no engine, setup or public-admission changes. See
[the signature packet](../../docs/content/hulk-signatures.md).

## Hulk starter cards — MARVEL-847

Actual research rows 10011–10014 and 10016–10019 append to 708 rows / 23 of
33 Hulk faces. All 700 predecessor raw objects remain byte-identical. Printed
authority is `datasets/cards/cards.json`; RR 1.8 and vendored FAQ 10016 govern
attack receipts, eligibility and the printed maximum. Actual-row tests and eight
killed data mutants are recorded in [the packet](../../docs/content/hulk-starter-cards.md).
Toe to Toe (10015) remains absent: the actual activation inside a labelled
attack requires a separate engine prerequisite. No engine, setup,
starting-counter or public admission change is included.


## Hulk Thunderclap — MARVEL-846

Actual research row 10005 appends to 709 rows / 24 of 33 Hulk faces. All 708
predecessor raw objects remain byte-identical. Printed authority is
`datasets/cards/cards.json`; current RR 1.8 choose, target, damage, Guard, Stun,
and Retaliate clauses govern. The vendored MarvelCDB FAQ queried 10005 and has
no entry for it. One distinct optional group receives simultaneous ordinary
damage through the independently reviewed bounded-damage prerequisite.
Actual paid hero-action, multiplayer, suspension, purity and data-mutation
proof is recorded in [the packet](../../docs/content/hulk-thunderclap-card.md).
Toe to Toe 10015 remains absent. This row adds no starting counters, setup,
public admission or engine source changes.


## Hulk customization cards - MARVEL-850

Actual research rows10030-10032 append to712 rows /27 of33 Hulk faces.
All709 predecessor raw UTF-8 objects remain byte-identical. Printed authority
is datasets/cards/cards.json, with current RR1.8 AND, defense and ownership
rules. No starting counters, engine, setup or public-admission changes. Beat Cop
10029 remains absent pending support-destination threat movement. See
[the customization packet](../../docs/content/hulk-customization-cards.md).


## Hulk obligation and nemesis - MARVEL-849

Actual research rows 10025-10028 append to 716 rows / 31 of 33 Hulk faces.
All 712 predecessor UTF-8 objects are preserved exactly. Corrected printed
Inner Demons has no Then; RR 1.8 governs printed resource counting and attacks.
No engine, starting-counter, setup or public-admission changes. Toe to Toe
10015 and Beat Cop 10029 remain absent. See
[the packet](../../docs/content/hulk-nemesis-cards.md).

## Hulk final two faces - MARVEL-847 / MARVEL-850

Actual research rows 10015 and 10029 complete 718 rows / all 33 Hulk faces.
All corrected 716 predecessor raw UTF-8 objects remain byte-identical and LF.
Printed authority is `datasets/cards/cards.json`; current RR 1.8 labeled
abilities, Stun, cost and movement govern. The vendored Toe to Toe Stun ruling
agrees with the current independent-instruction interpretation. Beat Cop uses
captured source threat after its exhaust/discard cost. Neither Beat Cop action
is an attack or thwart. No engine, starting-counter, setup or public-admission
changes. Actual-card behavior, inventory, byte preservation and mutation evidence
are recorded in [the packet](../../docs/content/hulk-final-two-cards.md).

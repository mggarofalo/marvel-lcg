# The ability dataset — authored

| | |
|---|---|
| Kind | authored |
| Product boundary | Core Set, Rise of Red Skull, Captain America and Wrecking Crew; Green Goblin and Ms. Marvel research rows remain publicly excluded |
| Records | 558 printed card faces, including explicit empty rows |
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
`MsMarvelSuitCardTests` exercise these rows. Bruno (05007) remains unauthored pending optional bounded stored-card selection. See `docs/content/ms-marvel-persona-evidence.md`.


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

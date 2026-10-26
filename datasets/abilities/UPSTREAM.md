# The ability dataset — authored

| | |
|---|---|
| Kind | authored |
| Product boundary | Core Set, Rise of Red Skull, Captain America and Wrecking Crew; Green Goblin research rows remain publicly excluded |
| Records | 513 printed card faces, including explicit empty rows |
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

# The ability dataset — authored

| | |
|---|---|
| Kind | authored |
| Product boundary | Marvel Champions Core Set and Rise of Red Skull |
| Records | 395 printed card faces, including explicit empty rows |
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

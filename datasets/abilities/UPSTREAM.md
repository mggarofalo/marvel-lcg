# The ability dataset — authored

| | |
|---|---|
| Kind | authored |
| Product boundary | Marvel Champions Core Set |
| Records | 209 printed card faces, including explicit empty rows |
| Authorities | Generated `datasets/cards/`, vendored Rules Reference and published rulings |
| Gated by | Ability admission/lowering tests, Core runtime scope tests and executable behavioral transcripts |

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

New products require the complete admission work described in
[scope.md](../../docs/scope.md). UI layout, gestures, art and transport settings
do not belong in this dataset.

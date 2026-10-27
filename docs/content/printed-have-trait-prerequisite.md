# Printed identity trait prerequisite

MARVEL-802 supplies the engine/content prerequisite discovered by MARVEL-801.
The candidate is the commit containing this record on `fix/printed-have-trait`,
based on `2660ed01`. No generated card data or authored ability rows change.

`CardCatalog.RequiredIdentityTrait` owns the reading of the canonical printed
fact. `PrintedPlayRestrictions.IdentityTrait` recognizes the exact sentence
prefixes `Play only if your identity has the ` and `Play only if you have the `,
with the existing nonempty trait and strict ` trait` suffix. Controlled-character
prerequisites remain a separate fact. `PrintedCardPlayPrerequisites` already
checks the acting identity's current effective trait; no legality change is
needed there. Reading a research face's prerequisite does not admit its runtime
content or implement its other abilities.

Authority is the generated `datasets/cards/cards.json`: Honorary Avenger
`03025` prints “Play only if your identity has the Avenger trait.” The Sorcerer
Supreme `09026` prints “Play only if you have the Mystic trait.” RR 1.8
`rr:you-your` resolves “you” to the identity when possible; `rr:identity.4`
puts the faceup side in play; `rr:traits.1` allows abilities to reference traits.
Doctor Strange `09001a` and Stephen Strange `09001b` both print MYSTIC, and Wong
`09002` is a MYSTIC ally. Tests use these actual catalog records without
claiming a supported Doctor Strange game.

`HaveTraitPlayRestrictionTests` verifies both actual prerequisite wordings,
controlled Spy and innocuous text negatives, strict sentence/suffix matching,
both Mystic forms in either seat, dynamic identity trait loss/grant/disposal,
and rejection when only another identity or a controlled ally is Mystic.
The rejection/purity test supplies an otherwise valid cost-2 Tenacity control
and a double-resource payment. Its baseline precedes all pricing queries and
captures cards, RNG state/consumption, continuous effects and agenda work. It
checks unchanged pending prompt and empty events after rejection, then compares
the successful control play's events and execution with an unprobed world.

Evidence on 2026-10-26, logs outside the repository under
`C:/Users/mggar/AppData/Local/Temp/printed-have-trait-`:

| Check | Result | Log suffix |
| --- | --- | --- |
| New tests against unchanged reader, after fixture validation | 7 failed, 6 passed | `baseline.log` |
| New tests, existing Avenger/controlled Spy tests and citation gate | 37 passed | `green.log` |
| Compiled mutant removing only the new prefix | exit 1; 7 failed, 6 passed | `mutant.log` |
| Restored sources, forced non-incremental Release rebuild | succeeded | `restored-build.log` |
| Same focused tests using rebuilt binaries | 37 passed | `restored-tests.log` |

The seven baseline/mutant failures distinguish the missing parse: the actual
09026 catalog trait is null, non-Mystic play remains priced, another identity or
ally wrongly suffices, and each form/seat ignores dynamic Mystic loss. Negative
sentence tests and the existing Avenger catalog spelling still pass. The mutant
was restored before the forced rebuild and final passing run.

Commands:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter FullyQualifiedName~HaveTraitPlayRestrictionTests
dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~HaveTraitPlayRestrictionTests|FullyQualifiedName~IdentityTraitPlayRestrictionTests|FullyQualifiedName~ControlledCharacterPlayRestrictionTests|FullyQualifiedName~RuleCitationTests'
```

No broad suite or native UI run is claimed. The actual authored Sorcerer Supreme
row and offer integration belong to the separate content packet after independent
review of this prerequisite.

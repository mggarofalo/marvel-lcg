# Public multiplayer Goblin checkpoint

Base: `fdaf4a9e`. `GreenGoblinMultiplayerTests` deals two admitted starters,
Spider-Man and Captain Marvel, through `DatasetGameFactory`. Every selected
legal decision is replayed in a second same-seed game, comparing canonical
digest, RNG, prompts, events and information. Both players must take turns.

Three of four fixed cases complete: Risky Standard/Expert seed 7 and Mutagen
Expert seed 8. Mutagen Standard seed 8 fails at decision 52, while resolving
`01013: choose a card`, offer 107. Printed 01013 is Photonic Blast; its target
is Goblin Thrall 02024. The engine raises because a labelled-power continuation
reads a defeat-triggered ability. The exact reproduction remains in the test;
it is not an accepted terminal game and must not enter a green candidate until
the engine finding is fixed and independently reviewed. The observing defeat
ability and safe continuation boundary still need diagnosis.

No production changes or victory claims. Complete command and failure log:
`dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release --filter FullyQualifiedName~GreenGoblinMultiplayerTests --logger "console;verbosity=normal"`,
`%TEMP%/goblin-multiplayer-proof.log` (three passed, one failed).

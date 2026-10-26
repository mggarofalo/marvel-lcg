# Public Green Goblin restart proof

Base candidate: `38a52f05`. `GreenGoblinRestartTests` uses production
`DatasetGameFactory` setup and `EngineHost` with its serialized memory store,
alongside an uninterrupted game dealt from the same specification.

All four Standard/Expert scenario recipes pass. Spider-Man seeds 1, 2, 3 and 4
match the complete-game packet. Each trace continues for up to 100 decisions
or the game's ending and must play cards, attack and reach a later round.
Every committed decision compares the persisted event sequence, state digest
fingerprint and RNG words against uninterrupted play. At dependent element,
option and defender prompts, a new host reloads the durable authority. The
visible sync response and serialized save remain identical. An invalid offer
after restart preserves both, and the trace continues using legal decisions.

This is managed production-session evidence, not rendered client evidence or
a claim of player victory. The first test attempt compared canonical digest
JSON with the journal's hashed fingerprint; the assertion now compares the
actual fingerprint contract. No production change was needed.

Focused command: `dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~GreenGoblinRestartTests`.
Result: four passed in 32 seconds. Log: `%TEMP%/goblin-restart-proof.log`.
Independent review and final combined integration gates remain required.

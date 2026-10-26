# Thor utility authoring evidence — MARVEL-563

Base: `2de3fe58` plus the reviewed Defender/Family Feud rows from `f2715ce3` (local cherry-pick `7a1750dc`). This packet authors four nonpublic research faces: `06021` Invulnerability, `06025` Avengers Mansion, `06033` Second Wind and `06034` Enhanced Physique. Exact ability inventory: **595 total, 19 Thor faces**. Public admission, setup and replay version are unchanged.

Authority: generated `datasets/cards/cards.json`, exact rows named above; `01091` has identical Mansion text and `03034` Enhanced Awareness shares the Uses/resource shape with different counter/resource type. The Thor implementation handoff is `docs/content/research/remaining/thor-implementation-handoff.md`. Vendored MarvelCDB FAQ was checked for all six faces and contains no matching entries. General authority: `rr:status-cards.1`, `rr:heal.1`, `rr:uses-x-type`, and printed Hero Action/Hero Resource restrictions.

`ThorUtilityEventTests` and `ThorUtilitySupportTests` load and lower the actual complete canonical ability book, using nonpublic research boards with real printed Thor identity forms. They establish:

- Invulnerability pays three, gives only its own hero tough, cannot initiate while already tough, and cannot initiate in alter-ego despite sufficient resources.
- Mansion exhausts in either form, lets its controller choose the other seat, draws exactly one for that seat, excludes foreign control and exhausted reuse, and preserves digest/RNG/pending work on an invalid choice before valid retry.
- Second Wind chooses an identity, including another seat's alter-ego, excludes an ally, heals four versus five from actual mental or declared wild payment, clamps at full health, cannot initiate when all identities are healthy, and preserves its paid choice on invalid retry. A captured offer is rejected atomically when the last damaged identity becomes healthy.
- Enhanced Physique enters through paid card play with three physical counters; repeated generator queries preserve digest/RNG/pending work; other-seat and alter-ego generation are absent; each use exhausts and spends one counter; the last use discards the source and still pays the card's cost.
- Repeated action queries and rejected insufficient payment match unprobed control worlds after the same valid choice, comparing final digest, RNG state and pending work.

Verification (Windows, Release, restored sources):

- `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental`: passed, zero warnings/errors.
- `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~ThorUtilityEventTests|FullyQualifiedName~ThorUtilitySupportTests|FullyQualifiedName~Marvel.Content.Tests.Cards.AbilityData'`: 46 passed (13 utility behavior cases and 33 ability-data cases).
- The paid Mansion play test also proves the four-resource cost, the shared one-per-player title limit across Thor/Core printings, and another player's separate allowance.

Fourteen actual-book data mutants ran serially against the compiled utility behavior tests. Thirteen were killed: Invulnerability wrong status and wrong target; Mansion wrong player, double draw and missing exhaustion; Second Wind wrong resource condition, base healing three, bonus healing four and heroes-only selection; Enhanced Physique two starting counters, retaining the empty source, missing Hero restriction and generating mental instead of physical. Each killed mutant produced behavioral test failures; the original book was restored byte-for-byte in `finally` before the nonincremental rebuild and final green run.

The survivor removes Invulnerability's explicit `form: hero` restriction. This is equivalent within the authored standalone ability: its `yourHero` selector resolves no target while the controller is in alter-ego, so initiation is already absent. The explicit form field is retained to state the printed restriction directly. The actual alter-ego/no-target behavior is asserted by `InvulnerabilityGivesOnlyOwnHeroToughAndCannotRepeatOrPlayInAlterEgo`.

Raw logs remain outside the repository: `C:/Users/mggar/Source/thor-utility-restored-build.log`, `thor-utility-restored-tests.log`, `thor-utility-mutants.json`, and individual `thor-mutant-*.log` files. The earlier 45-test run preceded the added Mansion payment/limit case; final restored verification is the 46-test run above.

No whole-pack, public setup, native UI or Linux coverage is claimed by this packet. Independent review follows authoring.

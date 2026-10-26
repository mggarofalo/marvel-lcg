# Thor E7 in-play census evidence

Bounded MARVEL-750 prerequisite on parent `de1a15cc`; public Thor admission,
actual Family Feud ability authoring, setup and version contracts remain closed.

Authority: generated `datasets/cards/cards.json` 06027 Family Feud says
"Place 1 additional threat here for each Asgard card in play." Printed starting
threat is 2. `datasets/marvelcdb-faq/faq.json` records 06027 queried with no entry.
Rules Reference `rr:in-play-and-out-of-play.1`, `.2`, `.6`, `.8` through `.11`
define live faces and excluded areas. `Places.CanAffect` owns published game-area
partitions and the engine's symmetric central-card exception.

Existing characters, support and controlled-card selectors omit other live card
kinds or require overlapping sums. New `cardsInPlay` traverses each in-play area
once, including hosted upgrades, then applies source reach and deterministic
physical-id order. Existing `withTrait` uses current effective traits.

`InPlayCardCensusTests` proves both Thor forms count once, another seat's ally,
support and Loki, cross-owner hosted and unhosted upgrades, shared encounter
areas, empty/many counts, out-of-play identical controls and both orientations
of beneath storage. A synthetic local-book 06027 reveal adds one count expression
after ordinary printed starting threat. This does not author the production row.
Queries retain digest, full RNG state, pending agenda count and execution against
an unprobed queued-reveal control. No hidden search observations are emitted.
Area-changing/repeated projection without complete projected placement refuses
explicitly; negative tests retain that boundary.

Verification (Release):

- `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental`: passed after restoration.
- Focused filter `FullyQualifiedName~InPlayCardCensusTests|FullyQualifiedName~RuleCitationTests|FullyQualifiedName~GlobalSupportQueryReviewTests`: 15 passed.
- Compiled mutant narrowed the census to the resolving player's play area:
  both identity-form census cases failed with missing other-area cards (2/2).
  Restored source was rebuilt nonincrementally before the green focused run.
- External logs: `%TEMP%/thor-e7-focused.log`, `thor-e7-mutant.log`,
  `thor-e7-restoredbuild.log`, `thor-e7-restored.log`.

Independent review and integrated broader gates belong to the lead packet.

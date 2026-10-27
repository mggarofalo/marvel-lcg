# Doctor Strange hosted Invocation return

MARVEL-838 proof packet, based on `0d55160ca3f778dccc6c7e1ddada7881d0eae3d3`.
The new `PublicDoctorStrangeHostedReturnTests` and `PublicStrangeHostedJourney`
use the production DatasetGameFactory and EngineHost. No card, world, setup,
ability book, admission or runtime state is prepared or changed by the fixture.

The initial read-only setup analysis examined seed candidates for Shadow of the
Past (`01190`) in the fourth encounter position. Seed 7 is pinned in the test;
the analysis stopped at its first match. This is not a repeated gameplay search.
Rhino has no modular set, Captain Marvel occupies seat 0, and Doctor Strange
occupies seat 1. Both players keep their printed starter hands and change to
hero form. Public host decisions end the turns and decline optional windows.
Shadow naturally brings Baron Mordo and Open the Dark Dimension (`09029`) into
play. At decision 14 the side scheme has 6 threat and the exact original public
top Invocation is facedown beneath it, still owned by Strange.

The stored session round-trips through strict JSON into an independent store,
production factory, EngineHost, Game and World. The existing restart helper
asserts distinct instances, unchanged stored bytes after restore and stale
rejection, and equality with the uninterrupted host. The continuation pays for
naturally dealt Astral Projection (`09003`) with its single selected scheme
target and uses offered basic thwarts. It defeats the scheme and returns the
hosted Invocation without an additional encounter round.

Each accepted continuation compares the complete session save, ordered emitted
events, canonical digest, full RNG state and words consumed, current prompt,
world projection, history and revision against the uninterrupted control.
The raw saved events must show, in order: movement of the exact hosted physical
Invocation from beneath this scheme into Strange's additional deck; detachment;
the five-card shuffle; revelation of the resulting public top; and departure of
the scheme to the encounter discard. The returned Invocation retains its owner
and incarnation, all five physical Invocations occur exactly once, and none is
in the Invocation discard. A second independent restart verifies the completed
return and its visible state.

At the hosted and completed checkpoints, projection checks cover restricted
seat 0, restricted seat 1 and spectator scope. Facedown beneath content has no
face or stable pile identity; the Invocation deck exposes exactly its public
top and conceals all other faces and identities. These are production View
projection assertions. This packet uses the cooperative host authority for
submission; it does not claim a separately authenticated spectator connection,
restricted-seat transport behavior, native UI behavior or query-purity coverage.

Verification on Windows:

- `dotnet test tests/Marvel.Server.Tests/Marvel.Server.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~PublicDoctorStrangeHostedReturnTests --logger 'console;verbosity=normal'` passed 1/1 (26.15 seconds).
- Evidence log: `C:/Users/mggar/Source/strange-hosted-return.log`.
- Early probe/fixture failures are not passing evidence. They included a missing
  Card property, root optional-window handling, a mistaken card name, and an
  overbroad Astral Projection target selection. The final target is a single
  engine-offered legal scheme. No production failure was established.

The existing restart fixture is reused unchanged. No commits from the remaining
restart packet (`4a59592d`, reviewed by `47b304e9`) are duplicated. This is one
bounded current-content checkpoint proof, not complete pack acceptance,
historical-save compatibility, a whole-game outcome or native acceptance.

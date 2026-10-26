# Thor setup and admission candidate evidence

MARVEL-563 / MARVEL-702, 2026-10-26. Implementation revision
`c823a4b13dc8ad294893cc3e9713e79346504248`, isolated branch
`feat/thor-complete-setup-candidate`, based on `33da5af3`. This is a reviewable
setup packet, not accepted pack delivery. It supplements
[the delivery checkpoint](thor-delivery-checkpoint.md); the lead owns final
integration, independent review and product acceptance.

## Composition and public boundary

The printed starter and quantity authorities are the four pinned component scans
in [mc06 provenance](../../datasets/rules-packs/mc06/UPSTREAM.md) and the
[starter source review](research/remaining/thor-starter-source.md). All four
scans were visually checked for this packet. The starter has 15 signature,
17 Aggression and eight Basic cards. The separate identity has both 06001 faces;
06026 is the obligation. Numbered nemesis components establish
06027×1, 06028×1, 06029×2 and 06030×1, resolving the divider's swapped quantities.
The four extra-pool cards 06031–06034 are absent from the starter.

Admission requires the exact 35-face pack inventory, every authored face and the
published recipe. Each missing authored/catalog face and changed pack membership
is exercised independently. Wrong identity, signature/customization quantities,
obligation, nemesis, association and aspect rule close every Thor face. Discovery
contains ten heroes; the unsupported-product controls use Black Widow.

Production setup covers Rhino and Absorbing Man in Standard and Expert at one
through four seats. All four extra-pool cards enter legal 40-card custom decks
through public setup at one seat. Custom multiplayer setup is not separately
exercised by this packet.

The characteristic journey selects a natural starter hand containing Defender
of the Nine Realms from seeds 1–20, explicitly keeps it, changes to Thor, and
plays Defender through the public host. Its discard-and-engage cost produces an
engaged minion and Thor's pending draw response. A serialized session copy is
loaded by a fresh factory and host. The identical prompt, two-card draw, card IDs
and continued save match the uninterrupted control. This is an engagement-cost
journey; it does not claim a new typed-resource-payment or full-game journey.

Every new setup authority is adjudicated outside the Core transcript corpus;
the offline generated catalog and expansion enumeration include Thor.

## Measured checks

All commands ran in the isolated Windows worktree with Release configuration.
Logs are retained outside the repository in `C:/Users/mggar/Source/`.

| Check at implementation revision | Result | Log |
|---|---|---|
| Content project, filter `FullyQualifiedName~Setup` | 1,042 passed | `thor-content-setup-final.log` |
| Server project, filters PublicThor, PublicMsMarvel, RuntimeProductAdmissionTests and EngineHostAuthoredSetupChoicesTests | 40 passed | `thor-server-setup-final.log` |
| Rules project, ExpansionSetupAuthorityTests | 2 passed | `thor-authority-tests.log` |
| `dotnet run --project tools/Marvel.Behavior.Index -c Release -- check` | Offline catalog and generated Core specification current | `thor-behavior-check.log` |
| Normal implementation commit hooks | Architecture 66 passed; complexity and file length passed | `thor-setup-commit.log` |

Mutation verification starts from `c823a4b1` and changes only ThorRecipe's
PlayerDeck quantity predicate to `true`. The admitted variant replaces one
06014 with 06013, retaining a legal 40-card deck. With filter
`FullyQualifiedName~ThorAdmissionTests|FullyQualifiedName~ThorSetupTests`, the
mutant compiles and reports 11 passed, one failed: the `player_deck` case finds
Thor in public discovery. Log: `thor-admission-mutant.log`.

The exact original source was restored. A forced `dotnet build
 tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release
 --no-incremental` succeeds with zero warnings/errors, then the same 12-case
filter passes with `--no-build`. Logs: `thor-admission-restored-build.log` and
`thor-admission-restored-tests.log`. Mutation, restoration and these checks ran
sequentially after the broader setup and server runs finished.

Committed UTF-8 source SHA-256 values at `c823a4b1`:

| Source | SHA-256 |
|---|---|
| `datasets/setup/setup.json` | `2a72ee06b659ddddaea62bcb01be7f05946d3a05b20f63e236384da5f0a03791` |
| `src/Marvel.Content/Setup/ThorRecipe.cs` | `1000d51f4d3890806b962070aefdfbf00d8bdd91220a3eea710d31e5e6e9b7d9a` |
| `src/Marvel.Content/Setup/StandaloneProductAdmission.cs` | `3e4e5cad5e8929a4afc93285cc65c8605a39b231103c9d39f710e8c3dd4a5cf99` |

## Integration limits and next action

This worktree does not contain the separately reviewed Hammer Throw preflight
chain. The lead reports that prerequisite and the actual Loki review integrated
on `4e349723`; their evidence remains separate from these setup results.
Neither rejected overkill revision 8265a038 nor 90174f85 is included here.
Current RR 1.8 actual-defeat behavior remains the authority.

Next: independently review this packet, integrate onto the lead's complete Thor
candidate, and run the final combined gates, whole public games and relevant
native journeys. No PR was opened or published from this worktree. The setup
checks and pending-response restart do not establish whole-game, native,
platform or final product acceptance.

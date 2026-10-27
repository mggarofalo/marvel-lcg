# Independent Kree Fanatic admission review

Reviewed source: `d1871dd20dbec6930c566f6231a452885e0fa532`.
Canonical delivery: MARVEL-567. Review worktree:
`C:/Users/mggar/Source/marvel-ronan-public-admission-review`.

Verdict: **accept the public admission change; no blocking findings in this scope**.
This verdict covers the modular set inventory, admission and public setup selection.
It does not independently reaccept the five ability implementations or establish
complete-game, restart, native UI or cross-platform delivery evidence.

## Authority and source review

The local original FFG PDF matches the pinned SHA-256
`09e3fba63e6483c64ffec1cfb09f1461fa81dfa4bfbba7fcaeb6d46bfab9a3a4`.
Independent visual inspection of its seven-page contact sheet confirms the physical
multiset: 90001 ×1, 90002 ×1, 90003 ×1, 90004 ×2, 90005 ×2.
The setup recipe and admission constant match that inventory. The exact catalog
pack inventory and all five authored faces are required together. Multiset
comparison permits recipe reordering while enforcing each physical quantity.

`RuntimeProductAdmission.Discover` removes the named recipe when the product is
incomplete, including an empty or foreign recipe that pack-based filtering alone
cannot identify. Its catalog-presence guard handles absent authored catalog faces.
Retained recipes still pass normal setup validation. The new product has no
standalone Ronan scenario, campaign, identity or player deck entry. Public setup
flows through the existing dealer and seeded world setup; the candidate adds no
direct placement, research-only factory or setup continuation shortcut.

## Independent behavioral evidence

New admission tests distinguish these plausible wrong implementations:

- Omitting the whole catalog pack must remove the modular recipe and all five
  playable faces without throwing or closing predecessor products.
- An unexpected sixth Ronan catalog face must close the whole product, even when
  the five known authored faces and the physical recipe remain present.
- Losing one physical copy of any of the five faces must close admission,
  including losing just one of either duplicated treachery. Every case first
  establishes a valid complete-set control.

New transport tests exercise both embedded and socket requests against the actual
dataset factory. An observer delegates unchanged to production `Create` and only
records the returned game. The requested module deals exactly seven Kree Fanatic
cards and replaces Bomb Scare; a subsequent default opening contains Bomb Scare
and no Kree Fanatic cards. No test edits the produced game.

Focused runs at the reviewed source plus these review tests:

| Command | Result | External log |
|---|---|---|
| `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter FullyQualifiedName~KreeFanatic --logger console;verbosity=normal` | 26/26, including seven independent cases | `C:/Users/mggar/Source/evidence/ronan-independent-admission-content.log` |
| `dotnet test tests/Marvel.Server.Tests/Marvel.Server.Tests.csproj -c Release --filter FullyQualifiedName~KreeFanatic --logger console;verbosity=normal` | 6/6, including two independent transport cases and the existing 80-setup matrix | `C:/Users/mggar/Source/evidence/ronan-independent-admission-server.log` |

The existing focused admission tests additionally passed every individual missing
catalog/authored face, malformed recipe, and legal recipe reordering probe. The
existing server tests passed normal discovery/opening on both transports, seeded
composition, default preservation, unsupported module/scenario rejection and all
supported Standard/Expert modular slots at one through four players. Their setup
helper uses offered legal answers and still requires the final Mulligan phase;
it does not skip setup effects.

The candidate's authority and mutant logs are recorded separately in
[its source evidence](ronan-public-admission.md). They were not rerun as independent
mutation evidence here. Normal commit hooks remain enabled; the review commit
hook output is retained at
`C:/Users/mggar/Source/evidence/ronan-independent-admission-commit.log`.

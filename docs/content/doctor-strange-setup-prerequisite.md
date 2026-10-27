# Doctor Strange typed setup prerequisite (MARVEL-836)

This packet adds composition and binding only. Doctor Strange remains excluded by
`RuntimeProductAdmission`; no canonical setup row, ability book, public discovery,
runner or agenda admission changes are included. Test-local catalogs use the
existing parser/dealer/blueprint/world seams, never a factory admission bypass.

Authority: the verified FFG divider in `datasets/rules-packs/mc08/UPSTREAM.md`
supplies the 15 signature + 17 Protection + 8 Basic starter, separate identity,
09027 obligation, 09028/09029/09030 x2/09031 nemesis set, and 09032–09036 x1.
`pack:mc08:the-invocation-deck` says to shuffle those five together, place the
separate deck facedown by the identity, and expose its top during the game.
Existing `InvocationDeck` and `GamePlayerSetup` own that lifecycle.

`HeroSetup.InvocationDeck` is an optional typed `InvocationDeckRecipe`, parsed
from `invocation_deck`. Its immutable cards require the exact five single faces.
A dedicated creation source and blueprint slot keep them outside ordinary deck
size, signature and customization calculations. Deck assessment continues to
reject an Invocation inserted as an ordinary custom card. Admission validates
referenced Invocation faces along with the rest of a retained recipe.

World setup validates the complete typed composition before allocating a world
or emitting setup events. Every Strange seat requires exactly five owned cards;
other identities cannot acquire that deck. Missing, duplicate, misowned and
misrouted cards reject. Physical cards begin in a player-owned set-aside area,
then the existing initializer binds their separate deck and discard pile.

Engine allocation choice: each seat's five Invocation cards follow its ordinary
player cards, in recipe order, before the next seat or scenario block. Linked
card insertion continues to occur in `Blueprints` before binding by final index.
Engine RNG choice: Invocation shuffle runs in seat order after all ordinary
player and encounter shuffles, before scenario abilities and opening hands. The
single world MT19937 stream is reused. Recipes without Invocations allocate no
extra cards or areas and consume no extra random words. New enum members append
to existing vocabularies; RNG, digest v3 and replay formats are unchanged.

Evidence is recorded against the committed packet below. This is internal setup
acceptance, not whole-pack gameplay, public admission, replay or native acceptance.

## Verification

Base revision: `de695836`. Final production sources passed a nonincremental
Release build of `Marvel.Content.Tests`, then all 16 `DoctorStrangeSetupTests`
cases (including the real AbilityRunner path), and 54 Rules setup, Invocation,
Doctor Strange authority and citation tests. Logs live outside the worktree:
`C:/Users/mggar/Source/strange-setup-restored-build.log`,
`strange-setup-restored-focused.log`, and `strange-setup-rules.log`.

The broader Content `FullyQualifiedName~Setup` filter passed 1,178 tests in
3m02s (`strange-setup-all-content.log`); that run precedes the final immutable
identity-list correction and upgrading the lifecycle probe to the real runner.
The final focused tests and architecture hook verify those corrections. The
first hook run overlapped the broader run and failed on the mutable static list;
the corrected hook run was serialized after all mutants and restored tests.

Six accepted setup controls pin digest fingerprint, RNG consumption and next
word for Core, Rise of Red Skull, Captain America, Ms. Marvel, Thor and Black
Widow. Their expected values were captured with the original `de695836`
WorldSetup source restored temporarily, then checked by the broader setup run.
The complete printed starter and custom deck probes keep Invocations outside
the ordinary 40 cards, and all one-through-four-seat cases put Strange at the
last seat to distinguish first-seat assumptions. Public admission still excludes
the test-local complete recipe even when all catalog faces are offered.

Four compiled mutants were killed: omit the binding (6 failing tests), omit
preallocation validation (missing recipe becomes a plausible game), accept the
wrong five-card composition (1 failing test), and shuffle Invocations before
the encounter deck (2 failing RNG tests). Each source was restored in `finally`;
the subsequent build used `--no-incremental`. Individual logs are
`strange-setup-mutant-*.log`. Final normal pre-commit policy, complexity and
file-length hooks passed; the commit also runs the normal hooks. Independent
review and final integrated regression remain the parent delivery's gates.

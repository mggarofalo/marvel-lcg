# Hank Pym entry ordering (MARVEL-885)

The owner adjudicated that an ally entering with zero remaining hit points is defeated before enforcing an excess ally limit. This is a project decision, not an official ruling on the collision. Rules Reference 1.8 [hit points](../../datasets/rules-reference/entries/hit-points.md#hit-points-3) defeats an ally at zero or fewer remaining hit points; [ally limit](../../datasets/rules-reference/entries/ally-limit.md) requires a player above the limit to discard controlled allies immediately, before entry abilities resolve. Neither clause explicitly orders these simultaneous obligations.

The entry interrupt still resolves before movement and live health assessment. After the normal defeat lifecycle, the limit is checked if the entering incarnation remains in play; a positive-health fourth ally follows the ordinary limit choice before entry responses. Actual Hank Pym tests cover zero payment, declined overpayment, and positive health at the limit; synthetic tests cover entry and defeat lifecycle ordering.

## Evidence

On the candidate based on `0d5703bc`, `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release -m:1` with filters for `AllyZeroHealthEntryTests`, `HankPymActualTests`, and `AllyEntryHealthIndependentTests` passed 25 tests. A compiled mutant that bypassed defeat when the entering controller already exceeded three allies failed both actual Hank zero-health cases by presenting the ally-limit prompt. After exact source restoration, a non-incremental Release build succeeded and the original two-class focused run passed 23 tests. The expanded run includes the surviving-ally limit continuation. Logs are in `C:/Users/mggar/Source/evidence/hank-defeat-first-*.log`.

The normal commit hooks passed, including 66 architecture tests, cyclomatic complexity, and file length. Wider integration and independent review remain separate.

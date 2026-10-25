---
name: marvel-godot-testing
description: Write and run scripted native acceptance tests for this repository's Marvel Champions Godot client. Use for client interaction verification, UI regressions, and agent acceptance testing; engine-only tests use the normal managed lanes.
---

# Godot client testing

Use the repository's GDScript driver for repeatable native interaction checks.
Prefer a saved, reviewable script over manual Computer Use traversal. This is a
repository-local skill; do not install it globally or add another testing framework
to run these checks.

Read [test guidance](../../../tests/AGENTS.md),
[UI guidance](../../../src/Marvel.Godot/AGENTS.md) and the
[scripted acceptance API](../../../docs/testing.md#scripted-godot-acceptance).
The API documentation owns operation signatures and evidence behavior.

Select checks by the behavior they prove. Follow the
[evidence selection protocol](../../../docs/testing.md#selecting-additional-native-evidence)
before adding separate package, OS or manual journeys; reuse existing gates and
name a concrete uncovered failure. Agents accept ordinary content and engine
expansion using authority review and appropriate tests. Owner acceptance applies
to materially new UI interactions or a concrete concern needing human judgment;
it explores comprehension and requirements, and does not assign regression
testing or routine pack approval to the owner.

## Select and run evidence

From the repository root, inspect existing cases before writing another journey:

```powershell
pwsh tools/native-cases.ps1 -Profile representative -List
pwsh tools/godot-acceptance.ps1
pwsh tools/godot-acceptance.ps1 -Script res://smoke/automation/opening_hand_acceptance.gd
```

The standalone runner discovers Godot and builds by default. Use `-NoBuild` only
when the current Godot sources have been built in Debug, which the editor
executable loads. Managed test lanes still use Release. Existing manifest cases use
`tools/native-cases.ps1 -GodotBin <installed-dotnet-godot> -Case <case-id>`;
the manifest owns their scale, motion, player count and scene inputs.

Use headless checks for input, composition and observable results. Add `-Rendered`
for screenshot evidence and rendering behavior; a headless success says nothing
about pixels. On Linux rendered checks need a display such as Xvfb. Use a fresh
external results directory per run. On Windows set `tty: true` for shell tool calls
and use `write_stdin` to follow running commands.

## Write the player's task

Start with [the opening-hand example](../../../src/Marvel.Godot/smoke/automation/opening_hand_acceptance.gd)
and the shared `ui_driver.gd` beside it. Reuse legal seeded setup or an explicitly
labelled prepared scene. Synthetic controls test the driver, not a played game.

- Locate visible controls by text/name or a scoped semantic helper. Resolve duplicate
  labels by scope or physical identity, never by choosing the first match.
- Deliver actual viewport input. Do not emit button signals, call action handlers,
  change gameplay state, or force focus to claim coverage of a user's gesture.
  Keep fixture setup distinct from the interaction under test.
- Await visible conditions and accepted revisions rather than sleeping a guessed
  duration. Check each operation's result and stop on failure. Print the completion
  marker only after the assertions and cleanup succeed.
- Assert the explanation, effect and commitment count as appropriate. Include
  cancellation, stale input or ambiguity when the changed behavior depends on it.
  A successful input dispatch alone is not an assertion of the user's outcome.
- Preserve existing distinguishing assertions during migration. When changing the
  driver, run its contract suite and affected journeys, and challenge new decisions
  with a focused negative control or mutant.

A driver gap is a reason to extend the shared driver and its behavioral checks.
Use Computer Use only for explicitly requested exploratory/comprehension review
or a concrete native/OS behavior the script cannot exercise; state that gap and
do not count manual clicks as a repeatable regression test.

Report the revision, commands, profiles, artifacts, failures and untested scope.
Separate automated evidence from the owner's product acceptance. Reproduce a
suspected pre-existing failure against the base before attributing it, preserve
the assertion, and track the finding without claiming a fully green matrix.

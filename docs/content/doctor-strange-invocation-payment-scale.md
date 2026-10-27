# Invocation payment viewport sizing

Production/test base: `c1b5656c9432f18ea4af1f9082731c617426dd51`.
Branch: `fix/invocation-payment-scale`. This is prepared nonpublic component
evidence, not public Doctor Strange admission or owner acceptance.

At 80%, the complete editor allowed its payment viewport to shrink to 36 pixels
while the Energy button's themed resource icons required a 48-pixel button. No
scroll position could expose the complete button. The existing native
full-visibility assertion rejected the click before any resolve. The lead's
unchanged-base reproduction is retained under
`C:/Users/mggar/Source/marvel-evidence/invocation-native/copy-review/base-rendered/80/spell/49024-8852594/`.

`DecisionPanelPromptRenderer` now measures the attached body's button minima and
reserves enough viewport height for the tallest button, retaining any larger
workspace minimum. This is Godot layout policy; costs, draft composition,
copy, rules, datasets and native assertions are unchanged. The existing native
case distinguishes the defect, so no duplicate synthetic regression was added.

## Verification

All artifacts are under
`C:/Users/mggar/Source/marvel-evidence/invocation-native/payment-scale/`.
Both final builds used `--no-incremental`: Debug client (`debug-final.log`) and
Release native test host (`release-final.log`), both zero warnings/errors.

`rendered/matrix.trx` passes all six `InvocationNativeJourneyTests` cases:
Spell Mastery and Master of the Mystic Arts at 80%, 100% and 150%, Windows
Godot 4.7.1 Mono, 1920x1080, two seats, reduced motion. Each per-case `run.log`
contains `UI_ACCEPTANCE_OK` and `INVOCATION_RESOLVE_COUNT 2`. Exact Energy 41,
recipient 0, reversible staging, paid continuation context and final Invocation
identity assertions remain intact. All six recipient captures were inspected;
80% payment captures were also inspected. The 150% Spell recipient surface has
lower frame padding beyond the viewport, pixel-identical to the base capture;
its complete commitment button and meaningful content remain visible. This run
does not claim a repair of that separate gallery-frame geometry.

The native command sets `GODOT_BIN` via `tools/godot-local.ps1`,
`MARVEL_NATIVE_INVOCATION=true`, `MARVEL_NATIVE_RENDERED=true`, and the external
`MARVEL_UI_EVIDENCE_DIRECTORY`, then runs the whole class with Release
`dotnet test --no-build`.

`focused/focused.trx` passes 57 layout, visual-system and presentation-boundary
tests using the filter
`FullyQualifiedName~LayoutTests|FullyQualifiedName~VisualSystemTests|FullyQualifiedName~DecisionControlStyleTests|FullyQualifiedName~PresentationBoundaryTests`.

An additional broad managed assembly run reported three
`EncounterReplacementMeaningTests` failures at line 40 (expected prompt was
null), then continued without a final result. The lead owns the missing exact
reveal-origin fixture correction separately. This redundant run was interrupted
through its owning PTY; its surviving exact-worktree child was stopped after
command-line validation. `managed.log` and `managed-interrupted.txt` preserve
the incomplete result. It is not reported as a completed or passing assembly.
No unrelated process was terminated. Every launched PTY was harvested.

Only the source renderer and this evidence record are owned by this fix.
Independent review and integration gates follow separately.

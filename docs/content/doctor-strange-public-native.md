# Doctor Strange public native proof and refreshed prepared media

MARVEL-839 evidence supplement, based on `0d55160ca3f778dccc6c7e1ddada7881d0eae3d3`
with the native scripts and read-only witness in this commit. This packet changes
no engine, ability book, setup data or View behavior. It does not constitute owner
acceptance or close the existing UI review item.

The uncovered boundary was public setup and the Invocation pile inspector. The
existing prepared two-seat Spell Mastery/Master journeys start after fixture
construction and cannot prove that boundary. The new
`invocation_public_acceptance.gd` uses the production embedded host and normal
setup UI, a printed starter, Doctor Strange/Rhino/Standard with recommended Bomb
Scare, and seed 2. Visible first-seat hero tiles, the advanced seed field, Keep
hand, Other piles, its typed Invocation menu, and action commitments receive real
viewport or popup keyboard input. No gameplay mutation, emitted control signal,
action-handler shortcut or forced focus prepares the public journey.

The public proof asserts:

- Keeping the hand commits revision 1, followed by a player-action decision.
- Typed Invocation ownership and both area owners are seat 0. Seven Rings is the
  only readable top; four concealed entries have neither face, stable identity
  nor location. The inspector exposes exactly `1 / 1`, with disabled navigation
  beyond that authorized face.
- Inspection and staging leave revision 1 unchanged. The named Stephen Strange
  action has an explicit **Use Natural Talent** commitment.
- One Natural Talent commitment advances to revision 2. The exact previously
  visible physical Seven Rings is in Invocation discard, Crimson Bands is public,
  and three deck entries remain anonymous. The hand remains six and the identity
  remains ready. The new top inspector and public history show the result.

This native trace reveals Crimson Bands after Natural Talent. It does not reuse
the prior fresh-host packet's Winds continuation or establish identical setup
inputs to that packet. It proves one characteristic public action; paid Special
resolution and Master remain explicitly prepared evidence below. It does not
claim public Spell Mastery, a complete game, exported-package verification,
multiplayer public setup or Linux execution.

Verification uses Godot `4.7.1.stable.mono.official.a13da4feb`, Windows,
1920×1080, 100% scale and reduced motion. Both the Debug client and Release test
host with copied dependencies were rebuilt before the final public run. The
standalone runner isolates authoritative saves and bounds/harvests its process.
The journey does not write deck libraries or campaign stores.

```powershell
dotnet build src/Marvel.Godot/Marvel.Godot.csproj -c Debug
dotnet build tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj -c Release
pwsh tools/godot-acceptance.ps1 -NoBuild -Rendered `
  -Script res://smoke/automation/invocation_public_acceptance.gd `
  -ResultsDirectory C:/Users/mggar/Source/evidence/strange-public-native/public-final-refactor
```

The final public run passed in 17.683 seconds with
`UI_ACCEPTANCE_OK invocation_public seed=2 resolves=2`; `result.json` reports exit
0, no timeout and success. Earlier locator, presentation-settlement, expected
next-top and witness-zone failures remain in their separate run directories and
are not passing evidence. The successful run emits an existing Godot warning
when pile dismissal attempts to restore focus to its MenuButton opener, which is
not focusable without accessibility enabled. Escape dismissal and subsequent
pointer interaction pass. Independent review below establishes a keyboard entry
gap; this packet does not claim opener focus restoration or keyboard-only pile
entry, or change that production behavior.

Prepared evidence stays separate. `InvocationNativeJourneyTests` still creates
its explicitly prepared two-seat board, pays Energy, commits the Special once,
stages/unstages Strange and commits only Spider-Man as recipient. Its existing
server assertions retain exactly two resolves, paid physical card, tough
recipient, exhaustion and exact Invocation discard/return checks. The refreshed
script opens history details and asserts the intermediate
`Reveal · Winds of Watoomb`, including when Master has returned Seven Rings.
Native wheel input scrolls that exact history row fully into view before the
additional `history-reveal.png` capture.

| External evidence folder | Measured result |
|---|---|
| `prepared-final/` | Six rendered Spell/Master cases at 80%, 100%, 150% passed in 57 seconds, with the exact reveal text assertion and expanded details. This precedes the added scroll-to-reveal capture. |
| `prepared-final-refactor/` | Both 100% cases passed in 26 seconds with native scrolling and a fully visible reveal row. No new final scroll capture is claimed at 80% or 150%. |
| `public-final-refactor/` | Final public journey passed, after the stricter physical/discard assertions and both candidate builds. |

All artifacts are under `C:/Users/mggar/Source/evidence/strange-public-native/`.
The TRX files and each prepared case's `run.log` retain authoritative resolve
counts; standalone result JSON and run logs retain the public result. Candidate
build logs are `build-debug-final-refactor.log` and `build-release-final-refactor.log`.

Inspected media includes public initial/next-top inspector captures (complete
Seven Rings/Crimson Bands faces, one authorized entry), public action commitment
(named identity and explicit button), and public history (original reveal,
Natural Talent and new public reveal). Inspected prepared media includes the
150% Spell result, 80%/100% Master expanded history, and both 100% reveal-focused
captures. The initial expanded-history captures show a clipped lower detail
viewport; use the reveal-focused captures to review the intermediate face.

Selected review images:

- `public-final-refactor/artifacts/public-invocation-top-inspector.png`
- `public-final-refactor/artifacts/public-natural-commitment.png`
- `public-final-refactor/artifacts/public-next-top-inspector.png`
- `public-final-refactor/artifacts/public-history.png`
- `prepared-final-refactor/100/master/history-reveal.png`
- `prepared-final-refactor/100/spell/history-reveal.png`
- `prepared-final/150/spell/result.png`

The pile inspector is a separate native popup viewport, so its own PNG is saved
alongside the unchanged table capture. A table-only PNG does not show that popup.
Media is external to the repository; this packet does not upload artifacts or
create another Plane owner-approval item. Root integration can attach this
evidence to the existing MARVEL-839 review.

Independent review of `55c94501a5da216c87f5b209e73786b38915b826` used
`review/strange-public-native`, based on `ed03a479` with the packet cherry-picked
as `ce971c3c`. Debug client and Release test host builds passed with no warnings
or errors. The rendered public rerun passed in 15.955 seconds, with exactly two
accepted authoritative resolve events (revisions 1 and 2). Both prepared 100%
cases passed in 19 seconds, each retaining `INVOCATION_RESOLVE_COUNT 2` and the
existing physical payment, recipient, exhaustion and Invocation assertions.
Evidence is under the external `independent-review/` folder beside this packet's
other evidence. Both original final-revision popup faces and both final prepared
reveal-focused captures were inspected, as were the equivalent four rerun images.
The full faces and the intermediate Winds reveal row are readable and unclipped.

The bounded `invocation_pile_keyboard_review.gd` probe uses the same public setup,
real pointer/menu input and Escape dismissal, then 80 actual Tab presses. It
fails the required keyboard reachability assertion: `InspectOtherRegions` has
runtime `focus_mode=3` (accessibility-only), and none of 25 distinct visited
controls is the opener. Focus after dismissal is the identity's `Card1Action`,
not the opener. The probe remains at revision 1 and never invokes Natural Talent.
Its `keyboard/result.json` records exit 1, no process timeout, and 11.422 seconds;
the failure trace and screenshot remain in `keyboard/artifacts/`.

This is a pre-existing user-visible keyboard entry and restoration gap, not merely
an inconsequential warning. `SpatialTablePileRenderer.RenderOverflow` leaves the
MenuButton's accessibility-only default; the region-menu path explicitly sets
`FocusMode.All`. `TabletopPileInspector` calls `CardFocusPreview.Restore` on that
overflow opener, which then attempts `GrabFocus`. These production files are
unchanged from `ed03a479`. The gameplay/media proof is accepted within its stated
scope; UI readiness for MARVEL-839 is rejected until a narrow keyboard/focus fix
passes the retained probe and verifies keyboard opening and dismissal. No
production fix or Plane upload is part of this independent review.

After that correction, owner review should focus on whether an experienced
player can discover the Invocation pile, understand that only its current top is
public, and understand the paid Special's recipient choice, committed costs and
Master's return of the exact Invocation. Routine keyboard regression discovery
remains agent-owned.

Independent closure reviewed source fix
`bd94c0ee6cd6ced218232fc6a6572a1ccc73e13a` in the same isolated review tree.
The only runtime change is `FocusMode.All` on the overflow `Other piles`
MenuButton. The add/add probe conflict was resolved to that source commit's exact
full script, whose assertions were inspected before execution. The retained
probe now checks actual Tab reachability, Enter opening, arrow/Enter Invocation
selection, Escape dismissal and restoration to the same opener, plus unchanged
draft, public pile and revision 1.

Fresh Debug client and Release test-host builds passed with no warnings or
errors; the Release server dependency was copied into the Debug runtime before
the rendered rerun. At Windows 1920×1080, 100% scale and reduced motion, the
closure run passed in 11.194 seconds, reached the opener after 25 actual Tab
presses with focus mode 2, and restored it after keyboard inspection. Its
authoritative log contains exactly one accepted resolve (Keep hand), zero
warnings and zero errors. The complete Seven Rings inspector face and the amber
opener focus ring were inspected in the rerun captures. A passive Mystical
Studies preview remains visible after traversal and does not obscure the opener.

Closure evidence is external at
`C:/Users/mggar/Source/evidence/strange-public-native/independent-closure/`:
`build-debug.log`, `build-release.log`, and `keyboard/` retain the builds,
result JSON, native log and captures. The worker's compiled negative control at
`C:/Users/mggar/Source/evidence/strange-pile-keyboard/mutant/` was also reviewed:
removing the focus setting makes the same probe fail opener restoration. The
worker's existing ten managed contracts and shared native driver checks cover
the adjacent behavior; this one-line focus change introduces no uncovered risk
requiring another managed sweep. All launched review processes were harvested.

The keyboard entry/restoration finding is closed. The previous UI-readiness
rejection above is superseded: this increment is accepted for the focused owner
review described above, while owner product acceptance remains outstanding.

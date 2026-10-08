# Native B1 live state

Printed faces and current state have separate owners. `CardPrintedIcons` keeps
printed scheme icons with their printed face. `CardStatusEntries` selects only
supplied live fields, status cards, counters, orientation, and retaliation.
`CardLiveStateRendering` renders those observations outside the printed rules.
There is no rules-text parsing, modifier summation, or card-name dispatch.

The table reserves an upright 132-pixel state/action region beside each physical
card. A quarter-turn exhausts the face while the local identity, health, and
state remain upright. State is not scrollable; the existing action region sits
below it and retains its 44-pixel control hit area. Printed resources, selection
corners, action/target controls, status words, and gold modification underlines
remain distinct channels. The encounter row reserves the same complete host
footprint before the support region. Dense reveal layouts retain the existing
support drawer rather than overlapping the active identity.

Health explicitly says remaining / maximum. Damage has a separate label. A
modified maximum is identified by the supplied `HP.IsModified` flag, with a fine
gold mark under only the maximum in the printed footer and a named state cue.
Taking damage alone does not create that mark. Threat and its supplied threshold
have their own caption; resolved quantities do not receive another per-player
multiplier. Printed per-player facts remain available during value inspection.

Inspection places current state beside the full face. Its value selector uses
the supplied current quantity and canonical printed spelling, preserving X and
dash. Source pages show only disclosed rows, one at a time, without a cumulative
equation. `DefineBase` reads “Base N”, distinct from an addition. Opening a source
uses its authorized title and rules through the existing card navigator.
Historical sources retain text but acquire no live target link. Duplicate titles
remain separate source rows. Choice galleries retain live state below their face.

Current keyword badges use supplied live fields. They do not recover an absent
current field from printed stats. View preserves explicitly supplied zero-valued fields on in-play cards;
an absent field remains absent.
Printed keyword rules and printed scheme icons remain printed facts, not a
renderer claim that their current value is nonzero.

## Evidence and limits

The refactor extracting state selection passed 33 focused tests before behavior
changed. `tools/godot-smoke-live-state.ps1` adds a normal native gate at 1920×1080
and 50%, 80%, 100%, and 150%. It checks complete rules, bounded labels, upright
state, host bounds, room for the production action hit area, modification marks,
and pointer/keyboard access to current and historical sources. Waiting encounter
counts 0, 1, and 4 are checked independently for both seats.

Canonical specimens are projected through restricted View policy from vendored
Core content. Advanced Ultron Drone 01143 has **4 HP** in the generated card
dataset; the public facedown Drone profile has **1 HP** and exposes no underlying
player-card identity. Explicitly synthetic snapshots add 5/7 modified health,
damage, all three status cards, counters, exhaustion, duplicate source titles,
a historical source, a near-threshold scheme, and a fresh clean copy. These are
renderer stress cases, not claims of a played game or enabled expansion content.
The long minions Weapons Runner, Radioactive Man, Whirlwind, Tiger Shark, and
Melter also carry a synthetic Stunned card to reject rules clipping.

Native checks found two measurement defects during implementation: glyph bounds
underestimated Label line height at 50%, and narrow state stacks could consume
the action region. Title measurement now uses the actual native Label; caption
and footer bounds account for font height. Two-column state labels retain room
for the production action control. The full 436-face gallery remains a separate
regression gate. Five behavioral mutants are rejected: concealed-state exposure,
lost repeated-status count, historical-source relinking, reversed maximum
modification flags, and stale removed-copy selection. Pinned inspection rebuilds
from each authoritative snapshot; removed or concealed copies close it.

This is automated geometry, input, and regression evidence. Visible desktop and
independent uncoached comprehension review remain pending because Computer Use
returned “Computer Use was not approved to use Godot.” No alternate GUI route is
used. Packaged Windows rendering remains pending. See [validation.md](validation.md).

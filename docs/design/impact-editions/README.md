# Impact Editions / B1

Approved on 7 October 2026 in MARVEL-460. The [self-contained gallery](approved-gallery.html)
and [design notes](approved-design-system.txt) are the frozen design reference.
[Vector sources and fixture facts](reference/) retain editable outlines rather than screenshot pixels.
These are synthetic design fixtures. Expansion examples in the reference do not
expand the executable Core product boundary.

## Native foundation (MARVEL-461)

`CardVisualTokens` owns card ink, paper, secondary text, aspect, modified-value
and resource colors, plus face typography and frame/resource spacing. The
existing `VisualSystem` continues to own shell spacing and interaction strokes:
focus is a bounded ring, selection has a heavier stroke and a non-color mark.
`CardFaceStyle`, card controls, inline markup and resource textures consume the
same tokens. A resource keeps its own red/yellow/blue/green identity regardless
of the card aspect. Canonical shapes and accessible names distinguish colors.
A one-pixel ink outline keeps the yellow resource edge distinct on paper,
while retaining its approved yellow fill. The same treatment applies to card
labels, inline rules and SVG-derived textures. Small functional glyphs use paper on ink or ink on paper, not aspect on aspect.

`CardTypography` loads embedded Barlow Condensed Bold for titles and numeric
values; Barlow Regular, Bold and Italic for rules, emphasis and body copy.
The shell inherits Barlow Regular and uses the same condensed title family.
The packaged faces supply Regular 400, Bold 700 and Italic 400; condensed titles
use Bold 700. System font fallback is disabled. Godot’s bundled fallback font
handles characters outside Barlow’s coverage; canonical game symbols always use
the dedicated icon font, with system fallback disabled. No local Avenir lookup
is involved. A missing packaged font fails
instead of silently substituting an unreviewed face. Barlow's available weights,
source pin and SIL OFL are recorded in
[font provenance](../../../src/Marvel.Godot/assets/fonts/UPSTREAM.md).
Godot's text server supplies shaping, ascent/descent and wrapping from these
embedded bytes. Labels use container alignment and measured font metrics;
no hand-estimated character counts choose wrapping.

The approved prototype used Avenir. Barlow is the redistributable native
substitution, not a claim of identical metrics. Native measurements and wrapping
are exercised by the replayable specimen below. Face geometry, number-adjacent
special stars, centered consequences, modified underlines and attachment layouts
are independently owned follow-on work; this foundation does not certify them.

Canonical Champions Icons meanings are distinct: `D` is consequential damage
(the dataset token is `[cost]`), `S` a printed special-rule star, and `U` unique.
The approved `stat-resource-study.svg` and extracted canonical symbol sources
pin these shapes. They are not interchangeable Unicode stars. Resource SVGs
remain the pinned font outlines; the renderer applies their semantic color.
The existing font's redistribution status remains unresolved; this change does
not assert a license grant for Champions Icons.

Original cut-paper silhouette vectors live under `src/Marvel.Godot/assets/art`.
They are extracted from the approved source drawings, have no raster or external
image dependency, and contain no gameplay rules. `BuiltInCardArt` implements
`ICardArtProvider`; a supplied local provider takes precedence, with an original
shield silhouette for unrepresented visible faces. Concealed cards do not ask
for art. Runtime selection is by authorized face asset identity, without a
card-name switch or network access.

## Replayable native specimen

Build the Godot project, then run the normal executable with an explicit user
argument (also supported by exported builds):

```sh
/path/to/Godot --path src/Marvel.Godot -- --marvel-b1-sample
```

The specimen is explicitly synthetic. It shows all five frame families,
resources on light and dark fields, canonical `D/S/U`, original silhouette art,
and packaged typography. The 50/80/100/150 percent buttons exercise pointer and
keyboard activation. It fails if the actual text or symbol font lacks required
glyphs or positive baseline metrics. `B1_FONT_METRICS` records wrapped long-title
measurements and `B1_PRIMITIVES_OK` marks these bounded native checks, not game
comprehension or complete B1 acceptance.

Use a 1920×1080 viewport and retain the exact source commit, OS, scale,
measurements and capture path in validation evidence. Windows packaged rendering
and independent engine-backed product review must be recorded separately from
local macOS specimen evidence.

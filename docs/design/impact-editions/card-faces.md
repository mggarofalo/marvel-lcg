# Native B1 card faces

The card renderer uses the approved Impact Editions composition: ink and paper,
an angled aspect field, a protected horizontal stat rail, condensed left-aligned
identity, top-left cost or stage, right-aligned traits, and independently colored
canonical resources. Board cards use the reference 172 × 240 footprint;
inspection cards use 400 × 560. Scheme footprints rotate those dimensions.

The existing table scale policy keeps physical table cards between 80% and 100%.
Inspection faces follow the full 50–150% interface scale. The renderer does not
shrink a particular card's body font to make its rules fit. It measures the actual
native RichTextLabel after it enters the scene tree, then allocates paper before
illustration. Dense text can reduce illustration space. Footer and input bounds
remain independent of that allocation.

Packaged Barlow supplies titles, body, and values. Bold italic is a fixed native
variation of the packaged italic face. Canonical special stars, uniqueness marks,
and consequences use the existing glyph outlines with explicit native bounds.
Current stat numerals and gold underlines use the supplied effective value and
modification flag. Printed X remains the printed inspection fact; resolving X
does not itself imply a modifier.

Stars follow the measured number width; the entire consequence group centers on
that number. Bare-star values occupy the value slot once. Boost count and boost
special star are independent, including zero-count boost stars.

The source-art provider remains first choice. Original packaged cut-paper
illustrations supply the fallback, at both compact and inspection sizes. No card
name branches or external art downloads are part of face rendering.

## Automated native evidence

`tools/godot-smoke-card-faces.ps1` is part of the normal Godot smoke gate. A focused
managed fixture projects every Core face and four research-only symbol examples
through the production visibility boundary. Separate synthetic specimens cover
a modified two-digit value with a special star and two consequences, zero cost,
zero thwart, four resources, and an unmodified resolved X. Three cost specimens
separately exercise absent, zero, and X costs.
It executes no expansion mechanics and does not expand playable content.

The native probe renders 218 specimens in both sizes (436 faces) at the 50%,
80%, 100%, and 150% interface profiles. It checks complete unscrolled rules, two-line titles, visible zero cost,
measured star adjacency, bare-star uniqueness, consequence count and centering,
the zero-boost special star, the ink contrast field beneath consequences,
resource/health separation, and effective-value underline presence and clearance.
The focused selection tests also reject legacy zero fields replacing X/dash/star
and reject deriving modification from printed/current string inequality. Temporary
fixture and log files are deleted.

Two native defects were found by these checks: rules height before scene entry
was zero, and SVG controls retained their texture minimum when size preceded
IgnoreSize initialization. The implementation uses the native ready boundary and
sets texture expansion policy before size.

These checks establish native geometry and lifecycle evidence. Visible desktop
and independent player-comprehension review remain pending: Computer Use returned
“Computer Use was not approved to use Godot.” No alternate GUI route was used.
Windows packaged rendering remains pending as recorded in [validation.md](validation.md).

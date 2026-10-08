# Native B1 persistent sources

MARVEL-465 consumes the [typed persistent facts](../../card-persistent-facts.md)
and the approved [source references](reference/attachments-a/). It does not
parse printed prose, infer attachment from storage, or add a contribution to
an already-effective host value.

`CardControl` separates its interaction shell from face content before adding
source content. `CardSourceGroups` selects current readable physical copies
by the authorized relation. Attached sources belong to their actual host;
controlled upgrade cards share a distinct ledger. Heroes and allies with a
Controlled relation remain physical character faces. A shared environment's
attachment is not duplicated on its affected recipients. Off-table hosts and
their sources remain reachable through the existing area drawers.

`CardSourceStrip` is the common paper/ink content for a tab and a ledger.
Tabs use the visible host width, including a quarter-turned host; the ledger
is intentionally 250 native pixels. Barlow Condensed titles, Barlow body text,
canonical colored resources, and neutral functional icons use the packaged
asset boundary. Native shaping wraps titles and semantic units within the
protected inset beside the angled rail. Type has its own row. Numeric
contributions flow as intact icon-and-value units. Action buttons have a
separate 44-pixel row below the meaning.

A source collection displays one complete compact source and a title picker
containing every authorized copy. Duplicate titles retain distinct identities.
Choosing a title replaces the visible source without editing a decision draft;
body inspection and engine-offered actions remain separate. Container layout
establishes the saved resting pose, so pointer exit cannot move the source over
its picker. Pointer and keyboard use the existing shared interaction path.
Returning focus after inspector dismissal is distinct from deliberately
focusing a source to open a preview; dismissal does not immediately reopen it.

On the table, a tab that would cover another physical card or pile collapses
to the existing host-local title picker. Automatic collapse does not claim
focus; a focused tab restores it to its replacement picker. This is a bounded
fallback for the fixed Astra table, whose overall placement remains MARVEL-431.
It is not a certification of every dense board arrangement.

Complex summaries retain the supplied verbs and targets: Charge grants
overkill for the next attack and discards afterward; Armored Rhino Suit
redirects **all** damage here and discards at damage >=5, with current damage
shown separately; Arc Reactor exhausts **this** source, while Concussion
Blasters exhausts **your hero** and requires its supplied energy resources.
Maximum-HP contributions say `max HP`. Unsupported producer meanings retain
rule inspection rather than a guessed modifier.

## Automated evidence

The initial interaction-shell refactor passed 26 focused face/state checks
before source behavior changed. The source work adds typed relationship and
meaning tests and a native input gate at 1920x1080, at 50%, 80%, 100%, and 150%.
The table face follows its existing bounded scale; source text reflows at the
actual native host width instead of scaling a larger raster strip.

`CardSourceFixtureTests` projects canonical Core data into explicitly synthetic
stress layouts: Nick Fury with Inspired (3 ATK/THW), Advanced Ultron Drone with
Genetically Enhanced and Spider-Tracer (5/7 HP, two damage), Rhino with three
distinct sources (6 ATK, two damage on the suit), exhausted Iron Man with
controlled Armor/Reactor (hand size 3, 11/15 HP, four damage), and the environment
source. These assembled layouts are not claims of legally played sequences.
Long and duplicate titles, one/two/three sources, and a pile obstructing the
expanded tab are additional synthetic layout probes.

The native gate checks complete titles and summaries, exact rotated-host and
ledger widths, rail clearance, separate action hit areas, pointer inspection,
keyboard inspection, distinct duplicate picker entries, and focus after source
replacement. It caught the pre-container resting-pose bug and the flow layout
that wrapped a stat chip into a tall narrow column; both were repaired and
retested. Six manual behavioral mutants were killed: concealed-source exposure,
recipient mistaken for host, wrong exhaustion target, reversed damage threshold,
maximum HP mislabeled as HP, and duplicate physical rows.

The complete seeded game journey also checks the legally played Web-Shooter
through the controlled-upgrade ledger, using actual keyboard menu input and
the full B1 inspector. It requires an unchanged revision and restored source
focus. The first full gate was interrupted when this journey stopped advancing;
focus restoration was separated from new preview activation, and the focused
50% journey then passed all 14 decisions. The old probe's storage-derived
attachment expectation was replaced with the typed controlled relationship.

## Product review boundary

The native probes are automated headless evidence. Computer Use did not approve
Godot earlier in this session; that refusal remains binding, and no alternate
GUI launch was attempted. Independent uncoached comprehension review and
packaged Windows rendering remain unverified. Passing these checks does not
establish complete product readiness.

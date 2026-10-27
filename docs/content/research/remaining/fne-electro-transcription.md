# Fear No Evil: Electro transcription candidate

Research for MARVEL-697, 2026-10-27. Repository baseline
`8cf73d300bb53c7961cb2fba6455573941366e34`; follows the
[pinned acquisition](fne-acquisition-candidate.md) and the
[Bullseye transcription](fne-bullseye-transcription.md) /
[independent proofread](fne-bullseye-proofread.md) format. This packet captures
one complete printed family for independent proofreading. It establishes neither
dataset admission nor executable support.

The external candidate is
`C:/Users/mggar/Source/evidence/fne-acquisition/electro-candidate.json`, SHA-256
`989ab97390703aa08c129bef258a47f4beea760c2b0c2830e6d88e940beb00e4`.
The pinned original manifest is `scan-manifest.json` in the same evidence root,
SHA-256 `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`.
Original images remain external in its `scans/` directory; source-storage and
license findings remain those of the acquisition packet.

The candidate uses `groups[].cards[]`, preserving full-card input fields plus
research metadata. Each face records printed title/number, absence of subtitle,
traits, exact rules text with markup and plain text, numeric attributes, special
stars, boost icons, stage, per-player flags, reverse-face status and physical
quantity. Flavor and printed illustrator/copyright credits are separate fields.
Each captured field points through `field_sources` to every corresponding
original's filename, complete SHA-256, printed card number, footer position and
visual region. Editorial ID allocation, key spellings and normalization choices
are identified separately from printed facts.

## Printed inventory and quantities

All **14 original scans were viewed individually at original resolution**,
including each repeated copy. They establish **10 gameplay faces on 14 physical
cards**, covering every printed ELECTRO footer position **1–14/14**.

| Candidate face | Printed title / stage | Printed number | Footer positions | Physical quantity |
|---|---|---|---|---:|
| 60076 | Electro I | 76 | 1/14 | 1 |
| 60077 | Electro II | 77 | 2/14 | 1 |
| 60078 | Electro III | 78 | 3/14 | 1 |
| 60079 | Electric Charge | 79 | 4/14 | 1 |
| 60080 | Drained of Power | 80 | 5/14 | 1 |
| 60081 | Whiplash | 81 | 6/14 | 1 |
| 60082 | Charging Up | 82 | 7/14, 8/14 | 2 |
| 60083 | Chain Lightning | 83 | 9/14, 10/14 | 2 |
| 60084 | Energy Overload | 84 | 11/14, 12/14 | 2 |
| 60085 | Shocking Revelation | 85 | 13/14, 14/14 | 2 |

The physical quantity and face-quantity sum are both 14. Every footer numerator
belongs to one face; this family source supplies no paired gameplay reverse.
`reverse_face: null` makes no claim about generic card-back artwork. Quantities
come from distinct printed footer positions, independently of filenames and
product-wide marketing counts.

## Facts needing particular care in review

Electro's printed stages are Roman I / II / III. SCH is 1 / 1 / 2, each with a
special reminder star; ATK is 1 / 2 / 2 without a star. HP is 15 / 17 / 20 per
player. Stages I and II find and attach Electric Charge, placing 2 per-player
charge counters on it; stage III places 3 per-player charge counters on Electric
Charge. The starred Forced Response places 1 counter after scheming at stages I
and II, and 2 at stage III. Only stage III prints an amplify icon.

Electric Charge prints a star-only ATK modifier, captured as
`stat_annotations.ATK+` with `value: "★"` and `special_star: true`, without a
numeric zero. Its starred attack interrupt has a remove-counter cost, an arrow,
an additional boost card and overkill. Its Hero Action spends an energy resource
or 2 resources of any type to remove a counter. It is Permanent and prints no
numeric boost icons or boost ability.

Drained of Power prints Uses (3 drain counters), preserved as `Uses: "3,drain"`
in the extractor's attribute convention. The Forced Interrupt offers the energy
resource or 2-resource payment when an upgrade would ready, and includes the
italic reminder `(and ready that upgrade)`. It prints 2 boost icons.

Whiplash is unique, with SCH 2, ATK 2, fixed HP 5 and 3 boost icons. Its
Forced Interrupt removes a charge counter as a cost before discarding an
encounter-deck card and dealing damage per boost icon. Its printed quotation is
retained separately as flavor.

Charging Up prints **fixed starting threat 6**, one amplify icon and 3 boost
icons on both copies. Its When Defeated clause places a fixed 3 charge counters;
neither that clause nor its starting threat prints a player symbol. The italic
narrative about cutting off Electro's power supply is flavor.

Both Chain Lightning copies print 1 boost icon. The second choice deals 1
damage, or 3 if Electric Charge has any charge counters, followed by removing 1
charge counter. Energy Overload and Shocking Revelation each print 2 boost
icons on both copies. Their energy-resource pictograms are retained as
`[energy]`; no resource letter is guessed. All choice bullets and the scope of
each printed sentence remain explicit. **No Electro family face prints a boost
star, boost separator or boost ability.**

Normalization removes layout-only wraps, normalizes small-cap titles to title
case, and retains printed capitals separately. HTML marks bold ability labels
and the italic readying reminder. Flavor remains outside rules text. The tokens
`[star]`, `[per_hero]`, `[energy]`, `[boost]` and `[amplify]` retain distinct
printed glyph meanings; the arrow and choice bullets remain literal. No
unresolved illegible word, numeral or icon was found during transcription.

## Validation and next acquisition step

The external `build-electro-candidate.py` reconstructs the candidate from the
manual transcription and pinned local manifest. `validate-electro-candidate.py`
checks its structure and source bindings offline. Its report is
`electro-validation.txt`; machine-readable results are
`electro-transcription-findings.json`, beside the candidate.

The acquisition `verify.py` passed offline, confirming all 201 original scan
hashes and primary acquisition inputs remain unchanged. Candidate checks passed
for the exact 14-scan family inventory, original byte lengths/hashes, 10 unique
expected face IDs, complete footer coverage 1–14/14, per-face quantity/position
consistency, field-source pointer resolution and every copy's source identity,
boost metadata consistency and `Printed.Plain`-compatible normalization.
These are structural checks, not independent semantic proofreading.

**Independent proofreading remains pending.** A separate reviewer must compare
every field with all 14 pinned originals, including repeated copies. Particular
review points are the villain stages and SCH stars, the star-only ATK modifier,
per-player charge quantities versus fixed counters, fixed starting threat 6,
energy glyphs, and choice/interrupt sentence boundaries.

The baseline includes the supplemental full-card stat-annotation preservation
repair (`8cf73d3`), so the earlier Bullseye importer limitation is addressed in
the code underlying this packet. Research-only metadata and quantities still
need deliberate handling in a separate acquisition change; the candidate is not
a drop-in admission claim. No dataset, supplement, generated output, setup,
ability, runtime or existing family file was edited or regenerated. Build/unit
lanes are unnecessary for this documentation-only packet; normal commit hooks
provide repository documentation checks.

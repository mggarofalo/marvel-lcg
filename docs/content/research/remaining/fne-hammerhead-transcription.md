# Fear No Evil: Hammerhead transcription candidate

Research for MARVEL-697, 2026-10-27. Baseline
`086153452abc6647a78e42af1166444bbca89aea`; follows the
[pinned acquisition](fne-acquisition-candidate.md) and
[Electro packet](fne-electro-transcription.md). This is a complete-family
transcription candidate for independent proofreading, not dataset admission or
executable support.

The external candidate is
`C:/Users/mggar/Source/evidence/fne-acquisition/hammerhead-candidate.json`, SHA-256
`524321a4697b49059a7b0c6056a6ae5b86d8a6358333ff4bf034e439e2b0bd8c`.
Its original `scan-manifest.json` is pinned at SHA-256
`feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`.
The original scans remain external in that evidence root's `scans/` directory.
Source-storage and license findings remain those of the acquisition packet.

The `groups[].cards[]` candidate records full-card inputs plus research metadata:
printed title and card number, absence of subtitle, traits, rules and boost text
with markup and plain text, numeric attributes, special stars, boost icons,
stages, per-player flags, reverse-face status and physical quantities. Flavor,
illustrator and copyright credits are separate. Each field's `field_sources`
points to every corresponding original filename, complete SHA-256, printed card
number, footer position and visual region. Editorial IDs, slug/key spellings and
normalization choices are identified separately from printed facts.

## Printed inventory and quantities

All **14 original scans were viewed individually at original resolution**,
including every repeated copy. They establish **11 gameplay faces on 14 physical
cards**, covering every HAMMERHEAD footer position **1–14/14**.

| Candidate face | Printed title / stage | Printed number | Footer positions | Physical quantity |
|---|---|---|---|---:|
| 60086 | Hammerhead I | 86 | 1/14 | 1 |
| 60087 | Hammerhead II | 87 | 2/14 | 1 |
| 60088 | Hammerhead III | 88 | 3/14 | 1 |
| 60089 | Energy Gatling Gun | 89 | 4/14 | 1 |
| 60090 | Hardheaded | 90 | 5/14 | 1 |
| 60091 | Chameleon | 91 | 6/14 | 1 |
| 60092 | Maggia Enforcers | 92 | 7/14, 8/14 | 2 |
| 60093 | Maggia Underboss | 93 | 9/14, 10/14 | 2 |
| 60094 | Consolidate Power | 94 | 11/14 | 1 |
| 60095 | Power Struggle | 95 | 12/14 | 1 |
| 60096 | “Head First!” | 96 | 13/14, 14/14 | 2 |

Physical quantity and face-quantity sum are both 14. Each footer numerator
belongs to one face. No paired gameplay reverse is supplied by this family
source; `reverse_face: null` makes no claim about generic card-back artwork.
Quantities derive from distinct printed positions, independently of filenames or
product-wide marketing counts.

## Facts needing particular care in review

Hammerhead prints Roman I / II / III, SCH 2 / 2 / 3, ATK 1 / 2 / 3 and HP
14 / 15 / 16 per player. All stages have a special ATK reminder star, with no
SCH star. Stage II prints Toughness and Retaliate 1; stage III prints Toughness
and Retaliate 2. The starred Forced Response requires an attack that damages a
character, stuns that character, and deals 1 damage if already stunned at stages
I and II, or 2 at stage III. The Luke Cage quotation remains flavor.

Energy Gatling Gun has ATK +1 and 2 boost icons. Its Forced Response checks
whether the attacked character survived, discards this attachment as the cost,
then attacks that character again. Hardheaded has SCH +1, 1 numeric boost icon
and a separate boost star. Its Hero Action prints energy, mental and physical
resource pictograms in that order; the boost ability gives the activating enemy
a tough status card. The attachment's granted stalwart is a sentence in its
rules text, not an opening keyword assigned to the attachment.

Chameleon is unique, with SCH 1 and a reminder star, ATK 1, fixed HP 5 and
3 boost icons. Its +X SCH uses the highest THW among characters in play, followed
by a separate When Revealed scheme. Maggia Enforcers has a star-only SCH box,
captured as `stat_annotations.SCH` with `value: "★"` and `special_star: true`,
without an invented numeric zero. Both copies have ATK 2, fixed HP 3, Guard and
no printed boost row. The printed possessive is retained as “Maggia Enforcers’s”;
the MAGGIA trait emphasis and italic `(including this one)` remain explicit.

Both Maggia Underboss copies print SCH 1 and ATK 1 with separate reminder stars,
fixed HP 4, Villainous and 1 boost icon. Its starred Forced Interrupt checks
whether the player is stunned when it activates and supplies an additional
boost card for that activation. Consolidate Power prints fixed starting threat
6 and 1 boost icon; its Forced Response gives a defeated minion to the villain
as a facedown boost card. Power Struggle prints fixed starting threat 5,
1 hazard icon and 1 boost icon. Its reveal defeats the minion with the fewest
remaining hit points, then discards until a minion is discarded and reveals it.
Neither side scheme has a per-player threat symbol.

Both “Head First!” copies have 2 boost icons. The alter-ego reveal grants surge.
The hero reveal causes an attack; if the player is stunned, Hammerhead gets
+2 ATK for that attack and it gains overkill. The quoted punctuation in the
title and the separate flavor quotation are retained. Hardheaded is the only
face in this family with a boost star, separator and boost ability.

Normalization removes layout-only wraps and title small capitals. HTML marks
bold labels, emphasized traits, italic reminders and the boost separator; flavor
remains outside rules text, with quote/attribution spacing normalized. The
candidate documents `[star]`, `[energy]`, `[mental]`, `[physical]`, `[hazard]`
and the literal cost/effect arrow. No unresolved illegible word, numeral or icon
was found during transcription.

## Validation and next acquisition step

External `build-hammerhead-candidate.py` reconstructs the candidate from manual
transcription and the pinned manifest. `validate-hammerhead-candidate.py` checks
structure and source bindings offline. Its report is `hammerhead-validation.txt`;
machine-readable results are `hammerhead-transcription-findings.json` beside the
candidate.

The acquisition `verify.py` passed offline, confirming all 201 original scan
hashes and primary inputs. Candidate checks passed for the exact 14-scan family
inventory, original byte lengths/hashes, 11 unique expected face IDs, complete
footer coverage 1–14/14, quantity/position consistency, field-source pointer
resolution and every copy's source identity, coverage totals, boost metadata
consistency and `Printed.Plain`-compatible normalization. These are structural
checks, not independent semantic proofreading.

**Independent proofreading remains pending.** Compare every captured field with
all 14 originals, including repeated copies. Review particularly the star-only
SCH, every stat/boost star, numeric boost counts, absence of a boost row on Maggia
Enforcers, fixed threat, resource glyph order, printed possessive, and the attack
and damage prerequisites in the villain response. Acquisition and deliberate
offline dataset admission follow that review as a separate change.

No dataset, supplement, generated output, setup, ability, runtime or existing
family file was edited or regenerated. Build/unit lanes are unnecessary for
this documentation-only packet; normal commit hooks provide repository policy
checks.

# Fear No Evil: Purple Man transcription candidate

Research for MARVEL-697, 2026-10-27. Repository baseline
`8c708a668d2fe79b5d0e95e0ef9657d6a0b56f8d`, including the accepted
[Typhoid Mary proofread](fne-typhoid-mary-proofread.md). Follows the
[pinned acquisition](fne-acquisition-candidate.md) and prior complete-family
candidate schema. This packet establishes a transcription candidate for independent
proofreading, without dataset admission or executable support.

External evidence root: `C:/Users/mggar/Source/evidence/fne-acquisition/`.
The candidate is `purple-man-candidate.json`, SHA-256
`12e0eeb14a76b532d721843de531e9cd53f916326774becea9e4ec3e8ba5d94b`.
The original `scan-manifest.json` is pinned at SHA-256
`feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`.
Original scans remain external in `scans/`; source-storage and license findings
remain those of the acquisition packet. No scan is redistributed here.

The `groups[].cards[]` records preserve printed title/number, subtitle absence,
type, traits, full rules text and plain text, stats, reminder stars, per-player
symbols, numeric boost icons, boost stars, stage numerals, reverse-face metadata,
physical quantities, flavor and credits. Each field has `field_sources` pointing
to every corresponding original filename, complete SHA-256, printed card number,
footer position and visual region. Editorial ID allocation, key/slug spellings,
normalization and numeric stage representation are identified separately.

## Printed inventory and quantities

All **15 originals were individually viewed at original resolution**, including
both repeated-copy pairs. They establish **13 gameplay faces on 15 physical
cards**, covering every PURPLE MAN footer position **1–15/15**.

| Candidate face | Printed title / stage | Printed number | Footer positions | Quantity |
|---|---|---|---|---:|
| 60097 | Purple Man I | 97 | 1/15 | 1 |
| 60098 | Purple Man II | 98 | 2/15 | 1 |
| 60099 | Purple Man III | 99 | 3/15 | 1 |
| 60100 | Converted | 100 | 4/15 | 1 |
| 60101 | Firefighter | 101 | 5/15, 6/15 | 2 |
| 60102 | Judge | 102 | 7/15 | 1 |
| 60103 | Security Guard | 103 | 8/15 | 1 |
| 60104 | Skater Punk | 104 | 9/15 | 1 |
| 60105 | "Fight." | 105 | 10/15 | 1 |
| 60106 | "Protect." | 106 | 11/15 | 1 |
| 60107 | "Serve." | 107 | 12/15 | 1 |
| 60108 | Power of Suggestion | 108 | 13/15 | 1 |
| 60109 | Sway the Masses | 109 | 14/15, 15/15 | 2 |

No supplied gameplay reverse faces occur in this family; `reverse_face: null`
does not describe generic card-back artwork. Quantities derive from distinct
printed footer positions, independently of filenames or packaging arithmetic.

## Facts needing particular care in review

Purple Man's three unique PSIONIC, UNDERLING villain stages print Roman I/II/III.
SCH is 1/2/3, ATK 0/1/1 and HP 13/15/17 per player. SCH and ATK each carry an
independent special star at every stage. Stage I grants INFLUENCED minions guard;
II grants guard and villainous; III grants guard, patrol, and villainous. Each
stage also grants every boost card from the Purple Man encounter set the quoted
starred Boost ability: after this activation, deal this card to yourself as a
facedown encounter card. That grant is retained in villain rules text; the
villains have no printed boost row themselves.

**Every non-villain face prints a boost star, without a local boost clause or
separator.** Its `boost_text` remains empty. `boost_star` is independent of that
text and of the numeric icons. Numeric boost counts are Converted 3, Firefighter
0, Judge 2, Security Guard 1, Skater Punk 0, each obligation 1, Power of
Suggestion 3 and Sway the Masses 2. Firefighter and Skater Punk therefore have
star-only rows, distinct from the absent villain boost rows.

Converted is a CONDITION attachment. It treats the attached ally as an
INFLUENCED minion with a blank text box, makes SCH equal to base THW, and prevents
consequential damage. Its When Revealed attaches to the highest-cost controlled
ally and engages it; otherwise, this card gains surge. The candidate records
that conditional surge in text, without assigning an unconditional keyword.

All four minion titles have CIVILIAN and INFLUENCED traits and Vulnerable, with
italic reminder text discarding the character if stunned or confused. Their
SCH/ATK/fixed HP are Firefighter 1/1/3, Judge 3/0/2, Security Guard 0/2/4 and
Skater Punk 1/2/2. Firefighter's ATK has a special star and its starred sentence
grants piercing to its attacks. Security Guard also prints Patrol. Their When
Defeated effects respectively discard the highest-cost support or upgrade from
play; place 3 threat on the main scheme; discard the ally with the highest ATK
(among allies) in play; and deal 3 damage to the defeating player's identity.
Both Firefighter copies and both Sway the Masses copies were individually read.

Each quoted obligation title retains its period and quotation marks. All three
print Uses (3 command counters) and a Forced Action exhausting this card and
removing 1 command counter as cost. "Fight." deals 2 damage to a hero or ally;
"Protect." gives the villain a tough status card; "Serve." places 2 threat on
the main scheme. No additional removal instruction is inferred.

Power of Suggestion prints **fixed starting threat 2 with a special star**,
independently of its starred **Hinder 3 per player** text. It grants each minion
the INFLUENCED trait and prints one large amplify icon. Its starting-threat
annotation explicitly says `per_player: false`. Sway the Masses chooses an
INFLUENCED minion from the encounter discard pile and reveals it; otherwise,
this card gains surge. Power of Suggestion and Sway the Masses have no printed
illustrator credit; the empty string records that absence. Security Guard's
credit is transcribed as `Andrea Di Vito Laura Villari`, without inserting a
conjunction absent from the scan.

Normalization removes layout-only wraps, retains bold/italic emphasis and trait
references in HTML, preserves printed capitals separately from title case, and
keeps flavor outside rules text. Apostrophe typography and quote/attribution
spacing are normalized. `[star]`, `[per_hero]`, `[amplify]` and the literal arrow
preserve distinct glyphs. No unresolved illegible word, numeral or icon was found
during this transcription. Semantic interpretation remains outside this packet.

## Validation and next acquisition step

External `build-purple-man-candidate.py` reconstructs the candidate from manual
transcription and the pinned manifest. `validate-purple-man-candidate.py` checks
structure and source bindings offline. `purple-man-validation.txt` records the
run; `purple-man-transcription-findings.json` contains machine-readable findings.

The acquisition `verify.py` passed offline, confirming all 201 original hashes
and primary inputs. Candidate checks passed for the exact 15-scan family
inventory, original byte lengths/hashes, 13 expected unique faces, complete
footer coverage 1–15/15, quantity/position consistency, no gameplay reverse
pairing, field-source pointer resolution and every copy's provenance, coverage
totals, plain-text normalization, boost-star independence from local boost text,
Roman stages, per-player HP, special SCH/ATK stars, fixed threat versus per-player
hinder, amplify, Vulnerable and command-counter text. These are structural checks;
they do not establish semantic transcription correctness.

**Independent proofreading remains pending.** Compare every field against all
15 originals, especially boost-star rows without local boost clauses, villain
stat stars, stage-specific keyword grants, conditional surge, blank-box and
consequential-damage wording, defeat effects, command counters and forced costs,
fixed starting threat versus per-player hinder, amplify, and the repeated copies.
Deliberate offline dataset admission follows review as a separate change.

No dataset, supplement, generated output, setup, ability, runtime or existing
family file was edited or regenerated. Build/unit lanes are unnecessary for
this documentation-only packet; normal commit hooks provide repository policy
checks.

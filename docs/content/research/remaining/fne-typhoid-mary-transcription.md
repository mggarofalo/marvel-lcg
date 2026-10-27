# Fear No Evil: Typhoid Mary transcription candidate

Research for MARVEL-697, 2026-10-27. Repository baseline
`55975a416ffe67558de464b6c69f44a7c1911bb0`, including the accepted
[Hammerhead proofread](fne-hammerhead-proofread.md). Follows the
[pinned acquisition](fne-acquisition-candidate.md) and the
[Hammerhead transcription](fne-hammerhead-transcription.md) candidate schema.
This is a complete-family transcription candidate for independent proofreading;
it establishes neither dataset admission nor executable support.

The external candidate is
`C:/Users/mggar/Source/evidence/fne-acquisition/typhoid-mary-candidate.json`,
SHA-256 `56cf6551e9d4d9737a26446521ca8db0ea9705972bca6a53d1e5c84c76661b10`.
The original `scan-manifest.json` in that evidence root is pinned at SHA-256
`feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`.
Original scans remain external in its `scans/` directory. Source-storage and
license findings remain those of the acquisition packet.

The `groups[].cards[]` records preserve full-card inputs plus research metadata:
printed title and card number, absence of subtitle, traits, rules and boost text
with markup and plain text, numeric attributes, special stars, boost icons,
printed stage letters, per-player flags, reverse-face relationships and physical
quantities. Flavor, illustrator and copyright credits are separate. Every field's
`field_sources` points to every corresponding original filename, complete
SHA-256, printed card number, footer position and visual region. Editorial IDs,
key/slug spellings and normalization choices are identified separately from
printed facts.

## Printed inventory and quantities

All **18 original scans were viewed individually at original resolution**,
including every repeated copy and reverse face. They establish **14 gameplay
faces on 15 physical cards**, covering every TYPHOID MARY footer position
**1–15/15**.

| Candidate face | Printed title / stage | Printed number | Footer positions | Face quantity |
|---|---|---|---|---:|
| 60110a | Typhoid Mary A | 110A | 1/15 | 1 |
| 60110b | Bloody Mary A | 110B | 1/15 | 1 |
| 60111a | Typhoid Mary B | 111A | 2/15 | 1 |
| 60111b | Bloody Mary B | 111B | 2/15 | 1 |
| 60112 | Disturbed Psyche | 112 | 3/15 | 1 |
| 60113a | Establish Trust | 113A | 4/15 | 1 |
| 60113b | Mary Walker | 113B | 4/15 | 1 |
| 60114 | Telekinesis | 114 | 5/15 | 1 |
| 60115 | Psionic Slash | 115 | 6/15, 7/15 | 2 |
| 60116 | Psi Nova | 116 | 8/15 | 1 |
| 60117 | Assembled Armor | 117 | 9/15 | 1 |
| 60118 | Hail of Blades | 118 | 10/15, 11/15 | 2 |
| 60119 | Scavenged Bulwark | 119 | 12/15 | 1 |
| 60120 | Hand Lookout | 120 | 13/15, 14/15, 15/15 | 3 |

The face-quantity sum is 18; subtracting the three opposite-face relationships
gives 15 physical cards. Positions 1, 2 and 4 each carry two reciprocal
`reverse_face` records: 60110a/b, 60111a/b and 60113a/b. Their printed number stems
and matching footer numerators establish pairing. Other faces have
`reverse_face: null`, without making a claim about generic card-back artwork.
Quantities derive from distinct printed footer positions, independently of
filenames or product-wide marketing counts.

## Facts needing particular care in review

The villains print **letter stages A and B**, independent of their bottom card
face suffixes. Both 110 faces print stage A; both 111 faces print stage B. The
candidate retains `printed_stage` and assigns no numeric `Stage` attribute.
Typhoid Mary has SCH/ATK 2/1 at A and 3/1 at B; Bloody Mary has 1/2 at A and
1/3 at B. HP and the reset amount are 10 per player at A and 13 per player at B.
Every villain face is unique and has MUTANT, PSIONIC and UNDERLING traits. Their
Forced Interrupt places 1 **damage token** on Disturbed Psyche instead of defeat
and resets HP. Only Typhoid Mary B prints Toughness; Bloody Mary B instead prints
a When Revealed dealing 1 damage to each identity. None has a special stat star
or boost row.

Disturbed Psyche prints the Setup keyword and a separate bold Setup instruction
to put the villain into play on a random side, with italic `(and reveal it)`.
Its Forced Interrupt flips the villain when the villain phase ends. Its separate
bold victory sentence counts **3 or more tokens**, without restricting their
type. This packet transcribes that instruction without implementing randomness,
timing, setup or token interpretation.

Establish Trust prints Permanent and Setup, starting threat **8 per player**
with an independent special reminder star, and a starred sentence granting
hinder **3 per player in expert mode**. Its When Defeated places 1 **threat token**
on Disturbed Psyche and flips this card. The reverse Mary Walker is a unique
PERSONA attachment with Permanent, star-only SCH and ATK modifier boxes, and a
starred sentence preventing the attached villain from activating. The candidate
preserves these as `stat_annotations.SCH+` and `.ATK+`, each `value: "★"` and
`special_star: true`, without invented numeric zeroes. Its Forced Response uses
the alternatives of the villain taking damage **or the round ending**, deals
each player 1 facedown encounter card, then flips the card.

Telekinesis prints SCH +2, **two mental resource glyphs** in its Hero Action
cost, 1 numeric boost icon and a separate boost star. Its Typhoid Mary boost
deals 3 indirect damage and exhausts each character damaged this way. Psionic
Slash and Hail of Blades have **star-only boost rows**, preserved as 0 numeric
icons with `boost_star: true`, distinct from an absent boost row. Their reveals
check for the Mary Walker attachment, flip it and reveal Establish Trust;
Psionic Slash then has the villain scheme, while Hail of Blades has the villain
attack, with italic `(even if you are in alter-ego form)`. Their boost abilities
respectively check Typhoid Mary to deal 3 damage to a controlled ally, or Bloody
Mary to deal 2 damage to the player.

Psi Nova deals 1 damage to each controlled character and stuns the player. Its
boost checks Typhoid Mary and stuns; it has 2 numeric boost icons and a boost
star. Assembled Armor prints ATK +2, 1 numeric boost icon and a boost star. Its
Forced Interrupt places incoming villain damage on the attachment instead, then
discards it at 5 or more damage, or 8 or more in expert mode. Its boost checks
Bloody Mary and confuses. Scavenged Bulwark has Surge, gives the villain a tough
status card on reveal, and gives Bloody Mary one when she is activating as its
boost; it has 2 numeric boost icons and a boost star.

All three Hand Lookout copies print SCH 0, ATK 2, fixed HP 4, HAND and NINJA,
Guard and Toughness, 1 numeric boost icon and a boost star. Their boost gives
the activating enemy an additional boost card, without a named-villain condition.
Every face numbered 114–120 has a boost separator, star and ability. No earlier
face in this family prints one.

Normalization removes layout-only wraps, normalizes small-cap titles to title
case, and retains printed capitals separately. HTML records bold ability labels,
bold victory text, italic reminders and the boost separator. Flavor remains
outside rules text; apostrophe typography and quotation/attribution spacing are
normalized. `[star]`, `[mental]`, `[per_hero]` and the literal arrow preserve
distinct printed glyphs. No unresolved illegible word, numeral or icon was found
during transcription. No semantic interpretation is supplied for the letter
stages or the star-only modifier boxes.

## Validation and next acquisition step

External `build-typhoid-mary-candidate.py` reconstructs the candidate from manual
transcription and the pinned manifest. `validate-typhoid-mary-candidate.py` checks
structure and source bindings offline. The report is
`typhoid-mary-validation.txt`; machine-readable findings are
`typhoid-mary-transcription-findings.json` beside the candidate.

The acquisition `verify.py` passed offline, confirming all 201 original scan
hashes and primary inputs. Candidate checks passed for the exact 18-scan family
inventory, original byte lengths/hashes, 14 unique expected face IDs, complete
footer coverage 1–15/15, reciprocal reverse pairing and shared positions,
quantity/position consistency, field-source pointer resolution and every copy's
source identity, coverage totals, boost metadata consistency including star-only
rows, letter stages, star-only attachment modifiers, per-player starting-threat
annotations and `Printed.Plain`-compatible normalization. These are structural
checks, not independent semantic proofreading.

**Independent proofreading remains pending.** Compare every captured field with
all 18 originals, including repeated copies and the three face pairs. Particular
review points are the stage letters versus card suffixes, damage tokens versus
threat tokens, the unrestricted token victory condition, per-player reset HP and
threat/hinder values, star-only modifier and boost rows, mental resource glyphs,
and each named-villain boost condition. Deliberate offline dataset admission
follows that review as a separate change; research metadata and printed letters
need explicit admission handling.

No dataset, supplement, generated output, setup, ability, runtime or existing
family file was edited or regenerated. Build/unit lanes are unnecessary for
this documentation-only packet; normal commit hooks provide repository policy
checks.

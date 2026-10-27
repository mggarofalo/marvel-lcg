# Fear No Evil: independent Purple Man proofread

Research for MARVEL-697, 2026-10-27. Reviewed repository revision
`a3f72960fe0e907522b033663d01999e0c1ad70d` and the
[transcription packet](fne-purple-man-transcription.md).

**Verdict: accept the printed transcription candidate.** Independent comparison
of all 15 original scans found no correction to a captured printed field. This
accepts 13 gameplay faces on 15 physical cards; dataset admission and executable
support remain separate work.

## Reviewed inputs and method

- Candidate: `C:/Users/mggar/Source/evidence/fne-acquisition/purple-man-candidate.json`,
  SHA-256 `12e0eeb14a76b532d721843de531e9cd53f916326774becea9e4ec3e8ba5d94b`.
- Manifest: `C:/Users/mggar/Source/evidence/fne-acquisition/scan-manifest.json`,
  SHA-256 `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`.
- Each original listed below was individually opened at original resolution,
  including both repeated-copy pairs. The images, rather than filenames or the
  author's inspection flags, established printed numbers, footer positions,
  quantities, and the semantic comparison.

The comparison covered titles, subtitle absence, type, traits, uniqueness,
complete rules and boost text, emphasis, stats, stars, player symbols, stage,
flavor, credits, copyright and footers. Layout wraps, title case, apostrophe
style and attribution spacing follow the candidate's documented normalization.
No scan or candidate was changed. Storage and licensing limits remain those of
the [acquisition packet](fne-acquisition-candidate.md).

## Face and quantity findings

Every row accepts the corresponding candidate fields. Each footer reads PURPLE MAN
and has denominator 15. Boost counts exclude the independent special star.

| Original file(s) | Printed face and number | Footer | Distinguishing facts checked |
|---|---|---|---|
| `60097.jpg` | Purple Man I, 97 | 1 | Unique villain; SCH 1 and ATK 0 both starred; 13 HP per player; INFLUENCED minions gain guard. |
| `60098.jpg` | Purple Man II, 98 | 2 | Unique villain; SCH 2 and ATK 1 both starred; 15 HP per player; guard and villainous. |
| `60099.jpg` | Purple Man III, 99 | 3 | Unique villain; SCH 3 and ATK 1 both starred; 17 HP per player; guard, patrol and villainous. |
| `60100.jpg` | Converted, 100 | 4 | CONDITION attachment; blank text box, base THW as SCH and no consequential damage; highest-cost controlled ally or conditional surge; 3 boost icons. |
| `60101.jpg`, `60101_2.jpg` | Firefighter, 101 | 5, 6 | CIVILIAN, INFLUENCED minion; 1/1/3; Vulnerable; starred ATK grants piercing; defeat discards highest-cost support or upgrade; zero numeric boost icons. |
| `60102.jpg` | Judge, 102 | 7 | CIVILIAN, INFLUENCED minion; 3/0/2; Vulnerable; defeat places 3 threat on main scheme; 2 boost icons. |
| `60103.jpg` | Security Guard, 103 | 8 | CIVILIAN, INFLUENCED minion; 0/2/4; Patrol and Vulnerable; defeat discards highest-ATK ally among allies in play; 1 boost icon. |
| `60104.jpg` | Skater Punk, 104 | 9 | CIVILIAN, INFLUENCED minion; 1/2/2; Vulnerable; defeat deals 3 damage to defeating player's identity; zero numeric boost icons. |
| `60105.jpg` | "Fight.", 105 | 10 | Obligation; Uses 3 command counters; Forced Action exhausts and removes 1 counter before the arrow to deal 2 damage to a hero or ally; 1 boost icon. |
| `60106.jpg` | "Protect.", 106 | 11 | Obligation; same uses and cost; gives villain a tough status card; 1 boost icon. |
| `60107.jpg` | "Serve.", 107 | 12 | Obligation; same uses and cost; places 2 threat on main scheme; 1 boost icon. |
| `60108.jpg` | Power of Suggestion, 108 | 13 | Side scheme; fixed starting threat 2 with star; starred Hinder 3 per player; every minion gains INFLUENCED; one amplify icon; 3 boost icons. |
| `60109.jpg`, `60109_2.jpg` | Sway the Masses, 109 | 14, 15 | Treachery; choose and reveal an INFLUENCED minion from encounter discard pile, otherwise conditional surge; 2 boost icons. |

The three villains each print the same granted starred Boost ability for boost
cards from the Purple Man encounter set: after the activation, deal that card to
yourself facedown as an encounter card. Their own cards have no boost row. All
10 non-villain faces print a boost star but no local boost clause or separator;
star-only Firefighter and Skater Punk rows are distinct from absent rows. All
four minions print the italic Vulnerable reminder. Minion HP is fixed. No
additional command-counter removal text is printed. Power of Suggestion's fixed
2 and its per-player hinder are separate facts. Security Guard's illustrator
credit has no printed conjunction; Power of Suggestion and Sway the Masses have
no illustrator credit. The independently read footer numerators cover 1–15;
no supplied opposite gameplay faces occur. Null reverse metadata does not assert
anything about generic card backs.

## Provenance and limits

An independent offline check verified the exact candidate and manifest hashes,
all 15 family scan byte lengths and SHA-256 hashes against both manifest and
candidate, 13 unique face IDs, per-face quantity and footer coverage, and all
413 field-source bindings: each pointer resolves and names every applicable copy
with matching hash, printed number, footer and nonempty region. The candidate's
plain text matched markup removal and glyph-token normalization. The acquisition
`verify.py` also passed offline for all 201 original scans and primary inputs.
These consistency checks support provenance; direct image comparison establishes
the semantic verdict.

No dataset, supplement, generated output, setup, ability or runtime source was
edited or regenerated. Normal commit hooks provide repository policy checks for
this documentation-only review. The next separate step is deliberate offline
dataset admission; this verdict does not establish runtime support.

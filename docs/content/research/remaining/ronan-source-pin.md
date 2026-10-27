# Ronan Kree Fanatic source pin

Canonical parent: MARVEL-567. This closes the local PDF acquisition check in
[the research packet](563-568.md); it does not author or admit the modular set.

## Pinned original

- Publisher: Fantasy Flight Games, copyright Marvel / FFG 2020.
- Source: https://images-cdn.fantasyflightgames.com/filer_public/af/e7/afe79820-8835-4503-b256-7208e425d769/ronan_modular_set.pdf
- Retrieved: 2026-10-27 UTC.
- Bytes: 9,816,209; seven PDF pages, one numbered physical card per page.
- SHA-256: `09e3fba63e6483c64ffec1cfb09f1461fa81dfa4bfbba7fcaeb6d46bfab9a3a4`.
- Local original: `C:/Users/mggar/Source/evidence/ronan_modular_set.pdf`.
- Inspected local render: `C:/Users/mggar/Source/evidence/ronan-source/contact.png`;
  individual pages and extracted text are in that same evidence directory.

The local PDF was rendered with PyMuPDF and visually inspected, including a
larger first-page image to resolve the stylized HP numeral. No live web content
is required by generation, tests or runtime. The PDF remains local under the
repository's product-PDF policy. Rights remain with Marvel/FFG; this source pin
grants no license. Printed behavior continues to come from the existing joined
`datasets/cards/cards.json` records, not an independent transcription here.

## Playable quantities checked against the numbered pages

| Generated face | Title | PDF pages / set positions | Copies |
|---|---|---|---:|
| 90001 | Ronan the Accuser | 1 | 1 |
| 90002 | Judge, Jury, Executioner | 2 | 1 |
| 90003 | The Accused | 3 | 1 |
| 90004 | Bring the Hammer Down | 4–5 | 2 |
| 90005 | You Dare Oppose Me? | 6–7 | 2 |

All seven belong to Kree Fanatic. This product supplies a modular encounter set,
not a villain scenario or campaign. The generated stats inspected against the
source include Ronan's 3 SCH / 3 ATK / 9 HP per player, the side scheme's
3 starting threat per player and acceleration icon, and treachery boost values
3 and 2 respectively. All five faces have a boost star. The Accused's source
phrase uses “that attack” where the joined text uses “the attack”; this does not
change the referenced occurrence. No generated-data correction is proposed.

## Target ties and implementation boundaries

Current `rr:first-player.1` delegates a specific encounter target with multiple
eligible options to the first player. Combine that rule with the printed
lowest-remaining-HP hero restriction: retain the tied legal heroes and ask the
first player. Do not sort away ties, choose the lowest object ID, or include an
alter-ego merely because it has fewer HP. `rr:engage.3` prevents a fresh engagement
with a player already engaged with that minion. Actual first-player choice and
no-new-engagement behavior still require execution tests.

The existing research packet remains the implementation scope. Required evidence
includes phase-start movement before attacks, boost entry, exact activation
recipient, friendly-character defeat responses and discard-to-facedown dealing.
This source check neither proves those compositions nor admits any face.

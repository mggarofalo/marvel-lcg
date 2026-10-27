# Doctor Strange Invocation ruling reconciliation — MARVEL-565

Reviewed 2026-10-26 against `dd5b2f4aea395ad93f6acf58bb71beadc670bc4f`.
This prerequisite adds card-scoped authority relationships, not executable card
behavior or product admission. The original vendored ruling records remain
unchanged and readable as historical evidence.

## Decisions and scope

The actual question/answer records in `datasets/rulings/rulings.json`, their raw
HTML pairs, generated printed text in `datasets/cards/cards.json`, the complete
MC08 Invocation section, and current RR 1.8 entries were inspected together.

| Current ruling | Reviewed cards | Explicit supersession | Decision |
|---|---|---|---|
| `ruling:b07d53e12625ab23` | `09005` Master of the Mystic Arts | `ruling:ea32bb34b1e7b1b0` | Resolve the Special, including self-discard; expose the next top, or recycle immediately if empty; then return the original physical Invocation to the top faceup. |
| `ruling:8883bc6b0af1c9b8` | `09035` Vapors of Valtorr | `ruling:5bafad5aafa9093b` | No status card does not prevent initiation: the choose instruction limits its sentence and self-discard remains an effect. |
| `ruling:8c9c9d30f4c01189` | `09033` Images of Ikonn and `09035` Vapors of Valtorr | `ruling:5bafad5aafa9093b`, for Vapors | The Invocation itself is a valid discard target even if the other effects cannot affect a target. The question explicitly names Images with an already-confused villain and no scheme threat, justifying its inclusion. |

The Master answer has scope 1.6 and observed month `2025-09`; the raw answer's
byline is **September 15, 2025**, despite its inherited `April, 2025:` section
label. See `datasets/rulings/pages/post-rrg-1-6.html`, lines 3459–3468.
The other raw answers are in `post-rrg-1-5.html`: question 109, byline November 2,
2023, and question 169, byline May 10, 2024. Both have scope 1.5. The conflicting
older answers have scope pre-1.5 and no observed month.

RR 1.8 `rr:choose-game-element.2` checks whether there are valid targets for
**any part** of the ability. `rr:target.2`, `rr:target.3` and
`rr:initiating-abilities.step.2` require at least one affected target.
Self-discard satisfies that requirement in these reviewed situations; it is not
an exemption from paying costs. `rr:special` still requires another ability's
explicit instruction. For Master, `rr:then` and
`pack:mc08:the-invocation-deck` supply the completion sequence, persistent faceup
top, separate discard and immediate empty-deck recycling without penalty.
These are scoped applications to printed cards, so the graph uses
`card_clarifications`; it does not replace any general RR clause through
`modifications`.

## Exact pins

All hashes below are SHA-256. Ruling hashes are the vendored content hashes;
card hashes cover the generated `text_plain` UTF-8 bytes.

| Source | Hash |
|---|---|
| `b07d53e12625ab23` | `eab706034466af8f3530e852a59ac9426fe1ca1bf5ba2290c389d1b4ddfaa9b94` |
| `ea32bb34b1e7b1b0` | `541ee215c92730386836fc25bbee9cb346c6021b340d0edcc2be0998460426cb` |
| `8883bc6b0af1c9b8` | `1a3c497a66bb0af95ce8338ef64cf4edf416e9594379d2aaec6a833db1827ef8` |
| `8c9c9d30f4c01189` | `f7f38ed0570b7a543e44dc1b00ddad8f29cb6766c51dbead2872026e52d7c9a6` |
| `5bafad5aafa9093b` | `5fd4cd0b160d86618e89c1eb44076b1af7b5912de331f3a16774118c03519555` |
| `09005` | `ddc813741af2905271e27ee1aff9e04e0333c4656334ce905a43f8dbdd09376c` |
| `09033` | `1e9f31fe70e9e0e18b5be9764489e6e4a36e1f1b38cec3919ba0612cde75e742` |
| `09035` | `23acfbf19eace2b149270386cf56049158fa05c1e7879c23c31dfd9ced12a50d` |

## Verification

- `CardRulingRelationshipTests`: 7 passed. The generic gate checks all current
  printed-text/ruling/superseded hashes and reference IDs. Focused cases require
  the exact reviewed card scope, explicit supersession, historical retention,
  current citation eligibility and separation from general RR modifications.
- Full `Marvel.Rules.Tests` Release project: 1,160 passed, zero skipped.
- Offline `Marvel.Rulings.Harvest check`: four raw page hashes verified;
  1,100 rulings regenerated with zero added, revised or removed.
- Offline `Marvel.MarvelCdb.Harvest check datasets/marvelcdb-faq/acquisition.json`:
  committed candidate matches `faq.json`.
- `Marvel.Behavior.Index check`: catalog and generated Core card-face
  specification remain current.
- Two temporary data mutations were rejected by the focused scope/supersession
  test: remove Master's supersession; remove Images from the reviewed scope.
  Original graph bytes were restored after each mutation.
- `git diff --check` passed. Unrelated graph entries and raw formatting are
  preserved. Integration must still review and implement the Master continuation
  against these authorities; this packet provides no runtime acceptance claim.

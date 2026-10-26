# Doctor Strange reprints — MARVEL-799

Candidate branch: `feat/doctor-strange-reprints`, based on `1b5fa2e9`.
Evidence date: 2026-10-26. This packet implements six nonpublic research faces;
it does not implement or admit the complete Doctor Strange product.

| Face | Printed card | Reviewed Core interpretation | Distinguishing execution evidence |
|---|---|---|---|
| 09017 | The Power of Protection | 01079 | Two wild resources for Protection card payment; one for Basic card payment, a Protection ability source, and context-free generation; two-copy title maximum |
| 09018 | Med Team | 01080 | Actual paid entry gives three medical counters; exhaust and spend one to heal two from another player's hero or ally; last counter discards while healing continues; full health, exhausted and empty-counter negatives |
| 09022 | Energy | 01088 | Actual two-energy payment and shared one-copy title maximum |
| 09023 | Genius | 01089 | Actual two-mental payment and shared one-copy title maximum |
| 09024 | Strength | 01090 | Actual two-physical payment and shared one-copy title maximum |
| 09025 | Avengers Mansion | 01091 | Non-first controller chooses either player's draw; only the selected hand grows, source exhausts and cannot act again |

Printed authority is `datasets/cards/cards.json`. All six faces match their Core
printing's name, type, traits, attributes, text and plain text exactly; a six-case
test pins the comparison, including printed copy-limit metadata. The vendored
MarvelCDB FAQ search found no entry naming these six reprint or corresponding Core
IDs. No new timing primitive or engine behavior is introduced.

`DoctorStrangeResourceReprintTests` and `DoctorStrangeSupportReprintTests` use the
actual appended rows. The book has 651 faces. A raw JSON-object comparison during
authoring verified all 645 previous objects byte-for-byte unchanged. The exact
allowlist gains only these six IDs; the counter inventory gains only
`09018 = medical, 3, uses`. `11028` and `09003` remain unauthored negative controls.
Existing Black Widow packet inventories retain their fixed row positions and
use the new total.

## Verification

Logs and TRX artifacts are outside the repository at
`C:/Users/mggar/AppData/Local/Temp/strange-reprint-evidence/`.

- Release focused content run: **97 passed**, zero failed/skipped. Filter includes
  both new test classes, `AbilityData`, `AbilityProgramTests`, `PowerOfAspectTests`,
  `AllPurposeCounterTests`, four affected Black Widow inventory/card classes, and
  `RuleCitationTests`. Exact command/output: `restored.log`, `restored.trx`.
- Three authored-data mutants were built and lowered through the production
  ability compiler, then killed by execution assertions: Protection changed to
  Justice (`power-mutant.log`, expected `GG`, actual `G`); healing two changed to
  one (`heal-mutant.log`, damage remains); chosen player changed to controller
  (`recipient-mutant.log`, selected player's top card does not move to hand).
  These are dataset-program mutants, not source-code mutations.
- Restored sources received a successful nonincremental Release build
  (`restored-build.log`), then the 97-test restored run above.
- Public exclusion: **4 passed**, zero failed/skipped. Checks are recorded separately in `public-exclusion.log` and
  `public-exclusion.trx`; they cover `RuntimeProductAdmissionTests` and
  `PublicBlackWidowQuantityIndependentTests` against the six-row candidate.

No setup, engine, renderer, protocol or public-admission code changes belong to
this packet. Independent review and integration into the canonical Doctor Strange
candidate remain next; remaining signatures, Invocations, starter/extra cards,
obligation, nemesis and complete-product admission remain separately scoped.

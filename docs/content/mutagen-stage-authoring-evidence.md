# Mutagen Formula stages and setup research evidence

Candidate base: `67a89a61` (frozen PR448). This packet authors seven actual faces:
02014–02016, 02017a/b and 02018a/b. Green Goblin now has 43/57 distinct authored
faces; fourteen Mutagen encounter faces 02019–02032 remain unauthored and fail
closed. No public admission or engine change is included.

Printed authority: generated `datasets/cards/cards.json`; quantities: pinned
`datasets/marvelsdb/pack/gob_encounter.json`; composition and ordinary setup:
`pack:mc02:how-to-use-this-pack` plus printed 02017a Contents and Setup.
Vendored `ruling:9a1cb4d914aa6778` confirms overkill satisfies attacks-and-damages.
An exact offline query of `datasets/marvelcdb-faq/faq.json` entries by code
02014, 02015, 02016, 02017a, 02017b, 02018a and 02018b found no matches.

The complete Mutagen scenario has 21 distinct faces on 31 physical cards (33 face
instances counting both scheme sides), including 26 physical encounter cards.
Standard allocates I/II and Expert II/III. Both recipes recommend Goblin Gimmicks;
production setup evidence chooses the supported Bomb Scare module. Setup recruits
one Thrall per seat, shuffles and advances once. Expert stage-II reveal deals two
encounters to every player; multiplayer ordering resolves before opening hands.

Actual-card tests cover stage-I/II/III attack threat, prevention, non-first-player
ownership, stage-II/III encounter bursts, first physical Goblin recruitment across
encounter reset, engaged-Goblin exemption, stage-two entry and final loss. Existing
escalation proofs now load the actual authored constant at 1–4 players, including
Goblin departure, non-Goblins, out-of-play cards and acceleration additions.

Verification (Release, Windows):

- Focused Mutagen, ability-data, setup, setup inventory, Core card-face transcripts
  and citation selection: 141 passed, zero failed. Log:
  `%TEMP%/mutagen-final-restored.log`; TRX:
  `%TEMP%/mutagen-stages/mutagen-final-restored.trx`.
- Behavior catalog regenerated and offline `check` passed:
  `%TEMP%/mutagen-behavior-check.log`.
- Mutation changes actual 02017b discard count from three to two: both completion
  cases fail with different deck counts (including reset), restored cases both
  pass. Logs `%TEMP%/mutagen-mutant-distinguished.log` and
  `%TEMP%/mutagen-restored-completion.log`. An earlier probe before the deck-count
  assertion survived; that missing observation was added and the mutant rerun.
- Authority/citation checks: 25 passed, zero failed; log
  `%TEMP%/mutagen-authority-final.log`. Live catalog inventory is 2,652 sources,
  4,412 obligations and 1,440 outside-Core obligations.
- Relevant global setup and Mutagen setup checks: 26 passed, zero failed after
  extracting the setup-order helper; `%TEMP%/mutagen-setup-final-refactored.log`.
- Normal hooks remain enabled. The initial commit was rejected for setup-test
  cyclomatic complexity (11); setup ordering now has its own cohesive helper.
  Final hook result is recorded by the delivery handoff.

Scope limits: nonpublic research worlds, no complete Mutagen game, native UI or
product acceptance claim. Follow-on authoring owns 02019–02032 and their focused
proof, then full Green Goblin admission and public setup/replay acceptance.

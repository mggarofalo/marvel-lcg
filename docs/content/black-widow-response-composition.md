# Rapid Response composition prerequisite

Work: MARVEL-78223c8c427-bc9a-4b68-b727-ede712d8e204. Printed authority is
`datasets/cards/cards.json` 08031: after an ally you control is defeated, discard
Rapid Response to put that ally into play from your discard pile and deal it one
damage. This packet uses a test-local interpretation, without ability-book or
product admission.

The imported five-case `BlackWidowRapidResponseCompositionTests` packet was red
at lowering for the proposed engine-chosen selector
`defeatedAllyInYourDiscardPile: "you"`. Its acceptance body had not executed.
Controller at defeat, owner, physical departure and current discard membership
must be proved independently; current ownership cannot establish former control.

The implementation captures controller at defeat and selects the matching physical
card and incarnation in its owner's discard. Current RR 1.8 `leaves-play.1`
invalidates an intervening in-play reentry. It does not invalidate out-of-play
movement alone: discard-to-hand-to-discard and reordering within discard are
positive controls, consistent with `SafeHouseCostSuspensionTests`. The older
vendored `ruling:c511a191fe8cc74d` (pre-1.5) also excludes a card currently in
hand; current RR and the printed current-discard condition govern this packet.
The MarvelCDB FAQ snapshot queried 08031 without an entry.

Return uses ordinary ally entry, binds the new incarnation as `chosen`, waits for
entry responses, then deals one damage. Tests distinguish Hawkeye's four fresh
arrows, ready state, one damage, another discarded Hawkeye, owner/controller
differences, decline, stale targets, rejected answers and an unavailable discard
cost. Synthetic entry-choice and one-HP fixtures prove suspension, refusal to
damage a reentered copy, re-defeat before Preparation completion, and completion
before Widowmaker/Synth-Suit. The imported body had not previously run; initial
expanded-run failures from already-ready response targets, an incomplete one-HP
board and cross-world reference equality were fixture findings, then corrected.

Verification: focused Rapid Response plus Safe House suspension tests passed
22/22 after a forced non-incremental Release rebuild (zero warnings/errors).
A compiled mutant removing the captured-controller check failed the original
owned-but-other-controlled case (1 failed / 4 passed); restoring and forcing the
rebuild returned 22/22 green. Logs are outside the tree in
`%LOCALAPPDATA%/Temp/rapid-response-{controller-mutant,restored-build,restored}.log`.
Normal pre-commit hooks remain required for both packet commits.

`Defeated.Controller` is an occurrence fact reconstructed by accepted-decision
replay, not a new persisted, digest, protocol, RNG or card-allocation field.
Existing admitted ability rows and public product admission are unchanged. The
runtime-selected DSL spelling and returned `chosen` binding are new capabilities.
No runtime version is changed by this prerequisite packet; the integrated pack's
history/replay verification remains the delivery owner's responsibility.

Integration control: extraneous resource generators on a discard-only response
were accepted in a diagnostic probe. The Counterattack worker owns that narrow
cost-preflight correction. This packet tests an unavailable discard source and
does not duplicate that shared fix. Re-run the extraneous-payment control when
the reviewed correction is integrated.

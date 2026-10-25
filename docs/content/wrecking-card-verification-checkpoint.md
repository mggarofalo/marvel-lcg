# Remaining Wrecking Crew canonical verification checkpoint

Reviewed baseline: `63bdee9f`. Printed expectations were read from
`datasets/cards/cards.json` before reviewing author findings. Vendored Wrecking
Crew rulings were checked, including `ruling:bda05e31eac5dbb5` (Escaped Convict's
attack is independent of counter movement) and `ruling:28d343ac007bd784` (Dead
Meat's defeat remains a defeat when an interrupt returns the ally to hand).

## Focused evidence

`WreckingRemainingTreacheryTests`: 9 cases fail on the baseline because canonical
rows 07015, 07016 and 07030 are absent. These are preserved positive requirements,
not proof of a defect in a proposed interpretation. The contracts distinguish
global side-scheme placement, remaining-health selection across players,
actual defeat versus damage prevention, and an undefended completed attack versus
defense or stun replacement. Log: `%TEMP%/wrecking-review-focused.log`; TRX:
`%TEMP%/wrecking-review-remaining.trx`.

`WreckingRemainingSchemeTests` and `WreckingRemainingSchemeChoiceTests` adapt
the existing `ae7d7c11` scheme contracts to the canonical book without injecting
rows. They cover threshold boundaries 9/10/14, 1–4 seats, exact reset, penalties,
ownership, highest-cost ties, empty player pools, toughness, departure protection,
and promotion/final-defeat topology. Candidate run pending.

## Engine handoff requirements

| Faces | Missing capability and distinguishing regression |
|---|---|
| 07001b, 07007, 07015 | MARVEL-669: placements across multiple schemes must expose one response window with player ordering; assert all placements precede penalties and counter ranking uses the resolved state. |
| 07009/24/38/53 | MARVEL-650: queue the selected villain's boostless attack after the containing attack; attack still occurs when counter cannot move; hero/alter-ego, ties and physical deck ownership must remain distinct. |
| 07016 | `WasDefeated` reads the invoking occurrence's defeats. `AbilityDamageCompletion` publishes damage completion but no caused-defeat receipt. Record the exact recipient's defeat; survival, tough and another character's defeat must not place threat. A returning defeated ally still qualifies. |
| 07030 | `UndefendedAttack` reads a correlated AttackEnds occurrence or live attack. Resumed reveal needs its scheduled attack's defended/made receipt. Stun replacement and defended attacks place no threat; undefended damage prevention still qualifies. |
| 07022 | Engine has `AttackDamageResult.Excess`, but DSL has no excess-routing instruction. Route actual excess to Thunderball's paired scheme independently of active counter; test ally defense, identity defense, prevention, excess zero and attachment discard after attack. |
| 07035 | DSL lacks prohibition against attacking selected other villains. Enforce for basic and labeled attacks, preserve Piledriver and minion targets, and lift on departure. +3 HP and the hero-attack-only paid response are independent obligations. |
| 07049 | `activationDamage` is recorded for scheduled activation continuations and afterActivation. The attack-end forced response needs its exact attack damage quantity before optional responses. Verify damage/prevention/overkill recipients and separate random-discard hero action affordability. |
| 07051 | `AbilityProgramQueries.Defenders` inspects constant effects on the attacking enemy only. An attachment interrupt cannot supply the attacked player's mandatory controlled-ally restriction. Test ready/exhausted allies, another player's allies, no eligible ally, and discard only after an actual attack. |
| 07031 | No typed moveThreat instruction or actual destination-response-trigger receipt exists. Preserve source/destination selection, move all threat, wait for the destination's forced response, and gain surge only when that response did not trigger. Threshold alone is insufficient. |

07001a setup remains a separate recipe obligation; this packet grants no public
product admission. Native and complete-game acceptance were not performed.

Pending: candidate scheme revision and run, all omitted rows, tie/interrupt/boost
coverage beyond the explicit cases, and meaningful mutation evidence. No green
whole-pack claim is supported.

Candidate review: author's four-row staged file had Git blob `6126491b` and 280
added lines, copied without edits. Focused canonical scheme run: **60 passed,
1 failed** (61 total). `PileDriveOffersOnlyEachPlayersTiedHighestCostAndWaitsToReset`
fails at line 32: after the each-player order is submitted, the expected tied
card choice is absent (`Sequence.Work` returns null). Assertions and failure
are preserved for author investigation; this is not a green four-row review.
Log: `%TEMP%/wrecking-review-schemes.log`. The author's subsequent six-row
candidate including Breakout is not reviewed in this packet.

Execution checkpoint: 20 exec invocations maximum allowance, approximately ten
active minutes. No broader suites or native work. Source data is an unstaged
copied author candidate; test/doc work is separately staged for a normal commit.

# Terminal fixture repair and winning-transcript audit

Base: `69fa1a2eb304bef1034c1691400346cfb8ae77fb`. The fixture commit changes seven test classes and this evidence record without changing engine behavior. The independent review below updates the winning digest pin after adjudicating its terminal-state difference.

Nine original cases reached `PlayersLose / EncounterCardsExhausted` before completing their claimed observation. `Discard.Card` calls `PlayerDeck.Reset` when the player's empty deck gains a discarded card. Rules Reference `rr:player-deck.1` and `.4` require the reset and immediate encounter deal; `rr:encounter-deck.4` makes an empty encounter deck/discard pair a loss. The interpreter's terminal guards then prevent further ability text or event cleanup. These tests genuinely failed on this integrated base; this is not a claim that their assertions failed before terminal guards existed.

The repaired cases retain every original assertion and add a local player-deck reserve plus an unfinished-outcome observation at the relevant boundary:

- Superhuman Strength and Indomitable: discarding their upgrade must leave play continuing before the stun/ready effect is observed.
- Both Energy Channel cases: add the reserve after mulligan, preserving the original opening hand, resources and maximum-X assertion. Payment and the completed discharge remain unfinished.
- Multiple Assess copies: event cleanup leaves play unfinished after each grant, including before the second event.
- Legal Practice: choosing and discarding the cards leaves play unfinished before continuation finishes and threat removal is checked.
- Press the Advantage: the intended top card remains the conditional draw; a reserve beneath it prevents that draw's reset from dealing the last encounter card after Klaw's stage-entry search.
- Lead from the Front: payment and the selected-player answer both leave play unfinished.
- Stacked Cosmic Flights: each interrupt's discard leaves play unfinished before combined prevention is measured.

Evidence is under `C:/Users/mggar/Source/evidence/`:

- `roars-terminal-original-baseline.log`: unchanged seven fixture classes plus the defeat-transcript class, **63 passed / 10 failed / 73 total**. Nine fixture cases and the winning pin reproduce the integrated failures.
- `roars-terminal-causal-diagnostic.log`: original fixtures with temporary outcome observations, **53 passed / 9 failed / 62 total**; all nine failures report `PlayersLose/EncounterCardsExhausted`. Diagnostics were removed.
- `roars-terminal-repaired-regression.log` and `roars-terminal-repaired-final.log`: intermediate repairs passed 63 cases but changed Energy Channel's maximum X by drawing added reserve cards. Those attempts are not accepted evidence.
- `roars-terminal-repaired-accepted.log`: repaired seven complete classes plus `RuleCitationTests`, **64 passed / 0 failed**. All original observations, including maximum X of eight, remain.

All runs use `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release` with the named complete-class filters. No full managed, native, second-platform or pack-acceptance claim follows from this packet.

## Separate winning-transcript audit

The genuine preterminal revision is `e7e3e745658521d69ed3e9dec72644880a9c7126`. Its defeat feature, canonical scene implementation and transcript runner are byte-identical to the integrated base. Its original defeat-transcript class passes (`roars-terminal-old-transcript.log`). Temporary runner diagnostics captured the final canonical card state, outcome and typed JSON events for `behavior:rr:winning-the-game:published-result` on both revisions, then were removed.

The only canonical card difference is physical card **30**, Uppercut `01054` copy 1: old `DiscardPile`, index 14; current `RevealingArea`, index 0. All other card properties and cards match. Both outcomes are `PlayersWin / Unspecified`. The old digest is `9abcbfbb07699ca2eb3b58207acbc69f9291f7462e63feb74d14237a40d02bbf`; current is `bb97bb09f4cd482d0154bb7ee2db3cfce490bbcf4e8a944b91b866173669d648`.

The final decision has nine old events and eight current events. The first seven match exactly, through final Rhino defeat/removal. The old-only eighth event discards that resolving Uppercut with trigger `Play`; the final `Attack_Finished` event matches exactly. The distinction is terminal event cleanup, governed by the already reviewed `AbilityResolutionPayment.IsResolvingEvent` guard and final-villain victory under `rr:winning-the-game`. The independent adjudication below is limited to this difference.

Raw artifacts: `roars-terminal-{old,current}-state.txt`, `roars-terminal-{old,current}-events.jsonl`, `roars-terminal-{old,current}-outcome.txt`. `roars-terminal-winning-summary.json` records the parsed exact differences and SHA-256 values; `roars-terminal-winning-delta.txt` preserves the raw unified comparison.

## Independent review

Reviewed `54a8869f5a614fbe7298fc57e78ee6d0dc0c1785` against `69fa1a2eb304bef1034c1691400346cfb8ae77fb`, the original and diagnostic logs, and both raw canonical states and final-decision events. The nine failures are terminal losses, the reserves preserve the asserted actions and results, and no original assertion was removed. The prepared states are focused synthetic fixtures, not complete deck-construction or gameplay evidence.

Rules Reference `rr:winning-the-game` awards victory when the final villain stage is defeated; `rr:event` places the event in its owner's discard pile after its effects resolve. The final attack defeats Rhino during the event effect, so ending play before the event's later discard is the supported interpretation. The existing `PlayersWinEventCleanupTests` also compare terminal and nonterminal event cleanup. The unchanged transcript's digest pin is updated to the independently compared current state, with explicit final-event observations that Uppercut moves into the revealing area and is not discarded. The attack-finished receipt is retained as recorded. This does not approve other terminal paths or change the historical transcript or archive.

The independent Release run of all seven fixture classes, the complete defeat-transcript class, `PlayersWinEventCleanupTests` and `RuleCitationTests` passed **81/81** (`roars-terminal-independent-review-restored.log`). The initial run failed only the newly entered digest because the audit prose had an extra trailing digit; the corrected value was checked against the raw state SHA-256 before the passing rerun (`roars-terminal-independent-review.log`).

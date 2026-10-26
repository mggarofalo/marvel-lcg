# Core Get Behind Me! cross-seat correction

Tracking: MARVEL-739. Joined printed faces `01078` and `05013` have the same
Hero Interrupt: cancel a treachery's When Revealed effects and have the villain
attack you. Neither restricts the revealed treachery to the paying player's
seat. `ruling:3724bc02d9defdbe` clarifies the cancellation followed by an attack;
the event does not replace the revealed card itself.

The Core row now omits the revealing-seat trigger restriction, matching the
reviewed Ms. Marvel printing. The independent full reveal tests run against
both physical source faces and both Rhino healing and Exhaustion. They retain
the paying player's attack, unchanged other player's health, cancellation,
pending event location, final event/treachery disposal and no extra encounter
card assertions.

Independent reproduction of the prior Core row failed both missing-offer
assertions (`C:/Users/mggar/Source/ms-defense-core01078-repro.log`). The corrected
combined actual-card/data checks pass 50/50; all 20 pinned Core transcript
checks pass without digest changes. Logs are
`ms-completion-core-crossseat.log` and `ms-completion-core-transcripts.log` in
the same external directory. The first broad filter used the wrong transcript
class suffix; the separate 20-case run is the transcript evidence.

This changes one authored trigger, not the card dataset or engine. Independent
review and final combined gates remain required for the correction.

## Independent correction review

Reviewed exact combined candidate
`4086334599a08e29e53fb780786e0fa379167a7f`, including correction
`600e18b3a253b88a8fd61100b43b0b3c0d52a8a2`, against the identical joined
01078/05013 text and raw `ruling:3724bc02d9defdbe`. The printed interrupt
restricts the card type and hero form, but does not restrict the revealing seat.
The retained row still requires a treachery and hero form; its villain attack
uses the paying/resolving player.

The full encounter-reveal theory now exercises both payer/revealer directions
for both source printings and both treacheries (eight cases). It also asserts
the answering seat, payment disposal and unfinished game outcome, retaining the
prior cancellation, nested attack, no-surge, identity and final-cleanup checks.
A data mutant restoring only 01078's revealing-seat restriction fails all four
Core cases at the missing interrupt prompt/offer, while the four 05013 controls
pass. Exact bytes restore in `finally`; the 108-case focused filter passes
before mutation and after restoration. Logs are
`C:/Users/mggar/Source/generation-core-independent-{focused,core-seat,restored}.log`.
The unchanged Core card-face transcript tests are included in that passing run.

Verdict: accept the Core cross-seat correction. This review adds tests and
evidence only; it does not broaden public product admission.

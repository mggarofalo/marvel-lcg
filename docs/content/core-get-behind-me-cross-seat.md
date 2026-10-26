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

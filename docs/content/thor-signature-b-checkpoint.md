# Thor signature batch B checkpoint

Base `f7d43b26`; isolated branch `feat/thor-signature-b`.

Author `06001a` Thor and `06004` For Asgard! only. The authored-face inventory
rises from 587 to 589. Public admission, research setup and replay versions remain
unchanged. Printed authority is `datasets/cards/cards.json`; relevant rules are
`rr:engage.2/.3`, `rr:limit`, `rr:search.1/.3`, and `rr:ability.1/.4`.
FAQ `06014` excludes engaging an already engaged minion again.

The actual Thor response draws two only for the destination player and expires
its use at both phase boundaries. Actual For Asgard pays its printed one, restricts
the event to alter-ego, chooses an own physical Asgard card from deck/discard,
and shuffles even without a match. The tests compare random state, pending work
and later execution against an unprobed control after queries and rejected payment.
The independent transfer test uses the actual Thor row instead of appending a
second synthetic Thor response to the full book.

Evidence is external at `C:/Users/mggar/Source/thor-signature-b-evidence`.
`restored-focused.log` covers the new tests, existing transfer review, ability-data
inventory and whole-book lowering. `final-safe.log` is the restored new-test run.
Data mutants change draw two to one, phase limit to two, Asgard to Avenger,
and no-match shuffle to exhaustion. Each must fail the focused observations;
`mutant-no-shuffle-isolated.log` isolates the shuffle observation. The earlier
replacement-with-draw mutant is weaker and supplemental only.

## Withheld candidate faces

`06005` Hammer Throw and `06014` Get Over Here! are not authored. Candidate
rows and full probes are preserved externally as `unsafe-candidate-rows.json`,
`ThorSignatureEventTests.cs` and `ThorActualEngagementTests.cs`; `actual2.log`
records the red observations. Reintroduce the candidate rows and those tests in
an isolated checkout to reproduce. These are hypotheses requiring independent
review, not authority-derived acceptance claims.

- Lethal Get Over Here (one remaining HP Hydra Mercenary) still creates a new
  engagement and Thor response. The chosen binding remains usable by
  `putIntoPlay` after damage; that does not establish a legal transfer of a
  defeated minion. A safe data composition was not established in this packet.
- Hammer Throw returns Mjolnir before the offered Chase Them Down defeat
  response. The probe expected it to remain in play through that response and
  nested threat choice. Damage and return share one printed sentence: independent
  authority review must settle the timing before declaring an engine defect.
- Transferred Vulture's defense prompt initially belongs to seat zero while Thor
  is seat one. This may be the ordinary multiplayer defense chooser. The probe's
  seat-one assumption is not an established bug; inspect the full defense flow.

The initial broad `Thor|Cards.Ability` run is incomplete: it found the duplicate
synthetic Thor response, then continued into long tests. It was stopped and its
own worktree test process terminated after it held an apphost file lock. During
that incomplete run, the first mutant script overlapped it; do not cite the broad
run as verification. The scoped restored run and isolated no-shuffle mutant are
serialized. No other worktree process was terminated.

Independent review is still required for these two admitted rows and the
withheld probes. No native UI or full-pack readiness is claimed.

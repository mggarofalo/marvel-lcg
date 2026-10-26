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

## Independent review of actual batch B

Reviewed source revision `1bf3b627ea19ee107e5eecf95f558d6afde68e52` in
`C:/Users/mggar/Source/marvel-thor-b-review`, branch `review/thor-b`.
Verdict: approve the two authored rows (`06001a`, `06004`) for integration into
the nonpublic research candidate. This is not whole-pack or public admission.

The joined printed text gives Thor a response after engaging a minion, drawing
two, once per phase. `WhenMinionEngaged`, destination-player subject, hero form
and `limitPerPhase: 1` match that instruction. Existing actual-row tests verify
destination seat one, a second engagement in the same phase, and renewed use
after both player and villain phase endings. New independent tests verify that
alter-ego engagement and declining a response consume no use, while changing
form does not refresh a spent use. `rr:engage.2/.3`, `rr:limit`, and the vendored
`06014` FAQ support these boundaries. No FAQ entry exists for the queried
`06001a` or `06004`. Rulings `ruling:0e6ffd0bb9270f47` and
`ruling:dc752ffec50b2356` also exclude re-engagement with the same player.

For Asgard's printed cost is one (`attributes.Cost`); its alter-ego action
searches the owner's deck and discard for an Asgard trait match, adds one chosen
physical card to hand, and shuffles. The row follows that text. Independent
observations distinguish matching Asgard cards in one's deck/discard from one's
hand/play area, another seat's deck/discard, and nonmatching cards. Existing
tests prove payment, form restriction, either searched area and no-match shuffle.
The retained Worthy tests separately distinguish named Mjolnir searches from
trait searches and check the round limit. The authored-face inventory gate
passes with exactly the two additions, 587 to 589. The transfer review correctly
uses the actual Thor row and retains synthetic bodies only for the transfer and
reveal controls; it no longer appends another Thor response to the same book.

The new `ThorBatchBReviewTests` captures digest, RNG and pending work before any
query, tests a rejected foreign choice after costs have committed, then compares
completion against an unprobed control. `ThorSearchPrivacyReviewTests` executes
actual For Asgard and confirms restricted viewers receive no foreign private
choice or hidden candidate identifiers; the answering seat sees offered matches,
not unrelated hidden deck cards. The unchosen deck card is concealed again after
completion. This establishes candidate exposure scope/lifetime, not a native UI
claim or a general full-deck inspection interface.

Review evidence under the existing external evidence directory:

- `independent-review-focused.log`: 47 passed, including both new content tests,
  actual Thor/For Asgard, retained engagement and Worthy tests, and ability-data
  admission/lowering checks.
- `independent-review-privacy.log`: 5 passed, including the new actual-card
  privacy test and existing search/public-situation controls.
- `independent-review-commit.log`: normal pre-commit policy evidence.

An initial review fixture called the lower-level `CardPlay.Play` path rather
than the offered event action, and obtained no search prompt. The final probes
execute `runner.ExecuteAction` with the actual offered action, matching the
player-intent path. The logs above are the corrected passing runs. Existing
data-mutant logs were inspected as supplemental evidence; review did not rerun
those mutations. No engine or ability-book changes were made by the reviewer.

### Withheld probes: authority clarification

Hammer Throw says "deal 8 damage to an enemy and return Mjolnir to your hand."
`rr:and` makes those effects simultaneous; `rr:response` places the optional
Chase Them Down response after its triggering defeat. Consequently, observing
Mjolnir already returned at that response does **not** establish a timing bug.
The preserved probe's expectation that Mjolnir remain in play through that
response must be corrected before it can serve as an acceptance test. This
review does not approve the withheld row or its full attack/overkill behavior.

Get Over Here has separate damage and conditional engagement sentences.
`rr:ability.4` requires sentence order; `rr:defeat.1` discards a defeated minion,
and `rr:ability.1` prevents the later engagement from interacting with that
discarded card without explicit out-of-play text. The next implementation must
bind the same physical enemy while requiring it still be a living in-play minion
after damage and intervening timing, and skip engagement/Thor response on defeat.
It must also retain nonlethal transfer, same-seat exclusion and Aerial checks.
Whether this needs selector composition or an engine capability remains to be
proved in an isolated implementation packet; this review did not reintroduce the
withheld rows or rerun their probes.

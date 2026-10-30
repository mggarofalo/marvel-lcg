# Independent Power resource activation review

Candidate: `c1363aea356b7833a4158f3ffb39286c0b590fa0`, compared with `01f9ebca`. **No blocking correctness defect found in the reviewed payment paths; acceptance is bounded by the gaps below.** This review covers the Power resource quote and commitment, not Pym responses, Hank entry, native interaction, or replay compatibility.

## Authority and inspection

The current RR 1.8 `rr:cost.3.1` treats resources for an ability on a card as paid for that card. `rr:cost.4.1` excludes excess from payment; `rr:cost.5.1` gives the player the division between simultaneous costs. Vendored rulings `5e6ac90dc4187eef` and `b796aaa9fae1580b` support respectively wholly excess Power generating one and a Power contribution qualifying despite other excess. The changed reprint assertions query `PossibleResources` rather than claiming a conditional bonus is unconditionally generated; the changed same-card ability assertions follow cost 3.1. These quote assertions alone do not establish commitment.

The author's direct committed-generation tests distinguish zero and reduced-zero, a sole necessary Power, explicit Power versus ordinary-double contribution, and ambiguous omitted allocation. The standalone and event-arrow tests use real card classifications with synthetic ability bodies; they do not establish a complete printed-card journey. The ownerless synthetic quote is a separate internal boundary. Live-quote controls are synthetic; an actual suspended change is not established by those controls.

## Independent evidence

Windows Release tests with `MSBUILDDISABLENODEREUSE=1` and `-m:1`: the independent Content controls plus the candidate generation, ability, commit and Drop Kick selections passed 29 tests. The independent Content controls passed again after a forced non-incremental restored build, and after adding agenda count, current step and occurrence comparisons. They exercise one Power icon paid with its other icon excess, and ambiguous payment rejected before retry, comparing digest, RNG and agenda observations across rejection. They do not compare the subsequent decision with a separate unprobed world.

A compiled mutant requiring more than one allocated icon before activating the bonus failed the one-paid/one-excess control with `G` instead of `GG`. Source was restored in a `finally` block, a non-incremental Content test-project build succeeded with zero warnings, and both independent Content controls passed again. Logs: `Source/evidence/power-review-mutant.log`, `power-review-restored-build.log`, `power-review-restored-tests.log`, and `power-review-final-tests.log`.

Isolated synthetic allocation searches with one conditional source and four or eight distinct ordinary sources, all necessarily paying, passed under a 30-second process-tree deadline. Test-runner durations were 104 ms and 3 seconds respectively; whole process elapsed times were 3.80 and 6.13 seconds. Logs: `Source/evidence/power-review-four.log` and `power-review-eight.log`. The four-source control is retained; the larger case was removed from the routine suite after measurement. The larger fixture's ten-resource cost is not established as reachable on a supported Power-card path.

## Open review concerns

The new implicit-generation search tests legal assignments for a different output. Deduplication only combines identical icons from the same source. When every distinct source is needed and a conditional source necessarily pays, no different output exists, so permutations of distinct sources remain. The bounded synthetic measurement demonstrates scaling, not an established supported-game failure. Separately, hand-discard affordability removes the previous failed-state memoization and can invoke allocation search repeatedly across subsets, including without a conditional source. Reachable worst-case latency remains unmeasured.

The native actual conditional-resource interaction and MARVEL-870 replay decision remain necessary before publication. Independent tests do not prove a reachable suspended live-quote change, preservation of unrelated conditional source masks in a combined event price, or extra-query versus unprobed-control parity. Prior author's logs and the independent synthetic-contract closure are separate evidence.

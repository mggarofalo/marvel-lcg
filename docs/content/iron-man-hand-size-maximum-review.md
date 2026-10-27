# Iron Man hand-size maximum — MARVEL-867

Review of source commit `88f5640b`.

Iron Man (`01029a`) has printed hand size 1 and gains one for each Tech upgrade
he controls, with a maximum hand size of 7. Assess the Situation (`12033`)
grants +1 hand size until the end of the phase. Ruling
`ruling:4bf1b4ba524db1d6` states that Iron Man's maximum is 7 regardless of
how it is reached. The later 1.7–1.8 ruling `ruling:39c26b6b8d77edfd` confirms
that this is a total maximum, while Star-Lord's Helmet caps only its own bonus.
The distinction matters alongside `rr:max-maximum.6`'s general ability-local
maximum rule.

The production ability book changes only Iron Man's existing row. Assess the
Situation is represented by a test-local row; its production row and pack are
not admitted by this change. Live tests cover other heroes, multiple and
negative modifiers, face changes, blanking, expiration, and drawing to the
resulting hand size. The View test checks the Iron Man source and maximum step;
the Godot unit test checks its displayed wording. The generic hand-size
projection test is synthetic: there is no current end-to-end consumer of a
projected hand-size read.

Source verification logs under `C:/Users/mggar/Source/evidence/` record 52
focused Content, 8 View, and 5 Godot tests passing. Removing the live cap failed
three intended tests; applying the projection cap before a projected modifier
failed the synthetic order check (expected 6, actual 5). The restored source
was rebuilt, and normal pre-commit hooks passed with 66 architecture tests.
The Split Personality fixture failed at the pre-cap baseline `8162fd96`; the
source change runs pending work and asserts no prompt before retaining its
original face and draw assertions.

## Independent review finding

**Reject the generic projection contract pending a bounds-order repair.** Both
projection helpers floor the live complete value before applying projected
adjustments. In a synthetic state with printed hand size 1, live modifier -8,
projected modifier +10 and maximum 7, the complete result is 3; both helpers
return 7. The two failing theory cases and their output are recorded in
`C:/Users/mggar/Source/evidence/iron-cap-review-floor.log`. The early floor
predates this change, but the new generic maximum contract inherits it. This is
not evidence of a reachable played-card hand-size regression. A repair must
apply both bounds after the complete projected modifiers and retain the other
projection behavior.

## Bounds-order repair

The repair evaluates live contributions without a floor, then applies projected
adjustments before the minimum and maximum. Live evaluation retains its ordered
source steps and final bounds. A signed health adjustment remains signed; area
projection also uses signed-safe saturating addition for its projected modifier.
The synthetic contract covers both projection helpers, a recovering negative
base, a negative final hand size, maximum ordering, and signed health.

A compiled early-floor mutation failed both recovering-negative cases (expected
3, actual 7) and both negative-final cases (expected 0, actual 2). After source
restoration, a forced Release rebuild and all 13 Iron Man tests passed. Focused
regressions passed 135 Content projection/Iron Man/Split Personality cases,
6 Rules value-evaluation cases, 8 View cases and 5 Godot inspection cases.
Logs are under `C:/Users/mggar/Source/evidence/iron-bounds-*.log`. The generic
projection cases are synthetic; no current played-card hand-size projection
consumer is established by this evidence.

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

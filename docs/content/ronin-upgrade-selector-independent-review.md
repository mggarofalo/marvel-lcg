# Ronin upgrade selector independent review

Reviewed source commit `1616709b` for MARVEL-866. This is a selector
prerequisite, not an authored Ronin ability or public product admission.
Printed card `12013` gives Ronin +1 THW and +1 ATK while an upgrade is
attached. RR 1.8 `rr:upgrade.3` distinguishes upgrades attached to another
card; `rr:attachment` identifies a separate encounter card type, and
`rr:attach-to.1` describes the attached card's in-play lifetime. The vendored
MarvelCDB acquisition records no FAQ entry for `12013`.

The source tests cover attached and unrelated upgrades, encounter attachments,
other card types, cards beneath the source, hand cards, projected host changes,
entry and departure, a stale source incarnation, upgrade-to-minion effective
kind, and printed-text blanking on both Ronin and the upgrade. A test-local
Ronin constant checks both stats. Source evidence reports six focused tests
passing after a restored build; live-kind inversion and projected-host omission
mutants failed as expected. The source commit's normal hooks passed. Logs are in
`C:/Users/mggar/Source/evidence/ronin-selector-{test,mutant,projected-mutant,rebuild,restored,hooks}.log`.

The independent review adds a synthetic effective minion-to-upgrade profile,
two concurrent matches and projected source departure. It also checks ordering
after reattachment. The live evaluator traverses hosted area order while the
projected evaluator traverses creation order. Because `last` accepts this
selector, these can identify different physical cards: create two upgrades on
Ronin, move the first to hand and back, and query `last(upgradesOnThis)`.
Live picks the reattached `01074`; projection picks the synthetic effective
upgrade `01067`. The Release focused review test compiled and failed at that
assertion, after passing its earlier effective-kind and departure assertions;
see `C:/Users/mggar/Source/evidence/ronin-selector-review-failure.log`.
This finding affects general selector composition; Ronin's existential constant
does not depend on order. Verdict: reject source `1616709b` pending repair and
independent retest.

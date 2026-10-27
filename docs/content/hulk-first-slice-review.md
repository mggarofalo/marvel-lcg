# Hulk first slice independent review

Accepted for the bounded, nonpublic packet at
`9a143090f6a91f6240b13058c78a7034c09566df`, based on
`fdd16b0f9f3f7c72631ed1d2033db2f6d06ea386`. Doctor Strange PR 454 remains the
integration prerequisite. This review does not establish complete Hulk content,
public admission, native behavior or whole-game acceptance.

The actual `10001a`, `10001b` and `10004` rows agree with the printed local
card dataset. Enraged uses dynamic whole-hand selection in the forced-interrupt
tier, restricted to its controller's turn and hero form. Existing tests distinguish
optional interrupts and responses, both simultaneous forced-interrupt orders,
empty and oversized hands, Banner form and a non-first owner. Banner draws before
the selected discard and keeps its once-per-round limit across form changes.
Leap tests distinguish settled physical payment from mixed declarations, wild
declarations and unused generated icons.

Reviewed RR 1.8 forced clauses 1 and 4–6, interrupt timing, wild-resource clauses
1 and 1.1, and cost clause 4.1 against the actual implementation. Local
`ruling:5bbba476a198a90c` is marked pre-1.5 and directly addresses zero-price
Unstoppable Force. Its application to Leap's identical payment phrase is an
inference, explicitly described in the packet; current RR cost/wild clauses do
not contradict it. The Leap test comment now labels that inference directly.

Added `HulkMandatoryDiscardReviewTests`: a non-first Hulk cannot decline,
discard only part of its hand, or substitute another player's hand card.
Rejected answers retain the same prompt, active seat, digest, RNG state/word
count, agenda and effects. A valid retry discards the complete owner hand into
that owner's discard pile, preserves the other hand and reaches the end phase.
This closes an adversarial gap in the actual-card tests; no runtime change was
needed.

Independent verification on Windows:

- Nonincremental Release build of `Marvel.Content.Tests`: zero warnings/errors.
- All 16 affected existing test classes, Hulk identity/Leap/reviewer classes,
  the whole AbilityData family and RuleCitationTests: **158 passed**, none
  failed or skipped (12 seconds). The reviewer control also passed separately.
- Independently parsed raw UTF-8 JSON object spans from predecessor and candidate:
  all **685 predecessor objects byte-identical**, exactly `10001a`, `10001b`,
  `10004` appended in that order, and **688 unique faces** total.
- Verified all 16 previous current-book count assertions change only 685 to
  688. The only additional old-test changes bound the two Strange
  `Skip(682)` checks with `Take(3)`. Exact original ordered IDs and
  no-starting-counter obligations remain. AbilityData admits only the three
  named faces and still compares the entire exact authored inventory, so the
  bounded predecessor packet checks do not hide arbitrary later additions.
- Diff inspection confirms no engine, setup, recipe, public-admission or
  product-scope edits. `git diff --check` passes.

External local logs are `%TEMP%/hulk-review-build.log`,
`hulk-review-adversarial.log`, `hulk-review-affected.log` and
`hulk-review-raw-prefix.log`; the exact combined filter is retained as
`hulk-review-filter.txt`.

Reviewed the implementation's six data-mutant failure logs and mutation driver;
these are implementation evidence, not independently repeated experiments.
The ForcedResponse mutant fails specifically on the optional-interrupt case;
its optional-response case passes, consistent with the documented initial
survivor and subsequent strengthening. The other logged mutants distinguish
global turn ownership, fixed four-card discard, an absent Banner limit,
physical-includes-only payment and empty payment qualification.

The implementation's full Content log records **7,495 passes and one failure**
from the unbounded Strange suffix. Its subsequent 53-case closure is recorded
separately. This review reran the complete affected classes after that correction;
it did not repeat the full Content suite and claims no final full-suite green.

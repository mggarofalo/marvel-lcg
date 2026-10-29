# Pym Particles actual card independent review

Reviewed `3d16f976da4383065eeb9d5a4ffc7d593165595a` against
`05e52f511c33ec509210bc94945aa0ef840b583a`. The printed `12006` text,
current `rr:initiating-abilities.step.5` and `.step.6`, `rr:cost.4.1`,
`rr:response`, and rulings `46eab153f9f2813b` and `6cb0530e539c7915`
support the row and timing. The earlier spend-mechanism review covers nested
payment, shared priority, and physical incarnation; its event-cancellation
fixture is synthetic. No card correctness finding was identified.

The previous 752 objects and 762,565-byte prefix match the base, including
SHA-256 `2028158d3fea58fe0ca4512acc3f8932b6ce763da5b1ba8c6cebb80fc115b91a`.
The transferred three form cases retain their assertions. The marker fixture
removes the actual row before inserting its own; standalone synthetic catalogs
remain separate, and catalog parsing still rejects duplicate card rows.

Two additional actual-row cases spend a non-first player's physical copy on an
Alliance payment and check the contributor's Tiny draw or Giant healing, the
other player's unchanged hand and damage, and the paid ally's continuation.
Windows Release passed all nine actual-card cases. A valid runtime JSON mutant
changing the Tiny draw recipient from `you` to `firstPlayer` failed the new
Tiny case with eight controls passing. Exact book bytes were restored, followed
by a non-incremental build and nine passing cases. Logs are
`Source/evidence/pym-actual-review-{focused,mutant,restored-build,restored-tests}.log`.
These are direct payment fixtures, not a public Ant-Man journey. The source
packet's 61 Content/data and 12 Server checks and its quantity mutants are
separate evidence on the source revision. Complete-pack, public gameplay,
native and replay acceptance remain outside this review.

# Team-Building Exercise engine checkpoint

The synthetic `playSharedHeroTraitCard` instruction implements a bounded
permanent-card branch and a separately bounded event-action branch. It is an engine-chosen DSL spelling, not an authored
12024 ability or public product admission. Its source must be an Action with
exhaust-this as its sole cost; the instruction must be the first effect. Other
source cost and effect shapes fail lowering. The event-action checkpoint and its explicit limits are recorded in
[team-building-event-invoke-checkpoint.md](team-building-event-invoke-checkpoint.md).

The source Action offers only affordable legal permanents in its controller's
hand sharing a live trait with their hero. Admission excludes the source as a
resource generator because the source exhausts before the choice. A scoped
one-resource reduction applies to the selected card's ordinary price, including
host-specific modifiers and the zero floor; it is not a resource. The selected
host, payment, form and traits are revalidated before mutation and information
clearing. Ordinary permanent play owns payment and entry; the source's effect
continuation waits for that child work, including suspended generator costs.

The synthetic tests cover a real payment and zero cost, live trait and form
changes, stale payment and host rejection, host-specific prices, source-generator
exclusion with an independent generator control, and unsupported source programs.
They check pending state, digest, agenda, RNG and information for stale answers.
A generator defeat interrupt shows that entry and the following parent draw wait,
then continue once. The Action resolution remains pending through suspended
payment and resolves on child completion. `AbilityResumedEffectCompletion` settles
the committed outcome. A separate immediate `ResolveEffect` call was removed;
its omission passed the synchronous status and suspended-payment tests. The
observed status does not establish whether that call has any effect in every
admitted context, so the continuation contract remains subject to review.

Focused Rules and Content/payment/host regression runs and a compiled mutant
omitting suspension are recorded in `C:/Users/mggar/Source/evidence/` under
`team-building-permanent-finish-*`. The mutant failed the suspended-generator
continuation test. Additional final verification is recorded under
`team-building-permanent-close-*`. The actual 12024 row, canonical ability book and public inventory remain outside this checkpoint. Independent
review is required before integration.

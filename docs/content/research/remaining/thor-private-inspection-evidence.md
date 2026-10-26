# E6 private inspection prerequisite

Synthetic engine capability only; Heimdall (`06020`) and Thor remain outside
this packet's admission and ability-book scope.

`{"inspectDiscardOrderEncounterTop":3}` accepts counts 1–3. An explicit Look
answer captures the encounter area's exact physical top group and incarnations
in the existing continuation memory. Previews before that commitment disclose
no hidden tops. The answering player then discards one captured card and answers
an ordinary ordered target request containing every survivor. The first selected
survivor becomes the next encounter card. The looked-at group stays in its deck;
the effect does not search, refill a short group, or shuffle unless discarding
the last card invokes the ordinary encounter reset. Membership and incarnations
are checked before each answer. Invalid selections preserve state, RNG and agenda.

Authority: generated printed `06020`; RR1.8 `rr:look-looked-at`, `.1`, `.1.1`,
`rr:discard.2`, `.5`, and `rr:encounter-deck.1`–`.3`; vendored ruling
`ruling:2001c6705b63bf4f` confirms that only the resolving player looks. The FAQ
snapshot inventories `06020` without a card-specific clarification. The primitive
does not create a discard-trigger timing tier: the current engine has no generic
WhenDiscarded trigger. A separate authored alteration requiring such a tier needs
implementation and authority review; it must not be inferred by the renderer.
As with other suspending choices, dependent `then`/`otherwise` composition raises.

`OrderedEncounterInspectionTests` proves 1/2/3/4-card groups, middle discard,
survivor order, non-first-player ownership, restricted-seat privacy and expiration,
pre-inspection preview purity, foreign/duplicate selection rejection, stale
incarnation rejection, empty-deck non-admission and immediate final-card reset.
Together with existing inspection regression tests: 18 passed. The compiled mutant
that applies submitted survivor order as bottom-first fails the 3/4-card cases;
restored source passes after rebuilding.

Raw logs are outside the checkout under `%LOCALAPPDATA%/Temp/`:
`thor-e6-mutant.log`, `thor-e6-restored.log`, `thor-e6-final.log`,
`thor-e6-toolrestore.log`, `thor-e6-hooks-final.log`.

Pending: actual Heimdall response/payment/entry proof after content authoring;
native interaction proof and focused owner review if the working interaction is
materially new. Independent mechanism, Decisions/Protocol and synthetic durable
save/restart evidence is recorded below. No session injection bypass was added for
this synthetic ability. Existing `AreaReordered` wire shape records chosen deck
order; the current session frontier conservatively classifies that event as a
random-information boundary even when RNG consumption is zero. This is existing
frontier policy, not a claim that ordering itself consumes RNG.

## Independent E6 review

Reviewed mechanism revision `a94418dbd1383991d170772f9d75fc3d56120835` in an
isolated worktree. Generated `06020`, RR1.8 look/discard/encounter-deck clauses,
and `ruling:2001c6705b63bf4f` agree with the private physical-group interpretation.
No authority or mechanism correctness finding remains in the reviewed scope.

Independent synthetic evidence adds `OrderedInspectionAdversarialTests`: invalid
decline/foreign discard; missing, duplicate, foreign, discarded and oversized
order targets; successful exact retry; preserved digest, RNG, agenda and events;
changed group order, area membership and reincarnation at both stages; exact
bottom preservation, no shuffle or acceleration, public discard visibility and
private inspection expiry. Focused inspection tests: **29 passed**.

`OrderedInspectionReplayTests` adds **5 passing** cases through production
`EngineHost`, `SessionSaveJson`, `SessionReplay` and restart. These cover 1–4
encounter cards through in-process transport and a three-card socket case.
Pending discard and pending order reconstruct identically to uninterrupted
digest, prompt and complete RNG state. The real Decisions composer rejects a
partial two-card order and retains submitted top-first order across Protocol.
The supported durable factory supplies a deterministic synthetic ability; this
is not actual Heimdall response/payment/entry or public Thor setup evidence.

The existing `AreaReordered` frontier classification remains conservative and
safe against undo crossing private order knowledge. It does not establish RNG
consumption; the independent nonempty-survivor cases assert unchanged RNG words.
The absence of a generic WhenDiscarded tier remains an explicit limitation.
No ability book, admission boundary or engine source was changed by this review.

Raw independent logs are outside the checkout under `%LOCALAPPDATA%/Temp/`:
`thor-inspection-review-server.log`, `thor-inspection-review-view.log`,
`thor-inspection-review-tools.log`, `thor-inspection-review-hooks.log`.
Tool restore and normal pre-commit hooks passed, including **66 architecture
policy tests**, complexity and file length.

Actual-card proof, native interaction/comprehension evidence, and any materially
new UI owner review remain outstanding. The author-reported compiled order
mutant was not independently rerun. This review accepts the synthetic mechanism
scope, not Thor or Heimdall product readiness.

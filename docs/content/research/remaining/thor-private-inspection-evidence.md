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

Pending: independent authority/code review; actual Heimdall response/payment/entry
proof after content authoring; Decisions/Protocol/native interaction proof and
focused owner review if the working interaction is materially new; save/restart
through the production session seam. No session injection bypass was added for
this synthetic ability. Existing `AreaReordered` wire shape records chosen deck
order; the current session frontier conservatively classifies that event as a
random-information boundary even when RNG consumption is zero. This is existing
frontier policy, not a claim that ordering itself consumes RNG.

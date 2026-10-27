# Terminal event cleanup repair

Candidate: `fix/victory-event-cleanup`, based on `3166260c521bd8ae5f5551aad109d7dbba07c1cb`. The exact delivered commit is recorded in the handoff; the final build, regression and mutant logs below cover the source delivered in that commit.

The shared event cleanup condition excludes a finished world, covering action, window and paid-cost restart callers without duplicating guards. Power completion stops before consuming modifiers or scheduling aftermath. An ordered ability batch completes the current execution handle, then stops before beginning the next ability. These boundaries preserve synchronous victory and ordinary nonterminal cleanup.

The retained independent event action/control checks that the winning event remains in the resolving area while the draw control discards normally. Two additional synthetic admitted contracts distinguish power and batch completion: a terminal Attack retains its one-use attack modifier and stops the following counter effect; a terminal ally entry ability leaves its stunned status untouched instead of starting the next Attack-labelled ability. Paired controls consume the modifier/status and finish normally. These tests establish engine shapes, not printed Kang effects or public pack completion.

The initial broader run found two existing labelled-power cases that ended the game before their claimed continuation assertions. Diagnostic events show that paying from an otherwise empty player deck resets the payment card and deals one encounter card; Hydra Soldier's When Defeated then deals the second/final encounter card. The world ends `PlayersLose / EncounterCardsExhausted`, leaving the soldier in play under immediate terminal semantics. The genuine pre-feature base `e7e3e7456585221d69ed3e9dec72644880a9c7126` passes the original two cases, but an added outcome observation proves the same terminal loss there while later departure and damage still continue. This is a hidden terminal fixture, not a claim that the original assertions already failed on that base. The corrected fixture supplies one player-deck card before payment and asserts `Outcome.Unfinished`; every original continuation, damage, modifier and discard assertion remains.

Evidence lives outside the repository under `C:/Users/mggar/Source/evidence/`:

- `victory-event-cleanup-probes.log`: compiled initial probes, 2 failures and 4 passes; event repair passes while power and batch defects are demonstrated.
- `victory-event-cleanup-restored-regression.log`: intermediate candidate, 253 passed / 3 failed. One failure exposed batch-handle completion ordering; the other two exposed the terminal fixture described above.
- `victory-event-cleanup-diagnostic.log`: corrected batch placement, 6 cleanup probes pass; two temporary labelled-fixture observations identify encounter exhaustion and causal events.
- `victory-event-cleanup-genuine-base.log`: unchanged pre-feature fixture, 2 passed.
- `victory-event-cleanup-genuine-base-diagnostic.log`: same base with an added unfinished-outcome observation, 2 failures reporting `PlayersLose / EncounterCardsExhausted` after soldier departure.
- `victory-event-cleanup-final-mutant.log`: omission of all three cleanup guards compiles; all 3 victory cases fail on the intended card/status/modifier observations, all 3 controls pass.
- `victory-event-cleanup-final-build.log`: restored sources rebuilt nonincrementally, zero warnings/errors.
- `victory-event-cleanup-final-regression.log`: 256 passed, including complete PlayersWin, Kang, repetition, continuation-binding, ordered-discard, rule-citation, attack-aftermath and labelled-power classes selected by the recorded command.

The combined mutant establishes the observed terminal boundaries as a family; it does not establish independent necessity of overlapping guards. A preliminary probe compile failed on an incorrect fixture enum name and is not behavioral mutation evidence. All temporary diagnostics and mutant sources were restored before final verification; the genuine-base worktree is clean.

Static audit found that each-player completion constructs an inert transition entering already guarded resumed traversal. No each-player guard was added. Sequence health/status settlement has no demonstrated reachable post-victory mutation in this packet, so no speculative guard was added. Direct playersWin choice options retain their documented pre-prompt unsupported boundary. No Agenda, AbilityRunner, ability-book, public-admission, capability or projection changes belong to this repair. No full managed suite, native review, complete-pack acceptance or second-platform verification is claimed. Independent acceptance remains with the original reviewer.

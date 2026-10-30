# Terminal instruction continuation repair

Repair candidate: `fix/victory-terminal-continuation`, based on `be5a16bd693a0a588df50d1314d7946e852ace3d`.
Addresses the synthetic immediate-exhaust finding in [the independent review](kang-final-victory-independent-review.md).

The interpreter stops later effect nodes once the world has a terminal result and skips post-node constant/health cleanup. Resumed traversal stops before advancing remaining structural frames or completing their cleanup. The each-time discard traversal also stops: its next discard calls the card-state executor directly rather than entering the normal effect-node boundary.

Synthetic regressions cover immediate exhaustion after victory, flat and nested suspended threat choices, and each-time encounter discards. Nonterminal controls retain exhaustion, all threat placements, both discards, and normal defeated-ally departure. The restored regression run includes the actual Standard/Expert Kang III cases and existing Kang, repetition, continuation-binding, ordered-discard and rule-citation tests.

Evidence lives outside the repository under `C:/Users/mggar/Source/evidence/`:

- `victory-terminal-restored-initial.log`: 10 passed.
- `victory-terminal-interpreter-mutant.log`: invalid attempt; CS0162 prevented compilation. This is not a killed behavioral mutant.
- `victory-terminal-interpreter-mutant-compiled.log`: omission of the interpreter and resumed-traversal terminal guards compiled; 4 victory cases failed on immediate exhaustion, 4 controls passed.
- `victory-terminal-discard-mutant.log`: omission of the discard-loop guard compiled; the terminal each-time case discarded 2 cards instead of 1, while 3 controls passed.
- `victory-terminal-restored-build.log`: restored sources rebuilt nonincrementally, 0 warnings/errors.
- `victory-terminal-restored-regression.log`: 119 passed after that rebuild.

The interpreter mutant tests the terminal boundary as a family; it does not establish a separately necessary behavioral effect for each overlapping guard. Direct `playersWin` options still fail closed during partial-resolution admission before a choice prompt; the DSL documentation now states that boundary. No ability-book, public admission, capability or projection changes, broad managed suite, native review, complete-pack claim or second-platform verification belongs to this packet. Independent review of the repair remains with the original reviewer.

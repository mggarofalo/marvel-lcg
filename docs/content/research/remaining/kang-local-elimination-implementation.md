# Kang local elimination implementation

Base: `c91cd4f26ecb0e317a9dba51703da4b6e76b6e85`.
Branch: `feat/kang-local-elimination`. The implementation revision is the commit
containing this evidence record. This is a nonpublic engine capability; Kang
production admission and authored ability rows remain unchanged.

Authority: actual generated card faces `11009b`–`11012b` complete when all
players at that stage are defeated. Their Forced Response stores one physical
Dominion immediately, retaining the local scheme and Kang until phase end.
`rr:player-elimination.step.5` removes the player and lets survivors continue;
`rr:player-elimination.4` ends the game when all players are eliminated.
MC11 game-area rules preserve local reach and move side schemes on reunion.

The engine chooses a narrow representation for local encounter components whose
player carrier is eliminated: transition-owned placement captures retain their
original zone, incarnation and game area. The player still detaches and normal
ownership/minion cleanup runs. Retained scenario roots and hosted descendants
survive until the actual departure. A committed join moves those retained cards
in physical-id order and emits movement events, then drops the captures.
Incarnation checks prevent a departed/reentered copy from regaining its capture.
This adds no serialized digest format; game-area metadata remains outside v3.

Seven focused cases cover Standard/Expert local failure through Dominion storage,
phase-end removal, side-scheme reunion and survivor central advancement; a live
local player preventing completion; terminal all-player loss; a stale retained
copy; pending clean-departure capture shrinkage; and suspended-choice rejection.
Fixtures use actual cards with a test-only ability overlay, not public admission.
The existing malformed eliminated-seat and arbitrary stale-topology guards remain.

Explicit unsupported boundary: local elimination while a Kang destination/order
choice is suspended raises before the elimination mutates player/cards/topology.
It does not silently rewrite retained choice captures. The rejection test compares
state, events, RNG and pending work, then completes the original join normally.

Verification on this Windows worktree, Release, node reuse disabled, serial builds:

- Restored nonincremental Content build; all 159 Kang tests passed.
- Serialized rules elimination/place regression filter: 29 passed.
- Seven compiling mutants killed: omit completion (2 failures), omit retained
  scenario preservation (4), omit incarnation guard (1), omit suspended-choice
  preflight (1), count initial seats at central reunion (3), complete with a live
  local player (3), and omit captured-group shrinkage (1).
- The first retention mutant failed compilation (CA1822) and is excluded from
  mutation evidence. The replacement compiled and failed intended assertions.
- The first focused compile caught an invalid test-zone enum and was corrected.
- An early Content/Rules run overlapped accidentally; final regression evidence
  above was rerun serially after restoration. No mutant verdict uses overlap.
- Initial normal hooks passed 66 architecture checks, then found complexity 12
  in capture and 11 in elimination. Hosted traversal was extracted and the
  retained-card exclusion expressed in the cleanup enumeration. Final regression
  and normal commit-hook outcomes are recorded in the handoff with this revision.

Raw logs are outside the repository under
`C:/Users/mggar/Source/kang-local-*.log`; mutation scripts and their exact anchors
are `kang-local-mutations.ps1` and `kang-local-retention-mutant.ps1` there.
Final runs use `kang-local-closure-content.log`, `kang-local-closure-rules.log`
and `kang-local-commit.log`.

Remaining: independent authority/source review; more complex simultaneous defeat
and completion compositions; complete setup/admission/public session replay;
broader integrated managed/platform gates and native interaction evidence.
Passing this packet does not establish complete Kang readiness.

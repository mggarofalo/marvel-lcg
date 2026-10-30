# Scheme-defeat replay evidence on the Doctor Strange candidate

This review covers candidate `cec4af78`. With the corrected Crossbones' Machine Gun,
both current legal games now change at the gun's first attack; see
[the Machine Gun replay boundary](crossbones-machine-gun-replay.md).

The seed-13 Crossbones/Spider-Man archive remains unchanged, with SHA-256
`2BC9FEAF2A6E11B9ADA12343C09FCEFDACB029A85A10E2BADFB89FB0DC339F55`.
It records 89 decisions selected by `campaign-research` v1, policy seed 279,
and a VillainWins result. These are historical regression facts, not rules
authorities. No runtime, replay contract, content or archive changes are needed.

`FrozenAllyDefeatGameRejectsAtFirstPaidStunCancellation` verifies the original
setup events and canonical digest, then every prompt, event and fingerprint
through decision 15. At the verified decision-16 prompt Spider-Man has stun 96
and plays Haymaker (01087, object 41), paying Genius (01089, object 43), without
choosing a target. The original three reveal/payment events remain exact.
The current engine additionally moves stun 96 to the removed area, detaches it,
and discards Haymaker. Those three events exactly match historical decision 17.
All card health and gameplay RNG consumption remain unchanged. The next prompt
is the turn menu, so the historical Haymaker target answer is never submitted.
The production journal comparison rejects with
`step 16 events diverged: expected 3, got 6`.

The Rules Reference requires status priority (`rr:status-cards.2`), payment
despite stun cancellation (`rr:stun-stunned.5`, `rr:cancel.2`), no attack
occurrence (`rr:stun-stunned.7`), and discarding the canceled event
(`rr:cancel.3`). Settling the status replacement before asking for the effect
target is the engine's chosen interaction order. A separately reconstructed,
fully verified prefix with only stun removed produces exactly the historical
three events and target prompt. This control is explicitly synthetic and does
not establish a played game or acceptance of the historical replay.

On that candidate, a fresh seed-13 game submitted the original intentions except
obsolete answer 17. Every submitted answer first matched its full current prompt.
Decision 16 produced exactly the combined historical 16/17 events and decision-17
state fingerprint, and every later event list and fingerprint matched the archive.
That 88-decision game reached the original VillainWins outcome and canonical
terminal digest, and its in-memory journal replayed independently. With the
corrected Machine Gun, a current game diverges at decision 45. The exact 88-decision
route now runs only as the causal control
`PreCorrectionMachineGunRowReproducesTheFrozenAllyThwartGame`, which reverts the
gun's trigger binding. Neither archive is rewritten.

The coverage retains the original scope: Surveillance Team removes threat from
Crossbones' Assault from 2 to 1 at historical decision 87; Black Cat's basic
thwart removes the last threat at 88, and the forced Crossbones scheme places
four threat on the main scheme (4 to 8), completing it. The exact eight events
retain Black Cat's thwart attribution and Crossbones' activation target, Peter
Parker. The game ends during this forced activation, before later defeat cleanup.
The adjacent unchanged `SchemeDefeatReplayBoundaryTests` retains the separate
seed-10 complete legal game in which Surveillance Team defeats the side scheme,
with exact defeat events and a full current journal replay. Seed 13 does not
claim a support defeat of its own.

Release validation runs and mutation checks are recorded in the delivery evidence
outside the repository. The archive hash and citation-resolution checks remain
executable assertions. This regression closure does not claim full Doctor
Strange content or product acceptance.

## Independent regression review

The independent review of candidate `cec4af78` accepts this replay-regression
closure. The review checked the vendored status, stun, cancellation and labeled
ability rules, the printed Haymaker and Genius records, both scheme-defeat test
classes, the production journal comparators and the candidate diff. There are no
blocking findings within this scope. The candidate changes only tests and
documentation; the archived seed-13 bytes and adjacent seed-10 tests are unchanged.

The independent Release build and focused run passed all six tests, with no
failures or skips:

```powershell
dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release --filter 'FullyQualifiedName~SchemeDefeatGameRegressionTests|FullyQualifiedName~SchemeDefeatReplayBoundaryTests|FullyQualifiedName~RuleCitationTests'
```

The distinguishing evidence includes the exact historical prefix and first
divergence, the synthetic unstunned control, the prompt-checked 88-decision game
of that candidate, explicit Black Cat thwart/forced-scheme consequences, its full
terminal digest and independent journal replay, and the separate positive
Surveillance Team defeat game. No historical target answer is submitted to a
different current prompt, and no positive defeat obligation is replaced by a
rejection-only test.

This review did not repeat the delivery's full acceptance sweep or compiled
cancellation mutation. Those remain separate delivery evidence. It establishes
acceptance of this regression closure on Windows, not platform certification,
broader content readiness or UI/product acceptance.

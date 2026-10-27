# Scheme-defeat regression journals

`scheme-defeat-10.jsonl.gz` and `scheme-defeat-13.jsonl.gz` record complete legal
Crossbones/Spider-Man games from implementation checkpoint
`93ad1744` for MARVEL-637. Their game seeds are 10 and 13; research-policy seeds
are 276 and 279. Surveillance Team defeats Crossbones' Assault at decision 49
in the first game; an ally basic thwart defeats it at decision 88 in the second.
The games contain 90 and 89 decisions respectively.

The `campaign-research` v1 policy selected the decisions. The acceptance test
replays those recorded decisions through the production engine and journal
comparisons; it does not regenerate or certify that research policy. The JSON
records are retained verbatim from the run logs, with LF separators and
deterministic gzip compression. They are regression evidence, not rules
authorities. Authority-derived focused tests in `SchemeDefeatResponsibilityTests`
and `AllyControllerPowerTests` establish the intended attribution.

Do not replace recorded outcomes to accommodate a changed engine. Investigate
divergence, preserve the original evidence, and make any compatibility decision
explicit. Running tests reads these files without modifying them.

The seed-10 history is explicitly incompatible with the current damage-window
eligibility correction. `SchemeDefeatReplayBoundaryTests` pins its original gzip
SHA-256, verifies setup and every prompt, event and fingerprint through decision
29, then verifies the original four-event prefix at decision 30. Crossbones has
strength 2 against Spider-Man's defense 3; the attack completes with zero damage
instead of offering the recorded Backflip. Strict production journal comparison
rejects the changed seven-event result. The research policy is not an admitted
simulation-replay policy, so these tests compare its recorded decisions directly.
The seed-13 history is incompatible at decision 16: the paid Haymaker attempt
removes Spider-Man's stun before an effect-target prompt. Its unchanged archive,
exact historical prefix and complete current legal route are documented in
[the seed-13 replay evidence](../../../docs/content/doctor-strange-scheme-replay.md).

A separate current legal game uses the seed-10 decision intentions, with Backflip
at 31 applied to the ensuing minion's positive damage, and intentions 75/76
ordered as minion selection followed by Backflip. It retains the exact support
defeat events at 49, completes all 90 decisions, and replays every new prompt,
event, RNG count and fingerprint to the same current terminal state. The new
journal exists only in test memory; neither historical recording is rewritten.

`PhotonicBlastGameRegressionTests` uses generated current games. Comparing the
11/277 game against `d649560f` first diverges at decision 7: defense reduces the
attack to zero, so Cosmic Flight is not offered. The lethal first-stage
Crossbones Photonic Blast now occurs at 37 (health 5 to 0), retains its draw and
stage transition, and continues to the complete game's replay. The minion case
at 1/267/14 remains, with explicit lethal damage and defeat checks for both cases.

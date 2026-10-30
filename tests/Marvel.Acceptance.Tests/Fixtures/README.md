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

Replay v29 records The Power of Justice (01062) as one {G} with a conditional {G}
for Justice costs; both journals record the unconditional `GG` (first at seed-10
decision 21 and seed-13 decision 15). `FrozenPromptCompatibility` projects the
current prompt record back to that one spelling before comparing it with a
recorded prompt. It changes no other prompt field, and events, RNG and state
fingerprints remain strict. The seed-10 boundary test proves that the strict
comparison still rejects the recorded spelling.

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
at 31 applied to the ensuing minion's positive damage. It retains the exact support
defeat events at 49. The new journal exists only in test memory; neither historical
recording is rewritten.

Both current legal games now meet Crossbones' Machine Gun (04064). Crossbones II's
reveal attaches it (seed 10 at 68, seed 13 at 43), and its forced interrupt binds the
attacked player (`rr:you-your.7`); the recordings come from a book whose trigger
never matched. At seed-10 decision 72 the recorded first twelve events remain exact;
Laser Rifle (04072) and the gun are then simultaneous forced interrupts, so the
first player orders them (`rr:forced.5`). The explicit decision puts Laser Rifle
first, keeping its recorded result, then the gun removes an ammo counter, discards
card 78 and deals three indirect damage to Spider-Man. At seed-13 decision 45 the
recorded nine events remain exact, followed by the gun: one ammo counter, card 57
(04067) discarded, two indirect damage. Strict comparison rejects both changed event
lists. The discard shifts every later encounter card, so the recorded intentions stop
applying: the `acting` policy, seeded with the recording's policy seed, completes
each game (77 and 71 decisions, both PlayersLose). Every new prompt, event, RNG count
and fingerprint replays to the same terminal state. This continuation is a generated
current game, not a derivation of the historical choices.

The seed-13 ally thwart at 88 is no longer reached by a current game. A causal
control reverts only the 04064 trigger binding to the scenario controller; every
recorded fact after the obsolete step-17 answer, the ally thwart and the terminal
digest then reproduce exactly. The control is not a current game and admits no
recorded history. [The Machine Gun replay note](../../../docs/content/crossbones-machine-gun-replay.md)
records the trace and compatibility decision.

`PhotonicBlastGameRegressionTests` uses generated current games. With the corrected
Machine Gun, the 11/277 game first differs from `1d78bc2d` at decision 18, where the
gun fires at Crossbones' attack, and it no longer contains a lethal Photonic Blast.
The first-stage case is now 49/315: its lethal Crossbones Photonic Blast at 18
(health 2 to 0) retains the draw and stage transition, and the gun Crossbones II
attaches later fires before the complete game replays. The minion case at 1/267/14
never reaches the gun and remains, with explicit lethal damage and defeat checks for
both cases.

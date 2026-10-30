# Crossbones' Machine Gun replay boundary

Crossbones' Machine Gun (04064) reads: "Forced Interrupt: When Crossbones attacks you,
remove 1 ammo counter from this card and discard the top card of the encounter deck →
take indirect damage equal to the number of boost icons on the discarded card." Its
trigger bound `"player":"you"`, the scenario controller, so it never matched. It now
binds `trigger.player`: for "attacks you", "you" is the attacked player
(`rr:you-your.7`). `CrossbonesMachineGunAttackTests` proves the corrected card in a
real two-player attack. This note records how the correction meets existing
regression games and saves.

## Acceptance games

Every divergence below was compared with the same game under `1d78bc2d`, the
predecessor with the old 04064 row. Each first difference is the gun's first firing,
at Crossbones II's first attack after his reveal attached the gun.

- `scheme-defeat-10` current legal game: decision 72. The recorded first twelve
  events remain exact. Laser Rifle (04072) and the gun are then simultaneous forced
  interrupts, which produces a new ordering prompt for the first player
  (`rr:forced.5`). Previously, Laser Rifle resolved alone without a prompt.
- `scheme-defeat-13` current legal game: decision 45. The recorded nine events remain
  exact, then the gun removes one ammo counter, discards card 57 (04067), and deals
  two indirect damage to Spider-Man, health 8 to 6.
- Photonic Blast 11/277: decision 18. The gun discards a card with no boost icons.
  The game then ends at decision 44 without a lethal Photonic Blast.

The gun resolves before the boost card is dealt (`rr:interrupt.3`,
`rr:attack-enemy-activation.step.1`). Spider-Man controls no other character, so all
indirect damage is his (`rr:indirect-damage.1`).

## Compatibility decisions

The frozen journals are unchanged. Their frozen-prefix tests stop at earlier
boundaries (decisions 30 and 16) that the gun does not reach.

The seed-10 current legal game keeps its intentions through decision 72, including
the exact support defeat at 49. It asserts the boundary, then submits an explicit
order with Laser Rifle first. This keeps Laser Rifle's recorded result ahead of the
gun's new events. The seed-13 game keeps its intentions through decision 45 and
asserts the gun's five events. In both games the discard shifts every later
encounter card, so the recorded intentions stop applying. The `acting` policy,
seeded with the recording's policy seed, completes each game (77 and 71 decisions,
both PlayersLose). The complete in-memory journal then replays exactly. This
continuation is a generated current game. It is not derived from the historical
choices.

The seed-13 ally thwart at decision 88 is no longer reached by a current game.
`PreCorrectionMachineGunRowReproducesTheFrozenAllyThwartGame` reverts only the 04064
trigger binding. Under the current engine, every recorded fact after the obsolete
step-17 answer then reproduces exactly, including the ally thwart and the terminal
digest. This proves the gun row is the only cause of the divergence. It also keeps
the complete-game ally-thwart evidence. It is a causal control. It is not a current
game, and it does not admit the recorded history.

The Photonic Blast first-stage case moves to 49/315. Its lethal Photonic Blast at
decision 18 takes Crossbones from 2 health to 0, keeps the draw and the stage
transition, and the gun fires later in the same replayed game. The minion case
1/267 never reaches the gun.

## Replay identity

This correction does not change `engine-replay-v29`. Under
[the release policy](../release-policy.md), the replay contract changes when "the
same setup and decision trace may resolve differently". The runtime dataset identity
changes separately when any byte of `abilities.json` changes.

The packet has one engine change. A nested ability whose damage eliminated the
attacked player threw `InvalidOperationException` ("the continuation owner is not
on the agenda") when it tried to resume. It now resumes after its own effect
windows (`rr:player-elimination.5`). The fallback applies only when the owner's seat
is eliminated and its activation has ended; any other missing owner still throws.
Before this change, every trace that reached this point failed, so no accepted v29
trace contained it. Every accepted v29 trace therefore resolves exactly as before.
That is why v29 is kept. The ability dataset hash changes, and that already decides
save admission:

- A v29 save made with the `1d78bc2d` datasets records the old ability hash. A
  restore needs matching dataset hashes or a named additive content migration.
  `ContentUpgradeAdmission` names only the frozen `91098022` triple, so such a save
  is quarantined unchanged. It is never replayed under the corrected row.
- The frozen v19/v20/v21 named routes require the current datasets to keep every
  admitted baseline entry ([save content upgrades](../save-content-upgrades.md)).
  04064 is an admitted RoRS entry, so changing it invalidates that admission. The
  repository datasets at `1d78bc2d` already differ from the baseline in seven
  admitted ability entries (01003, 01004, 01017, 01029a, 01075, 01078 and 01189).
  The production repository datasets therefore had no frozen admission before this
  change. The server route tests use the extracted frozen content and are
  unaffected.

As with any dataset change, the release needs a new product version. No migration
route, quarantine fixture or identity string changes.

# Ranked pair enemy attack prerequisite

Synthetic composition on the selected-character owner head
`2bccd3fba47ddd82c0f52dbaf9a8565f1d1bb27b`; no actual ability-book row,
setup record or public Hulk admission is included.

The local generated 10028 text directs the highest-ATK enemy to attack the
highest-ATK hero or ally, with first-player ties and surge if no attack was made.
Current RR 1.8 decides ties (`rr:first-player.1`), exact ally recipients and
defense changes (`rr:attack-enemy-activation.1.1–.3`, `.4`), and stun replacement
(`rr:stun-stunned.6–.7`). Historical Clash rulings are unnecessary.

The executable initial composition retained the highest-ATK character as the
outer `chosen` binding and supplied ranked enemies to `enemyAttacks`. Release
compilation succeeded; `baseline.log` fails because the enemy request requires
both tied enemies (`Min = 2`) rather than exactly one. Nesting another chooseCard
would replace `chosen`; no general named bindings are needed.

The extension adds only `chooseEnemy: "firstPlayer"` to the typed activation
instruction. Its enemy choice uses the existing target request and structural
answer path without changing `chosen`, runner or agenda. Omission retains the
ordinary collection behavior. The printed attack is one instruction, with no
separate earlier enemy-selection effect; choosing the character and then the
enemy establishes its pair before initiating that attack. Empty enemy pools
do not offer a useless character choice. A disappeared typed choice is rejected
as an unsupported stale continuation, preserving state and pending work.

`RankedEnemyActivationTests` covers both ties with non-resolving first player,
exactly one scheduled enemy, owned-by-zero allies controlled by distinct players,
undefended selected-ally damage, a different player's defending ally, unique
modified hero/ally and enemy maxima without selection prompts, stun removal and
otherwise surge, empty enemies and stale enemy choice purity. The stale snapshot
includes digest, full RNG state/consumption, current frame/occurrence, pending
agenda, continuous effects and attack/activation state.

Evidence is external at
`C:/Users/mggar/Source/marvel-evidence/hulk-ranked-attack/`:

- `baseline.log`: compiled pre-extension failing composition (1 failed).
- `focused.log`: 34 passed (8 ranked cases, selected-ally, selected-character
  boundary, complete typed-activation and nested activation-order classes).
- `mutant-chooser.log`: compiled resolving-player chooser mutant killed by both
  tied cases (2 failed, 6 passed).
- `mutant-count.log`: compiled all-enemy answer-count mutant killed by both tied
  cases (2 failed, 6 passed).
- `restored-build.log` / `restored.log`: forced nonincremental Release build,
  then the same focused selection passes (34 passed).

The surge checks use direct synthetic reveal and drain real activation and surge
work, asserting the extra encounter card is dealt to the original resolving
player even when the selected character belongs to another player. They do not
claim full actual-card encounter entry/discard cleanup. Actual 10028 authoring,
full encounter lifecycle, public setup and independent review remain separate.

## Independent prerequisite review

Reviewed source `c662142903580603d0f2927911f2fc4e1a1751e1` against the
generated 10028 text and current RR 1.8 first-player, enemy-attack, stun and
Tough entries. No production defect was found within this prerequisite's scope.
The singular enemy and recipient belong to one printed attack instruction.
Establishing the recipient before the attacker is an engine sequencing choice:
both ties are settled before attack initiation, with no intervening effect or
replacement of the outer selected-character binding. The card does not prescribe
a separate enemy-selection effect before recipient selection.

The source cases distinguish first-player authority, a single selected enemy
from the default all-enemy ordering, modified current ATK maxima, exact selected
hero/ally recipients, changed defenders, departed choices and empty enemy pools.
Existing selected-character boundary cases also reject a departed/reentered
selected incarnation before scheduling; their accepted ancestor repair is retained.

The independent additions cover two further observations:

- A completed attack prevented entirely by Tough still suppresses surge, for
  both a selected hero and a selected ally. The card asks whether an attack was
  made; Tough prevents damage rather than replacing the attack.
- Before the first enemy-description query, snapshot digest, complete RNG state
  and consumption, agenda/frame/occurrence, effects and attack/activation state.
  Repeated descriptions and declined, empty or all-enemy answers preserve that
  snapshot. The valid retry completes with the same state and RNG as a separate
  unprobed control, with damage on the original selected ally.

Independent evidence is external at
`C:/Users/mggar/Source/marvel-evidence/hulk-ranked-attack-review/`:

- `focused.log`: 37 passed across the full ranked, selected-ally,
  selected-character-boundary, typed-activation and nested-activation-order
  classes, including the three new cases.
- `mutant-damage-completion.log`: compiled mutation of `activationMade` from
  `result.Made` to positive damage placement; both Tough controls fail on the
  incorrectly dealt surge card (2 failed, 9 passed).
- `restored-build.log` and `restored.log`: nonincremental Release rebuild after
  restoring the production source, followed by the same 37 passing cases.

The review accepts this engine prerequisite only. Actual 10028 authoring and
whole-encounter entry/surge/discard cleanup remain the actual-card author's
obligation. This review adds no ability-book row, public setup/admission,
presentation acceptance, integration or broader pack-completion claim.

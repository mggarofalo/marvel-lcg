# Black Widow B2: retained Stealth Strike defeat

Base: `f601932b`, 2026-10-26. This is a nonpublic engine prerequisite,
not production authoring or Black Widow admission. Printed authority is
`datasets/cards/cards.json`, face `08013` (Stealth Strike). Its printed
three-resource Hero Action deals four attack damage; the threat removal
requires that this attack actually defeated its enemy.

## Implementation

`AbilityDamageCompletion.Defeats` retains settled attack-occurrence defeat
tuples for ordinary attacks as well as staged attacks. The tuple identifies
the physical card and its defeated incarnation. Recipient snapshots serve
staged attack aftermath and are not a requirement for retaining an ordinary
attack's actual defeat result.

The test-local full printed composition chooses the enemy, resolves one labeled
attack, and then checks `wasDefeatedByDamage(chosen)` before choosing a scheme.
This retains the causal enemy binding while permitting either current eligible
scheme. The scheme choice belongs after the complete attack. The attack body
contains only its damage instruction. No suspension guard, projection traversal,
overkill ordering, ability-book row or public admission changes.

The initial trial with this outer composition failed both ordinary lethal
controls because the ordinary attack receipt was absent. Moving the scheme
choice inside the labeled attack remains explicitly unsupported, verified
before payment with unchanged state/RNG/pending work.

## Evidence

`StealthStrikeContinuationTests` provides eleven cases:

- Ordinary lethal minion and Hydra Soldier defeat both offer threat removal;
  surviving Madame Hydra and actual authored Loki healing offer none.
- Both main and side schemes can be selected, directly and after a synthetic
  defeat response suspends for its own option choice. Threat is unchanged
  throughout that response. A synthetic reentry during the response preserves
  the original defeated incarnation without attributing defeat to the new copy.
- The scheme continuation retains the exact defeat tuple; a scheme still in
  the encounter deck is rejected without changing the card digest, RNG state,
  pending work or receipt, and the valid answer succeeds on retry.
- Zero threat finishes without a scheme prompt. Repeated direct choice queries
  preserve state/RNG/pending work and produce the same final state and event
  trace as an otherwise identical unprobed resolution.
- An arbitrary scheme choice inside the labeled attack still fails before costs.

The compiled distinguishing mutant substitutes four damage taken for actual
defeat. Both the surviving Madame Hydra and healed Loki cases fail by offering
unearned threat removal (two failed, seven passed). The production source was
restored and rebuilt with `--no-incremental`; the restored regression selection
passed 168 tests. Two additional final tests then passed with all eleven local
Stealth Strike cases. The final combined selection is recorded at handoff.

External logs are under `C:/Users/mggar/AppData/Local/Temp/bw-stealth-*`:
`initial.log`, `receipt.log`, `expanded.log`, `arithmetic-mutant.log`,
`restored-build.log`, `restored-focused.log`, and `final-local.log`.
The regression selection includes `PowerTrace`, `PowerContinuationDependency`,
`ContinuationBindings`, `LabelledPowerDefeatContinuation`, `LokiRr18Overkill`,
and `Preparation`, alongside Stealth Strike.

## Remaining proof

Independent review and integrated broader gates remain required. This packet
does not include durable JSON replay/host-restart evidence. The supported
`IDurableGameFactory` injection seam used by `PreparationReplayReviewFactory`
can supply this same nonpublic row for that evidence. No public Black Widow
setup journey is claimed.

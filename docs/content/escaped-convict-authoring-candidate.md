# Escaped Convict authoring candidate

The four canonical rows 07009/07024/07038/07053 implement the generated printed text using a captured least-threat villain, first-player tie choice, independent counter movement, hero-form and historical attack-ancestry conditions, and a delayed boostless attack. Interpretation follows the user's resolution of vendored ruling `8369361114d2929b` ([Hall of Heroes, ruling 191](https://hallofheroeslcg.com/latest-ffg-rulings-post-rrg-1-5/#191)): a standalone scheme has no this attack, while an attack-descended scheme retains ancestry; activations remain sequential.

This is a focused authoring candidate, not scenario admission or pack acceptance. The engine's deferred-target admission permits the selected villain's independent attack when moving the counter to its current holder has no effect.

`WreckingEscapedConvictTests` covers all four ordinary boosts, hero-form standalone schemes, alter-ego, first-player tied choice against a different player, no additional boost draw, physical discard ownership, printed minion statistics, and Surge dealing an additional encounter card. The canonical Waiting attack -> scheme -> Convict chain checks attack ancestry in hero and alter-ego form and verifies activation order. Its hero damage is seven: Waiting makes Wrecker's attack four and Bulldozer's printed ATK is three.

`WreckingEscapedConvictReferenceTests` adds synthetic perturbations at an optional attack-end response on the original villain. The checkpoint observes that `world.Attack` is null and only the original attack has started; its end boundary event retires after the response. Moving the counter cannot retarget the captured villain. Replacing the selected A stage with its own Extreme B stage follows that same character; removing the selected character schedules no replacement attack. These are arranged lineage controls, not played Extreme scenarios.

Focused verification on the candidate tree passes all 19 Convict cases. Rule-citation validation also passes. Logs: `C:/Users/mggar/Source/escaped-convict-finish-final.log`.

Four temporary canonical-row mutations are killed by behavioral assertions (the dataset is restored byte-for-byte afterward):

| Mutation | Failing cases |
|---|---:|
| Remove attack-ancestry condition | 4 |
| Remove hero-form condition | 1 |
| Deal boost cards for delayed attack | 7 |
| Resolve current `query: villain` instead of captured `chosen` | 3 |

Mutation logs and summary: `C:/Users/mggar/Source/escaped-convict-mutant-{ancestry,hero,boost,target}.log` and `escaped-convict-mutations-summary.log`. The target mutation's three failures are the counter, promotion, and departure controls. Full Content and complete-game suites are left to the integration boundary rather than duplicated for this packet. Independent review remains required before acceptance.

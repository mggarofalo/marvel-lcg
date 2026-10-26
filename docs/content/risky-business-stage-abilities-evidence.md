# Risky Business stage and environment authoring (MARVEL-689 / MARVEL-690)

Bounded research packet based on `fd311be9`. This implements the eight faces
`02001a/b`, `02002a/b`, `02003a/b`, and `02006a/b`. It does not complete Risky
Business, MARVEL-690, Green Goblin, scenario setup, or public admission.

Authority: generated `datasets/cards/cards.json`; the local mc02 new-rules
clauses on changing form and defeating a villain stage; RR indirect damage,
each player, per-player icon, When Revealed and villain defeat.

Norman faces explicitly identify native attack/damage replacement ownership in
their row notes. Goblin faces author the actual distinct When Revealed damage
and identify native scheme replacement ownership. Environment faces declare
ordinary per-player entry counters and identify native zero-counter flip/pool
initialization ownership. No duplicate DSL interrupt or synthetic no-op ability
stands in for these native clauses.

`RiskyBusinessStageAbilityTests` uses `AuthoredCards.Runner()` on nonpublic
research boards. Its 19 cases cover every stage reveal in both player forms,
multiplayer allocation restricted to the resolving player's characters, first
player 1 choosing the order, stage III direct damage with allies present,
same-side promotion to stages II and III, and both entry pools at 1/2/4 players.
Promotion cases use an explicitly synthetic lethal state and occurrence to
isolate stage entry, including Norman, from Norman's damage replacement.

Release focused verification: 57 passed, 0 failed. Filter combines this class,
the authored-data admission/sentinel checks, and existing Content GreenGoblin
regressions. Command:

```
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter "FullyQualifiedName~RiskyBusinessStageAbilityTests|FullyQualifiedName~AbilityDataAnUnauthoredCardSaysSoRatherThanDoingNothingTests|FullyQualifiedName~GreenGoblin" --nologo
```

Targeted data mutations were restored after verification. Stage II indirect to
direct: killed (1 failing case). Stage III damage 4 to 3: killed (4 failing
cases). Criminal Enterprise perPlayer true to false: killed (2 failing cases).
The distinguishing multiplayer test also rejects a global friendly-character
selection in place of characters controlled by the affected player.

Remaining MARVEL-690 scope includes main-scheme faces 02004a/b and 02005a/b,
encounter cards 02007–02013, complete scenario recipes/modes and their integration
evidence. Whole-pack admission, Mutagen Formula and remaining modular content
remain separate. This packet supplies no native UI, save/restart or complete-game
claim. Broader combined-candidate checks belong to the lead integration lane.

## MARVEL-690 encounter packet — 2026-10-25

Implemented 02007–02013 against generated `datasets/cards/cards.json` and the
Green Goblin deep research record. This is a nonpublic research-board packet;
main schemes, setup, admission and product acceptance remain outside its claim.
It extends reviewed stage/environment base `dfb25d44` without engine edits.

Hired Gun offers the resolving player a facedown villain boost versus 2 infamy;
when Criminal Enterprise is absent, only the boost instruction can resolve.
The five printed shared boost abilities place infamy or remove madness from the
live environment face. All in a Day's Work removes two madness when available
and the last single counter otherwise, using fixed-count DSL branches because
the removal primitive requires sufficient counters. Native counter exhaustion
handles the physical flip. Oscorp Manufacturing adds its per-player reveal
threat only during Norman's form. Mad Genius selects hero-form identities by
remaining health, delegates ties to the first player, uses the actual completed
activation receipt for surge, and mills only the revealing player's deck during
Norman's form. Printed guard and scheme icons remain card-fact behavior.

`RiskyBusinessEncounterAbilityTests` has 23 passing cases covering each shared
boost in both environment forms, two-counter placement/removal and last-counter
flip, Oscorp in both forms, both Hired Gun options and unavailable-infamy choice,
nonzero resolving seat ownership, Norman's infamy-count mill, least-health attack,
stun cancellation surge, nonzero first-player tie ownership and no-hero surge.
`GreenGoblinBoostFlipTests` now executes the actual authored Payoff rather than a
test-local approximation; its existing mid-attack Norman-zero-ATK assertions pass.

Focused Release run: **26 passed, 0 failed**, including those classes and
`RuleCitationTests`. Command:

```
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter "FullyQualifiedName~RiskyBusinessEncounterAbilityTests|FullyQualifiedName~GreenGoblinBoostFlipTests|FullyQualifiedName~RuleCitationTests" --nologo
```

Two data mutants were tested with `--no-build`, reading the modified ability
book at runtime, and restored byte-for-byte in a `finally` block. Oscorp's
Norman condition changed to Green Goblin: killed by both form cases. Mad Genius
minimum changed to maximum remaining health: killed by the attack case. Temporary
logs are `%TEMP%/risky-encounter-tests.log`,
`%TEMP%/risky-encounter-mutant-oscorp-form.log` and
`%TEMP%/risky-encounter-mutant-mad-genius-rank.log`.

Remaining integration work includes the main schemes/setup packet and independent
review of this exact encounter diff. This focused evidence does not establish
public admission, full scenario play, restart/native behavior or whole-pack
completion. Broader combined-candidate checks belong to the lead integration lane.

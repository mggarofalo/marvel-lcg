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

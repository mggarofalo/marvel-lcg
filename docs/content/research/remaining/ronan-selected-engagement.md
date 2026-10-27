# Ronan selected-hero engagement composition

MARVEL-567. Baseline: `b753c463a6baa3756a954d08db3ae40336c190d3`.
This test-only packet establishes an existing composition for the selected-hero
engagement body. It changes no engine, production ability book, setup or admission.
The test-local `WhenRevealed` trigger isolates body execution; Ronan's real forced
villain-phase-start timing remains a separate prerequisite.

The earlier handoff at `a3296a796ea57169f826a3343205f736d2109dde` proves that
the proposed spelling `engagedWithChosenPlayer` is rejected. That result alone
does not establish a missing engine capability. Its claim of two missing Ronan
prerequisites is provisional and superseded for selected-hero engagement by this
executable composition:

```json
{"chooseCard":{"chooser":"firstPlayer",
  "from":{"minBy":{"of":{"query":"heroes"},"by":"remainingHealth"}},
  "effect":{"eachPlayer":{"order":"playerOrder","effect":{"if":{
    "test":{"and":[{"isYourIdentity":"chosen"},{"isInPlay":"chosen"},
      {"inForm":{"player":"you","form":"hero"}}]},
    "then":{"putIntoPlay":{"card":"this","where":"engagedWithYou"}}
  }}}}
}}
```

`eachPlayer` preserves the chosen physical identity and ability source while
scoping `you` to each live player. Only the selected identity's current seat
passes the guards. `order: playerOrder` avoids the otherwise unnecessary
first-player ordering prompt; all other seats have no effect. The order cannot
alter the one eligible seat's engagement. The explicit in-play and hero guards
prevent a departed or flipped selected identity from becoming a destination.
The chosen incarnation binding prevents departure/reentry from substituting
another incarnation. The source remains `this`, independently of all these seats.

The existing `AbilityDeckAndRevealExecution.PutIntoPlay` minion branch schedules
`EncounterEntry`; its in-play branch uses `MinionEngagement.Transfer`. This is
the engagement path used by Thor 06014 Get Over Here!, retaining minion state
and incarnation instead of entering play again. Current RR 1.8 `rr:engage.3`
prevents repeat same-player engagement. `rr:first-player.1` requires all tied
eligible heroes, including the current engagement, to remain selectable.

`RonanSelectedEngagementTests` proves actual Ronan movement to either tied hero,
current-seat selection as a no-op, a unique minimum distinct from resolver and
first player, unique current minimum, no heroes, lower-health alter-ego and ally
exclusion, and selected-identity form change/departure/reentry before the scoped
continuation. Ronan retains damage, exhaustion, counters, confused status,
attachment host and incarnation, and does not gain fresh Toughness.

A synthetic Vulture source executes the identical body with actual Quickstrike
and the production Thor response. Only actual seat movement causes Quickstrike
and Thor's draw; the original minion retains damage/incarnation, and Hawkeye's
entry response does not fire. Existing `ThorEngagementReviewTests` additionally
exercise the shared entry/transfer lifecycle. These are nonpublic research boards,
not a played Ronan modular set or full-phase proof.

Verification command:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~RonanSelectedEngagementTests|FullyQualifiedName~RuleCitationTests|FullyQualifiedName~ThorEngagementReviewTests'
```

Evidence: `C:/Users/mggar/Source/evidence/ronan-selected-engagement-test.log`.
The focused Release run passed **16/16**, zero skipped: ten new composition cases,
four existing Thor engagement review cases and two citation checks.
No new production decision is added, so this packet introduces no engine mutant.
Integration still requires review of this composition and the separate phase-start
capability before authoring Ronan's production row.

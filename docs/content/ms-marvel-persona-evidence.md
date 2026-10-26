# Ms. Marvel persona and resource packet

Base revision: `10d071dd201573ca2158c3aa0f331bf64ee3f880`. This packet
authors 05008 Nakia Bahadir and 05009 Biokinetic Polymer Suit only. The actual
ability inventory is 551 distinct rows, including nine Ms. Marvel faces.
No engine, setup, public admission or Red Dagger changes are included.

Authority: joined `datasets/cards/cards.json` printed 05006–05009 text;
vendored FAQ and rulings were inspected. The event-versus-chosen-ally cost
distinction follows ruling:5b44b2adcc60d4b7. The earlier deep-research document's
capability observations describe its historical revision, not this candidate.

Before row authoring, characteristic synthetic Nakia and existing event-restricted
resource tests passed 14 cases. An initial expiry probe called generic EndOfPhase;
the selected-player reducer explicitly uses EndOfPlayerPhase. The retained actual
test calls PhaseEnd.EndPlayerPhase, proving the reachable expiry boundary.

Actual-row tests prove Nakia's alter-ego/readiness requirement, exhaustion,
player-specific pricing, repeated-query purity including RNG/pending work,
form-change survival, unused phase expiry, rejected-payment preservation and
consumption by one actual paid Big Hands. Suit tests prove event and event-ability
cost contexts, rejection for allies/upgrades/absent context, controller and hero
form, one wild icon, atomic failed payment, actual paid Big Hands and exhaustion.
These are nonpublic engine fixtures and do not claim a full pack journey.

Six temporary actual-row mutants loaded and failed behavioral assertions:
Nakia amount 2, omitted exhaustion, omitted form restriction; Suit omitted event
restriction, omitted exhaustion, omitted form restriction. Dataset bytes were
restored exactly. Logs and summary: `%TEMP%/msm-persona-mutant-*.log` and
`%TEMP%/msm-persona-mutants-summary.json`. The restored focused command passes
53 cases:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~MsMarvelPersonaTests|FullyQualifiedName~MsMarvelSuitCardTests|FullyQualifiedName~AbilityData|FullyQualifiedName~AbilityProgramTests'
```

Final restored run: `%TEMP%/msm-persona-final.log`. Normal pre-commit hooks
remain enabled; their result and the exact delivered commit are in the handoff.
Broad managed lanes and independent review remain with the integrating lead.

Aamir 05006 remains unauthored. `AbilityCardInstruction` has no deck-bottom
operation; `AbilityEffect`/`AbilityEffectLowering` expose ShuffleInto, which
necessarily shuffles, and no selected-card bottom placement. A prerequisite must
move the exact selected own-discard card to the bottom, report completion for
printed "then draw", preserve ordering and cover empty-deck/reset behavior.

Bruno 05007 remains unauthored as a whole card. `storeBeneath`, `beneathThis`
and ordinary return-to-hand movement support storage and retrieval mechanics,
but `AbilityStructuralLowering.ChooseCardEffect` accepts only from/effect/chooser
and binds one card. Existing grouped primitives are specialized:
ChooseDiscardToShuffle selects own discard then shuffles; DiscardHandCards
selects a fixed hand-card group then discards; counted attacks select enemies.
None can select zero through three exact cards from beneathThis for one grouped
retrieval. The engine prerequisite needs a bounded optional group with pre-mutation
validation, resolver/controller ownership, hidden-card secrecy and continuation
semantics. Repeated single-card mutations would not establish that contract.

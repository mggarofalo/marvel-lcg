from . import *

# Library Labyrinth

def GetAbilities() -> Sequence['Ability']:

    def library_labyrinth(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 5, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            library_labyrinth,
        ).SetCostFunc(CostFunc.DealPlayerEncounterCard(
            1, "Initiator"))
        .SetTarget(MainScheme)
        .SetName('"This way?"')
        .LimitOncePerRoundPerPlayer(),
    ]


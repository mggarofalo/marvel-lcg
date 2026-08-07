from . import *

# Goblin Reinforcements

def GetAbilities() -> Sequence['Ability']:

    def goblin_reinforcements(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        size = len(Worlds.GetOnFieldMinions(effect, CardFinder2("GOBLIN")))
        this.PlaceThreatOnSchemes([this], size, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            goblin_reinforcements
        ),
    ]


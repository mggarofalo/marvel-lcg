from . import *

# Naughty Children

def GetAbilities() -> Sequence['Ability']:

    def naughty_children_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        value = FacesCounter.GetPrintedResourcesTypes(player.hand_cards.GetAll())
        this.PlaceThreatOnSchemes([this], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            naughty_children_revealed
        ),
    ]


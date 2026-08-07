from . import *

# Time Stone

def GetAbilities() -> Sequence['Ability']:

    def time_stone_revealed(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        player = message.GetToPlayer()
        if player:
            faces = player.DiscardDeckTopCards(4, effect)
            value = FacesCounter.GetDifferentTypesCount(faces)
            this.PlaceThreatOnSchemes("MainScheme", value, effect)

        PlaceThisCardInTheInfinityStoneDeckDiscardPile(effect)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            time_stone_revealed
        ),
    ]


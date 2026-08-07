from . import *

# Reality Stone

def GetAbilities() -> Sequence['Ability']:

    def reality_stone_revealed(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        player = message.GetToPlayer()
        if player:
            player.DiscardControlCards(effect, ally=True, upgrade=True, support=True)

        PlaceThisCardInTheInfinityStoneDeckDiscardPile(effect)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            reality_stone_revealed
        ),
    ]


from . import *

# Mind Stone

def GetAbilities() -> Sequence['Ability']:

    def mind_stone_revealed(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        player = message.GetToPlayer()
        if player:
            identity = player.GetIdentity()
            if identity.IsConfused():
                player.DiscardRandomHandCards(1, effect)
            else:
                Faces.GiveStatus([identity], "Confused", effect)

        PlaceThisCardInTheInfinityStoneDeckDiscardPile(effect)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            mind_stone_revealed
        ),
    ]


from . import *

# Upper Manhattan - D

def GetAbilities() -> Sequence['Ability']:

    def upper_manhattan(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        player = message.GetToPlayer()

        if player:
            player.DiscardHandCards((1, 1), effect)
            symbiote = Worlds.FindCardOnField(
                effect,
                trait="SYMBIOTE",
                card_type=Environment
            )
            if symbiote:
                player.DiscardDeckTopCards(4, effect)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            upper_manhattan
        )
    ]


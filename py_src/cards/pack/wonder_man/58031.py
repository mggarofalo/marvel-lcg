from . import *

# Cameo

def GetAbilities() -> Sequence['Ability']:

    def cameo(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        ally = Search.Collection(
            effect,
            initiator,
            card_type=Ally,
            card_class="IdentitySpecific",
        )

        if ally:
            Faces.ShuffleAllTo([ally], initiator.player_deck, effect)
        
        initiator.DiscardHandCards((2, 2), effect)
        Faces.RemoveAllFromGame([this], effect)


    return [
        AbilityFactory.WhenCardSetup(
            "This",
            cameo
        ),
    ]


from . import *

# * Iron Man: Tony Stark

def GetAbilities() -> Sequence['Ability']:

    def iron_man(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            card_type=Upgrade,
            trait="TECH",
        )
        if face:
            Faces.AddToHand([face], initiator, effect)

    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            iron_man
        ),
    ]


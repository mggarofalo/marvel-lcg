from . import *

# * Beast: Hank McCoy

def GetAbilities() -> Sequence['Ability']:

    def beast(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            card_type=Resource,
        )
        if face:
            initiator.GainCard(face, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            beast
        ),
    ]


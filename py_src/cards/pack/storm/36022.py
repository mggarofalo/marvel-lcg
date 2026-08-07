from . import *

# * Forge

def GetAbilities() -> Sequence['Ability']:

    def forge(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            traits=["X-MEN", "X-FORCE"],
            card_type=Support
        )
        if face:
            initiator.GainCard(face, effect)

    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            forge
        ),
    ]


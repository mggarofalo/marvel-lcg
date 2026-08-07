from . import *

# * Wiccan: William Kaplan

def GetAbilities() -> Sequence['Ability']:

    def wiccan(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            card_class="IdentitySpecific",
            card_type=Event,
        )
        if face:
            Faces.AddToHand([face], initiator, effect)


    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            wiccan
        ),
    ]


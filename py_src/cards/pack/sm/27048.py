from . import *

# * Ghost-Spider: Gwen Stacy

def GetAbilities() -> Sequence['Ability']:

    def ghost_spider(effect: 'Effect', message: 'Message.WhenCardLeavePlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            card_type=Event,
            card_class="IdentitySpecific"
        )
        if face:
            initiator.GainCard(face, effect)

    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_you_control_trait="WEB-WARRIOR"),
        AbilityFactory.WhenCardLeavePlay(
            AbilityType.Interrupt,
            "This",
            ghost_spider
        ),
    ]


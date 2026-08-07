from . import *

# Soaring Hearts

def GetAbilities() -> Sequence['Ability']:

    def soaring_hearts(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=False,
            include_discard_pile=True,
            card_class="IdentitySpecific",
            card_type=Event,
        )
        if face:
            initiator.GainCard(face, effect)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            soaring_hearts
        ).SetPlay().SetLabel()
        .SetTarget("TeamUp"),
    ]


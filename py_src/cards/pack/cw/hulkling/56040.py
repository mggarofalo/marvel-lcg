from . import *

# Shapeshifter

def GetAbilities() -> Sequence['Ability']:

    def shapeshifter(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            trait="SHAPESHIFT",
            card_type=Upgrade
        )
        if face:
            Faces.AddToHand([face], initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            shapeshifter
        ).SetPlay().SetLabel(),
    ]


from . import *

# For Asgard!

def GetAbilities() -> Sequence['Ability']:

    def for_asgard(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            trait="ASGARD",
        )
        if face:
            initiator.GainCard(face, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            for_asgard
        ).SetPlay().SetLabel(),
    ]


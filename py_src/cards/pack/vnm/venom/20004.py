from . import *

# Locked and Loaded

def GetAbilities() -> Sequence['Ability']:

    def locked_and_loaded(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            trait="WEAPON",
            card_type=Upgrade
        )
        if face:
            initiator.GainCard(face, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            locked_and_loaded
        ).SetPlay().SetLabel(),
    ]


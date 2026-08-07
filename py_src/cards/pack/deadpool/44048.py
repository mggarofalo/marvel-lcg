from . import *

# Mulligan

def GetAbilities() -> Sequence['Ability']:

    def mulligan(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DiscardAllHands(effect)
        initiator.DrawUp(initiator.hand_size, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            mulligan
        ).SetPlay(you_have_played_another_card_this_phase=False).SetLabel(),
    ]


from . import *

# Spiritual Meditation

def GetAbilities() -> Sequence['Ability']:

    def spiritual_meditation(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(2, effect)
        initiator.DiscardHandCards((1, 1), effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            spiritual_meditation
        ).SetPlay(only_if_your_identity_has_trait="MYSTIC").SetLabel(),
    ]


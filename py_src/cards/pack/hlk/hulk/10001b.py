from . import *

# * Bruce Banner

def GetAbilities() -> Sequence['Ability']:

    def bruce_banner(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)
        initiator.DiscardHandCards((1, 1), effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            bruce_banner
        ).SetName('Experimental Research')
        .LimitOncePerRound()
    ]


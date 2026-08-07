from . import *

# Forward Momentum

def GetAbilities() -> Sequence['Ability']:

    def forward_momentum(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        value = 3
        if HavePlayedEventThisTurn(initiator, "ATTACK"):
            value = 5
        this.RemoveThreatFromSchemes(effect.targets, value, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            forward_momentum
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]


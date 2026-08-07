from . import *

# Grand Strategy

def GetAbilities() -> Sequence['Ability']:

    def grand_strategy(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp("Max", effect)
        Faces.RemoveAllFromGame([this], effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            grand_strategy
        ).SetPlay().SetLabel()
        .SetHasNoTargetEffect(),
    ]


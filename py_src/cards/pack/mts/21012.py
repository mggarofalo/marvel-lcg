from . import *

# * Power Man: Victor Alvarez

def GetAbilities() -> Sequence['Ability']:

    def power_man(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.GainUntilPhaseEnd(
            effect,
            attack=2
        )

    return [
        AbilityFactory.ThisEnterPlayWithCounters(2, 'chi'),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            power_man
        ).SetCostFunc(CostFunc.Counter("This", 1, 'chi')),
    ]


from . import *

# Command Team

def GetAbilities() -> Sequence['Ability']:

    def command_team(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            command_team
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'command'))
        .SetTarget(Ally, canbe_ready=True)
    ]


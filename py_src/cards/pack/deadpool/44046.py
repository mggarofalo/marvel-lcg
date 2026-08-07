from . import *

# Break Time

def GetAbilities() -> Sequence['Ability']:

    def break_time(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.HealthUnits(effect.targets, "All", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            break_time
        ).SetPlay().SetLabel()
        .SetTarget(Identity, range="All", canbe_heal=True),
    ]


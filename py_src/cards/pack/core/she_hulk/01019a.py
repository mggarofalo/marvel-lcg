from . import *

# * She-Hulk

def GetAbilities() -> Sequence['Ability']:

    def she_hulk(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.Response,
            "You",
            she_hulk,
            to_this_form=True,
        ).SetName('"Do You Even Lift?"')
        .SetTarget(Enemy),
    ]


from . import *

# One-Two Punch

def GetAbilities() -> Sequence['Ability']:

    def one_two_punch(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.Response,
            "You",
            one_two_punch,
        ).SetPlay().SetLabel()
        .SetTarget(name="She-Hulk", canbe_ready=True),
    ]


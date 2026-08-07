from . import *

# Chase Them Down

def GetAbilities() -> Sequence['Ability']:

    def chase_them_down(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Event)
        this.RemoveThreatFromSchemes(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.Response,
            "YourHero",
            Enemy,
            chase_them_down,
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]


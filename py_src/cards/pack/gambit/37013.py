from . import *

# Operative Skill

def GetAbilities() -> Sequence['Ability']:

    def operative_skill(effect: 'Effect', message: 'Message.WhenUnitWouldThwart') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.RemoveAdditionalThreat(1, effect)


    return [
        AbilityFactory.WhenUnitWouldThwart(
            AbilityType.Interrupt,
            "You",
            operative_skill,
        ).SetCostFunc(CostFunc.Counter("This", 1, 'operative')),
    ]


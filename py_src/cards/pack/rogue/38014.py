from . import *

# Judoka Skill

def GetAbilities() -> Sequence['Ability']:

    def judoka_skill(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.would_atk_message.GainATKForThisAttack(-2, effect)


    return [
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.Interrupt,
            "You",
            judoka_skill,
            against_who=Enemy
        ).SetCostFunc(CostFunc.Counter("This", 1, 'judo')),
    ]


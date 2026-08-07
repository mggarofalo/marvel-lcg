from . import *

# Leadership Skill

def GetAbilities() -> Sequence['Ability']:

    def leadership_skill(effect: 'Effect', message: 'Message.WhenUnitWouldThwart|Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.trigger.GainForThisActive(effect, message, attack=1, thwart=1)

    return [
        AbilityFactory.WhenUnitMakeThwart(
            AbilityType.Interrupt,
            Ally,
            None,
            leadership_skill,
            is_basic_thwart=True,
        ).SetCostFunc(CostFunc.Counter("This", 1, 'leadership')),
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.Interrupt,
            Ally,
            leadership_skill,
            is_basic_attack=True,
        ).SetCostFunc(CostFunc.Counter("This", 1, 'leadership')),
    ]


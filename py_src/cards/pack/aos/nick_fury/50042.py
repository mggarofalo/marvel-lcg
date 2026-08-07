from . import *

# EM Shield

def GetAbilities() -> Sequence['Ability']:

    def em_shield(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.PreventDamage("All", effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Interrupt,
            "You",
            em_shield,
            is_from_attack=True
        ).SetLabel('defense')
        .SetCostFunc(CostFunc.Discard("This")),
    ]


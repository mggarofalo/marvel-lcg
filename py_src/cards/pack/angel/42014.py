from . import *

# Aerial Intervention

def GetAbilities() -> Sequence['Ability']:

    def aerial_intervention(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        num = initiator.DeclareNumber(1, 3)
        message.PreventDamage(num, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Interrupt,
            None,
            aerial_intervention,
            is_from_attack=True
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Exhaust("YouControlUnit", trait="AERIAL")),
    ]


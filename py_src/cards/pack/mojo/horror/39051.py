from . import *

# Vampire

def GetAbilities() -> Sequence['Ability']:

    def vampire(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        heal_message = this.HealHealth(this.sustained, effect)
        if heal_message and heal_message.value:
            pass
        else:
            Faces.GiveStatus([this], "Tough", effect)

    return [
        AbilityFactory.UpdateAttacksDealDamage(
            with_piercing=True,
            deal_damage_to="This",
            update_damage_rule="Double",
        ),
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            None,
            vampire,
        ),
    ]


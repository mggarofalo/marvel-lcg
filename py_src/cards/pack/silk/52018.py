from . import *

# Energy Shield

def GetAbilities() -> Sequence['Ability']:

    def energy_shield(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        value = effect.GetPaidResources().GetColor("Y")
        message.PreventDamage(value, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            Unit2
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Interrupt,
            "AttachedCharacter",
            energy_shield
        ).SetCost(Cost("Y", same_type=True)),
    ]


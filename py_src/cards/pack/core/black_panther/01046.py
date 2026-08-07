from . import *

# Energy Daggers

def GetAbilities() -> Sequence['Ability']:

    def energy_daggers(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Upgrade)
        target = effect.targets[0].CastTo(Identity)

        damage = 1
        if effect.this == message.sequence[-1]:
            damage = 2

        villain = Worlds.FindVillain(effect)
        units = [villain] if villain else []
        units += target.GetControlByPlayer().GetEngagedMinions()
        this.DealDamage(units, damage, effect)

    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            energy_daggers
        ).SetTarget("Players")
    ]


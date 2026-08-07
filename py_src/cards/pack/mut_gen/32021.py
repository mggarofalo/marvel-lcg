from . import *

# Shadow and Steel

def GetAbilities() -> Sequence['Ability']:

    def shadow_and_steel(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.PreventDamage("All", effect)
        this.DealDamage([message.attacker], 4, effect)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroInterrupt,
            Enemy,
            shadow_and_steel,
            conditions=[
                lambda effect, message:
                    not message.IsBePrevent(),
            ],
        ).SetPlay().SetLabel('attack', 'defense')
        .SetTarget("TeamUp"),
    ]


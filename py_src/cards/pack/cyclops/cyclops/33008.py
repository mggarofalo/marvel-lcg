from . import *

# Full Blast

def GetAbilities() -> Sequence['Ability']:

    def full_blast(effect: 'Effect', message: 'Message.WhenEffectWouldResolve') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        def action(effect: 'Effect', message: 'Message.WhenUnitWouldAttack'):
            message.DealAdditionalDamage(8, effect)
            message.GainOverKill(effect)


        this.effect.RegisterTemp(
            AbilityFactory.WhenUnitWouldAttack(
                AbilityType.Temp0,
                None,
                action,
                by_effect=message.effect
            ),
            unregister_after_exec=True
        )


    return [
        AbilityFactory.WhenCardWouldResolveAbility(
            AbilityType.HeroInterrupt,
            ["Action"],
            None,
            full_blast,
            ability_name="Optic Blast",
            trigger_player="You",
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Exhaust(name="Cyclops")),
    ]


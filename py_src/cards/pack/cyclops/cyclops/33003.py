from . import *

# * Ruby Quartz Visor

def GetAbilities() -> Sequence['Ability']:

    def ruby_quartz_visor(effect: 'Effect', message: 'Message.WhenPlayerPayingResources') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        def action(effect: 'Effect', message: 'Message.WhenUnitWouldAttack'):
            message.GainPiercing(effect)
            message.GainRanged(effect)

        this.effect.RegisterTemp(
            AbilityFactory.WhenUnitWouldAttack(
                AbilityType.Temp0,
                None,
                action,
                by_effect=message.for_effect
            ),
            # AbilityFactory.UnitAttackGainKeyword(
            #     player.GetHero(),
            #     piercing=True,
            #     ranged=True
            # ),
            unregister_after_exec=True
        )


    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.HeroResource,
            Resources("Y"),
            for_ability="Optic Blast",
            ex_operation=ruby_quartz_visor,
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

